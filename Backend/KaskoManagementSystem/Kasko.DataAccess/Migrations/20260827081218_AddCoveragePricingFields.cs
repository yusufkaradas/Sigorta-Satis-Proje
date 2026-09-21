using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasko.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddCoveragePricingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DefaultLimit",
                table: "Coverages",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "Coverages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PricingType",
                table: "Coverages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Rate",
                table: "Coverages",
                type: "decimal(9,4)",
                precision: 9,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultLimit",
                table: "Coverages");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "Coverages");

            migrationBuilder.DropColumn(
                name: "PricingType",
                table: "Coverages");

            migrationBuilder.DropColumn(
                name: "Rate",
                table: "Coverages");
        }
    }
}
