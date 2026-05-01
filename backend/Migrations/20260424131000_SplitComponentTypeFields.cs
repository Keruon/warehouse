using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class SplitComponentTypeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComponentTypes_CategoryId",
                table: "ComponentTypes");

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

            migrationBuilder.Sql("""
UPDATE "ComponentTypes"
SET "Kind" = COALESCE(NULLIF(TRIM("Name"), ''), 'Legacy'),
    "Value" = COALESCE(NULLIF(TRIM("Name"), ''), 'Unknown')
WHERE COALESCE(TRIM("Kind"), '') = '' OR COALESCE(TRIM("Value"), '') = '';
""");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ComponentTypes");

            migrationBuilder.Sql("""
INSERT INTO "ComponentCategories" ("Id", "Name", "ParentId", "Description", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy")
SELECT '00000000-0000-0000-0000-000000000001'::uuid, 'Legacy Imported', NULL, 'Fallback category for migrated component types.', TRUE, NOW(),
       '00000000-0000-0000-0000-000000000001'::uuid, NOW(), '00000000-0000-0000-0000-000000000001'::uuid
WHERE NOT EXISTS (SELECT 1 FROM "ComponentCategories" WHERE "Name" = 'Legacy Imported');

UPDATE "ComponentTypes" ct
SET "CategoryId" = cc."Id"
FROM "ComponentCategories" cc
WHERE ct."CategoryId" IS NULL AND cc."Name" = 'Legacy Imported';

UPDATE "ComponentTypes" ct
SET "CategoryId" = fallback."Id"
FROM (
    SELECT "Id" FROM "ComponentCategories" ORDER BY "CreatedAt" LIMIT 1
) AS fallback
WHERE ct."CategoryId" IS NULL;
""");

            migrationBuilder.AlterColumn<System.Guid>(
                name: "CategoryId",
                table: "ComponentTypes",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(System.Guid),
                oldType: "uuid",
                oldNullable: true);

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

            migrationBuilder.AlterColumn<System.Guid>(
                name: "CategoryId",
                table: "ComponentTypes",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(System.Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ComponentTypes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
UPDATE "ComponentTypes"
SET "Name" = TRIM(CONCAT_WS(' ', "Kind", "Value", "Footprint"));
""");

            migrationBuilder.DropColumn(
                name: "Footprint",
                table: "ComponentTypes");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "ComponentTypes");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "ComponentTypes");

            migrationBuilder.CreateIndex(
                name: "IX_ComponentTypes_CategoryId",
                table: "ComponentTypes",
                column: "CategoryId");
        }
    }
}
