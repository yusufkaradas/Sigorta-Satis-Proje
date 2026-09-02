using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasko.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseKaskoRateVersion2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "EffectiveUntil",
                value: new DateTime(2026, 8, 31, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.InsertData(
                table: "PricingRules",
                columns: new[] { "Id", "Code", "CreatedBy", "CreatedDate", "DeletedBy", "DeletedDate", "Description", "EffectiveFrom", "EffectiveUntil", "IsActive", "IsDeleted", "Name", "UpdatedBy", "UpdatedDate", "Value", "Version" },
                values: new object[] { new Guid("10000000-0000-0000-0000-000000000020"), "BASE_KASKO_RATE", null, new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "01.09.2026 itibarıyla geçerli yeni temel kasko oranı.", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, true, false, "Temel Kasko Oranı V2", null, null, 0.0215m, 2 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000020"));

            migrationBuilder.UpdateData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "EffectiveUntil",
                value: null);
        }
    }
}
