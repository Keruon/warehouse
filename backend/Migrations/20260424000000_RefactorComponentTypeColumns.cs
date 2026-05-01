using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class RefactorComponentTypeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "ComponentTypes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "ComponentTypes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Footprint",
                table: "ComponentTypes",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""ComponentTypes"" SET ""Kind"" = ""Name"", ""Value"" = '' WHERE ""Kind"" = '';
            ");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ComponentTypes");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentTypes_CategoryId_Kind_Value_Footprint",
                table: "ComponentTypes",
                columns: new[] { "CategoryId", "Kind", "Value", "Footprint" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComponentTypes_CategoryId_Kind_Value_Footprint",
                table: "ComponentTypes");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ComponentTypes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE ""ComponentTypes"" SET ""Name"" = ""Kind"" WHERE ""Name"" = '';
            ");

            migrationBuilder.DropColumn(name: "Footprint", table: "ComponentTypes");
            migrationBuilder.DropColumn(name: "Value", table: "ComponentTypes");
            migrationBuilder.DropColumn(name: "Kind", table: "ComponentTypes");
        }
    }
}
