using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financarias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // As linhas existentes sao lixo de teste, e a trava de boot garante que nunca houve banco de producao
            // (RN-02 do SRS 0006). Sem este DELETE, os defaults que o EF gera deixariam usuarios com hash vazio
            // e pepper versao 0: a migration passaria e o erro so apareceria no primeiro login.
            migrationBuilder.Sql("DELETE FROM users;");

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "password_pepper_version",
                table: "users",
                type: "integer",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverter remove as colunas, mas nao traz de volta as linhas apagadas no Up.
            migrationBuilder.DropColumn(
                name: "password_hash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "password_pepper_version",
                table: "users");
        }
    }
}
