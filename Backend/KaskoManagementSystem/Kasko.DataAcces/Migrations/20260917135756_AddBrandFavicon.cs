using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasko.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandFavicon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FaviconImage",
                table: "BrandSettings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaviconImage",
                table: "BrandSettings");
        }
    }
}
