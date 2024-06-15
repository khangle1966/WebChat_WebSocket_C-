using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WebChatServer.Models;

namespace WebChatServer.Data
{
    public class MessageRepository
    {
        private readonly string filePath = "messages.json";

        public async Task AddMessage(Message message)
        {
            var messages = await GetAllMessages();
            messages.Add(message);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(messages, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<List<Message>> GetAllMessages()
        {
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                return JsonSerializer.Deserialize<List<Message>>(json, options) ?? new List<Message>();
            }
            return new List<Message>();
        }

        public async Task<List<Message>> GetMessagesForRoom(int chatRoomId)
        {
            var messages = await GetAllMessages();
            return messages.Where(m => m.ChatRoomId == chatRoomId).ToList();
        }

        public async Task ClearMessagesForRoom(int chatRoomId)
        {
            var messages = await GetAllMessages();
            messages.RemoveAll(m => m.ChatRoomId == chatRoomId);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(messages, options);
            await File.WriteAllTextAsync(filePath, json);
        }
    }
}
