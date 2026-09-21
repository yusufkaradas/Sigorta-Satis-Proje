using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kasko.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SeedPackageCoverages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var ekonomikPackageId =
                new Guid("11111111-1111-1111-1111-111111111111");

            var standartPackageId =
                new Guid("22222222-2222-2222-2222-222222222222");

            var kapsamliPackageId =
                new Guid("33333333-3333-3333-3333-333333333333");

            var camKirilmasiCoverageId =
                new Guid("DD1B1CC2-8B4F-43AA-8ED6-81BFE49200DF");

            var hirsizlikCoverageId =
                new Guid("A0DD1498-C060-4547-8FA4-9D5CC54C9BF0");

            var hirsizlikTeminatiCoverageId =
                new Guid("7733CFDE-16A2-4329-B613-52A2F5CC8F1B");

            migrationBuilder.InsertData(
                table: "PackageCoverages",
                columns: new[]
                {
                    "Id",
                    "InsurancePackageId",
                    "CoverageId",
                    "IsDefault",
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
                        new Guid("41111111-1111-1111-1111-111111111111"),
                        ekonomikPackageId,
                        camKirilmasiCoverageId,
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
                        new Guid("41111111-1111-1111-1111-111111111112"),
                        ekonomikPackageId,
                        hirsizlikCoverageId,
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
                        new Guid("42222222-2222-2222-2222-222222222221"),
                        standartPackageId,
                        camKirilmasiCoverageId,
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
                        new Guid("42222222-2222-2222-2222-222222222222"),
                        standartPackageId,
                        hirsizlikCoverageId,
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
                        new Guid("42222222-2222-2222-2222-222222222223"),
                        standartPackageId,
                        hirsizlikTeminatiCoverageId,
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
                        new Guid("43333333-3333-3333-3333-333333333331"),
                        kapsamliPackageId,
                        camKirilmasiCoverageId,
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
                        new Guid("43333333-3333-3333-3333-333333333332"),
                        kapsamliPackageId,
                        hirsizlikCoverageId,
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
                        new Guid("43333333-3333-3333-3333-333333333333"),
                        kapsamliPackageId,
                        hirsizlikTeminatiCoverageId,
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
                table: "PackageCoverages",
                keyColumn: "Id",
                keyValue:
                    new Guid("41111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "PackageCoverages",
                keyColumn: "Id",
                keyValue:
                    new Guid("41111111-1111-1111-1111-111111111112"));

            migrationBuilder.DeleteData(
                table: "PackageCoverages",
                keyColumn: "Id",
                keyValue:
                    new Guid("42222222-2222-2222-2222-222222222221"));

            migrationBuilder.DeleteData(
                table: "PackageCoverages",
                keyColumn: "Id",
                keyValue:
                    new Guid("42222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "PackageCoverages",
                keyColumn: "Id",
                keyValue:
                    new Guid("42222222-2222-2222-2222-222222222223"));

            migrationBuilder.DeleteData(
                table: "PackageCoverages",
                keyColumn: "Id",
                keyValue:
                    new Guid("43333333-3333-3333-3333-333333333331"));

            migrationBuilder.DeleteData(
                table: "PackageCoverages",
                keyColumn: "Id",
                keyValue:
                    new Guid("43333333-3333-3333-3333-333333333332"));

            migrationBuilder.DeleteData(
                table: "PackageCoverages",
                keyColumn: "Id",
                keyValue:
                    new Guid("43333333-3333-3333-3333-333333333333"));
        }
    }
}