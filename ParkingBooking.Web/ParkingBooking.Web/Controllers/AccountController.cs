using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ParkingBooking.Web.Models;
using ParkingBooking.Web.Services;
using ParkingBooking.Web.Data;
using System.Configuration;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

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
            ModelState.Remove("Role");
            ModelState.Remove("RefreshToken");
            ModelState.Remove("RefreshTokenExpiry");

            if (ModelState.IsValid)
            {
                try
                {
                    user.Role = Roles.User;

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
        public async Task<IActionResult> Login(string email, string password) 
        {
            try 
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    _logger.LogWarning("Пользователь с email {Email} не найден.", email);
                    return RedirectToAction("Register", "Account");
                }

                var passwordVerificationResult = new PasswordHasher<User>().VerifyHashedPassword(user, user.Password, password);

                if (passwordVerificationResult != PasswordVerificationResult.Success)
                {
                    _logger.LogWarning("Неверный пароль для пользователя {Email}.", email);
                    TempData["Message"] =  "Неверный пароль";
                    return View();
                }

                var accestoken = _jwtService.GenerateToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("RefreshToken:ExpiresTokenDays"));

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

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
                    Expires = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("RefreshToken:ExpiresTokenDays"))
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

                Response.Cookies.Delete("JWT");
                Response.Cookies.Delete("RefreshToken");

                _logger.LogInformation("Куки для пользователя {UserId} удалены", userId);
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
                if (user == null  || user.RefreshTokenExpiry <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Недействительный или истёкший refresh token для пользователя {UserId}.", userId);
                    return Unauthorized();
                }

                var newAccessToken = _jwtService.GenerateToken(user);

                var newRefreshToken = _jwtService.GenerateRefreshToken();
                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("RefreshToken:ExpiresTokenDays"));

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
                    Expires = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("RefreshToken:ExpiresTokenDays"))
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

        [HttpPost]
        public async Task<IActionResult> CheckUser()
        {
            try
            {
                var accessToken = Request.Cookies["JWT"];
                var refreshToken = Request.Cookies["RefreshToken"];

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogWarning("Отсутствует access token или refresh token.");
                    return Json(new { isChangedOrDeleted = false }); // Пользователь не авторизован
                }

                refreshToken = Uri.UnescapeDataString(refreshToken);

                var principal = _jwtService.GetPrincipalFromExpiredToken(accessToken);
                var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier).Value);

                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("Пользователь {UserId} не найден в базе данных.", userId);
                    return Json(new { isChangedOrDeleted = true });
                }

                if (user.RefreshToken != refreshToken || user.RefreshTokenExpiry <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Недействительный или истёкший refresh token для пользователя {UserId}.", userId);
                    return Json(new { isChangedOrDeleted = true });
                }

                return Json(new { isChangedOrDeleted = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке пользователя.");
                return Json(new { isChangedOrDeleted = true }); 
            }
        }

        public IActionResult AccountInfo()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    _logger.LogWarning("Попытка получить информацию о пользователе неаутентифицированным пользователем.");
                    return RedirectToAction("Index", "Home");
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var user = _context.Users.Find(userId);

                if (user == null)
                {
                    _logger.LogWarning("Пользователь с идентификатором {UserId} не найден.", userId);
                    return RedirectToAction("Index", "Home");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка при получении информации о пользователе.");
                TempData["Message"] = "Произошла непредвиденная ошибка";
                return RedirectToAction("Index", "Home");
            }
        }


        public async Task<IActionResult> EditUser(int id)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
                return View(user);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении данных в базе данных");
                TempData["Message"] = "Ошибка при работе с базой данных";
                return RedirectToAction("AccountInfo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка");
                TempData["Message"] = "Произошла непредвиденная ошибка";
                return RedirectToAction("AccountInfo");
            }

        }
        [HttpPost]
        public async Task<IActionResult> EditUser(User existuser)
        {
            if (existuser != null)
            {
                try
                {
                    var accessToken = Request.Cookies["JWT"];
                    var refreshToken = Request.Cookies["RefreshToken"];

                    if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                    {
                        _logger.LogWarning("Отсутствует access token или refresh token.");
                        return RedirectToAction("AccountInfo");
                    }

                    refreshToken = Uri.UnescapeDataString(refreshToken);

                    var principal = _jwtService.GetPrincipalFromExpiredToken(accessToken);

                    var user = await _context.Users.FindAsync(existuser.UserId);
                    if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiry <= DateTime.UtcNow)
                    {
                        _logger.LogWarning("Недействительный или истёкший refresh token для пользователя {UserId}.", existuser.UserId);
                        return RedirectToAction("AccountInfo");
                    }

                    var newAccessToken = _jwtService.GenerateToken(user);

                    var newRefreshToken = _jwtService.GenerateRefreshToken();
                    user.RefreshToken = newRefreshToken;
                    user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("RefreshToken:ExpiresTokenDays"));
                    user.Name = existuser.Name;
                    user.Email = existuser.Email;
                    user.PhoneNumber = existuser.PhoneNumber;

                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Данные успешно обновлены для пользователя {UserName} {UseerEamil}.", user.Name, user.Email);
                    return RedirectToAction("AccountInfo");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Ошибка при обновлении данных в базе данных");
                    TempData["Message"] = "Ошибка при обновлении данных в базе данных";
                    return RedirectToAction("AccountInfo");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Произошла непредвиденная ошибка при попытке обновления данных пользователя");
                    TempData["Message"] = "Произошла непредвиденная ошибка при попытке обновления данных пользователя";
                    return RedirectToAction("AccountInfo");
                }

            }
            _logger.LogWarning("Объект для обновления данных был null.");
            return RedirectToAction("AccountInfo");

        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser()
        {
            try
            {
                var userid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userid);

                if (user == null)
                {
                    _logger.LogWarning("Пользователь не найден. {UserId}", userid);
                    TempData["Message"] = "пользователь не найден.";
                    return RedirectToAction("AccountInfo");
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();


                _logger.LogInformation("Пользователь {Name} успешно удален", user.Name);
                TempData["Message"] = "Пользователь успешно удален.";
                return RedirectToAction("AccountInfo");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при удалении данных в базе данных ";
                return RedirectToAction("AccountInfo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка при удалении пользователя");
                TempData["Message"] = "Произошла ошибка при удалении.";
                return RedirectToAction("AccountInfo");
            }
        }

       
        public async Task<IActionResult> BecomeAdmin() 
        {
            var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

            // Проверка на режим разработки
            if (!env.IsDevelopment())
            {
                _logger.LogWarning("Метод GetAdmin вызван не в режиме разработки.");
                return NotFound();
            }
            try
            {
                var accessToken = Request.Cookies["JWT"];
                var refreshToken = Request.Cookies["RefreshToken"];

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogWarning("Отсутствует access token или refresh token.");
                    return NotFound();
                }

                refreshToken = Uri.UnescapeDataString(refreshToken);

                var principal = _jwtService.GetPrincipalFromExpiredToken(accessToken);
                var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier).Value);

                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning("Пользователь {UserId} не найден в базе данных.", userId);
                    return NotFound();
                }

                if (user.Role != Roles.Admin)
                {
                    user.Role = Roles.Admin;

                    var newRefreshToken = _jwtService.GenerateRefreshToken();
                    user.RefreshToken = newRefreshToken;
                    user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("RefreshToken:ExpiresTokenDays"));

                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Права администратора успешно выданы.");
                }

                return RedirectToAction("Index","Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке пользователя.");
                return NotFound();
            }

        }
    }

}
