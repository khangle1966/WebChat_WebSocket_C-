namespace WebChatServer.Controllers
{
    using System;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Text;
    using WebChatServer.Data;
    using WebChatServer.Models;

    [ApiController]
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly UserRepository _userRepository;

        public AuthController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] string username, [FromForm] string password, [FromForm] string email)
        {
            var existingUser = await _userRepository.GetUserByUsername(username);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "User already exists.";
                return RedirectToAction("Register", "Home");
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
            return RedirectToAction("Login", "Home");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
        {
            var user = await _userRepository.GetUserByUsername(username);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login", "Home");
            }

            using var hmac = new HMACSHA512(Convert.FromBase64String(user.PasswordSalt));
            var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
            if (computedHash != user.PasswordHash)
            {
                TempData["ErrorMessage"] = "Invalid password.";
                return RedirectToAction("Login", "Home");
            }

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
            var tokenString = tokenHandler.WriteToken(token);

            HttpContext.Session.SetString("Token", tokenString ?? string.Empty);
            HttpContext.Session.SetString("Username", user.Username ?? string.Empty);
            HttpContext.Session.SetString("Email", user.Email ?? string.Empty);
            return RedirectToAction("Chat", "Home");
        }
    }
}
