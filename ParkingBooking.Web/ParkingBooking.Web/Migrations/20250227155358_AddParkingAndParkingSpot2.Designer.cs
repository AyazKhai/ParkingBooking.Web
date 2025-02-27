﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using ParkingBooking.Web.Data;

#nullable disable

namespace ParkingBooking.Web.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250227155358_AddParkingAndParkingSpot2")]
    partial class AddParkingAndParkingSpot2
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ParkingBooking.Web.Models.Parking", b =>
                {
                    b.Property<int>("ParkingId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("ParkingId"));

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("ParkingId");

                    b.ToTable("Parkings");
                });

            modelBuilder.Entity("ParkingBooking.Web.Models.ParkingSpot", b =>
                {
                    b.Property<int>("ParkingSpotId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("ParkingSpotId"));

                    b.Property<string>("Information")
                        .HasColumnType("text");

                    b.Property<string>("Number")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("character varying(10)");

                    b.Property<int>("ParkingId")
                        .HasColumnType("integer");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)");

                    b.HasKey("ParkingSpotId");

                    b.HasIndex("ParkingId");

                    b.ToTable("ParkingSpots");
                });

            modelBuilder.Entity("ParkingBooking.Web.Models.User", b =>
                {
                    b.Property<int>("UserId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("UserId"));

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("UserId");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ParkingBooking.Web.Models.ParkingSpot", b =>
                {
                    b.HasOne("ParkingBooking.Web.Models.Parking", "Parking")
                        .WithMany("ParkingSpots")
                        .HasForeignKey("ParkingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Parking");
                });

            modelBuilder.Entity("ParkingBooking.Web.Models.Parking", b =>
                {
                    b.Navigation("ParkingSpots");
                });
#pragma warning restore 612, 618
        }
    }
}
