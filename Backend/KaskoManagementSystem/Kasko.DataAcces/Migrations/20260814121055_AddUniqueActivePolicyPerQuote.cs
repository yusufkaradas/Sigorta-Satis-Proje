using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasko.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueActivePolicyPerQuote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Policies_QuoteId",
                table: "Policies");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_QuoteId",
                table: "Policies",
                column: "QuoteId",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Policies_QuoteId",
                table: "Policies");

            migrationBuilder.CreateIndex(
                name: "IX_Policies_QuoteId",
                table: "Policies",
                column: "QuoteId");
        }
    }
}
