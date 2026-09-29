-- =========================================
-- Infrastructure, outreach and finance (development data)
-- =========================================
-- The three areas that used to have nowhere to save now own their tables. This seed fills them
-- with a small, coherent set so their screens and the board report have something to show:
--   * school spaces, including the ones class groups are already scheduled into,
--   * linkage projects across the last periods, some tied to real students,
--   * two fiscal years of the annual financial summary.
--
-- Requires: migrations (20260928025109_AreaStorage), test-data.sql and seed-academic-history.sql.
-- Run:
--   docker exec -i upzmg_db sh -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U dev_user -P "$MSSQL_DEV_PASSWORD" -C -d upzmg -b -I' < seed-area-data.sql
--
-- Idempotent: it removes its own rows first, and anything left behind by automated checks.

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
BEGIN TRANSACTION;

DECLARE @Now DATETIME2 = GETUTCDATE();
DECLARE @Admin UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM systemUser WHERE Email = N'admin@upzmg.local');
IF @Admin IS NULL THROW 51000, 'Missing the test users. Run test-data.sql first.', 1;

-- Its own rows, plus whatever the automated checks may have left.
DELETE FROM outreachProjects WHERE Id LIKE 'dddddddd-%' OR OrganizationName LIKE N'Empresa de prueba%';
DELETE FROM annualFinances   WHERE Id LIKE 'dddddddd-%' OR FiscalYear >= 2090;
DELETE FROM SchoolFacility   WHERE Id LIKE 'dddddddd-%' OR Name LIKE N'Laboratorio de prueba%';

-- ── School spaces ───────────────────────────────────────────────────────────
-- Type follows SchoolFacilityType: 0 Direccion, 3 Biblioteca, 8 CafeteriaComedor,
-- 9 TallerLaboratorio, 11 Aula, 12 Auditorio.
INSERT INTO SchoolFacility (Id, Name, Type, Capacity, InUse, HasInternet, HasComputers,
                            SupportDisabilities, Problems, SquareMeters, ComputersInOperation)
VALUES
    ('dddddddd-0000-0000-0000-000000000001', N'Aula A-101',                11, 40, 1, 1, 0, 1, N'[]', 54.0,  0),
    ('dddddddd-0000-0000-0000-000000000002', N'Aula A-102',                11, 40, 1, 1, 0, 1, N'["Proyector con lámpara agotada"]', 54.0, 0),
    ('dddddddd-0000-0000-0000-000000000003', N'Aula B-201',                11, 35, 1, 1, 0, 0, N'[]', 48.0,  0),
    ('dddddddd-0000-0000-0000-000000000004', N'Laboratorio de Cómputo 1',   9, 30, 1, 1, 1, 1, N'[]', 72.5, 30),
    ('dddddddd-0000-0000-0000-000000000005', N'Laboratorio de Cómputo 2',   9, 30, 1, 1, 1, 1, N'["Aire acondicionado intermitente"]', 72.5, 24),
    ('dddddddd-0000-0000-0000-000000000006', N'Laboratorio de Biotecnología', 9, 24, 1, 1, 1, 1, N'[]', 96.0, 6),
    ('dddddddd-0000-0000-0000-000000000007', N'Taller de Automatización',   9, 24, 1, 1, 1, 0, N'["Falta señalización de protección civil"]', 120.0, 8),
    ('dddddddd-0000-0000-0000-000000000008', N'Biblioteca',                 3, 80, 1, 1, 1, 1, N'[]', 210.0, 12),
    ('dddddddd-0000-0000-0000-000000000009', N'Auditorio',                 12, 250, 1, 1, 0, 1, N'[]', 380.0, 2),
    ('dddddddd-0000-0000-0000-00000000000A', N'Cafetería',                  8, 120, 1, 1, 0, 1, N'[]', 160.0, 0);

-- ── Linkage projects ────────────────────────────────────────────────────────
-- Spread over the last three periods, with a few tied to students that really exist.
DECLARE @Periods TABLE (RowNum INT, Id UNIQUEIDENTIFIER, Label NVARCHAR(100));
INSERT INTO @Periods
SELECT TOP 3 ROW_NUMBER() OVER (ORDER BY StartDate DESC), Id, SchoolCycle + N' ' + SEP_Period
FROM periods
WHERE EXISTS (SELECT 1 FROM courseEnrollment e WHERE e.PeriodId = periods.Id)
ORDER BY StartDate DESC;

DECLARE @P1 UNIQUEIDENTIFIER = (SELECT Id FROM @Periods WHERE RowNum = 1);
DECLARE @P2 UNIQUEIDENTIFIER = (SELECT Id FROM @Periods WHERE RowNum = 2);
DECLARE @P3 UNIQUEIDENTIFIER = (SELECT Id FROM @Periods WHERE RowNum = 3);
IF @P1 IS NULL THROW 51000, 'No periods with enrolments. Run seed-academic-history.sql first.', 1;

-- Four active students to tie a project to, whichever the history produced.
DECLARE @Students TABLE (RowNum INT, Id UNIQUEIDENTIFIER, EnrollmentNumber NVARCHAR(20));
INSERT INTO @Students
SELECT TOP 4 ROW_NUMBER() OVER (ORDER BY EnrollmentNumber), Id, EnrollmentNumber
FROM students
WHERE Status = 0
ORDER BY EnrollmentNumber;

INSERT INTO outreachProjects (Id, PeriodId, OrganizationName, Sector, CompanySize, ActivityType,
                              ProjectName, StudentId, StudentEnrollmentNumber, StartDate, EndDate,
                              AccreditedHours, CreatedBy, DateCreated)
SELECT
    p.Id, p.PeriodId, p.OrganizationName, p.Sector, p.CompanySize, p.ActivityType, p.ProjectName,
    s.Id, s.EnrollmentNumber, p.StartDate, p.EndDate, p.AccreditedHours, @Admin, DATEADD(DAY, -p.AgeDays, @Now)
FROM (VALUES
    ('dddddddd-0000-0000-0000-000000000101', @P1, N'Grupo Industrial Tlajomulco', N'Privado', N'Grande',   N'Estadía',                  N'Automatización de línea de empaque',        1, '2026-01-12', '2026-04-10', 480, 20),
    ('dddddddd-0000-0000-0000-000000000102', @P1, N'Ayuntamiento de Tlajomulco',  N'Público', N'Grande',   N'Servicio social',          N'Digitalización del archivo municipal',      2, '2026-01-20', '2026-04-20', 480, 18),
    ('dddddddd-0000-0000-0000-000000000103', @P1, N'Biotecnología del Valle',     N'Privado', N'Mediana',  N'Prácticas profesionales',  N'Control de calidad en fermentación',        3, '2026-02-02', '2026-05-02', 360, 15),
    ('dddddddd-0000-0000-0000-000000000104', @P2, N'Cruz Roja Mexicana',          N'Social',  N'Mediana',  N'Servicio social',          N'Soporte técnico a sistemas de atención',    4, '2025-09-08', '2025-12-12', 480, 120),
    ('dddddddd-0000-0000-0000-000000000105', @P2, N'Aceros del Occidente',        N'Privado', N'Grande',   N'Proyecto de vinculación',  N'Mantenimiento predictivo con sensores',     0, '2025-09-15', '2025-12-15', 240, 115),
    ('dddddddd-0000-0000-0000-000000000106', @P3, N'Cooperativa Agrícola Zapotlán', N'Social', N'Pequeña', N'Proyecto de vinculación',  N'Sistema de riego automatizado',             0, '2025-05-12', '2025-08-08', 300, 240),
    ('dddddddd-0000-0000-0000-000000000107', @P3, N'Consultoría MIPYME Jalisco',  N'Privado', N'Micro',    N'Prácticas profesionales',  N'Tablero de indicadores para MIPYMES',       0, '2025-05-20', '2025-08-15', 240, 235)
) AS p (Id, PeriodId, OrganizationName, Sector, CompanySize, ActivityType, ProjectName, StudentRow, StartDate, EndDate, AccreditedHours, AgeDays)
LEFT JOIN @Students s ON s.RowNum = p.StudentRow;

-- ── Annual financial summary ────────────────────────────────────────────────
-- Two closed years, the second slightly larger, both with a small surplus.
INSERT INTO annualFinances (Id, FiscalYear, FederalIncome, StateIncome, OwnIncomeEnrollment, OwnIncomeServices,
                            PayrollExpense, MaterialsExpense, ServicesExpense, InvestmentExpense,
                            Notes, CreatedBy, DateCreated, LastModified)
VALUES
    ('dddddddd-0000-0000-0000-000000000201', 2024, 28500000.00, 12400000.00, 3250000.00, 780000.00,
     31200000.00,  4100000.00, 5600000.00, 3200000.00,
     N'Ejercicio cerrado. La inversión corresponde al equipamiento del Laboratorio de Cómputo 2.',
     @Admin, DATEADD(DAY, -400, @Now), DATEADD(DAY, -400, @Now)),
    ('dddddddd-0000-0000-0000-000000000202', 2025, 30100000.00, 13250000.00, 3480000.00, 910000.00,
     33050000.00,  4350000.00, 5900000.00, 2950000.00,
     N'Ejercicio cerrado. Incremento salarial del 4 % aplicado en mayo.',
     @Admin, DATEADD(DAY, -40, @Now), DATEADD(DAY, -40, @Now));

COMMIT TRANSACTION;

PRINT '=== Area data loaded ===';
SELECT (SELECT COUNT(*) FROM SchoolFacility)   AS espacios,
       (SELECT COUNT(*) FROM outreachProjects) AS proyectos,
       (SELECT COUNT(*) FROM annualFinances)   AS ejercicios;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
