using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasko.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SeedInsurancePackages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "InsurancePackages",
                columns: new[]
                {
                    "Id",
                    "Code",
                    "Name",
                    "Description",
                    "Factor",
                    "IsActive",
                    "CreatedDate",
                    "UpdatedDate",
                    "IsDeleted",
                    "DeletedDate",
                    "CreatedBy",
                    "UpdatedBy",
                    "DeletedBy"
                },
                values: new object[,]
                {
                    {
                        new Guid("11111111-1111-1111-1111-111111111111"),
                        "EKONOMIK",
                        "Ekonomik Paket",
                        "Temel kasko paketi",
                        1.0000m,
                        true,
                        new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        false,
                        null,
                        null,
                        null,
                        null
                    },
                    {
                        new Guid("22222222-2222-2222-2222-222222222222"),
                        "STANDART",
                        "Standart Paket",
                        "Genişletilmiş kasko paketi",
                        1.0000m,
                        true,
                        new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        false,
                        null,
                        null,
                        null,
                        null
                    },
                    {
                        new Guid("33333333-3333-3333-3333-333333333333"),
                        "KAPSAMLI",
                        "Kapsamlı Paket",
                        "Geniş kapsamlı kasko paketi",
                        1.0000m,
                        true,
                        new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc),
                        null,
                        false,
                        null,
                        null,
                        null,
                        null
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "InsurancePackages",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "InsurancePackages",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "InsurancePackages",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));
        }
    }
}