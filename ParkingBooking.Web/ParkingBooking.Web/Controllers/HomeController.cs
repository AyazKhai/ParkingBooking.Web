using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingBooking.Web.Data;
using ParkingBooking.Web.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace ParkingBooking.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _applicationDbContext;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext applicationDbContext)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var id = Convert.ToInt32(User.FindFirstValue(ClaimTypes.NameIdentifier));

                try
                {
                    var bookings = await _applicationDbContext.Bookings
                    .Where(b => b.UserId == id && b.Status == BookingStatus.Confirmed)
                    .Include(b => b.ParkingSpot)
                        .ThenInclude(ps => ps.Parking)
                    .OrderByDescending(b => b.StartTime)
                    .ToArrayAsync();
                    ViewData["Bookings"] = bookings;
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "������ ��� ������ � ����� ������");
                    TempData["Message"] = "������ ��� ������ � ����� ������";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "��������� �������������� ������");
                    TempData["Message"] = "��������� �������������� ������";
                    return RedirectToAction("Index");
                }
             
            }
            else 
            {
                _logger.LogWarning("������������ �� ����������������");
                return RedirectToAction("Login", "Account");
            }
            return View();
        }

        

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
