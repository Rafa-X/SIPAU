-- =========================================
-- Academic history for the intelligent module (development data)
-- =========================================
-- The projection, the dropout model and the anomaly alerts are computed from what is in
-- the database; an empty database correctly has nothing to model. This script loads a
-- synthetic but coherent history on top of test-data.sql:
--   * cohorts entering every period from 2018 Ene-Abr to 2026 Ene-Abr, so every term
--     of a 15-term programme is already populated when the record starts,
--   * registrations (assignatureRegistrations) and kardex from 2023 Ene-Abr to 2026 Ene-Abr,
--   * graduates, temporary and definitive withdrawals, and extraordinary exams.
--
-- A hidden performance score (0..99) drives both grades and withdrawals, as in real life;
-- working students and older entrants withdraw a little more often. Nothing encodes the
-- outcome directly, so the model has a real, noisy relationship to discover.
-- Two deliberate events give the detectors something to find: a small intake in
-- 2024 Ene-Abr and a harder 2026 Ene-Abr (lower pass rate).
--
-- Requires, in order: migrations, test-data.sql, seed-official-catalogs.sql.
-- Run (Git Bash / PowerShell, from the repo root):
--   docker exec -i upzmg_db sh -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U dev_user -P "$MSSQL_DEV_PASSWORD" -C -d upzmg -b' < seed-academic-history.sql
--
-- Idempotent: it first removes its own rows (enrolment numbers starting with 'UPH' and
-- subjects whose code starts with 'A5E0A5E0'). Periods are reused, never deleted.

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
BEGIN TRANSACTION;

-- 0) Dependencies
DECLARE @CareerComputing  UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111111';
DECLARE @CareerBiotech    UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111112';
DECLARE @CareerIndustrial UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111113';

IF (SELECT COUNT(*) FROM careers WHERE Id IN (@CareerComputing, @CareerBiotech, @CareerIndustrial)) < 3
    THROW 51000, 'Missing the test careers. Run test-data.sql first.', 1;

DECLARE @Classroom  UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM SchoolFacility WHERE Type = 11 ORDER BY Name);
DECLARE @Laboratory UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM SchoolFacility WHERE Type = 9 ORDER BY Name);
IF @Classroom IS NULL OR @Laboratory IS NULL
    THROW 51000, 'Missing the test facilities. Run test-data.sql first.', 1;

CREATE TABLE #Teachers (Idx INT PRIMARY KEY, Id UNIQUEIDENTIFIER NOT NULL);
INSERT INTO #Teachers (Idx, Id)
SELECT ROW_NUMBER() OVER (ORDER BY EmployeeNumber) - 1, Id FROM employees WHERE IsTeacher = 1;
DECLARE @TeacherCount INT = (SELECT COUNT(*) FROM #Teachers);
IF @TeacherCount = 0
    THROW 51000, 'Missing teachers. Run test-data.sql first.', 1;

-- The schema requires a reason on every registration; "Causa desconocida" fills the ones without a withdrawal.
DECLARE @NoReason UNIQUEIDENTIFIER = '88888888-0000-0000-0000-000000000001';
IF NOT EXISTS (SELECT 1 FROM WithdrawReason WHERE Id = @NoReason)
    THROW 51000, 'Missing withdrawal reasons. Run seed-official-catalogs.sql first.', 1;

-- 1) Remove a previous run
DECLARE @OldStudents TABLE (Id UNIQUEIDENTIFIER PRIMARY KEY);
INSERT INTO @OldStudents SELECT Id FROM students WHERE EnrollmentNumber LIKE 'UPH%';

DECLARE @OldSubjects TABLE (Id UNIQUEIDENTIFIER PRIMARY KEY);
INSERT INTO @OldSubjects SELECT Id FROM asignatures WHERE CAST(AsignatureCode AS CHAR(36)) LIKE 'A5E0A5E0-%';

DELETE FROM kardex                     WHERE StudentId IN (SELECT Id FROM @OldStudents) OR AsignatureId IN (SELECT Id FROM @OldSubjects);
DELETE FROM tutoringSessions           WHERE StudentId IN (SELECT Id FROM @OldStudents)
                                          OR GroupId IN (SELECT Id FROM classGroups WHERE AsignatureId IN (SELECT Id FROM @OldSubjects));
DELETE FROM assignatureRegistrations   WHERE StudentId IN (SELECT Id FROM @OldStudents)
                                          OR GroupId IN (SELECT Id FROM classGroups WHERE AsignatureId IN (SELECT Id FROM @OldSubjects));
DELETE FROM studentDisabilities        WHERE StudentId IN (SELECT Id FROM @OldStudents);
DELETE FROM scolarshipAssigneds        WHERE StudentId IN (SELECT Id FROM @OldStudents);
DELETE FROM titulationProcesses        WHERE StudentId IN (SELECT Id FROM @OldStudents);
DELETE FROM IndigenousLanguagesSpeakers WHERE SpeakerId IN (SELECT Id FROM @OldStudents);
DELETE FROM students                   WHERE Id IN (SELECT Id FROM @OldStudents);
DELETE FROM classGroups                WHERE AsignatureId IN (SELECT Id FROM @OldSubjects);
DELETE FROM asignatures                WHERE Id IN (SELECT Id FROM @OldSubjects);

-- 2) Periods: G = 0 is 2018 Ene-Abr, G = 24 is 2026 Ene-Abr (the active period in test-data.sql)
DECLARE @FirstRecorded INT = 15;   -- 2023 Ene-Abr
DECLARE @LastRecorded  INT = 24;   -- 2026 Ene-Abr

CREATE TABLE #Periods (
    G INT PRIMARY KEY, Id UNIQUEIDENTIFIER NULL, IdCode NVARCHAR(20) NOT NULL,
    SchoolCycle NVARCHAR(4) NOT NULL, SepPeriod NVARCHAR(8) NOT NULL, StartDate DATE NOT NULL, EndDate DATE NOT NULL);

;WITH Numbers AS (SELECT TOP (25) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1 AS G FROM sys.all_objects)
INSERT INTO #Periods (G, IdCode, SchoolCycle, SepPeriod, StartDate, EndDate)
SELECT G,
       CONCAT(2018 + G / 3, '-', CHOOSE(G % 3 + 1, 'EA', 'MA', 'SD')),
       CAST(2018 + G / 3 AS NVARCHAR(4)),
       CHOOSE(G % 3 + 1, N'Ene-Abr', N'May-Ago', N'Sep-Dic'),
       DATEFROMPARTS(2018 + G / 3, CHOOSE(G % 3 + 1, 1, 5, 9), 1),
       EOMONTH(DATEFROMPARTS(2018 + G / 3, CHOOSE(G % 3 + 1, 4, 8, 12), 1))
FROM Numbers;

-- Reuse periods that already exist (test-data.sql creates the 2026 ones)
UPDATE p SET Id = e.Id FROM #Periods p JOIN periods e ON e.IdCode = p.IdCode;
UPDATE #Periods SET Id = NEWID() WHERE Id IS NULL;

INSERT INTO periods (Id, IdCode, SchoolCycle, SEP_Period, StartDate, EndDate, IsActive)
SELECT p.Id, p.IdCode, p.SchoolCycle, p.SepPeriod, p.StartDate, p.EndDate, 0
FROM #Periods p
WHERE NOT EXISTS (SELECT 1 FROM periods e WHERE e.Id = p.Id);

-- 3) Careers: intake share and a pass-rate bias (Biotechnology is deliberately harder)
CREATE TABLE #Careers (Idx INT PRIMARY KEY, Id UNIQUEIDENTIFIER NOT NULL, Duration INT NOT NULL,
                       Share DECIMAL(4, 2) NOT NULL, PassBias INT NOT NULL);
INSERT INTO #Careers (Idx, Id, Duration, Share, PassBias) VALUES
    (0, @CareerComputing,  15, 0.40,  3),
    (1, @CareerIndustrial, 15, 0.35,  1),
    (2, @CareerBiotech,    15, 0.25, -8);

UPDATE c
SET Duration = CASE WHEN k.DurationInPeriods BETWEEN 6 AND 15 THEN k.DurationInPeriods ELSE 15 END
FROM #Careers c JOIN careers k ON k.Id = c.Id;

-- 4) Subjects: five per term (shared core, languages, career specific, its laboratory, sociocultural training)
CREATE TABLE #SubjectNames (CareerIdx INT NOT NULL, Term INT NOT NULL, Slot INT NOT NULL, Name NVARCHAR(120) NOT NULL);

INSERT INTO #SubjectNames (CareerIdx, Term, Slot, Name)
SELECT c.Idx, s.Term, 1, s.Name
FROM #Careers c
CROSS JOIN (VALUES
    (1, N'Álgebra Lineal'), (2, N'Cálculo Diferencial'), (3, N'Cálculo Integral'),
    (4, N'Probabilidad y Estadística'), (5, N'Ecuaciones Diferenciales'), (6, N'Métodos Numéricos'),
    (7, N'Investigación de Operaciones'), (8, N'Ingeniería Económica'), (9, N'Formulación de Proyectos'),
    (10, N'Administración de Proyectos'), (11, N'Calidad y Mejora Continua'), (12, N'Innovación y Emprendimiento'),
    (13, N'Ética Profesional'), (14, N'Seminario de Titulación'), (15, N'Estadía Profesional')) AS s (Term, Name);

INSERT INTO #SubjectNames (CareerIdx, Term, Slot, Name)
SELECT c.Idx, s.Term, 2, s.Name
FROM #Careers c
CROSS JOIN (VALUES
    (1, N'Inglés I'), (2, N'Inglés II'), (3, N'Inglés III'), (4, N'Inglés IV'), (5, N'Inglés V'),
    (6, N'Inglés VI'), (7, N'Inglés VII'), (8, N'Inglés VIII'), (9, N'Inglés IX'),
    (10, N'Habilidades Directivas'), (11, N'Comunicación Profesional'), (12, N'Desarrollo Sustentable'),
    (13, N'Liderazgo de Equipos'), (14, N'Negociación Empresarial'), (15, N'Informe de Estadía')) AS s (Term, Name);

INSERT INTO #SubjectNames (CareerIdx, Term, Slot, Name) VALUES
    (0, 1, 3, N'Fundamentos de Programación'), (0, 2, 3, N'Programación Estructurada'), (0, 3, 3, N'Estructuras de Datos'),
    (0, 4, 3, N'Bases de Datos I'), (0, 5, 3, N'Sistemas Operativos'), (0, 6, 3, N'Redes de Computadoras'),
    (0, 7, 3, N'Ingeniería de Software'), (0, 8, 3, N'Programación Web'), (0, 9, 3, N'Bases de Datos II'),
    (0, 10, 3, N'Sistemas Distribuidos'), (0, 11, 3, N'Inteligencia Artificial'), (0, 12, 3, N'Seguridad Informática'),
    (0, 13, 3, N'Cómputo en la Nube'), (0, 14, 3, N'Aprendizaje Automático'), (0, 15, 3, N'Proyecto de Software'),

    (1, 1, 3, N'Dibujo Industrial'), (1, 2, 3, N'Química Industrial'), (1, 3, 3, N'Física'),
    (1, 4, 3, N'Procesos de Manufactura'), (1, 5, 3, N'Estudio del Trabajo'), (1, 6, 3, N'Control Estadístico de la Calidad'),
    (1, 7, 3, N'Planeación y Control de la Producción'), (1, 8, 3, N'Logística y Cadena de Suministro'), (1, 9, 3, N'Ergonomía'),
    (1, 10, 3, N'Seguridad e Higiene Industrial'), (1, 11, 3, N'Manufactura Esbelta'), (1, 12, 3, N'Simulación de Sistemas'),
    (1, 13, 3, N'Automatización Industrial'), (1, 14, 3, N'Diseño de Plantas'), (1, 15, 3, N'Proyecto Industrial'),

    (2, 1, 3, N'Química General'), (2, 2, 3, N'Biología Celular y Molecular'), (2, 3, 3, N'Química Orgánica'),
    (2, 4, 3, N'Bioquímica'), (2, 5, 3, N'Microbiología'), (2, 6, 3, N'Genética'),
    (2, 7, 3, N'Termodinámica'), (2, 8, 3, N'Fenómenos de Transporte'), (2, 9, 3, N'Ingeniería de Bioprocesos'),
    (2, 10, 3, N'Biología Molecular Aplicada'), (2, 11, 3, N'Bioseparaciones'), (2, 12, 3, N'Biotecnología Ambiental'),
    (2, 13, 3, N'Biotecnología Alimentaria'), (2, 14, 3, N'Bioinformática'), (2, 15, 3, N'Proyecto de Biotecnología');

INSERT INTO #SubjectNames (CareerIdx, Term, Slot, Name)
SELECT CareerIdx, Term, 4, CONCAT(N'Laboratorio de ', Name)
FROM #SubjectNames
WHERE Slot = 3;

INSERT INTO #SubjectNames (CareerIdx, Term, Slot, Name)
SELECT c.Idx, s.Term, 5, s.Name
FROM #Careers c
CROSS JOIN (VALUES
    (1, N'Formación Sociocultural I'), (2, N'Formación Sociocultural II'), (3, N'Formación Sociocultural III'),
    (4, N'Formación Sociocultural IV'), (5, N'Expresión Oral y Escrita I'), (6, N'Expresión Oral y Escrita II'),
    (7, N'Valores del Ser'), (8, N'Inteligencia Emocional'), (9, N'Desarrollo Interpersonal'),
    (10, N'Habilidades del Pensamiento'), (11, N'Habilidades Organizacionales'), (12, N'Ética y Responsabilidad Social'),
    (13, N'Planeación del Trabajo'), (14, N'Dirección de Equipos'), (15, N'Solución de Conflictos')) AS s (Term, Name);

CREATE TABLE #Subjects (CareerIdx INT NOT NULL, Term INT NOT NULL, Slot INT NOT NULL,
                        Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(), PRIMARY KEY (CareerIdx, Term, Slot));
INSERT INTO #Subjects (CareerIdx, Term, Slot)
SELECT n.CareerIdx, n.Term, n.Slot
FROM #SubjectNames n JOIN #Careers c ON c.Idx = n.CareerIdx
WHERE n.Term <= c.Duration;

INSERT INTO asignatures (Id, CareerId, AsignatureCode, AsignatureName, Credits, Cuatrimestre, IsMandatory)
SELECT s.Id, c.Id,
       -- The 'A5E0A5E0' prefix marks the subjects this script owns.
       CAST(CONCAT('A5E0A5E0-0000-4000-8000-', RIGHT(CONCAT('000000000000', s.CareerIdx * 1000 + s.Term * 10 + s.Slot), 12)) AS UNIQUEIDENTIFIER),
       n.Name,
       CHOOSE(s.Slot, 6, 5, 8, 4, 4),
       s.Term,
       1
FROM #Subjects s
JOIN #Careers c ON c.Idx = s.CareerIdx
JOIN #SubjectNames n ON n.CareerIdx = s.CareerIdx AND n.Term = s.Term AND n.Slot = s.Slot;

-- 5) Students: one cohort per career and period; intake grows about one student per period
CREATE TABLE #Students (
    Seq INT PRIMARY KEY, Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    G0 INT NOT NULL, CareerIdx INT NOT NULL, Duration INT NOT NULL,
    Perf INT NULL, Works BIT NULL, EntryAge INT NULL, BirthDays INT NULL, IsMale BIT NULL,
    FirstName INT NULL, SecondName INT NULL, LastName1 INT NULL, LastName2 INT NULL,
    Drops BIT NULL, DropG INT NULL, DropKind INT NULL, ReasonId UNIQUEIDENTIFIER NULL,
    EndG INT NULL, Status INT NULL);

;WITH Numbers AS (SELECT TOP (60) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS K FROM sys.all_objects),
Cohorts AS (
    SELECT p.G, c.Idx, c.Duration,
           -- 2024 Ene-Abr (G = 18) takes in far fewer students: the enrolment dip to detect.
           CAST(ROUND(CASE WHEN p.G = 18 THEN 10 ELSE 32 + p.G + (p.G * 7) % 5 - 2 END * c.Share, 0) AS INT) AS Size
    FROM #Periods p CROSS JOIN #Careers c)
INSERT INTO #Students (Seq, G0, CareerIdx, Duration)
SELECT ROW_NUMBER() OVER (ORDER BY co.G, co.Idx, n.K), co.G, co.Idx, co.Duration
FROM Cohorts co
JOIN Numbers n ON n.K <= co.Size;

-- Deterministic pseudo-random traits: every run produces the same people.
UPDATE s
SET Perf       = h.Perf,
    Works      = CASE WHEN h.Work < 30 THEN 1 ELSE 0 END,
    EntryAge   = 18 + CASE WHEN h.Age < 45 THEN 0 WHEN h.Age < 75 THEN 1 WHEN h.Age < 87 THEN 2
                           WHEN h.Age < 93 THEN 3 ELSE 4 + h.Age % 6 END,
    BirthDays  = h.Birth * 3,
    IsMale     = CASE WHEN h.Sex < 58 THEN 1 ELSE 0 END,
    FirstName  = h.Name1 % 20,
    SecondName = h.Name2 % 20,
    LastName1  = h.Last1 % 30,
    LastName2  = h.Last2 % 30
FROM #Students s
CROSS APPLY (SELECT
    ABS(CAST(HASHBYTES('MD5', CONCAT('perf-',  s.Seq)) AS INT) % 100) AS Perf,
    ABS(CAST(HASHBYTES('MD5', CONCAT('work-',  s.Seq)) AS INT) % 100) AS Work,
    ABS(CAST(HASHBYTES('MD5', CONCAT('age-',   s.Seq)) AS INT) % 100) AS Age,
    ABS(CAST(HASHBYTES('MD5', CONCAT('birth-', s.Seq)) AS INT) % 100) AS Birth,
    ABS(CAST(HASHBYTES('MD5', CONCAT('sex-',   s.Seq)) AS INT) % 100) AS Sex,
    ABS(CAST(HASHBYTES('MD5', CONCAT('name1-', s.Seq)) AS INT) % 100) AS Name1,
    ABS(CAST(HASHBYTES('MD5', CONCAT('name2-', s.Seq)) AS INT) % 100) AS Name2,
    ABS(CAST(HASHBYTES('MD5', CONCAT('last1-', s.Seq)) AS INT) % 100) AS Last1,
    ABS(CAST(HASHBYTES('MD5', CONCAT('last2-', s.Seq)) AS INT) % 100) AS Last2) h;

-- Who withdraws, when and why. Low performance dominates; work and a late start add risk.
UPDATE s
SET Drops    = d.Drops,
    -- Always before the last term, so a withdrawal never replaces a graduation.
    DropG    = CASE WHEN d.Drops = 1 THEN s.G0 + CASE WHEN d.Offset > s.Duration - 2 THEN s.Duration - 2 ELSE d.Offset END END,
    DropKind = CASE WHEN d.Drops = 1 THEN CASE WHEN h.Kind < 25 THEN 1 ELSE 2 END END,
    ReasonId = CASE WHEN d.Drops = 1 THEN
                   CASE WHEN s.Works = 1 AND h.Reason < 50 THEN '88888888-0000-0000-0000-000000000007'
                        WHEN s.Perf < 35 THEN CASE WHEN h.Reason % 2 = 0 THEN '88888888-0000-0000-0000-000000000003'
                                                   ELSE '88888888-0000-0000-0000-000000000012' END
                        ELSE CHOOSE(h.Reason % 5 + 1,
                                    '88888888-0000-0000-0000-000000000004', '88888888-0000-0000-0000-000000000005',
                                    '88888888-0000-0000-0000-000000000006', '88888888-0000-0000-0000-000000000017',
                                    '88888888-0000-0000-0000-000000000001') END END
FROM #Students s
CROSS APPLY (SELECT
    ABS(CAST(HASHBYTES('MD5', CONCAT('noise-',  s.Seq)) AS INT) % 20)  AS Noise,
    ABS(CAST(HASHBYTES('MD5', CONCAT('when-',   s.Seq)) AS INT) % 100) AS [When],
    ABS(CAST(HASHBYTES('MD5', CONCAT('when2-',  s.Seq)) AS INT) % 6)   AS When2,
    ABS(CAST(HASHBYTES('MD5', CONCAT('kind-',   s.Seq)) AS INT) % 100) AS Kind,
    ABS(CAST(HASHBYTES('MD5', CONCAT('reason-', s.Seq)) AS INT) % 100) AS Reason) h
CROSS APPLY (SELECT
    CAST(CASE WHEN (100 - s.Perf) + s.Works * 15 + CASE WHEN s.EntryAge >= 22 THEN 10 ELSE 0 END + h.Noise >= 95
              THEN 1 ELSE 0 END AS BIT) AS Drops,
    -- Most withdrawals happen in the first terms.
    CASE WHEN h.[When] < 30 THEN 0 WHEN h.[When] < 55 THEN 1 WHEN h.[When] < 72 THEN 2
         WHEN h.[When] < 84 THEN 3 WHEN h.[When] < 92 THEN 4 ELSE 5 + h.When2 END AS Offset) d;

-- Where each trajectory ends, and the status it has today (after 2026 Ene-Abr)
UPDATE #Students
SET EndG   = CASE WHEN Drops = 1 THEN DropG ELSE G0 + Duration - 1 END,
    Status = CASE WHEN Drops = 1 AND DropG <= @LastRecorded THEN DropKind
                  WHEN Drops = 0 AND G0 + Duration - 1 <= @LastRecorded THEN 3
                  ELSE 0 END;

DECLARE @FirstNamesMale TABLE (Idx INT PRIMARY KEY, Name NVARCHAR(40));
INSERT INTO @FirstNamesMale VALUES
    (0, N'José'), (1, N'Luis'), (2, N'Carlos'), (3, N'Juan'), (4, N'Miguel'), (5, N'Jorge'), (6, N'Alejandro'),
    (7, N'Diego'), (8, N'Fernando'), (9, N'Ricardo'), (10, N'Eduardo'), (11, N'Javier'), (12, N'Daniel'),
    (13, N'Emiliano'), (14, N'Santiago'), (15, N'Sebastián'), (16, N'Iván'), (17, N'Óscar'), (18, N'Raúl'), (19, N'Héctor');

DECLARE @FirstNamesFemale TABLE (Idx INT PRIMARY KEY, Name NVARCHAR(40));
INSERT INTO @FirstNamesFemale VALUES
    (0, N'María'), (1, N'Guadalupe'), (2, N'Fernanda'), (3, N'Daniela'), (4, N'Sofía'), (5, N'Valeria'), (6, N'Andrea'),
    (7, N'Paola'), (8, N'Ximena'), (9, N'Regina'), (10, N'Camila'), (11, N'Alejandra'), (12, N'Mariana'),
    (13, N'Karla'), (14, N'Diana'), (15, N'Lucía'), (16, N'Natalia'), (17, N'Itzel'), (18, N'Renata'), (19, N'Jimena');

DECLARE @LastNames TABLE (Idx INT PRIMARY KEY, Name NVARCHAR(40));
INSERT INTO @LastNames VALUES
    (0, N'Hernández'), (1, N'García'), (2, N'Martínez'), (3, N'López'), (4, N'González'), (5, N'Pérez'),
    (6, N'Rodríguez'), (7, N'Sánchez'), (8, N'Ramírez'), (9, N'Cruz'), (10, N'Flores'), (11, N'Gómez'),
    (12, N'Morales'), (13, N'Vázquez'), (14, N'Reyes'), (15, N'Jiménez'), (16, N'Torres'), (17, N'Díaz'),
    (18, N'Gutiérrez'), (19, N'Ruiz'), (20, N'Mendoza'), (21, N'Aguilar'), (22, N'Ortiz'), (23, N'Castillo'),
    (24, N'Romero'), (25, N'Álvarez'), (26, N'Chávez'), (27, N'Rivera'), (28, N'Navarro'), (29, N'Salazar');

INSERT INTO students
    (Id, EnrollmentNumber, CURP, Name, MiddleName, LastName, BirthDate, Gender, CareerId, PeriodIdStart,
     InscriptionType, Status, IndigenousLanguages, PostalCode, EnglishLevel,
     MaritalStatus, NumberOfChildren, Works, BirthCountry, BirthState, Address)
SELECT s.Id,
       CONCAT('UPH', RIGHT(p.SchoolCycle, 2), RIGHT(CONCAT('0000', s.Seq), 4)),
       -- Shaped like a CURP (18 characters); not a real one.
       CONCAT('XE', CHOOSE(s.Seq % 4 + 1, 'HA', 'MO', 'RU', 'GA'), CONVERT(CHAR(6), b.BirthDate, 12),
              CASE WHEN s.IsMale = 1 THEN 'H' ELSE 'M' END, 'JC', 'XXX', RIGHT(CONCAT('0', s.Seq % 100), 2)),
       CASE WHEN s.IsMale = 1 THEN fm.Name ELSE ff.Name END,
       CASE WHEN s.IsMale = 1 THEN sm.Name ELSE sf.Name END,
       CONCAT(l1.Name, N' ', l2.Name),
       b.BirthDate,
       CASE WHEN s.IsMale = 1 THEN 'H' ELSE 'M' END,
       c.Id,
       p.Id,
       0,
       s.Status,
       '[]',
       CONCAT('45', RIGHT(CONCAT('000', 100 + s.Seq % 800), 3)),
       s.Seq % 4,
       CASE WHEN s.EntryAge >= 24 THEN 1 ELSE 0 END,
       CASE WHEN s.EntryAge >= 25 THEN 1 ELSE 0 END,
       s.Works,
       N'México',
       CASE WHEN s.Seq % 9 = 0 THEN N'Michoacán' WHEN s.Seq % 13 = 0 THEN N'Nayarit' ELSE N'Jalisco' END,
       NULL
FROM #Students s
JOIN #Periods p ON p.G = s.G0
JOIN #Careers c ON c.Idx = s.CareerIdx
JOIN @FirstNamesMale fm ON fm.Idx = s.FirstName
JOIN @FirstNamesMale sm ON sm.Idx = (s.SecondName + 7) % 20
JOIN @FirstNamesFemale ff ON ff.Idx = s.FirstName
JOIN @FirstNamesFemale sf ON sf.Idx = (s.SecondName + 7) % 20
JOIN @LastNames l1 ON l1.Idx = s.LastName1
JOIN @LastNames l2 ON l2.Idx = s.LastName2
-- Born so that the age at the start of the first period is exactly EntryAge.
CROSS APPLY (SELECT DATEADD(DAY, -s.BirthDays, DATEADD(YEAR, -s.EntryAge, p.StartDate)) AS BirthDate) b;

-- 6) Enrolments inside the recorded window: one per student and period
CREATE TABLE #Enrolments (
    Seq INT NOT NULL, G INT NOT NULL, Term INT NOT NULL, FinalStatus INT NOT NULL,
    ReasonId UNIQUEIDENTIFIER NOT NULL, IsDropPeriod BIT NOT NULL, PRIMARY KEY (Seq, G));

INSERT INTO #Enrolments (Seq, G, Term, FinalStatus, ReasonId, IsDropPeriod)
SELECT s.Seq, p.G, p.G - s.G0 + 1,
       -- FinalStatus: 0 Activo, 1 BajaTemporal, 2 BajaDefinitiva, 3 Egresado
       CASE WHEN s.Drops = 1 AND p.G = s.DropG THEN s.DropKind
            WHEN s.Drops = 0 AND p.G = s.EndG THEN 3
            ELSE 0 END,
       CASE WHEN s.Drops = 1 AND p.G = s.DropG THEN s.ReasonId ELSE @NoReason END,
       CASE WHEN s.Drops = 1 AND p.G = s.DropG THEN 1 ELSE 0 END
FROM #Students s
JOIN #Periods p ON p.G BETWEEN s.G0 AND s.EndG
WHERE p.G BETWEEN @FirstRecorded AND @LastRecorded;

-- 7) Class groups: one per period, career, term and subject that has students
CREATE TABLE #Groups (
    G INT NOT NULL, CareerIdx INT NOT NULL, Term INT NOT NULL, Slot INT NOT NULL,
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(), Enrolled INT NOT NULL,
    PRIMARY KEY (G, CareerIdx, Term, Slot));

INSERT INTO #Groups (G, CareerIdx, Term, Slot, Enrolled)
SELECT e.G, s.CareerIdx, e.Term, sl.Slot, COUNT(*)
FROM #Enrolments e
JOIN #Students s ON s.Seq = e.Seq
CROSS JOIN (VALUES (1), (2), (3), (4), (5)) AS sl (Slot)
GROUP BY e.G, s.CareerIdx, e.Term, sl.Slot;

INSERT INTO classGroups
    (Id, FacilityId, Schedule, Capacity, Enrolled, Weekhours, PeriodId, CareerId, TeacherId, AsignatureId, IsTheory)
SELECT g.Id,
       CASE WHEN g.Slot = 4 THEN @Laboratory ELSE @Classroom END,
       CONCAT(N'[{"Day":"', CHOOSE(g.Slot, N'Lunes y miércoles', N'Martes y jueves', N'Lunes y miércoles', N'Viernes', N'Martes y jueves'),
              N'","Time":"', CHOOSE((g.Term + g.Slot) % 4 + 1, N'07:00-09:00', N'09:00-11:00', N'11:00-13:00', N'13:00-15:00'),
              N' hrs"}]'),
       40,
       g.Enrolled,
       CHOOSE(g.Slot, 4, 3, 5, 2, 2),
       p.Id,
       c.Id,
       t.Id,
       sub.Id,
       CASE WHEN g.Slot = 4 THEN 0 ELSE 1 END
FROM #Groups g
JOIN #Periods p ON p.G = g.G
JOIN #Careers c ON c.Idx = g.CareerIdx
JOIN #Subjects sub ON sub.CareerIdx = g.CareerIdx AND sub.Term = g.Term AND sub.Slot = g.Slot
JOIN #Teachers t ON t.Idx = (g.G + g.Term + g.Slot) % @TeacherCount;

-- 8) Registrations: one row per subject taken
-- InscriptionType: 0 NuevoIngreso, 2 Reinscripcion
INSERT INTO assignatureRegistrations (Id, StudentId, GroupId, PeriodId, InscriptionType, FinalStatus, WithdrawReasonId)
SELECT NEWID(), s.Id, g.Id, p.Id,
       CASE WHEN e.G = s.G0 THEN 0 ELSE 2 END,
       e.FinalStatus,
       e.ReasonId
FROM #Enrolments e
JOIN #Students s ON s.Seq = e.Seq
JOIN #Periods p ON p.G = e.G
JOIN #Groups g ON g.G = e.G AND g.CareerIdx = s.CareerIdx AND g.Term = e.Term;

-- 9) Kardex: an ordinary attempt per subject, and an extraordinary one after a failure
CREATE TABLE #Attempts (
    Seq INT NOT NULL, G INT NOT NULL, Term INT NOT NULL, Slot INT NOT NULL,
    Passed BIT NOT NULL, Grade DECIMAL(3, 1) NOT NULL,
    ExtraPassed BIT NULL, ExtraGrade DECIMAL(3, 1) NULL,
    PRIMARY KEY (Seq, G, Slot));

INSERT INTO #Attempts (Seq, G, Term, Slot, Passed, Grade, ExtraPassed, ExtraGrade)
SELECT e.Seq, e.G, e.Term, sl.Slot, a.Passed,
       CASE WHEN a.Passed = 1 THEN 7.0 + ROUND((s.Perf + h.G2) / 200.0 * 3.0, 1)
            ELSE 3.0 + h.G2 % 30 / 10.0 END,
       x.ExtraPassed,
       CASE WHEN x.ExtraPassed = 1 THEN 7.0 + h.G3 % 16 / 10.0
            WHEN x.ExtraPassed = 0 THEN 4.0 + h.G3 % 25 / 10.0 END
FROM #Enrolments e
JOIN #Students s ON s.Seq = e.Seq
JOIN #Careers c ON c.Idx = s.CareerIdx
CROSS JOIN (VALUES (1), (2), (3), (4), (5)) AS sl (Slot)
CROSS APPLY (SELECT
    ABS(CAST(HASHBYTES('MD5', CONCAT('ord-', e.Seq, '-', e.G, '-', sl.Slot)) AS INT) % 100) AS G1,
    ABS(CAST(HASHBYTES('MD5', CONCAT('grd-', e.Seq, '-', e.G, '-', sl.Slot)) AS INT) % 100) AS G2,
    ABS(CAST(HASHBYTES('MD5', CONCAT('ext-', e.Seq, '-', e.G, '-', sl.Slot)) AS INT) % 100) AS G3) h
CROSS APPLY (SELECT CAST(CASE WHEN h.G1 <
        55 + s.Perf * 0.5 + c.PassBias
        - CASE WHEN e.IsDropPeriod = 1 THEN 45 ELSE 0 END      -- courses abandoned when leaving
        - CASE WHEN e.G = @LastRecorded THEN 8 ELSE 0 END      -- the harder latest period
        THEN 1 ELSE 0 END AS BIT) AS Passed) a
-- Nobody sits an extraordinary exam in the period they leave.
CROSS APPLY (SELECT CASE WHEN a.Passed = 1 OR e.IsDropPeriod = 1 THEN NULL
                         WHEN h.G3 < 40 + s.Perf / 2 THEN CAST(1 AS BIT)
                         ELSE CAST(0 AS BIT) END AS ExtraPassed) x;

-- Opportunity: 0 Ordinario, 1 Extraordinario
INSERT INTO kardex (Id, StudentId, AsignatureId, PeriodId, Grade, IsPassed, Opportunity)
SELECT NEWID(), s.Id, sub.Id, p.Id, a.Grade, a.Passed, 0
FROM #Attempts a
JOIN #Students s ON s.Seq = a.Seq
JOIN #Periods p ON p.G = a.G
JOIN #Subjects sub ON sub.CareerIdx = s.CareerIdx AND sub.Term = a.Term AND sub.Slot = a.Slot;

INSERT INTO kardex (Id, StudentId, AsignatureId, PeriodId, Grade, IsPassed, Opportunity)
SELECT NEWID(), s.Id, sub.Id, p.Id, a.ExtraGrade, a.ExtraPassed, 1
FROM #Attempts a
JOIN #Students s ON s.Seq = a.Seq
JOIN #Periods p ON p.G = a.G
JOIN #Subjects sub ON sub.CareerIdx = s.CareerIdx AND sub.Term = a.Term AND sub.Slot = a.Slot
WHERE a.ExtraPassed IS NOT NULL;

COMMIT TRANSACTION;

-- 10) Summary
PRINT '=== Academic history loaded ===';

SELECT p.SchoolCycle + ' ' + p.SEP_Period AS period,
       COUNT(DISTINCT r.StudentId) AS enrolled,
       COUNT(DISTINCT CASE WHEN r.FinalStatus IN (1, 2) THEN r.StudentId END) AS withdrawn,
       COUNT(DISTINCT CASE WHEN r.FinalStatus = 3 THEN r.StudentId END) AS graduated
FROM periods p
JOIN assignatureRegistrations r ON r.PeriodId = p.Id
JOIN students s ON s.Id = r.StudentId AND s.EnrollmentNumber LIKE 'UPH%'
GROUP BY p.SchoolCycle, p.SEP_Period, p.StartDate
ORDER BY p.StartDate;

SELECT COUNT(*) AS students,
       SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) AS active,
       SUM(CASE WHEN Status IN (1, 2) THEN 1 ELSE 0 END) AS withdrawn,
       SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) AS graduated
FROM students
WHERE EnrollmentNumber LIKE 'UPH%';

SELECT CAST(100.0 * SUM(CASE WHEN k.IsPassed = 1 THEN 1 ELSE 0 END) / NULLIF(COUNT(*), 0) AS DECIMAL(5, 2)) AS pass_rate,
       COUNT(*) AS kardex_rows
FROM kardex k
JOIN students s ON s.Id = k.StudentId
WHERE s.EnrollmentNumber LIKE 'UPH%';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
