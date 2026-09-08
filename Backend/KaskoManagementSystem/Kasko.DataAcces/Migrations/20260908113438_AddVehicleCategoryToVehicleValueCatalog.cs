using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasko.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleCategoryToVehicleValueCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
        """
        IF COL_LENGTH(N'dbo.VehicleValueCatalogs', N'VehicleCategory') IS NULL
        BEGIN
            ALTER TABLE [VehicleValueCatalogs]
            ADD [VehicleCategory] nvarchar(50) NULL;
        END
        """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VehicleCategory",
                table: "VehicleValueCatalogs");
        }
    }
}
