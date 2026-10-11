using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Financarias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "index_quotes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    market_index = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    trading_date = table.Column<DateOnly>(type: "date", nullable: false),
                    close_value = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_index_quotes", x => x.id);
                    table.CheckConstraint("ck_index_quotes_close_value_positive", "close_value > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_index_quotes_market_index_trading_date",
                table: "index_quotes",
                columns: new[] { "market_index", "trading_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "index_quotes");
        }
    }
}
