using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financarias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixEmailHostSplitOnLastAtSign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "email_host",
                table: "users",
                type: "text",
                nullable: true,
                computedColumnSql: "split_part(email, '@', -1)",
                stored: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComputedColumnSql: "split_part(email, '@', 2)",
                oldStored: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "email_host",
                table: "users",
                type: "text",
                nullable: true,
                computedColumnSql: "split_part(email, '@', 2)",
                stored: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComputedColumnSql: "split_part(email, '@', -1)",
                oldStored: true);
        }
    }
}
