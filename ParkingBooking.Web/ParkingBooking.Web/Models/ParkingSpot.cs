using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkingBooking.Web.Models
{
    public class ParkingSpot
    {
        public int ParkingSpotId { get; set; }

        [Required]
        [StringLength(10)]
        public string Number { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }
        public string? Information { get; set; }

        public int ParkingId { get; set; }

        [ForeignKey("ParkingId")]
        public Parking Parking { get; set; }
    }

    
}
