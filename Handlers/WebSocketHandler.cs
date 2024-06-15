using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebChatServer.Data;
using WebChatServer.Models;

namespace WebChatServer.Handlers
{
    public class WebSocketHandler
    {
        private static ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();
        private static ConcurrentDictionary<string, (string Username, int ChatRoomId)> _userSessions = new ConcurrentDictionary<string, (string Username, int ChatRoomId)>();
        private readonly MessageRepository _messageRepository;
        public static List<ChatRoomModel>? _chatRooms { get; set; }

        public WebSocketHandler(MessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
            _chatRooms = new List<ChatRoomModel>
            {
                new ChatRoomModel { Id = 1, Name = "Room 1", LastActive = DateTime.Now.AddMinutes(-1), LastMessageTimestamp = DateTime.Now.AddMinutes(-1) },
                new ChatRoomModel { Id = 2, Name = "Room 2", LastActive = DateTime.Now.AddMinutes(-5), LastMessageTimestamp = DateTime.Now.AddMinutes(-5) },
            };
        }

        public async Task Handle(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                string connectionId = context.Connection.Id;
                string? username = context.Request.Query["username"].ToString();
                int chatRoomId;

                if (string.IsNullOrEmpty(username) || !int.TryParse(context.Request.Query["chatRoomId"], out chatRoomId))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Username and chatRoomId are required");
                    return;
                }

                _sockets.TryAdd(connectionId, webSocket);
                _userSessions.TryAdd(connectionId, (username, chatRoomId));

                var messages = await _messageRepository.GetMessagesForRoom(chatRoomId);
                foreach (var message in messages)
                {
                    var chatMessage = new { User = message.UserName, Message = message.Text, CreatedAt = message.CreatedAt.ToString("MM/dd/yyyy HH:mm:ss tt") };
                    var chatMessageJson = JsonSerializer.Serialize(chatMessage);
                    var buffer = Encoding.UTF8.GetBytes(chatMessageJson);
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                }

                await Receive(webSocket, async (result, buffer) =>
                {
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var chatMessage = new { User = username, Message = message, CreatedAt = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss tt") };
                        var chatMessageJson = JsonSerializer.Serialize(chatMessage);

                        await _messageRepository.AddMessage(new Message { UserName = username, Text = message, ChatRoomId = chatRoomId, CreatedAt = DateTime.Now });

                        var chatRoom = _chatRooms?.FirstOrDefault(r => r.Id == chatRoomId);
                        if (chatRoom != null)
                        {
                            chatRoom.LastMessageTimestamp = DateTime.Now;
                            chatRoom.IsOnline = true; // Cập nhật trạng thái trực tuyến
                        }

                        await SendMessageToRoomAsync(chatRoomId, chatMessageJson);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (_sockets.TryRemove(connectionId, out WebSocket? _))
                        {
                            _userSessions.TryRemove(connectionId, out _);
                            if (result.CloseStatus.HasValue)
                            {
                                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                            }
                        }
                    }
                });
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }

        private async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                handleMessage(result, buffer);
            }
        }

        private async Task SendMessageToRoomAsync(int chatRoomId, string message)
        {
            foreach (var (connectionId, (username, roomId)) in _userSessions)
            {
                if (roomId == chatRoomId && _sockets.TryGetValue(connectionId, out WebSocket? socket) && socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}
