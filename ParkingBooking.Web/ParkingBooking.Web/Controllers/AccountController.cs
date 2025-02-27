using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ParkingBooking.Web.Models;
using ParkingBooking.Web.Services;
using ParkingBooking.Web.Data;
using System.Configuration;

namespace ParkingBooking.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly JWTService _jwtService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public AccountController(JWTService jwtService, ApplicationDbContext context, IConfiguration configuration)
        {
            _jwtService = jwtService;
            _context = context;
            _configuration = configuration;
        }
        //https://localhost:7096/Account/Register?name=testuser&password=TestPassword123

        [HttpGet]
        public IActionResult Register()
        {
            return View(); 
        }
        [HttpPost]
        public IActionResult Register(User user) 
        {
            user.Role =  "User";
            // Исключаем Role из валидации модели
            ModelState.Remove("Role");
            if (ModelState.IsValid)
            {
                var passwordHash = new PasswordHasher<User>().HashPassword(user, user.Password);
                user.Password = passwordHash;
                
                _context.Add(user);
                _context.SaveChanges();
                return RedirectToAction("Login");
            }
            return View(user);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(); 
        }
        [HttpPost]
        public IActionResult Login(string email, string password) 
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            Console.WriteLine(user.Role);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found");
                return View();
            }

            var passwordVerificationResult = new PasswordHasher<User>().VerifyHashedPassword(user, user.Password, password);

            if (passwordVerificationResult != PasswordVerificationResult.Success)
            {
                ModelState.AddModelError("", "Invalid password");
                return View();
            }

            var token = _jwtService.GenerateToken(user);
            Response.Cookies.Append("JWT", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, 
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:ExpiryInMinutes"))
            });
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            Response.Cookies.Delete("JWT"); 
            return RedirectToAction("Index", "Home"); 
        }


    }
}
