using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Storage.Data;

#nullable disable

namespace backend.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260424195500_HardenProductionProjectSchema")]
    public partial class HardenProductionProjectSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WarehouseLocations_LocationKind",
                table: "WarehouseLocations",
                column: "LocationKind");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseAreas_ZoneType_Production",
                table: "WarehouseAreas",
                column: "ZoneType",
                unique: true,
                filter: "\"ZoneType\" = 'Production'");

            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseLocations_WarehouseShelves_ShelfId",
                table: "WarehouseLocations");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseLocations_WarehouseShelves_ShelfId",
                table: "WarehouseLocations",
                column: "ShelfId",
                principalTable: "WarehouseShelves",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WarehouseLocations_WarehouseShelves_ShelfId",
                table: "WarehouseLocations");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseLocations_LocationKind",
                table: "WarehouseLocations");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseAreas_ZoneType_Production",
                table: "WarehouseAreas");

            migrationBuilder.AddForeignKey(
                name: "FK_WarehouseLocations_WarehouseShelves_ShelfId",
                table: "WarehouseLocations",
                column: "ShelfId",
                principalTable: "WarehouseShelves",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}