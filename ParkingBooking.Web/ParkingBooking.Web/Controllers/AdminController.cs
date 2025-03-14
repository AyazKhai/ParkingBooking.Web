﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingBooking.Web.Data;
using ParkingBooking.Web.Models;
using ParkingBooking.Web.Services;

//using System.Data.Entity;
using System.Security.Claims;

namespace ParkingBooking.Web.Controllers
{
    [Authorize(Policy = "AdminOrModerator")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IConfiguration _configuration;
        private readonly JWTService _jwtService;

        public AdminController(ApplicationDbContext applicationDbContext, ILogger<AdminController> logger, JWTService jwtService, IConfiguration configuration)
        {
            _applicationDbContext = applicationDbContext;
            _logger = logger;
            _jwtService = jwtService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                if (User.Identity?.IsAuthenticated != true)
                {
                    _logger.LogWarning("Пользователь не аутентифицирован.");
                    return RedirectToAction("Login", "Account"); 
                }

                ViewData["parkings"] = await _applicationDbContext.Parkings
                    .Include(p => p.ParkingSpots)
                    .ToArrayAsync();


                _logger.LogInformation("Парковки загружены для  {Name}.", User.Identity.Name );

            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при работе с базой данных";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка при выоде парковок");
                TempData["Message"] = "Произошла ошибка";
            }
            return View();
        }
       

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateParking(string address)
        {

            if (string.IsNullOrEmpty(address))
            {
                _logger.LogWarning("Введен некорректный адрес - пустой при создании Парковки");
                TempData["Message"] = "Некоректный адрес";
                return RedirectToAction("Index");
            }

            try
            {
                var parking = new Parking
                {
                    Address = address,
                    Status = ParkingStatus.Inactive
                };

                _applicationDbContext.Parkings.Add(parking);
                await _applicationDbContext.SaveChangesAsync();

                _logger.LogInformation("Парковка {addres} создана успешно", parking.Address);
                TempData["Message"] = "Парковка создана";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при работе с базой данных";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка при выводе парковок");
                TempData["Message"] = "Произошла непредвиденная ошибка";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteParking(int parkingId)
        {
            try
            {
                var parking = await _applicationDbContext.Parkings
                .FirstOrDefaultAsync(s => s.ParkingId == parkingId);

                if (parking == null)
                {
                    _logger.LogWarning("Парковка не найдена. {ParkingId}", parkingId);
                    TempData["Message"] = "Парковка не найдена.";
                    return RedirectToAction("Index");
                }

                _applicationDbContext.Parkings.Remove(parking);
                await _applicationDbContext.SaveChangesAsync();

                _logger.LogInformation("Парковка {addres} удалена успешно", parking.Address);
                TempData["Message"] = "Парковка успешно удалена";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при удалении данных в базе данных ";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка при удалении парковки");
                TempData["Message"] = "Произошла ошибка";
                return RedirectToAction("Index");
            }

        }
        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> EditParking(Parking parking)
        {
            if (parking != null)
            {
                try
                {
                    var existingParking = await _applicationDbContext.Parkings
                       .FirstOrDefaultAsync(s => s.ParkingId == parking.ParkingId);

                    if (existingParking == null)
                    {
                        _logger.LogWarning("Парковочное место не найдена для редактирования.");
                        TempData["Message"] = "Парковочное место не найдено.";
                        return RedirectToAction("Index");
                    }
                    if (existingParking.Address == parking.Address && existingParking.Status == parking.Status)
                    {
                        _logger.LogWarning("Данные не изменены.");
                        return RedirectToAction("GetParkingSpot", new { id = parking.ParkingId });
                    }

                    existingParking.Address = parking.Address;
                    existingParking.Status = parking.Status;

                    _applicationDbContext.Parkings.Update(existingParking);
                    await _applicationDbContext.SaveChangesAsync();

                    _logger.LogInformation("Данные парковки {address} успешно обновлены", existingParking.Address);
                    TempData["Message"] = "Данне  парковки успешно обновлено.";
                    return RedirectToAction("GetParkingSpot", new { id = parking.ParkingId });
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Ошибка при работе с базой данных");
                    TempData["Message"] = "Ошибка при изменении данных в базе данных ";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Произошла непредвиденная ошибка при удалении парковки");
                    TempData["Message"] = "Произошла ошибка";
                    return RedirectToAction("Index");
                }
            }
            _logger.LogWarning("Объект для обновления данных был null.");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteParkingSpot(int parkingSpotId, int parkingId)
        {
            try
            {
                var spot = await _applicationDbContext.ParkingSpots
                .FirstOrDefaultAsync(s => s.ParkingSpotId == parkingSpotId);
                if (spot == null)
                {
                    _logger.LogWarning("Парковочное место не найдено. {ParkingSpotId}", parkingSpotId);
                    TempData["Message"] = "Парковочное место не найдено.";
                    return RedirectToAction("Index");
                }


                int id = spot.ParkingId;
                _applicationDbContext.ParkingSpots.Remove(spot);
                await _applicationDbContext.SaveChangesAsync();

                _logger.LogInformation("Парковочное место {number} удалено успешно", spot.Number);
                TempData["Message"] = "Парковочное место успешно удалено.";
                return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при удалкении данных в базе данных ";
                return RedirectToAction("GetParkingSpot", new { id = parkingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка при попытке удаления парковочного места");
                TempData["Message"] = "Произошла ошибка";
                return RedirectToAction("GetParkingSpot", new { id = parkingId });
            }


        }
        [HttpPost]
        public async Task<IActionResult> CreateParkingSpot(ParkingSpot spot) {

            if (spot != null)
            {
                try
                {
                    var parkingExists = await _applicationDbContext.Parkings.FirstOrDefaultAsync(p => p.ParkingId == spot.ParkingId);

                    if (parkingExists == null)
                    {
                        _logger.LogWarning("Парковка с указанным {Number} не найдена.", spot.Number);
                        TempData["Message"] = "Парковка с указанным идентификатором не найдена.";
                        return RedirectToAction("Index");
                    }

                    _applicationDbContext.ParkingSpots.Add(spot);
                    await _applicationDbContext.SaveChangesAsync();

                    _logger.LogInformation("Парковочное место успешно создано {Number}.", spot.Number);
                    TempData["Message"] = "Данные добавлены";
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Ошибка при добавлении данных в базу данных");
                    TempData["Message"] = "Ошибка при добавлении данных в базу данных ";
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Произошла непредвиденная ошибка при попытке создания нового парковочного места.");
                    TempData["Message"] = "Произошла ошибка";
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
                }

            }
            _logger.LogWarning("Объект для добавления данных был null.");
            TempData["Message"] = "Данные не добавлены";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> GetParkingSpot(int id)
        {
            try
            {
                 var parkingSpots = await _applicationDbContext.ParkingSpots.Where(s => s.ParkingId == id).ToArrayAsync();
                ViewData["parking"] = await _applicationDbContext.Parkings.FirstOrDefaultAsync(p => p.ParkingId == id);

                ViewData["parkingSpots"] = parkingSpots;
                _logger.LogInformation("Парковочные места загружены.{count} мест", parkingSpots.Length);
                return View();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при работе с базой данных";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка");
                TempData["Message"] = "Произошла непредвиденная ошибка";
                return RedirectToAction("Index");
            }
            
        }

        public async Task<IActionResult> EditParkingSpot(int id)
        {
            try
            {
                var parkingSpot = await _applicationDbContext.ParkingSpots
               .FirstOrDefaultAsync(s => s.ParkingSpotId == id);

                if (parkingSpot == null)
                {
                    _logger.LogWarning("Парковочное место с ID {ParkingSpotId} не найдено.", id);
                    TempData["Message"] = "Парковочное место не найдено.";
                    return RedirectToAction("Index");
                }
                var parking = await _applicationDbContext.Parkings
                .FirstOrDefaultAsync(p => p.ParkingId == parkingSpot.ParkingId);

                if (parking == null)
                {
                    _logger.LogWarning("Парковка с ID {ParkingId} не найдена.", parkingSpot.ParkingId);
                    TempData["Message"] = "Парковка не найдена.";
                    return RedirectToAction("Index");
                }

                ViewData["parking"] = parking;
                return View(parkingSpot);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при работе с базой данных";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка");
                TempData["Message"] = "Произошла непредвиденная ошибка";
                return RedirectToAction("Index");
            }
        }
        [HttpPost]
        public async Task<IActionResult> EditParkingSpot(ParkingSpot spot)
        {
            if (spot != null)
            {
                try
                {
                    var existingSpot = await _applicationDbContext.ParkingSpots
                        .FirstOrDefaultAsync(s => s.ParkingSpotId == spot.ParkingSpotId);

                    if (existingSpot == null)
                    {
                        _logger.LogWarning("Парковочное место не найдено");
                        TempData["Message"] = "Парковочное место не найдено.";
                        return RedirectToAction("Index");
                    }


                    existingSpot.Number = spot.Number;
                    existingSpot.Status = spot.Status;
                    existingSpot.Information = spot.Information;
                    existingSpot.ParkingId = spot.ParkingId;

                    _applicationDbContext.ParkingSpots.Update(existingSpot);
                    await _applicationDbContext.SaveChangesAsync();

                    _logger.LogInformation("Парковочное место {number} успешно обновлено", existingSpot.Number);
                    TempData["Message"] = "Парковочное место успешно обновлено.";
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });


                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Ошибка при обновлении данных в базе данных");
                    TempData["Message"] = "Ошибка при обновлении данных в базе данных";
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Произошла непредвиденная ошибка при попытке редактированиря данных парковочного места.");
                    TempData["Message"] = "Произошла непредвиденная ошибка";
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
                }
            }
            _logger.LogWarning("Объект для обновления данных был null.");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetBookings(int? parkingId) 
        {
            try 
            {
                var query  = _applicationDbContext.Bookings
                    .Include(b => b.ParkingSpot)
                        .ThenInclude(ps => ps.Parking)
                    .Include(b => b.User).OrderByDescending(b => b.StartTime)
                    .AsQueryable();

                if (parkingId.HasValue) 
                {
                    query = query.Where(b => b.ParkingSpot.ParkingId == parkingId.Value);
                }
                ViewData["parkings"] = await _applicationDbContext.Parkings.ToArrayAsync();

                var bookings = await query.ToArrayAsync();

                ViewData["parkingId"] = parkingId; 
                _logger.LogInformation("Бронирования загружены. {Count} бронирований", bookings.Length);
                return View(bookings);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при работе с базой данных";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка");
                TempData["Message"] = "Произошла непредвиденная ошибка";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            try
            {
                var booking = await _applicationDbContext.Bookings
                    .Where(b => b.Id == bookingId).Include(b => b.ParkingSpot)
                    .FirstOrDefaultAsync();

                if (booking == null)
                {
                    _logger.LogWarning("Бронирование не найдено.");
                    TempData["Message"] = "Бронирование не найдено.";
                    return RedirectToAction("GetBookings");
                }
                booking.Status = BookingStatus.Cancelled;

                _applicationDbContext.Bookings.Update(booking);
                await _applicationDbContext.SaveChangesAsync();

                _logger.LogInformation("Бронирование места {name} {startTime}/{endTime} отменено успешно", booking.ParkingSpot.Number, booking.StartTime, booking.EndTime);
                return RedirectToAction("GetBookings");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при удалении данных в базе данных ";
                return RedirectToAction("GetBookings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка при попытке отмены бронирования");
                TempData["Message"] = "Произошла ошибка";
                return RedirectToAction("GetBookings");
            }
        }

        
        public async Task<IActionResult> UsersInfo()
        {
            try
            {
                var users = await _applicationDbContext.Users.ToArrayAsync();
                return View(users);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных при попытке получить данные пользователей.");
                TempData["Message"] = "Ошибка при работе с базой данных";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка при попытке получить данные пользователей.");
                TempData["Message"] = "Произошла непредвиденная ошибка";
                return RedirectToAction("Index");
            }
        }

        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> EditUser(int id) 
        {
            try 
            {
                var user = await _applicationDbContext.Users.FirstOrDefaultAsync(u => u.UserId == id);
                if (user == null)
                {
                    _logger.LogWarning("Пользователь с ID {UserId} не найден.", id);
                    return RedirectToAction("UsersInfo");
                }

                _logger.LogInformation("Пользователь {Name} загружен для редактирования", user.Name);
                return View(user);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при работе с базой данных";
                return RedirectToAction("UsersInfo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка");
                TempData["Message"] = "Произошла непредвиденная ошибка";
                return RedirectToAction("UsersInfo");
            }

        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
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
                        return RedirectToAction("UsersInfo");
                    }

                    var principal = _jwtService.GetPrincipalFromExpiredToken(accessToken);

                    var user = await _applicationDbContext.Users.FindAsync(existuser.UserId);
                    if (user == null || user.RefreshTokenExpiry <= DateTime.UtcNow)
                    {
                        _logger.LogWarning("Недействительный или истёкший refresh token для пользователя {UserId}.", existuser.UserId);
                        return RedirectToAction("UsersInfo");
                    }
                    var newAccessToken = _jwtService.GenerateToken(user);

                    var newRefreshToken = _jwtService.GenerateRefreshToken();
                    user.RefreshToken = newRefreshToken;
                    user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(_configuration.GetValue<int>("RefreshToken:ExpiresTokenDays"));
                    user.Name = existuser.Name;
                    user.Email = existuser.Email;
                    user.PhoneNumber = existuser.PhoneNumber;
                    user.Role = existuser.Role;

                    _applicationDbContext.Users.Update(user);
                    await _applicationDbContext.SaveChangesAsync();


                    _logger.LogInformation("Данные пользователя {Name} успешно обновлено", user.Name);
                    TempData["Message"] = "Данные пользователя успешно обновлены";
                    return RedirectToAction("UsersInfo");
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Ошибка при обновлении данных в базе данных");
                    TempData["Message"] = "Ошибка при обновлении данных в базе данных";
                    return RedirectToAction("UsersInfo");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Произошла непредвиденная ошибка при попытке обновления данных пользователя");
                    TempData["Message"] = "Произошла непредвиденная ошибка при попытке обновления данных пользователя";
                    return RedirectToAction("UsersInfo");
                }

            }
            _logger.LogWarning("Объект для обновления данных был null.");
            return View();

        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteUser(int userid) 
        {
            try
            {
                var user = await _applicationDbContext.Users
                .FirstOrDefaultAsync(u => u.UserId == userid);

                if (user == null)
                {
                    _logger.LogWarning("Пользователь не найден. {UserId}", userid);
                    TempData["Message"] = "пользователь не найден.";
                    return RedirectToAction("UsersInfo");
                }

                _applicationDbContext.Users.Remove(user);
                await _applicationDbContext.SaveChangesAsync();

                _logger.LogInformation("Пользователь {Name} успешно удален", user.Name);
                TempData["Message"] = "Пользователь успешно удален.";
                return RedirectToAction("UsersInfo");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при удалении данных в базе данных ";
                return RedirectToAction("UsersInfo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка при удалении пользователя");
                TempData["Message"] = "Произошла ошибка при удалении.";
                return RedirectToAction("UsersInfo");
            }
        }


    }
}
