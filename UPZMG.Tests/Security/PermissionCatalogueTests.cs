using UPZMG.Implementations.Security;
using Xunit;

namespace UPZMG.Tests.Security;

/// <summary>
/// The catalogue is what the seed and the authorisation checks both read. These tests keep the
/// two honest: a permission that is granted but not defined, or a role left unable to work,
/// would only show up as a 403 in somebody's face.
/// </summary>
public class PermissionCatalogueTests
{
    [Fact]
    public void EveryPermissionIsDefinedOnce()
    {
        var names = AppPermissions.All.Select(p => p.Name).ToList();

        Assert.Equal(names.Count, names.Distinct().Count());
        Assert.All(AppPermissions.All, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.DisplayName));
            Assert.False(string.IsNullOrWhiteSpace(p.Description));
        });
    }

    [Fact]
    public void EveryDefaultGrantsSomethingThatExists()
    {
        var defined = AppPermissions.All.Select(p => p.Name).ToHashSet();

        foreach (var (role, granted) in AppPermissions.DefaultsByRole)
        {
            Assert.NotEmpty(granted);
            Assert.All(granted, name =>
                Assert.True(defined.Contains(name), $"El rol {role} recibe «{name}», que no existe en el catálogo."));
        }
    }

    [Fact]
    public void AdminCanDoEverything()
    {
        var admin = AppPermissions.DefaultsByRole["Admin"].ToHashSet();

        Assert.All(AppPermissions.All, p => Assert.Contains(p.Name, admin));
    }

    [Fact]
    public void OnlyAdminAdministersUsers()
    {
        foreach (var (role, granted) in AppPermissions.DefaultsByRole)
        {
            if (role == "Admin") continue;
            Assert.DoesNotContain(AppPermissions.ManageUsers, granted);
        }
    }

    [Fact]
    public void EveryAreaCanCaptureItsOfficialFormats()
    {
        // Filling the 911 and SII formats is what every area was given the system for.
        foreach (var (role, granted) in AppPermissions.DefaultsByRole)
        {
            Assert.True(granted.Contains(AppPermissions.CaptureReports),
                $"El área {role} no podría capturar sus formatos oficiales.");
            Assert.True(granted.Contains(AppPermissions.ReadIndicators),
                $"El área {role} no podría ver los indicadores.");
        }
    }

    [Fact]
    public void CaptureOfStudentDataStaysWithTheAreasThatServeThem()
    {
        // Personal academic data is not open to every area: Recursos Humanos and Recursos
        // Materiales have no business reading a student file.
        Assert.DoesNotContain(AppPermissions.CaptureStudents, AppPermissions.DefaultsByRole["Recursos Humanos"]);
        Assert.DoesNotContain(AppPermissions.ReadRisk, AppPermissions.DefaultsByRole["Recursos Humanos"]);
        Assert.DoesNotContain(AppPermissions.CaptureStudents,
            AppPermissions.DefaultsByRole["Recursos Materiales y Servicios Generales"]);
    }

    [Fact]
    public void TheIrrevocablePermissionIsPartOfTheCatalogue() =>
        Assert.Contains(AppPermissions.Irrevocable, AppPermissions.All.Select(p => p.Name));
}
