using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasko.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingRuleVersionIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PricingRules_Code",
                table: "PricingRules");

            migrationBuilder.CreateIndex(
                name: "IX_PricingRules_Code_Version",
                table: "PricingRules",
                columns: new[] { "Code", "Version" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PricingRules_Code_Version",
                table: "PricingRules");

            migrationBuilder.CreateIndex(
                name: "IX_PricingRules_Code",
                table: "PricingRules",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
