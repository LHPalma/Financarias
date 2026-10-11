using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Financarias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorporateEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "corporate_events",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    issuer_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    isin = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    share_class = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    cum_date = table.Column<DateOnly>(type: "date", nullable: false),
                    approval_date = table.Column<DateOnly>(type: "date", nullable: true),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: true),
                    amount = table.Column<decimal>(type: "numeric(24,10)", precision: 24, scale: 10, nullable: true),
                    per_shares = table.Column<int>(type: "integer", nullable: true),
                    factor = table.Column<decimal>(type: "numeric(24,10)", precision: 24, scale: 10, nullable: true),
                    label = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corporate_events", x => x.id);
                    table.CheckConstraint("ck_corporate_events_fields_by_kind", "(kind IN ('CashDividend', 'InterestOnEquity', 'Yield') AND amount IS NOT NULL AND amount >= 0 AND per_shares IS NOT NULL AND per_shares > 0 AND factor IS NULL) OR (kind IN ('StockSplit', 'ReverseSplit', 'BonusShares') AND factor IS NOT NULL AND factor > 0 AND amount IS NULL AND per_shares IS NULL)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_corporate_events_issuer_code_cum_date",
                table: "corporate_events",
                columns: new[] { "issuer_code", "cum_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "corporate_events");
        }
    }
}
