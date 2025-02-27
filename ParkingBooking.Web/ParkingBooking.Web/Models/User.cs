using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ParkingBooking.Web.Models
{
    public class User: IdentityUser
    {
        [Key]
        public int UserId { get; set; }
        public string Name { get; set; }
        public string PasswordHash { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
}
