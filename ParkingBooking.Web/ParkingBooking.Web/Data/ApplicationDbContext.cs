using Microsoft.EntityFrameworkCore;
using ParkingBooking.Web.Models;

namespace ParkingBooking.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
