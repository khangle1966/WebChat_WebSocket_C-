using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
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
        private static ConcurrentDictionary<string, string> _usernames = new ConcurrentDictionary<string, string>();
        private readonly MessageRepository _messageRepository;

        public WebSocketHandler(MessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task Handle(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                string connectionId = context.Connection.Id;
                string? username = context.Request.Query["username"].ToString();

                // Kiểm tra và xử lý trường hợp username null hoặc rỗng
                if (string.IsNullOrEmpty(username))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Username is required");
                    return;
                }

                _sockets.TryAdd(connectionId, webSocket);
                _usernames.TryAdd(connectionId, username);

                // Gửi tất cả tin nhắn trước đó cho người dùng mới kết nối
                var messages = await _messageRepository.GetAllMessages();
                foreach (var message in messages)
                {
                    var chatMessage = new { User = message.UserName, Message = message.Text };
                    var chatMessageJson = JsonSerializer.Serialize(chatMessage);
                    var buffer = Encoding.UTF8.GetBytes(chatMessageJson);
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                }

                await Receive(webSocket, async (result, buffer) =>
                {
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var chatMessage = new { User = username, Message = message };
                        var chatMessageJson = JsonSerializer.Serialize(chatMessage);

                        // Lưu tin nhắn vào file
                        await _messageRepository.AddMessage(new Message { UserName = username, Text = message });

                        await SendMessageToAllAsync(chatMessageJson);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (_sockets.TryRemove(connectionId, out WebSocket? _))
                        {
                            _usernames.TryRemove(connectionId, out _);
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

        private async Task SendMessageToAllAsync(string message)
        {
            foreach (var socket in _sockets.Values)
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
    }
}
