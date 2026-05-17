using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cargobell.Data.Migrations
{
    /// <inheritdoc />
    public partial class CoutierRQ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiverEmail",
                table: "CourierRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverName",
                table: "CourierRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReceiverPhone",
                table: "CourierRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiverEmail",
                table: "CourierRequests");

            migrationBuilder.DropColumn(
                name: "ReceiverName",
                table: "CourierRequests");

            migrationBuilder.DropColumn(
                name: "ReceiverPhone",
                table: "CourierRequests");
        }
    }
}
