using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            //var parkings = _applicationDbContext.Parkings.ToList();
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

        public async Task<IActionResult> UsersInfo() {
            var users = _applicationDbContext.Users.ToList();
            return View(users);
        }

        public async Task<IActionResult> BookingsInfo()
        {
            var users = _applicationDbContext.Users.ToList();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteParkingSpot(int parkingSpotId) 
        {
            var spot = await _applicationDbContext.ParkingSpots
                .FirstOrDefaultAsync(s => s.ParkingSpotId == parkingSpotId);
            if (spot == null)
            {
                TempData["Message"] = "Парковочное место не найдено.";
                return RedirectToAction("Index");
            }

            if (spot != null) 
            {
                try
                {
                    int id = spot.ParkingId;
                    _applicationDbContext.ParkingSpots.Remove(spot);
                    await _applicationDbContext.SaveChangesAsync();

                    TempData["Message"] = "Парковочное место успешно удалено.";
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
                }
                catch (DbUpdateException ex)
                {
                    TempData["Message"] = "Ошибка при удалкении данных в базе данных: ";
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
                }
                catch (Exception ex)
                {
                    TempData["Message"] = "Произошла ошибка";
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
                }
            }
            TempData["Message"] = "Данные не удалены";
            return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
            

        }
        [HttpPost]
        public async Task<IActionResult> CreateParkingSpot(ParkingSpot spot) {
            var parkingExists = _applicationDbContext.Parkings.FirstOrDefault(p => p.ParkingId == spot.ParkingId);

            if (parkingExists == null)
            {
                TempData["Message"] = "Парковка с указанным идентификатором не найдена.";
                return RedirectToAction("Index");
            }


            if (spot != null)
            {
                try
                {
                    _applicationDbContext.ParkingSpots.Add(spot);
                    _applicationDbContext.SaveChanges();
                    TempData["Message"] = "Данные добавлены";
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
                }
                catch(DbUpdateException ex) 
                {
                    TempData["Message"] = "Ошибка при добавлении данных в базу данных: " ;
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
                }
                catch (Exception ex)
                {
                    TempData["Message"] = "Произошла ошибка";
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
                }
 
            }

            TempData["Message"] = "Данные не добавлены";
            return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
        }
        [HttpGet]
        public IActionResult GetParkingSpot(int id)
        {
            ViewData["parkingSpots"] = _applicationDbContext.ParkingSpots.Where(s => s.ParkingId == id).ToArray();
            ViewData["parking"] = _applicationDbContext.Parkings.FirstOrDefault(p => p.ParkingId == id);
            return View();
        }

        public IActionResult EditParkingSpot(int id) 
        {
            var parkingSpot = _applicationDbContext.ParkingSpots
                .FirstOrDefault(s => s.ParkingSpotId == id);

            ViewData["parking"] = _applicationDbContext.Parkings.FirstOrDefault(p => p.ParkingId == parkingSpot.ParkingId);
            return View(parkingSpot);
        }
        [HttpPost]
        public async Task<IActionResult> EditParkingSpot(ParkingSpot spot) 
        {
            if (spot != null) 
            {
                try
                {
                    var existingSpot = _applicationDbContext.ParkingSpots
                        .FirstOrDefault(s => s.ParkingSpotId == spot.ParkingSpotId);

                    if (existingSpot == null)
                    {
                        TempData["Message"] = "Ошибка: Парковочное место не найдено.";
                        return RedirectToAction("Index"); 
                    }

                   // existingSpot = spot;

                    existingSpot.Number = spot.Number;
                    existingSpot.Status = spot.Status;
                    existingSpot.Information = spot.Information;
                    existingSpot.ParkingId = spot.ParkingId;

                    _applicationDbContext.ParkingSpots.Update(existingSpot);
                    _applicationDbContext.SaveChanges();

                    TempData["Message"] = "Парковочное место успешно обновлено.";
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });


                }
                catch (DbUpdateException ex)
                {
                    TempData["Message"] = "Ошибка при обновлении данных в базе данных";
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
                }
                catch (Exception ex)
                {
                    TempData["Message"] = "Произошла ошибка";
                    return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
                }
            }
            return View();
        }




    }
}
