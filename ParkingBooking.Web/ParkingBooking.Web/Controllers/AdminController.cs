using Microsoft.AspNetCore.Mvc;
using ParkingBooking.Web.Data;
using ParkingBooking.Web.Models;

namespace ParkingBooking.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public AdminController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        public async Task<IActionResult> Index()
        {
            var parkings = _applicationDbContext.Parkings.ToList();
            return View(parkings);
        }
        [HttpPost]
        public IActionResult GetParkingSpot(int id) {
            var spots = _applicationDbContext.ParkingSpots.Where(s => s.ParkingId == id).ToList();
            return View(spots);
        }

        public async Task<IActionResult> UsersInfo() {
            var users = _applicationDbContext.Users.ToList();
            return View(users);
        }

        public async Task<IActionResult> BookingsInfo()
        {
            var users = _applicationDbContext.Users.ToList();
            return View(users);
        }





    }
}
