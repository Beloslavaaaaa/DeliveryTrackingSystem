using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryTrackingSystem.Migrations
{
    /// <inheritdoc />
    public partial class CourierRequest_NewProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DropoffAddress",
                table: "CourierRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropoffAddress",
                table: "CourierRequests");
        }
    }
}
