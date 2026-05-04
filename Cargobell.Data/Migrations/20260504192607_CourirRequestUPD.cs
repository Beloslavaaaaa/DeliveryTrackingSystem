using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cargobell.Data.Migrations
{
    /// <inheritdoc />
    public partial class CourirRequestUPD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DestinationZone",
                table: "CourierRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedPrice",
                table: "CourierRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsFragile",
                table: "CourierRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PackageType",
                table: "CourierRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinationZone",
                table: "CourierRequests");

            migrationBuilder.DropColumn(
                name: "EstimatedPrice",
                table: "CourierRequests");

            migrationBuilder.DropColumn(
                name: "IsFragile",
                table: "CourierRequests");

            migrationBuilder.DropColumn(
                name: "PackageType",
                table: "CourierRequests");
        }
    }
}
