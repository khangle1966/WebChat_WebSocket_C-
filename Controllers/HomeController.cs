﻿using Microsoft.AspNetCore.Mvc;
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

            WebSocketHandler.UpdateChatRoomStatus();
            var chatRooms = WebSocketHandler._chatRooms;
            return View(chatRooms);
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
            if (username == null)
            {
                return RedirectToAction("Login");
            }

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
