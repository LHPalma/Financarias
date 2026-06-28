using Financarias.Domain.Calendar;

namespace Financarias.Domain.UnitTests.Calendar;

public class BusinessDayCalendarTests
{
    // Semana de referência: 08/01/2024 (seg) a 12/01/2024 (sex); 13-14 fim de semana; 15/01 (seg).
    private static readonly IReadOnlySet<DateOnly> NoHolidays = new HashSet<DateOnly>();

    [Fact(DisplayName = "Count conta início exclusivo e fim inclusivo (seg→sex = 4 dias úteis)")]
    public void Count_MondayToFriday_ExcludesStartIncludesEnd()
    {
        // Arrange
        var monday = new DateOnly(2024, 1, 8);
        var friday = new DateOnly(2024, 1, 12);

        // Act
        var count = BusinessDayCalendar.Count(monday, friday, NoHolidays);

        // Assert — ter, qua, qui, sex (segunda excluída por ser o início)
        Assert.Equal(4, count.Value);
    }

    [Fact(DisplayName = "Count pula sábado e domingo")]
    public void Count_AcrossWeekend_SkipsSaturdayAndSunday()
    {
        // Arrange
        var friday = new DateOnly(2024, 1, 12);
        var nextFriday = new DateOnly(2024, 1, 19);

        // Act
        var count = BusinessDayCalendar.Count(friday, nextFriday, NoHolidays);

        // Assert — sáb/dom fora; seg–sex da semana seguinte = 5
        Assert.Equal(5, count.Value);
    }

    [Fact(DisplayName = "Count pula feriado no meio da semana")]
    public void Count_WithHolidayMidWeek_SkipsIt()
    {
        // Arrange
        var monday = new DateOnly(2024, 1, 8);
        var friday = new DateOnly(2024, 1, 12);
        var holidays = new HashSet<DateOnly> { new(2024, 1, 10) }; // quarta

        // Act
        var count = BusinessDayCalendar.Count(monday, friday, holidays);

        // Assert — ter, [qua feriado], qui, sex = 3
        Assert.Equal(3, count.Value);
    }

    public static TheoryData<DateOnly, DateOnly> NonPositiveRanges => new()
    {
        { new DateOnly(2024, 1, 8), new DateOnly(2024, 1, 8) },   // início == fim
        { new DateOnly(2024, 1, 12), new DateOnly(2024, 1, 8) },  // início depois do fim
    };

    [Theory(DisplayName = "Count retorna 0 quando início é igual ou posterior ao fim")]
    [MemberData(nameof(NonPositiveRanges))]
    public void Count_StartAtOrAfterEnd_ReturnsZero(DateOnly start, DateOnly end)
    {
        // Act & Assert
        Assert.Equal(0, BusinessDayCalendar.Count(start, end, NoHolidays).Value);
    }

    [Fact(DisplayName = "AddBusinessDays (T+2) a partir de sexta cai na terça (atravessa o fim de semana)")]
    public void AddBusinessDays_TwoDaysFromFriday_LandsOnTuesday()
    {
        // Arrange
        var friday = new DateOnly(2024, 1, 12);

        // Act
        var settlement = BusinessDayCalendar.AddBusinessDays(friday, 2, NoHolidays);

        // Assert — sáb/dom fora; seg = +1, ter = +2
        Assert.Equal(new DateOnly(2024, 1, 16), settlement);
    }

    [Fact(DisplayName = "AddBusinessDays (T+2) pula feriado na contagem")]
    public void AddBusinessDays_WithHoliday_SkipsIt()
    {
        // Arrange
        var friday = new DateOnly(2024, 1, 12);
        var holidays = new HashSet<DateOnly> { new(2024, 1, 15) }; // segunda feriado

        // Act
        var settlement = BusinessDayCalendar.AddBusinessDays(friday, 2, holidays);

        // Assert — seg feriado; ter = +1, qua = +2
        Assert.Equal(new DateOnly(2024, 1, 17), settlement);
    }
}
