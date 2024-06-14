using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WebChatServer.Models;

namespace WebChatServer.Data
{
    public class UserRepository
    {
        private readonly string filePath = "users.txt";

        public async Task AddUser(User user)
        {
            var userLine = $"{user.Username}|{user.PasswordHash}|{user.PasswordSalt}|{user.Email}";
            await File.AppendAllTextAsync(filePath, userLine + Environment.NewLine);
        }

        public async Task<User?> GetUserByUsername(string username)
        {
            if (File.Exists(filePath))
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts[0] == username)
                    {
                        return new User
                        {
                            Username = parts[0],
                            PasswordHash = parts[1],
                            PasswordSalt = parts[2],
                            Email = parts[3]
                        };
                    }
                }
            }
            return null;
        }
    }
}
