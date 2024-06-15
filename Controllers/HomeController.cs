using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using WebChatServer.Models;
using WebChatServer.Handlers;
using WebChatServer.Services;
using WebChatServer.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebChatServer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ChatFacade _chatFacade;
        private readonly WebSocketHandler _webSocketHandler;
        private readonly UserRepository _userRepository;

        public HomeController(ILogger<HomeController> logger, ChatFacade chatFacade, WebSocketHandler webSocketHandler, UserRepository userRepository)
        {
            _logger = logger;
            _chatFacade = chatFacade;
            _webSocketHandler = webSocketHandler;
            _userRepository = userRepository;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("HomeController Index action invoked");

            var username = HttpContext.Session.GetString("Username");
            var email = HttpContext.Session.GetString("Email");
            if (!string.IsNullOrEmpty(username))
            {
                ViewData["Username"] = username;
                ViewData["Email"] = email;
            }

            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        public IActionResult Chat()
        {
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var chatRooms = GetChatRooms();
            return View(chatRooms);
        }

        private List<ChatRoomModel> GetChatRooms()
        {
            var chatRooms = new List<ChatRoomModel>
            {
                new ChatRoomModel { Id = 1, Name = "Room 1", LastActive = DateTime.Now.AddMinutes(-1) },
                new ChatRoomModel { Id = 2, Name = "Room 2", LastActive = DateTime.Now.AddMinutes(-5) },
                new ChatRoomModel { Id = 3, Name = "Room 3", LastActive = DateTime.Now.AddMinutes(-2) },
                new ChatRoomModel { Id = 4, Name = "Room 4", LastActive = DateTime.Now.AddMinutes(-10) },
                new ChatRoomModel { Id = 5, Name = "Room 5", LastActive = DateTime.Now.AddMinutes(-3) },
            };

            foreach (var room in chatRooms)
            {
                room.IsOnline = (DateTime.Now - room.LastActive).TotalMinutes <= 3;
            }

            return chatRooms;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(int id, string message)
        {
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var username = HttpContext.Session.GetString("Username");
            await _chatFacade.SaveMessageAsync(id, username, message);
            _chatFacade.UpdateChatRoomStatus(id, true);

            return RedirectToAction("ChatRoom", new { id = id });
        }

        public async Task<IActionResult> ChatRoom(int id)
        {
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var messages = await _chatFacade.GetMessagesAsync(id);
            ViewData["RoomId"] = id;
            return View(messages);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
