using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasko.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FilterPackageCoverageUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PackageCoverages_InsurancePackageId_CoverageId",
                table: "PackageCoverages");

            migrationBuilder.CreateIndex(
                name: "IX_PackageCoverages_InsurancePackageId_CoverageId",
                table: "PackageCoverages",
                columns: new[] { "InsurancePackageId", "CoverageId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PackageCoverages_InsurancePackageId_CoverageId",
                table: "PackageCoverages");

            migrationBuilder.CreateIndex(
                name: "IX_PackageCoverages_InsurancePackageId_CoverageId",
                table: "PackageCoverages",
                columns: new[] { "InsurancePackageId", "CoverageId" },
                unique: true);
        }
    }
}
