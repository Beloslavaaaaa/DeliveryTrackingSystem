using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cargobell.Data.Migrations
{
    /// <inheritdoc />
    public partial class Shipmnet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFragile",
                table: "Shipments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFragile",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Shipments");
        }
    }
}
