using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Financarias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchInterestPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "search_interest_points",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    geo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    comparison_key = table.Column<string>(type: "character varying(520)", maxLength: 520, nullable: false),
                    range_start = table.Column<DateOnly>(type: "date", nullable: false),
                    range_end = table.Column<DateOnly>(type: "date", nullable: false),
                    term = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_search_interest_points", x => x.id);
                    table.CheckConstraint("ck_search_interest_points_day_in_range", "day BETWEEN range_start AND range_end");
                    table.CheckConstraint("ck_search_interest_points_value_range", "value BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateIndex(
                name: "ix_search_interest_points_geo_comparison_key_range_start_range",
                table: "search_interest_points",
                columns: new[] { "geo", "comparison_key", "range_start", "range_end", "term", "day" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "search_interest_points");
        }
    }
}
