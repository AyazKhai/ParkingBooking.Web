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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Настройка связи один-ко-многим между Parking и ParkingSpot
            modelBuilder.Entity<Parking>()
                .HasMany(p => p.ParkingSpots) // У одной стоянки много парковочных мест
                .WithOne(ps => ps.Parking) // У парковочного места одна стоянка
                .HasForeignKey(ps => ps.ParkingId); // Внешний ключ в ParkingSpot

            modelBuilder.Entity<Parking>()
                .HasMany(p => p.ParkingSpots)
                .WithOne(ps => ps.Parking)
                .HasForeignKey(ps => ps.ParkingId)
                .OnDelete(DeleteBehavior.Cascade); // Каскадное удаление

            base.OnModelCreating(modelBuilder);
        }

        
        public DbSet<Parking> Parkings { get; set; }
        public DbSet<ParkingSpot> ParkingSpots { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
