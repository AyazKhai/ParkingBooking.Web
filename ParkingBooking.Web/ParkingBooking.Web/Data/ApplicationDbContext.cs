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
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Parking>()
               .HasIndex(p => p.Address)
               .IsUnique();

            modelBuilder.Entity<Parking>()
                .HasMany(p => p.ParkingSpots)
                .WithOne(ps => ps.Parking)
                .HasForeignKey(ps => ps.ParkingId)
                .OnDelete(DeleteBehavior.Cascade); 

            modelBuilder.Entity<ParkingSpot>()
            .HasIndex(p => p.Number)
            .IsUnique();

            modelBuilder.Entity<ParkingSpot>()
                .HasIndex(p => p.Number)
                .IsUnique();

        }

        
        public DbSet<Parking> Parkings { get; set; }
        public DbSet<ParkingSpot> ParkingSpots { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Booking> Bookings { get; set; }
    }
}
