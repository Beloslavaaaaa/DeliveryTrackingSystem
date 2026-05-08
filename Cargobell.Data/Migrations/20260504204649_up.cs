using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cargobell.Data.Migrations
{
    /// <inheritdoc />
    public partial class up : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TimeInterval",
                table: "CourierRequests",
                newName: "DestinationZone");

            migrationBuilder.AddColumn<bool>(
                name: "IsExactTime",
                table: "CourierRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreferredPickupTimeEnd",
                table: "CourierRequests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsExactTime",
                table: "CourierRequests");

            migrationBuilder.DropColumn(
                name: "PreferredPickupTimeEnd",
                table: "CourierRequests");

            migrationBuilder.RenameColumn(
                name: "DestinationZone",
                table: "CourierRequests",
                newName: "TimeInterval");
        }
    }
}
