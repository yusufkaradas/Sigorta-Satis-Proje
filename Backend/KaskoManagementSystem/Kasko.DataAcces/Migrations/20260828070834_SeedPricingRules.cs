using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Kasko.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SeedPricingRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "PricingRules",
                columns: new[] { "Id", "Code", "CreatedBy", "CreatedDate", "DeletedBy", "DeletedDate", "Description", "IsActive", "IsDeleted", "Name", "UpdatedBy", "UpdatedDate", "Value" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "BASE_KASKO_RATE", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Araç değerine uygulanacak temel kasko oranı.", true, false, "Temel Kasko Oranı", null, null, 0.0200m },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "AGE_0_2", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "0-2 Yaş Katsayısı", null, null, 1.0000m },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "AGE_3_5", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "3-5 Yaş Katsayısı", null, null, 1.1000m },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "AGE_6_8", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "6-8 Yaş Katsayısı", null, null, 1.2000m },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "AGE_9_12", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "9-12 Yaş Katsayısı", null, null, 1.3500m },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "AGE_13_15", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "13-15 Yaş Katsayısı", null, null, 1.5000m },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "USAGE_PRIVATE", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "Özel Kullanım Katsayısı", null, null, 1.0000m },
                    { new Guid("10000000-0000-0000-0000-000000000008"), "USAGE_COMMERCIAL", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "Ticari Kullanım Katsayısı", null, null, 1.2500m },
                    { new Guid("10000000-0000-0000-0000-000000000009"), "USAGE_RENTAL", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "Kiralık Kullanım Katsayısı", null, null, 1.4000m },
                    { new Guid("10000000-0000-0000-0000-000000000010"), "DRIVER_25_PLUS", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "25+ Yaş Sürücü Katsayısı", null, null, 1.0000m },
                    { new Guid("10000000-0000-0000-0000-000000000011"), "DRIVER_21_24", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "21-24 Yaş Sürücü Katsayısı", null, null, 1.1500m },
                    { new Guid("10000000-0000-0000-0000-000000000012"), "DRIVER_18_20", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "18-20 Yaş Sürücü Katsayısı", null, null, 1.3000m },
                    { new Guid("10000000-0000-0000-0000-000000000013"), "CLAIMS_0", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "Hasarsızlık Katsayısı", null, null, 0.9000m },
                    { new Guid("10000000-0000-0000-0000-000000000014"), "CLAIMS_1", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "1 Hasar Katsayısı", null, null, 1.0000m },
                    { new Guid("10000000-0000-0000-0000-000000000015"), "CLAIMS_2", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "2 Hasar Katsayısı", null, null, 1.1500m },
                    { new Guid("10000000-0000-0000-0000-000000000016"), "CLAIMS_3_PLUS", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "3+ Hasar Katsayısı", null, null, 1.3000m },
                    { new Guid("10000000-0000-0000-0000-000000000017"), "REGION_LOW", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "Düşük Bölge Riski", null, null, 0.9500m },
                    { new Guid("10000000-0000-0000-0000-000000000018"), "REGION_NORMAL", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "Normal Bölge Riski", null, null, 1.0000m },
                    { new Guid("10000000-0000-0000-0000-000000000019"), "REGION_HIGH", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, null, true, false, "Yüksek Bölge Riski", null, null, 1.1000m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                table: "PricingRules",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000019"));
        }
    }
}
