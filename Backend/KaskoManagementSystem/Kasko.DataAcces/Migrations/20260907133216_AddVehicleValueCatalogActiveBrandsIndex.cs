using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasko.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleValueCatalogActiveBrandsIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_VehicleValueCatalogs_ActiveBrands",
                table: "VehicleValueCatalogs",
                columns: new[] { "BrandCode", "BrandName" },
                filter: "[IsDeleted] = 0 AND [IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VehicleValueCatalogs_ActiveBrands",
                table: "VehicleValueCatalogs");
        }
    }
}
