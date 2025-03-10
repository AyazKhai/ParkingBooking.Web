using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkingBooking.Web.Data;
using ParkingBooking.Web.Models;
using System.Security.Claims;

namespace ParkingBooking.Web.Controllers
{
    
    public class BookingController : Controller
    {
        private readonly ILogger<BookingController> _logger;
        private readonly ApplicationDbContext _applicationDbContext;

        public BookingController(ApplicationDbContext applicationDbContext, ILogger<BookingController> logger)
        {
            _applicationDbContext = applicationDbContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var parkings = await _applicationDbContext.Parkings.Where(p => p.Status == ParkingStatus.Active)
                    .Include(p => p.ParkingSpots.Where(ps => ps.Status == ParkingSpotStatus.Free))
                    .ToArrayAsync();
                ViewData["parkings"] = parkings;
                _logger.LogInformation("Парковки загружены. {Count} парковок", parkings.Length);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при работе с базой данных";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Непредвиденная ошибка загрузки парковочных мест");
                TempData["Message"] = "Произошла ошибка";
            }
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> ReserveSpot(int id, string startDate, string startTime, string endDate, string endTime)
        {
            try
            {
                var parking = await _applicationDbContext.Parkings.FirstOrDefaultAsync(p => p.ParkingId == id && p.Status == ParkingStatus.Active);
                if (parking == null)
                {
                    _logger.LogWarning("Парковка не найдена. {ParkingId}", id);
                    TempData["Message"] = "Парковка не найдена.";
                    return RedirectToAction("Index"); 
                }

                ViewData["parking"] = parking;

              
                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(startTime) &&
                    !string.IsNullOrEmpty(endDate) && !string.IsNullOrEmpty(endTime))
                {

                    ViewData["Parking"] = parking;
                    ViewData["StartDate"] = startDate;
                    ViewData["StartTime"] = startTime;
                    ViewData["EndDate"] = endDate;
                    ViewData["EndTime"] = endTime;

                    var start = DateTime.Parse($"{startDate} {startTime}");
                    var end = DateTime.Parse($"{endDate} {endTime}");

                    var now = DateTime.Now;


                    //UTC
                    var startUtc = start.ToUniversalTime();
                    var endUtc = end.ToUniversalTime();

                    var reservations = await _applicationDbContext.ParkingSpots
                    .Where(ps => ps.Status == ParkingSpotStatus.Free && ps.ParkingId == id)
                     .Where(ps => !_applicationDbContext.Bookings
                         .Any(r => r.ParkingSpotId == ps.ParkingSpotId &&
                                   r.StartTime < endUtc &&
                                   r.EndTime > startUtc && r.Status == BookingStatus.Confirmed))
                     .ToListAsync();

                    _logger.LogInformation("Найдено {Count} свободных мест", reservations.Count);
                    return View(reservations); 
                }

                
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при работе с базой данных";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка при загрузке доступных для бронирования парковочных мест");
                TempData["Message"] = "Произошла непредвиденная ошибка при загрузке доступных для бронирования парковочных мест";
            }
            return View();
        }

        
        public async Task<IActionResult> MakeReservation(int id, string startDate, string startTime, string endDate, string endTime) 
        {
            if (!User.Identity.IsAuthenticated) 
            {
                _logger.LogWarning("Не авторизированный пользователь пытался сделать бронь.");
                return RedirectToAction("Login", "Account");
            }
            try
            {
                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(startTime) &&
                   !string.IsNullOrEmpty(endDate) && !string.IsNullOrEmpty(endTime))
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    var start = DateTime.Parse($"{startDate} {startTime}");
                    var end = DateTime.Parse($"{endDate} {endTime}");

                    //UTC
                    var startUtc = start.ToUniversalTime();
                    var endUtc = end.ToUniversalTime();

                    var isAvailable = !await _applicationDbContext.Bookings
                    .AnyAsync(b => b.ParkingSpotId == id &&
                                   b.StartTime < endUtc &&
                                   b.EndTime > startUtc && b.Status == BookingStatus.Confirmed);

                    if (!isAvailable)
                    {
                        _logger.LogWarning("Место уже занято на указанное время. {ParkingSpotId} {StartTime} {EndTime}", id, startUtc, endUtc);
                        TempData["Message"] = "Место уже занято на указанное время.";
                        return RedirectToAction("Index");
                    }

                    var booking = new Booking
                    {
                        ParkingSpotId = id,
                        UserId = Convert.ToInt32(userId),
                        StartTime = startUtc,
                        EndTime = endUtc,
                        Status = BookingStatus.Confirmed
                    };

                    _applicationDbContext.Bookings.Add(booking);
                    await _applicationDbContext.SaveChangesAsync();

                    _logger.LogInformation("Бронирование успешно. {Id}. с {StartTime} по {EndTime} пользователем {Name}", booking.Id, booking.StartTime, booking.EndTime, User.Identity.Name);
                    TempData["Message"] = "Бронирование успешно!";
                    return RedirectToAction("Index");
                }
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Ошибка при работе с базой данных");
                TempData["Message"] = "Ошибка при работе с базой данных";
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Произошла непредвиденная ошибка при попытке бронирования парковочного места");
                TempData["Message"] = "Произошла непредвиденная ошибка при попытке бронирования парковочного места";
            }

           
            return RedirectToAction("Index");
        }

    }
}
