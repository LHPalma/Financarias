using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Financarias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_quotes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ticker = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    trading_date = table.Column<DateOnly>(type: "date", nullable: false),
                    market_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    bdi_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    company_name = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    specification = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    isin = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    open_price = table.Column<decimal>(type: "numeric(13,2)", precision: 13, scale: 2, nullable: false),
                    high_price = table.Column<decimal>(type: "numeric(13,2)", precision: 13, scale: 2, nullable: false),
                    low_price = table.Column<decimal>(type: "numeric(13,2)", precision: 13, scale: 2, nullable: false),
                    average_price = table.Column<decimal>(type: "numeric(13,2)", precision: 13, scale: 2, nullable: false),
                    close_price = table.Column<decimal>(type: "numeric(13,2)", precision: 13, scale: 2, nullable: false),
                    trades = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<long>(type: "bigint", nullable: false),
                    volume = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    quote_factor = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_quotes", x => x.id);
                    table.CheckConstraint("ck_daily_quotes_counts_positive", "trades > 0 AND quantity > 0 AND quote_factor > 0");
                    table.CheckConstraint("ck_daily_quotes_high_not_below_low", "high_price >= low_price");
                    table.CheckConstraint("ck_daily_quotes_prices_positive", "open_price > 0 AND high_price > 0 AND low_price > 0 AND average_price > 0 AND close_price > 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_daily_quotes_ticker_trading_date_market_type",
                table: "daily_quotes",
                columns: new[] { "ticker", "trading_date", "market_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_quotes");
        }
    }
}
