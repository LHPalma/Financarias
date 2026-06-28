using Financarias.Domain.Holidays;
using Financarias.Domain.Holidays.Exceptions;

namespace Financarias.Domain.UnitTests.Holidays;

public class HolidayTests
{
    [Fact(DisplayName = "Create monta o feriado com data, nome e país")]
    public void Create_WithValidData_SetsProperties()
    {
        // Arrange
        var date = new DateOnly(2024, 1, 1);

        // Act
        var holiday = Holiday.Create(date, "Confraternização Universal", CountryCode.BR);

        // Assert
        Assert.Equal(date, holiday.Date);
        Assert.Equal("Confraternização Universal", holiday.Name);
        Assert.Equal(CountryCode.BR, holiday.CountryCode);
    }

    [Theory(DisplayName = "Create lança InvalidHolidayNameException quando o nome é vazio ou em branco")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsInvalidHolidayNameException(string? name)
    {
        // Act & Assert
        Assert.Throws<InvalidHolidayNameException>(
            () => Holiday.Create(new DateOnly(2024, 1, 1), name!, CountryCode.BR));
    }
}
