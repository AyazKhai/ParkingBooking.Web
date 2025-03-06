using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingBooking.Web.Data;
using ParkingBooking.Web.Models;

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
                ViewData["parkings"] = _applicationDbContext.Parkings.ToArray();

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
                // Получаем информацию о парковке
                var parking = _applicationDbContext.Parkings.FirstOrDefault(p => p.ParkingId == id);
                if (parking == null)
                {
                    TempData["Message"] = "Парковка не найдена.";
                    return RedirectToAction("Index"); 
                }

                ViewData["parking"] = parking;

              
                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(startTime) &&
                    !string.IsNullOrEmpty(endDate) && !string.IsNullOrEmpty(endTime))
                {
                    var start = DateTime.Parse($"{startDate} {startTime}");
                    var end = DateTime.Parse($"{endDate} {endTime}");

                    //UTC
                    var startUtc = start.ToUniversalTime();
                    var endUtc = end.ToUniversalTime();

                    var reservations = await _applicationDbContext.ParkingSpots
                        .Where(ps => !_applicationDbContext.Bookings
                            .Any(r => r.ParkingSpotId == ps.ParkingSpotId &&
                                      r.StartTime < endUtc &&
                                      r.EndTime > startUtc))
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
        public async Task<IActionResult> MakeReservation(Booking booking) 
        {
            TempData["Message"] = "Бронирование успешно";
            return RedirectToAction("Index");
        }







    }
}
