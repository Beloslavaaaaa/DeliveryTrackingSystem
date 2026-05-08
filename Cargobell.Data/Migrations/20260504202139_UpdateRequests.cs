using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cargobell.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DestinationZone",
                table: "CourierRequests",
                newName: "TimeInterval");

            migrationBuilder.AddColumn<decimal>(
                name: "CodAmount",
                table: "CourierRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CustomSenderEmail",
                table: "CourierRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomSenderName",
                table: "CourierRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomSenderPhone",
                table: "CourierRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCashOnDelivery",
                table: "CourierRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCustomSender",
                table: "CourierRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodAmount",
                table: "CourierRequests");

            migrationBuilder.DropColumn(
                name: "CustomSenderEmail",
                table: "CourierRequests");

            migrationBuilder.DropColumn(
                name: "CustomSenderName",
                table: "CourierRequests");

            migrationBuilder.DropColumn(
                name: "CustomSenderPhone",
                table: "CourierRequests");

            migrationBuilder.DropColumn(
                name: "IsCashOnDelivery",
                table: "CourierRequests");

            migrationBuilder.DropColumn(
                name: "IsCustomSender",
                table: "CourierRequests");

            migrationBuilder.RenameColumn(
                name: "TimeInterval",
                table: "CourierRequests",
                newName: "DestinationZone");
        }
    }
}
