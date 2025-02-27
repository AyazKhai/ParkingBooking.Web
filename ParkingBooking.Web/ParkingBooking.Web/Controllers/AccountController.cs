using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ParkingBooking.Web.Models;
using ParkingBooking.Web.Services;
using ParkingBooking.Web.Data;

namespace ParkingBooking.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly JWTService _jwtService;
        private readonly ApplicationDbContext _context;
        public AccountController(JWTService jwtService, ApplicationDbContext context)
        {
            _jwtService = jwtService;
            _context = context;
        }
        //https://localhost:7096/Account/Register?name=testuser&password=TestPassword123
        public IActionResult Register(string name, string password) 
        {
            var user = new User
            {
                Name = name,
                PhoneNumber = "213213",
                Email = "asas@mail.ru",
                Role = "User"
            };
            var passwordHash = new PasswordHasher<User>().HashPassword(user, password);
            user.PasswordHash = passwordHash;
            _context.Add(user);
            _context.SaveChanges();
            return View();
        }

        public IActionResult Login(string name, string password) 
        {
            var user = _context.Users.FirstOrDefault(u => u.Name == name);

            if (user == null)
            {
                return Unauthorized("User not found");
            }

            var passwordVerificationResult = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, password);

            if (passwordVerificationResult != PasswordVerificationResult.Success)
            {
                return Unauthorized("Invalid password");
            }

            var token = _jwtService.GenerateToken(user);
            return Ok(new { Token = token });
        }


    }
}
