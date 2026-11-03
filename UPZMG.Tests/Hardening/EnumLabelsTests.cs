using UPZMG.Shared.Enums;
using Xunit;

namespace UPZMG.Tests.Hardening;

/// <summary>
/// What the capture screens and their listings print for an enum value. The identifiers carry no
/// spaces or accents, and they used to reach the screen exactly like that.
/// </summary>
public class EnumLabelsTests
{
    [Theory]
    [InlineData(MaritalStatus.UnionLibre, "Unión libre")]
    [InlineData(TitulationModality.ExperienciaProfesional, "Experiencia profesional")]
    [InlineData(StudentStatus.BajaTemporal, "Baja temporal")]
    [InlineData(EducationLevel.MediaSuperiorOMenor, "Media superior o menor")]
    [InlineData(EducationLevel.Maestria, "Maestría")]
    [InlineData(Days.Sabado, "Sábado")]
    [InlineData(SchoolFacilityType.ServicioMedico, "Servicio médico")]
    [InlineData(InscriptionType.Reinscripcion, "Reinscripción")]
    [InlineData(Modality.NOEscolarizada, "No escolarizada")]
    [InlineData(EducationServicesList.LicenciaturaTSU, "Licenciatura TSU")]
    [InlineData(GoalType.SNI, "SNI")]
    public void ReadsAsWords(Enum value, string expected) =>
        Assert.Equal(expected, value.Label());

    [Fact]
    public void SexIsSpelledOut()
    {
        Assert.Equal("Mujer", Gender.M.Label());
        Assert.Equal("Hombre", Gender.H.Label());
    }

    [Theory]
    [InlineData("Sábado", Days.Sabado)]
    [InlineData("miercoles", Days.Miercoles)]
    [InlineData("Lunes", Days.Lunes)]
    public void AStoredDayIsReadBackWithOrWithoutItsAccent(string stored, Days expected) =>
        Assert.Equal(expected, DayLabels.Parse(stored));

    [Theory]
    [InlineData("Lunes y miércoles", new[] { Days.Lunes, Days.Miercoles })]
    [InlineData("Martes, Jueves", new[] { Days.Martes, Days.Jueves })]
    [InlineData("Lun/Mié.", new[] { Days.Lunes, Days.Miercoles })]
    [InlineData("Viernes", new[] { Days.Viernes })]
    [InlineData("lo que sea", new Days[0])]
    [InlineData("", new Days[0])]
    public void AStoredCellGivesEveryDayItNames(string stored, Days[] expected) =>
        Assert.Equal(expected, DayLabels.ParseAll(stored));

    [Fact]
    public void NoLabelKeepsAnInnerCapitalUnlessItIsAnAcronym()
    {
        var everyValue = typeof(EnumLabels).Assembly.GetTypes()
            .Where(type => type.IsEnum && type.Namespace == typeof(Days).Namespace && type != typeof(ScheduleTime))
            .SelectMany(type => Enum.GetValues(type).Cast<Enum>());

        foreach (var value in everyValue)
        {
            var words = value.Label().Split(' ').Skip(1);
            Assert.All(words, word => Assert.True(
                word.All(char.IsUpper) || !word.Any(char.IsUpper),
                $"{value.GetType().Name}.{value} reads \"{value.Label()}\""));
        }
    }
}
