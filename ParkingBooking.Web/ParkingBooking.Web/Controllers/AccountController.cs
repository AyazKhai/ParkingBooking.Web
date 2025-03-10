using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ParkingBooking.Web.Models;
using ParkingBooking.Web.Services;
using ParkingBooking.Web.Data;
using System.Configuration;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ParkingBooking.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly JWTService _jwtService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(JWTService jwtService, ApplicationDbContext context, IConfiguration configuration, ILogger<AccountController> logger)
        {
            _jwtService = jwtService;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(); 
        }

        [HttpPost]
        public IActionResult Register(User user) 
        {
            user.Role =  "User";
            ModelState.Remove("Role");
            ModelState.Remove("RefreshToken");
            ModelState.Remove("RefreshTokenExpiry");

            if (ModelState.IsValid)
            {
                try
                {
                    user.Role = "User";

                    var passwordHash = new PasswordHasher<User>().HashPassword(user, user.Password);
                    user.Password = passwordHash;

                    var refreshToken = _jwtService.GenerateRefreshToken();
                    user.RefreshToken = refreshToken;

                    _context.Add(user);
                    _context.SaveChanges();

                    _logger.LogInformation("Пользователь {Email} успешно зарегистрирован.", user.Email);

                    return RedirectToAction("Login");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Ошибка при работе с базой данных");
                    TempData["Message"] = "Ошибка при работе с базой данных";
                    return View(user);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Произошла непредвиденная ошибка при регистрации нового пользователя");
                    TempData["Message"] = "Произошла непредвиденная ошибка";
                    return View(user);
                }
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
            try 
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == email);

                if (user == null)
                {
                    _logger.LogWarning("Пользователь с email {Email} не найден.", email);
                    ModelState.AddModelError("", "User not found");
                    return RedirectToAction("Register", "Account");
                }

                var passwordVerificationResult = new PasswordHasher<User>().VerifyHashedPassword(user, user.Password, password);

                if (passwordVerificationResult != PasswordVerificationResult.Success)
                {
                    _logger.LogWarning("Неверный пароль для пользователя {Email}.", email);
                    ModelState.AddModelError("", "Invalid password");
                    return View();
                }

                var accestoken = _jwtService.GenerateToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

                _context.Users.Update(user);
                _context.SaveChanges();

                Response.Cookies.Append("JWT", accestoken, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:ExpiryInMinutes"))
                });

                Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddDays(7) 
                });

                _logger.LogInformation("Пользователь {Email} успешно авторизован.", email);
                return RedirectToAction("Index", "Home");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных для пользователя {Email}.", email);
                TempData["Message"] = "Ошибка при работе с базой данных";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка при авторизации пользователя {Email}.", email);
                TempData["Message"] = "Произошла непредвиденная ошибка";
                return RedirectToAction("Index", "Home");
            }

            
        }

        public IActionResult Logout()
        {
            try 
            {
                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("Попытка выхода неаутентифицированного пользователя.");
                    return RedirectToAction("Index", "Home");
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var user = _context.Users.Find(userId);

                if (user != null)
                {
                    user.RefreshToken = " ";
                    user.RefreshTokenExpiry = null;
                    _context.Users.Update(user);
                    _context.SaveChanges();

                    _logger.LogInformation("Пользователь {UserId} успешно вышел из системы.", userId);
                }

                // Удаляем access token и refresh token из cookies
                Response.Cookies.Delete("JWT");
                Response.Cookies.Delete("RefreshToken");

                return RedirectToAction("Index", "Home");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при работе с базой данных";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка при попытке выйти из акккаунта.");
                TempData["Message"] = "Произошла непредвиденная ошибка";
                return RedirectToAction("Index", "Home");
            }
            
        }

        [HttpPost]
        public async Task<IActionResult> RefreshToken()
        {
            try 
            {
                var accessToken = Request.Cookies["JWT"];
                var refreshToken = Request.Cookies["RefreshToken"];

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogWarning("Отсутствует access token или refresh token.");
                    return Unauthorized();
                }

                refreshToken = Uri.UnescapeDataString(refreshToken);

                var principal = _jwtService.GetPrincipalFromExpiredToken(accessToken);
                var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier).Value);

                var user = await _context.Users.FindAsync(userId);
                if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiry <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Недействительный или истёкший refresh token для пользователя {UserId}.", userId);
                    return Unauthorized();
                }

                var newAccessToken = _jwtService.GenerateToken(user);

                var newRefreshToken = _jwtService.GenerateRefreshToken();
                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                Response.Cookies.Append("JWT", newAccessToken, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("JwtSettings:ExpiryInMinutes"))
                });

                Response.Cookies.Append("RefreshToken", newRefreshToken, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/",
                    Expires = DateTime.UtcNow.AddDays(7) // Срок действия refresh token
                });

                _logger.LogInformation("Токены успешно обновлены для пользователя {UserName} {UseerEamil}.", user.Name, user.Email);
                return Ok();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при работе с базой данных";
                return StatusCode(500, "Ошибка при работе с базой данных. Пожалуйста, попробуйте позже.");
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogError(ex, "Ошибка при валидации токена.");
                return Unauthorized("Недействительный токен.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная при обновлении токена");
                return StatusCode(500, "Произошла непредвиденная ошибка. Пожалуйста, попробуйте позже.");
            }

            
        }

    }

}
