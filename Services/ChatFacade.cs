using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WebChatServer.Data;
using WebChatServer.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebChatServer.Handlers;

namespace WebChatServer.Services
{
    public class ChatFacade
    {
        private readonly MessageRepository _messageRepository;
        private readonly UserRepository _userRepository;
        private readonly List<ChatRoomModel>? _chatRooms;

        public ChatFacade(MessageRepository messageRepository, UserRepository userRepository, List<ChatRoomModel>? chatRooms)
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
            _chatRooms = chatRooms;
        }

        public async Task SaveMessageAsync(int chatRoomId, string username, string messageText)
        {
            var message = new Message
            {
                ChatRoomId = chatRoomId,
                UserName = username,
                Text = messageText,
                CreatedAt = DateTime.Now
            };
            await _messageRepository.AddMessage(message);
        }

        public async Task<List<Message>> GetMessagesAsync(int chatRoomId)
        {
            var messages = await _messageRepository.GetAllMessages();
            return messages.Where(m => m.ChatRoomId == chatRoomId).ToList();
        }

        public async Task UpdateChatRoomStatus(int chatRoomId, bool isOnline)
        {
            var chatRoom = WebSocketHandler._chatRooms?.FirstOrDefault(r => r.Id == chatRoomId);
            if (chatRoom != null)
            {
                chatRoom.IsOnline = isOnline;
                if (isOnline)
                {
                    chatRoom.LastMessageTimestamp = DateTime.Now;
                }
                else
                {
                    await _messageRepository.ClearMessagesForRoom(chatRoomId);
                }
            }
        }

            public async Task<User?> AuthenticateUserAsync(string username, string password)
        {
            var user = await _userRepository.GetUserByUsername(username);
            if (user == null) return null;

            using var hmac = new HMACSHA512(Convert.FromBase64String(user.PasswordSalt));
            var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            if (computedHash != user.PasswordHash) return null;

            return user;
        }

        public async Task<string> GenerateTokenAsync(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("aVeryStrongSecretKeyThatIsDefinitely32CharactersLong!");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Username ?? string.Empty)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = "http://localhost",
                Audience = "http://localhost"
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public async Task<bool> RegisterUserAsync(string username, string password, string email)
        {
            var existingUser = await _userRepository.GetUserByUsername(username);
            if (existingUser != null)
            {
                return false;
            }

            using var hmac = new HMACSHA512();
            var user = new User
            {
                Username = username,
                PasswordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password))),
                PasswordSalt = Convert.ToBase64String(hmac.Key),
                Email = email
            };

            await _userRepository.AddUser(user);
            return true;
        }

        public async Task ClearMessagesForRoom(int chatRoomId)
        {
            await Task.Run(async () => await _messageRepository.ClearMessagesForRoom(chatRoomId));
        }
    }
}
