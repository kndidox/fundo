using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fundo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedBuiltInRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "rule_definitions",
                columns: new[] { "Id", "DenialReason", "Field", "IsActive", "Operator", "Priority", "Value" },
                values: new object[,]
                {
                    { new Guid("9f6a6e6e-9b1a-4b6e-8f3a-000000000001"), "StateNotAllowed", "State", true, "Equals", 0, "NY" },
                    { new Guid("9f6a6e6e-9b1a-4b6e-8f3a-000000000002"), "SsnBlacklisted", "Ssn", true, "In", 0, "123456789,987654321" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "rule_definitions",
                keyColumn: "Id",
                keyValue: new Guid("9f6a6e6e-9b1a-4b6e-8f3a-000000000001"));

            migrationBuilder.DeleteData(
                table: "rule_definitions",
                keyColumn: "Id",
                keyValue: new Guid("9f6a6e6e-9b1a-4b6e-8f3a-000000000002"));
        }
    }
}
