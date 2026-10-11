using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Financarias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RoundToPresentValueResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                CREATE OR REPLACE FUNCTION to_present_value(p_amount numeric, p_day date, p_base_day date DEFAULT NULL) RETURNS numeric
                LANGUAGE plpgsql STABLE AS $$
                DECLARE
                    from_level numeric;
                    base_level numeric;
                BEGIN
                    SELECT l.level INTO from_level FROM inflation_levels l WHERE l.day = p_day;

                    IF p_base_day IS NULL THEN
                        SELECT l.level INTO base_level FROM inflation_levels l ORDER BY l.day DESC LIMIT 1;
                    ELSE
                        SELECT l.level INTO base_level FROM inflation_levels l WHERE l.day = p_base_day;
                    END IF;

                    IF from_level IS NULL OR base_level IS NULL THEN
                        RAISE EXCEPTION 'No inflation level for % (base %)', p_day, p_base_day USING ERRCODE = '22008';
                    END IF;

                    RETURN round(to_reais(p_amount, p_day) * base_level / from_level, 8);
                END
                $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"
                CREATE OR REPLACE FUNCTION to_present_value(p_amount numeric, p_day date, p_base_day date DEFAULT NULL) RETURNS numeric
                LANGUAGE plpgsql STABLE AS $$
                DECLARE
                    from_level numeric;
                    base_level numeric;
                BEGIN
                    SELECT l.level INTO from_level FROM inflation_levels l WHERE l.day = p_day;

                    IF p_base_day IS NULL THEN
                        SELECT l.level INTO base_level FROM inflation_levels l ORDER BY l.day DESC LIMIT 1;
                    ELSE
                        SELECT l.level INTO base_level FROM inflation_levels l WHERE l.day = p_base_day;
                    END IF;

                    IF from_level IS NULL OR base_level IS NULL THEN
                        RAISE EXCEPTION 'No inflation level for % (base %)', p_day, p_base_day USING ERRCODE = '22008';
                    END IF;

                    RETURN to_reais(p_amount, p_day) * base_level / from_level;
                END
                $$;");
        }
    }
}
