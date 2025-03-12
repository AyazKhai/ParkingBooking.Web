namespace ParkingBooking.Web.Models
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string Moderator = "Moderator";

        public static List<string> AllRoles => new() { Admin, Moderator, User };
    }
}
