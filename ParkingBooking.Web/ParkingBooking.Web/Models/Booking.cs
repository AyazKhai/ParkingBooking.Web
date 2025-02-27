namespace ParkingBooking.Web.Models
{
    public class Booking
    {
        private User _user;
        private ParkingSpot _spot;

        public Booking(User user, ParkingSpot spot)
        {
            _user = user;
            _spot = spot;
        }
    }
}
