using System.ComponentModel.DataAnnotations;

namespace ParkingBooking.Web.Models
{
    public class Parking
    {
        public int ParkingId { get; set; } // Уникальный идентификатор стоянки

        [Required]
        public string Address { get; set; }
        public ICollection<ParkingSpot> ParkingSpots { get; set; } = new List<ParkingSpot>();
    }
}
