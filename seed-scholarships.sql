-- =========================================
-- Scholarships and titulation (development data)
-- =========================================
-- Both areas keep one row per student rather than a total per format, so this seed fills them
-- the same way the screens do:
--   * grants of the last two periods, spread over the official programmes and their sources,
--   * titulation processes for students who actually finished, which is what makes the
--     titulation rate (titled over graduates) mean anything.
--
-- Requires: migrations (20260929032030_ScholarshipsAndTitulation), test-data.sql,
-- seed-official-catalogs.sql and seed-academic-history.sql.
-- Run:
--   docker exec -i upzmg_db sh -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U dev_user -P "$MSSQL_DEV_PASSWORD" -C -d upzmg -b -I' < seed-scholarships.sql
--
-- Idempotent: it removes its own rows first.

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
BEGIN TRANSACTION;

-- Its own rows, plus anything the automated checks may have left behind.
DELETE FROM scolarshipAssigneds WHERE Id LIKE 'eeeeeeee-%' OR ScolarshipObjective LIKE N'Prueba %';
DELETE FROM titulationProcesses WHERE Id LIKE 'eeeeeeee-%';

-- ── Scholarships ────────────────────────────────────────────────────────────
-- Active students of the two most recent periods with enrolments, one grant each.
DECLARE @Periods TABLE (RowNum INT, Id UNIQUEIDENTIFIER);
INSERT INTO @Periods
SELECT TOP 2 ROW_NUMBER() OVER (ORDER BY StartDate DESC), Id
FROM periods
WHERE EXISTS (SELECT 1 FROM courseEnrollment e WHERE e.PeriodId = periods.Id)
ORDER BY StartDate DESC;

IF NOT EXISTS (SELECT 1 FROM @Periods) THROW 51000, 'No periods with enrolments. Run seed-academic-history.sql first.', 1;

-- The programmes, numbered so each student gets one in rotation.
DECLARE @Programs TABLE (RowNum INT, Id UNIQUEIDENTIFIER, SourceType INT);
INSERT INTO @Programs
SELECT ROW_NUMBER() OVER (ORDER BY OfficialName), Id, SourceType FROM ScolarshipProgram;

DECLARE @ProgramCount INT = (SELECT COUNT(*) FROM @Programs);

-- Roughly one in six active students holds a grant, which is the order of magnitude the
-- university reports; enough to make the breakdown by source readable.
;WITH Beneficiaries AS (
    SELECT s.Id AS StudentId,
           ROW_NUMBER() OVER (ORDER BY s.EnrollmentNumber) AS RowNum
    FROM students s
    WHERE s.Status = 0
)
INSERT INTO scolarshipAssigneds
    (Id, ScolarshipProgramId, PeriodId, StudentId, ScolarshipObjective, FinantialSource, Amount, AssignedDate)
SELECT
    CAST(CONCAT('eeeeeeee-0000-0000-0001-', RIGHT(CONCAT('000000000000', b.RowNum), 12)) AS UNIQUEIDENTIFIER),
    p.Id,
    per.Id,
    b.StudentId,
    CASE WHEN p.SourceType = 0 THEN N'Permanencia escolar' ELSE N'Apoyo al desempeño académico' END,
    p.SourceType,
    CASE p.SourceType WHEN 0 THEN 9600.00 WHEN 1 THEN 7200.00 WHEN 2 THEN 4800.00 ELSE 12000.00 END,
    DATEADD(DAY, 15, (SELECT StartDate FROM periods WHERE Id = per.Id))
FROM Beneficiaries b
JOIN @Programs p ON p.RowNum = ((b.RowNum - 1) % @ProgramCount) + 1
JOIN @Periods per ON per.RowNum = ((b.RowNum - 1) % 2) + 1
WHERE b.RowNum % 6 = 1;

-- ── Titulation ──────────────────────────────────────────────────────────────
-- Only graduates: the rate is titled over graduates, so titling anyone else would inflate it.
DECLARE @Assessor UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM employees WHERE IsTeacher = 1 ORDER BY EmployeeNumber);
IF @Assessor IS NULL THROW 51000, 'No teachers registered. Run test-data.sql first.', 1;

;WITH Graduates AS (
    SELECT s.Id AS StudentId, s.CareerId,
           ROW_NUMBER() OVER (ORDER BY s.EnrollmentNumber) AS RowNum
    FROM students s
    WHERE s.Status = 3
)
INSERT INTO titulationProcesses
    (Id, StudentId, CareerId, TitulationAssessorId, Modality, CreditsEgressDate,
     CertificateTitulationDate, TitleExpeditionDate, HasProfessionalLicence, ProfessionalLicenceNumber)
SELECT
    CAST(CONCAT('eeeeeeee-0000-0000-0002-', RIGHT(CONCAT('000000000000', g.RowNum), 12)) AS UNIQUEIDENTIFIER),
    g.StudentId,
    g.CareerId,
    @Assessor,
    -- Modality rotates over the four the university recognises.
    (g.RowNum % 4),
    DATEFROMPARTS(2025, 1 + (g.RowNum % 11), 15),
    DATEFROMPARTS(2025, 1 + (g.RowNum % 11), 28),
    DATEFROMPARTS(2025, 1 + (g.RowNum % 11), 28),
    CASE WHEN g.RowNum % 3 <> 0 THEN 1 ELSE 0 END,
    CASE WHEN g.RowNum % 3 <> 0 THEN CONCAT('CED', RIGHT(CONCAT('0000000', g.RowNum), 7)) ELSE NULL END
-- Roughly seven in ten graduates completed the paperwork, so the gap between egreso and
-- titulación the board report discusses is visible instead of assumed.
FROM Graduates g
WHERE g.RowNum % 10 < 7;

COMMIT TRANSACTION;

PRINT '=== Scholarships and titulation loaded ===';
SELECT (SELECT COUNT(*) FROM scolarshipAssigneds) AS becas,
       (SELECT COUNT(*) FROM titulationProcesses) AS titulaciones,
       (SELECT COUNT(*) FROM students WHERE Status = 3) AS egresados;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
