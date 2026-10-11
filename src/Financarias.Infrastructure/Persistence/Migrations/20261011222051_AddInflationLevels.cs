using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Financarias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInflationLevels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inflation_levels",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    level = table.Column<decimal>(type: "numeric(38,16)", precision: 38, scale: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inflation_levels", x => x.id);
                    table.CheckConstraint("ck_inflation_levels_level_positive", "level > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_inflation_levels_day",
                table: "inflation_levels",
                column: "day",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "inflation_levels");
        }
    }
}
