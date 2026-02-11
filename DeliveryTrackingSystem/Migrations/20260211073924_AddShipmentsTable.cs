using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryTrackingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Ratings");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "StatusHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StatusId1",
                table: "StatusHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "DeliveryRoutes",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_StatusHistories_StatusId1",
                table: "StatusHistories",
                column: "StatusId1");

            migrationBuilder.AddForeignKey(
                name: "FK_StatusHistories_Statuses_StatusId1",
                table: "StatusHistories",
                column: "StatusId1",
                principalTable: "Statuses",
                principalColumn: "StatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StatusHistories_Statuses_StatusId1",
                table: "StatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_StatusHistories_StatusId1",
                table: "StatusHistories");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "StatusHistories");

            migrationBuilder.DropColumn(
                name: "StatusId1",
                table: "StatusHistories");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "DeliveryRoutes");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Ratings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
