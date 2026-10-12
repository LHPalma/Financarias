using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Financarias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexCompositionItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "index_composition_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    index_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    quadrimester = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ticker = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    asset_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    share_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    theoretical_quantity = table.Column<long>(type: "bigint", nullable: false),
                    weight_percent = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    base_date = table.Column<DateOnly>(type: "date", nullable: true),
                    divisor = table.Column<decimal>(type: "numeric(24,8)", precision: 24, scale: 8, nullable: true),
                    source_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_index_composition_items", x => x.id);
                    table.CheckConstraint("ck_index_composition_items_quadrimester", "quadrimester BETWEEN 1 AND 3");
                    table.CheckConstraint("ck_index_composition_items_quantity_positive", "theoretical_quantity > 0");
                    table.CheckConstraint("ck_index_composition_items_weight_range", "weight_percent BETWEEN 0 AND 100");
                });

            migrationBuilder.CreateIndex(
                name: "ix_index_composition_items_index_code_year_quadrimester_kind_t",
                table: "index_composition_items",
                columns: new[] { "index_code", "year", "quadrimester", "kind", "ticker" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "index_composition_items");
        }
    }
}
