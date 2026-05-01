using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Storage.Data;

#nullable disable

namespace backend.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260424183000_AddProductionProjectSupport")]
    public partial class AddProductionProjectSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveProjectLocationId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationKind",
                table: "WarehouseLocations",
                type: "text",
                nullable: false,
                defaultValue: "Warehouse");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ActiveProjectLocationId",
                table: "Users",
                column: "ActiveProjectLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_WarehouseLocations_ActiveProjectLocationId",
                table: "Users",
                column: "ActiveProjectLocationId",
                principalTable: "WarehouseLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_WarehouseLocations_ActiveProjectLocationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_ActiveProjectLocationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ActiveProjectLocationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LocationKind",
                table: "WarehouseLocations");
        }
    }
}
