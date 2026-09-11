using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConferenceRoomAPI.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseLocalBookingTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_RoomId_StartUtc_EndUtc",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_TimeRange",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "StartUtc",
                table: "Bookings",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "EndUtc",
                table: "Bookings",
                newName: "EndTime");

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "Bookings",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.Sql(
                """
                UPDATE "Bookings"
                SET "Date" = substr("StartTime", 1, 10),
                    "StartTime" = substr("StartTime", 12),
                    "EndTime" = substr("EndTime", 12)
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RoomId_Date_StartTime_EndTime",
                table: "Bookings",
                columns: new[] { "RoomId", "Date", "StartTime", "EndTime" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_TimeRange",
                table: "Bookings",
                sql: "\"EndTime\" > \"StartTime\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_RoomId_Date_StartTime_EndTime",
                table: "Bookings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bookings_TimeRange",
                table: "Bookings");

            migrationBuilder.Sql(
                """
                UPDATE "Bookings"
                SET "StartTime" = "Date" || ' ' || "StartTime",
                    "EndTime" = "Date" || ' ' || "EndTime"
                """);

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Bookings");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "Bookings",
                newName: "StartUtc");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "Bookings",
                newName: "EndUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RoomId_StartUtc_EndUtc",
                table: "Bookings",
                columns: new[] { "RoomId", "StartUtc", "EndUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bookings_TimeRange",
                table: "Bookings",
                sql: "\"EndUtc\" > \"StartUtc\"");
        }
    }
}
