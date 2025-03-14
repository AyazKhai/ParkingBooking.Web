﻿namespace ParkingBooking.Web.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ParkingSpotId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public BookingStatus Status { get; set; }

        public ParkingSpot ParkingSpot { get; set; }
        public User User { get; set; }
    }
}
