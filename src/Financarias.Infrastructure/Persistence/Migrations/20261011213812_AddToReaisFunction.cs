using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financarias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddToReaisFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                CREATE FUNCTION to_reais(amount numeric, day date) RETURNS numeric
                LANGUAGE plpgsql IMMUTABLE AS $$
                DECLARE
                    divisor numeric;
                BEGIN
                    divisor := CASE
                        WHEN day >= DATE '1994-07-01' THEN 1::numeric
                        WHEN day >= DATE '1993-08-01' THEN 2750::numeric
                        WHEN day >= DATE '1989-01-16' THEN 2750000::numeric
                        WHEN day >= DATE '1986-02-28' THEN 2750000000::numeric
                        WHEN day >= DATE '1970-05-15' THEN 2750000000000::numeric
                    END;

                    IF divisor IS NULL THEN
                        RAISE EXCEPTION 'No currency conversion for %', day USING ERRCODE = '22008';
                    END IF;

                    RETURN amount / divisor;
                END
                $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION to_reais(numeric, date);");
        }
    }
}
