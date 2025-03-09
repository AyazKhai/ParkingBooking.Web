using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingBooking.Web.Data;
using ParkingBooking.Web.Models;

namespace ParkingBooking.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public AdminController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                  ViewData["parkings"] = await _applicationDbContext.Parkings.Where(p => p.Status == ParkingStatus.Active)
                    .Include(p => p.ParkingSpots)
                    .ToArrayAsync();

            }
            catch (Exception ex)
            {
                TempData["Message"] = "Произошла ошибка";
            }
            return View();
        }

        public async Task<IActionResult> UsersInfo() {
            var users = await _applicationDbContext.Users.ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> BookingsInfo()
        {
            var users = await _applicationDbContext.Users.ToArrayAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateParking(string address)
        {
            
            if (string.IsNullOrEmpty(address))
            {
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
                TempData["Message"] = "Парковка создана";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException ex)
            {
                TempData["Message"] = "Ошибка при работе с базой данных";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Произошла непредвиденная ошибка";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteParking(int parkingId) 
        {
            var parking = await _applicationDbContext.Parkings
                .FirstOrDefaultAsync(s => s.ParkingId == parkingId);

            if (parking == null) 
            {
                TempData["Message"] = "Парковочное место не найдено.";
                return RedirectToAction("Index");
            }

            try
            {
                _applicationDbContext.Parkings.Remove(parking);
                await _applicationDbContext.SaveChangesAsync();

                TempData["Message"] = "Парковка успешно удалена";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException ex)
            {
                TempData["Message"] = "Ошибка при удалкении данных в базе данных ";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Произошла ошибка";
                return RedirectToAction("Index");
            }

        }
        [HttpPost]
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
                        TempData["Message"] = "Ошибка: Парковочное место не найдено.";
                        return RedirectToAction("Index");
                    }
                    if (existingParking.Address == parking.Address && existingParking.Status == parking.Status) 
                    {
                        return RedirectToAction("GetParkingSpot", new { id = parking.ParkingId });
                    }

                    existingParking.Address = parking.Address;
                    existingParking.Status = parking.Status;

                    _applicationDbContext.Parkings.Update(existingParking);
                    await _applicationDbContext.SaveChangesAsync();

                    TempData["Message"] = "Данне  парковки успешно обновлено.";
                    return RedirectToAction("GetParkingSpot", new {id = parking.ParkingId });
                }
                catch (DbUpdateException ex)
                {
                    TempData["Message"] = "Ошибка при изменении данных в базе данных ";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Message"] = "Произошла ошибка";
                    return RedirectToAction("Index");
                }
            }
            return RedirectToAction("Index");
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
                TempData["Message"] = "Ошибка при удалкении данных в базе данных ";
                return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Произошла ошибка";
                return RedirectToAction("GetParkingSpot", new { id = spot.ParkingId });
            }
            

        }
        [HttpPost]
        public async Task<IActionResult> CreateParkingSpot(ParkingSpot spot) {
            var parkingExists = await _applicationDbContext.Parkings.FirstOrDefaultAsync(p => p.ParkingId == spot.ParkingId);

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
                    await _applicationDbContext.SaveChangesAsync();
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
        public async Task<IActionResult> GetParkingSpot(int id)
        {
            ViewData["parkingSpots"] = await _applicationDbContext.ParkingSpots.Where(s => s.ParkingId == id).ToArrayAsync();
            ViewData["parking"] = await _applicationDbContext.Parkings.FirstOrDefaultAsync(p => p.ParkingId == id);
            return View();
        }

        public async Task<IActionResult> EditParkingSpot(int id) 
        {
            var parkingSpot = await _applicationDbContext.ParkingSpots
                .FirstOrDefaultAsync(s => s.ParkingSpotId == id);

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
                    var existingSpot = await _applicationDbContext.ParkingSpots
                .FirstOrDefaultAsync(s => s.ParkingSpotId == spot.ParkingSpotId);

                    if (existingSpot == null)
                    {
                        TempData["Message"] = "Ошибка: Парковочное место не найдено.";
                        return RedirectToAction("Index"); 
                    }


                    existingSpot.Number = spot.Number;
                    existingSpot.Status = spot.Status;
                    existingSpot.Information = spot.Information;
                    existingSpot.ParkingId = spot.ParkingId;

                    _applicationDbContext.ParkingSpots.Update(existingSpot);
                    await _applicationDbContext.SaveChangesAsync();

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
