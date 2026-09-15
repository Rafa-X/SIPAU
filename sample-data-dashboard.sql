/* ============================================================================
   SIGIE — sample data for the home dashboard
   ----------------------------------------------------------------------------
   Adds students and teachers on top of test-data.sql so the dashboard has
   figures worth reading. Run it AFTER test-data.sql.

   Safe to re-run: students are keyed by enrollment number (DEMO####) and
   teachers by employee number (2001-2010); existing rows are skipped.

   sqlcmd needs -I (QUOTED_IDENTIFIER ON), same as test-data.sql.
   Undo: see the commented block at the end.
   ============================================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

-- Fixed ids created by test-data.sql
DECLARE @CareerComputing  UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @CareerBiomedic   UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111112';
DECLARE @CareerIndustrial UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111113';
DECLARE @PeriodEA UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222201';
DECLARE @PeriodMA UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222202';
DECLARE @PeriodSD UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222203';

IF NOT EXISTS (SELECT 1 FROM careers WHERE Id = @CareerComputing)
   OR NOT EXISTS (SELECT 1 FROM periods WHERE Id = @PeriodEA)
    THROW 50010, 'Run test-data.sql first: its careers and periods are missing.', 1;

BEGIN TRANSACTION;

-- 1) Students: one intake per period and career, sized so the charts have shape.
WITH Intakes AS (
    SELECT * FROM (VALUES
        (1, @PeriodEA, @CareerComputing, 16), (2, @PeriodEA, @CareerBiomedic, 9), (3, @PeriodEA, @CareerIndustrial, 12),
        (4, @PeriodMA, @CareerComputing, 11), (5, @PeriodMA, @CareerBiomedic, 6), (6, @PeriodMA, @CareerIndustrial, 8),
        (7, @PeriodSD, @CareerComputing, 7),  (8, @PeriodSD, @CareerBiomedic, 4), (9, @PeriodSD, @CareerIndustrial, 5)
    ) AS v (Ord, PeriodId, CareerId, Size)
),
Numbers AS (
    SELECT TOP (20) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N FROM sys.all_objects
),
Seeds AS (
    SELECT i.PeriodId, i.CareerId, ROW_NUMBER() OVER (ORDER BY i.Ord, n.N) AS Seq
    FROM Intakes i
    JOIN Numbers n ON n.N <= i.Size
)
INSERT INTO students
    (Id, EnrollmentNumber, CURP, Name, MiddleName, LastName, BirthDate, Gender, CareerId, PeriodIdStart,
     InscriptionType, Status,
     IndigenousLanguages, PostalCode, EnglishLevel,
     MaritalStatus, NumberOfChildren, Works, BirthCountry, BirthState,
     Address)
SELECT
    NEWID(),
    CONCAT('DEMO', RIGHT(CONCAT('0000', s.Seq), 4)),
    CONCAT('DEMO', RIGHT(CONCAT('000000', s.Seq), 6), IIF(s.Seq % 2 = 0, 'HJCRXX', 'MJCRXX'), '00'),
    IIF(s.Seq % 2 = 0,
        CHOOSE(s.Seq / 2 % 7 + 1, N'Luis', N'Jorge', N'Carlos', N'Diego', N'Miguel', N'Andrés', N'Emilio'),
        CHOOSE(s.Seq / 2 % 7 + 1, N'Ana', N'María', N'Daniela', N'Fernanda', N'Valeria', N'Sofía', N'Lucía')),
    IIF(s.Seq % 2 = 0,
        CHOOSE(s.Seq % 5 + 1, N'Alejandro', N'Antonio', N'Eduardo', N'Javier', N'Ricardo'),
        CHOOSE(s.Seq % 5 + 1, N'Isabel', N'Guadalupe', N'Alejandra', N'Paola', N'Beatriz')),
    CHOOSE(s.Seq % 9 + 1, N'Hernández', N'García', N'Martínez', N'López', N'González', N'Pérez', N'Rodríguez', N'Sánchez', N'Ramírez'),
    DATEADD(DAY, -((s.Seq * 37) % 1800), '2006-08-15'),
    IIF(s.Seq % 2 = 0, 'H', 'M'),
    s.CareerId,
    s.PeriodId,
    0,                                                                   -- InscriptionType.NuevoIngreso
    CASE WHEN s.Seq % 13 = 0 THEN 2 WHEN s.Seq % 9 = 0 THEN 1 ELSE 0 END, -- a few definitive and temporary leaves
    '[]',
    '45640',
    s.Seq % 4,
    0,
    0,
    IIF(s.Seq % 3 = 0, 1, 0),
    N'México',
    N'Jalisco',
    N'Tlajomulco de Zúñiga, Jalisco'
FROM Seeds s
WHERE NOT EXISTS (
    SELECT 1 FROM students st
    WHERE st.EnrollmentNumber = CONCAT('DEMO', RIGHT(CONCAT('0000', s.Seq), 4)));

-- 2) Teachers: a staff with a realistic spread of highest degrees (7 of 10 postgraduate).
INSERT INTO employees
    (Id, EmployeeNumber, FullName, Email, Rfc, Curp, Gender, BirthDate, HighestEducationLevel, TitleStatus, HasDisability, ExternalXPYears, FirstHiredDate, IsTeacher)
SELECT NEWID(), t.Number, t.FullName, t.Email, t.Rfc, t.Curp, t.Gender, t.BirthDate, t.Level, 0, 0, t.Xp, t.Hired, 1
FROM (VALUES
    (2001, N'Mtra. Laura Mendoza Ruiz',   'docente.demo01@upzmg.local', 'MERL800312AB1', 'MERL800312MJCNZR01', 'M', '1980-03-12', 7, 8,  '2015-08-17'),
    (2002, N'Dr. Ricardo Salinas Ortega', 'docente.demo02@upzmg.local', 'SAOR750921CD2', 'SAOR750921HJCLRC02', 'H', '1975-09-21', 9, 12, '2012-01-09'),
    (2003, N'Ing. Pablo Navarro Cruz',    'docente.demo03@upzmg.local', 'NACP880105EF3', 'NACP880105HJCVRB03', 'H', '1988-01-05', 4, 4,  '2019-08-19'),
    (2004, N'Mtra. Elena Castillo Vega',  'docente.demo04@upzmg.local', 'CAVE850614GH4', 'CAVE850614MJCSGL04', 'M', '1985-06-14', 7, 6,  '2017-01-16'),
    (2005, N'Lic. Andrea Fuentes Mora',   'docente.demo05@upzmg.local', 'FUMA900228IJ5', 'FUMA900228MJCNRN05', 'M', '1990-02-28', 3, 3,  '2021-08-16'),
    (2006, N'Dra. Patricia Ibarra León',  'docente.demo06@upzmg.local', 'IALP780807KL6', 'IALP780807MJCBNT06', 'M', '1978-08-07', 9, 10, '2013-08-19'),
    (2007, N'Mtro. Hugo Reyes Delgado',   'docente.demo07@upzmg.local', 'REDH830419MN7', 'REDH830419HJCYLG07', 'H', '1983-04-19', 7, 7,  '2016-08-15'),
    (2008, N'Ing. Sergio Aguilar Ponce',  'docente.demo08@upzmg.local', 'AUPS910930OP8', 'AUPS910930HJCGNR08', 'H', '1991-09-30', 4, 2,  '2022-01-10'),
    (2009, N'Esp. Rosa Medina Torres',    'docente.demo09@upzmg.local', 'METR870716QR9', 'METR870716MJCDRS09', 'M', '1987-07-16', 6, 5,  '2018-08-20'),
    (2010, N'Mtro. Víctor Luna Herrera',  'docente.demo10@upzmg.local', 'LUHV820125ST0', 'LUHV820125HJCNRC10', 'H', '1982-01-25', 7, 9,  '2014-08-18')
) AS t (Number, FullName, Email, Rfc, Curp, Gender, BirthDate, Level, Xp, Hired)
WHERE NOT EXISTS (SELECT 1 FROM employees e WHERE e.EmployeeNumber = t.Number);

COMMIT TRANSACTION;

SELECT
    (SELECT COUNT(*) FROM students  WHERE EnrollmentNumber LIKE 'DEMO%')      AS sample_students,
    (SELECT COUNT(*) FROM employees WHERE EmployeeNumber BETWEEN 2001 AND 2010) AS sample_teachers;

/* Undo (removes only the rows this script adds):
DELETE FROM students  WHERE EnrollmentNumber LIKE 'DEMO%';
DELETE FROM employees WHERE EmployeeNumber BETWEEN 2001 AND 2010;
*/
