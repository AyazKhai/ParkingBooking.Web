using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingBooking.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingAndParkingSpot2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSpot_Parking_ParkingId",
                table: "ParkingSpot");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ParkingSpot",
                table: "ParkingSpot");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Parking",
                table: "Parking");

            migrationBuilder.RenameTable(
                name: "ParkingSpot",
                newName: "ParkingSpots");

            migrationBuilder.RenameTable(
                name: "Parking",
                newName: "Parkings");

            migrationBuilder.RenameIndex(
                name: "IX_ParkingSpot_ParkingId",
                table: "ParkingSpots",
                newName: "IX_ParkingSpots_ParkingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParkingSpots",
                table: "ParkingSpots",
                column: "ParkingSpotId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Parkings",
                table: "Parkings",
                column: "ParkingId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSpots_Parkings_ParkingId",
                table: "ParkingSpots",
                column: "ParkingId",
                principalTable: "Parkings",
                principalColumn: "ParkingId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSpots_Parkings_ParkingId",
                table: "ParkingSpots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ParkingSpots",
                table: "ParkingSpots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Parkings",
                table: "Parkings");

            migrationBuilder.RenameTable(
                name: "ParkingSpots",
                newName: "ParkingSpot");

            migrationBuilder.RenameTable(
                name: "Parkings",
                newName: "Parking");

            migrationBuilder.RenameIndex(
                name: "IX_ParkingSpots_ParkingId",
                table: "ParkingSpot",
                newName: "IX_ParkingSpot_ParkingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParkingSpot",
                table: "ParkingSpot",
                column: "ParkingSpotId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Parking",
                table: "Parking",
                column: "ParkingId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSpot_Parking_ParkingId",
                table: "ParkingSpot",
                column: "ParkingId",
                principalTable: "Parking",
                principalColumn: "ParkingId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
