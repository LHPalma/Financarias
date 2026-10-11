using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Financarias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIssuers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "issuers",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    issuer_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    cvm_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    trading_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    sector = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    subsector = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    segment = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    listing_market = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    activity = table.Column<string>(type: "text", nullable: true),
                    website = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_issuers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_issuers_issuer_code",
                table: "issuers",
                column: "issuer_code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "issuers");
        }
    }
}
