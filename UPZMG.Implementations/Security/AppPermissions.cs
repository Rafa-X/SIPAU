namespace UPZMG.Implementations.Security;

/// <summary>
/// What the system can be asked to do, named once. Authorisation is checked against these
/// names and not against role names, so an administrator can change who does what without a
/// developer — which is the point of having an administration screen for permissions at all.
/// </summary>
public static class AppPermissions
{
    // Capture, one per area that owns its data.
    public const string CaptureStudents = "alumnos.capturar";
    public const string CaptureStaff = "personal.capturar";
    public const string CaptureClassGroups = "grupos.capturar";
    public const string CaptureScholarships = "becas.capturar";
    public const string CaptureTitulation = "titulacion.capturar";
    public const string CaptureFacilities = "infraestructura.capturar";
    public const string ReadFacilities = "infraestructura.consultar";
    public const string CaptureOutreach = "vinculacion.capturar";
    public const string CaptureFinance = "finanzas.capturar";

    // Official formats and the board report.
    public const string CaptureReports = "formatos.capturar";
    public const string PublishReports = "formatos.publicar";
    public const string ReadBoardReport = "informe.consultar";
    public const string WriteBoardReport = "informe.redactar";

    // Figures and the analysis layer.
    public const string ReadIndicators = "indicadores.consultar";
    public const string ReadRisk = "riesgo.consultar";
    public const string TrainRisk = "riesgo.reentrenar";

    // Administration.
    public const string ManageUsers = "usuarios.administrar";
    public const string ManagePeriods = "periodos.administrar";

    /// <summary>The catalogue as the administration screen shows it.</summary>
    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(CaptureStudents, "Capturar alumnos", "Registrar, consultar y corregir alumnos."),
        new(CaptureStaff, "Capturar personal", "Registrar, consultar y corregir docentes y empleados."),
        new(CaptureClassGroups, "Capturar grupos de clase", "Registrar, consultar y corregir grupos y sus horarios."),
        new(CaptureScholarships, "Capturar becas", "Asignar becas y consultar el padrón."),
        new(CaptureTitulation, "Capturar titulación", "Registrar procesos de titulación de egresados."),
        new(CaptureFacilities, "Capturar infraestructura", "Dar de alta espacios físicos."),
        new(ReadFacilities, "Consultar infraestructura", "Ver los espacios registrados y su uso."),
        new(CaptureOutreach, "Capturar vinculación", "Registrar convenios y proyectos de vinculación."),
        new(CaptureFinance, "Capturar finanzas", "Registrar el resumen financiero anual."),
        new(CaptureReports, "Capturar formatos oficiales", "Llenar y enviar los formatos 911 y SII."),
        new(PublishReports, "Publicar formatos", "Crear, editar y retirar plantillas de formatos."),
        new(ReadBoardReport, "Consultar el informe de Junta", "Ver el informe consolidado de un ciclo."),
        new(WriteBoardReport, "Redactar el informe de Junta", "Escribir los bloques narrativos y enviar el informe."),
        new(ReadIndicators, "Consultar indicadores", "Ver el tablero, las gráficas y las series institucionales."),
        new(ReadRisk, "Consultar riesgo de deserción", "Ver el modelo y los alumnos señalados."),
        new(TrainRisk, "Reentrenar el modelo de riesgo", "Forzar un nuevo entrenamiento."),
        new(ManageUsers, "Administrar usuarios y roles", "Altas, bajas, roles y permisos del sistema."),
        new(ManagePeriods, "Administrar periodos", "Crear periodos y definir cuál está activo."),
    ];

    /// <summary>
    /// What each role can do out of the box. It reproduces exactly the access the system had
    /// when authorisation was hard-coded by role, so enabling this model changes nothing until
    /// somebody decides to change it.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> DefaultsByRole =
        new Dictionary<string, string[]>
        {
            ["Admin"] = [.. All.Select(p => p.Name)],

            ["Control Escolar"] =
            [
                CaptureStudents, CaptureTitulation, CaptureReports,
                ReadBoardReport, ReadIndicators, ReadRisk,
            ],

            ["Servicios Estudiantiles"] =
            [
                CaptureStudents, CaptureScholarships, CaptureOutreach, CaptureReports,
                ReadBoardReport, ReadIndicators, ReadRisk,
            ],

            ["Recursos Humanos"] =
            [
                CaptureStaff, CaptureReports, ReadBoardReport, ReadIndicators,
            ],

            ["Direccion Academica"] =
            [
                CaptureClassGroups, ReadFacilities, CaptureOutreach, CaptureReports,
                ReadBoardReport, ReadIndicators, ReadRisk,
            ],

            ["Recursos Materiales y Servicios Generales"] =
            [
                CaptureFacilities, ReadFacilities, CaptureReports, ReadBoardReport, ReadIndicators,
            ],
        };

    /// <summary>Without this one nobody could ever grant permissions again.</summary>
    public const string Irrevocable = ManageUsers;

    // Screens that serve more than one task accept any of a few permissions. A policy name is
    // an attribute argument, so the combination has to be a constant; "|" means "any of these".
    /// <summary>The configuration hub: it leads to periods, templates and users.</summary>
    public const string AnyAdministration = ManageUsers + "|" + ManagePeriods + "|" + PublishReports;

    /// <summary>Bulk import writes student or staff records, depending on the chosen file.</summary>
    public const string CaptureStudentsOrStaff = CaptureStudents + "|" + CaptureStaff;
}

/// <param name="Name">Stored name; what the authorisation check compares.</param>
/// <param name="DisplayName">How the administration screen lists it.</param>
public sealed record PermissionDefinition(string Name, string DisplayName, string Description);
