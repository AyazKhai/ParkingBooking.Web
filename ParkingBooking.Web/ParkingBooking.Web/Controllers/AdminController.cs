using Microsoft.AspNetCore.Mvc;
using ParkingBooking.Web.Data;

namespace ParkingBooking.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public AdminController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        public IActionResult Index()
        {
            return View();
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
