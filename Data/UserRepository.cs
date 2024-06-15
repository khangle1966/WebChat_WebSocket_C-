using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WebChatServer.Models;

namespace WebChatServer.Data
{
    public class UserRepository
    {
        private readonly string filePath = "users.json";

        public async Task AddUser(User user)
        {
            var users = await GetAllUsers();
            users.Add(user);
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            var json = JsonSerializer.Serialize(users, options);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<User?> GetUserByUsername(string username)
        {
            var users = await GetAllUsers();
            return users.Find(u => u.Username == username);
        }

        private async Task<List<User>> GetAllUsers()
        {
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                return JsonSerializer.Deserialize<List<User>>(json, options) ?? new List<User>();
            }
            return new List<User>();
        }
    }
}
