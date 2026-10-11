using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financarias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyTickerToDailyQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "legacy_ticker",
                table: "daily_quotes",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_daily_quotes_legacy_ticker",
                table: "daily_quotes",
                column: "legacy_ticker",
                filter: "legacy_ticker IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_daily_quotes_legacy_ticker",
                table: "daily_quotes");

            migrationBuilder.DropColumn(
                name: "legacy_ticker",
                table: "daily_quotes");
        }
    }
}
