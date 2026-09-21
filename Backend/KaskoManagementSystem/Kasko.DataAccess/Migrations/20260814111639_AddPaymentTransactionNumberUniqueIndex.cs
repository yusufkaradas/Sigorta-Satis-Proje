using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasko.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactionNumberUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Policies_PolicyId",
                table: "Payments");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionNumber",
                table: "Payments",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionNumber",
                table: "Payments",
                column: "TransactionNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Policies_PolicyId",
                table: "Payments",
                column: "PolicyId",
                principalTable: "Policies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Policies_PolicyId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TransactionNumber",
                table: "Payments");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionNumber",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Policies_PolicyId",
                table: "Payments",
                column: "PolicyId",
                principalTable: "Policies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
