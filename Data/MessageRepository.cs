using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
            var json = JsonSerializer.Serialize(messages);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<List<Message>> GetAllMessages()
        {
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<List<Message>>(json) ?? new List<Message>();
            }
            return new List<Message>();
        }
    }
}
