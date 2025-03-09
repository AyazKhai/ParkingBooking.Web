using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingBooking.Web.Data;
using ParkingBooking.Web.Models;
using System.Security.Claims;

namespace ParkingBooking.Web.Controllers
{
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public BookingController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["parkings"] = await _applicationDbContext.Parkings.Where(p => p.Status == ParkingStatus.Active)
                    .Include(p => p.ParkingSpots.Where(ps => ps.Status == ParkingSpotStatus.Free))
                    .ToArrayAsync();

            }
            catch (Exception ex)
            {
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

                    if (start <= now || end <= now || end <= start)
                    {
                        return View();
                    }

                    //UTC
                    var startUtc = start.ToUniversalTime();
                    var endUtc = end.ToUniversalTime();

                    var reservations = await _applicationDbContext.ParkingSpots
                    .Where(ps => ps.Status == ParkingSpotStatus.Free && ps.ParkingId == id)
                     .Where(ps => !_applicationDbContext.Bookings
                         .Any(r => r.ParkingSpotId == ps.ParkingSpotId &&
                                   (r.StartTime < endUtc &&
                                   r.EndTime > startUtc)))
                     .ToListAsync();

                    return View(reservations); 
                }

                return View();
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Произошла ошибка при загрузке данных.";
                return RedirectToAction("Index");
            }
        }
        public async Task<IActionResult> MakeReservation(int id, string startDate, string startTime, string endDate, string endTime) 
        {
            if (!User.Identity.IsAuthenticated) 
            {
                return RedirectToAction("Login", "Account");
            }
             
            

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
                               b.EndTime > startUtc);

                if (!isAvailable)
                {
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

                TempData["Message"] = "Бронирование успешно!";
                return RedirectToAction("Index");
            }

            



            TempData["Message"] = "Бронирование успешно";
            return RedirectToAction("Index");
        }

    }
}
