/* ============================================================================
   SIPAU — official SEP / SII catalogues
   ----------------------------------------------------------------------------
   These lists are dictated by the federal 911 and the state SII formats. The
   report templates read them with "rowsFrom" / "optionsFrom", so each list
   lives once, here, instead of being retyped in every JSON file.

   Run it after the migrations. sqlcmd needs -I, same as test-data.sql.
   Safe to re-run: a row is skipped when its id or its text already exists.

   Ids are sequential on purpose: SQL Server sorts uniqueidentifier by its last
   group first, so ordering by Id keeps each list in its official order.
   Undo: see the commented block at the end.
   ============================================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRANSACTION;

-- 1) Disabilities: the eight types broken down by the 911.9A questionnaire (section VI.4).
INSERT INTO Disabilities (Id, Name)
SELECT CAST(s.Id AS UNIQUEIDENTIFIER), s.Name
FROM (VALUES
    ('77777777-0000-0000-0000-000000000001', N'Física / motriz'),
    ('77777777-0000-0000-0000-000000000002', N'Intelectual'),
    ('77777777-0000-0000-0000-000000000003', N'Múltiple'),
    ('77777777-0000-0000-0000-000000000004', N'Auditiva: Hipoacusia'),
    ('77777777-0000-0000-0000-000000000005', N'Auditiva: Sordera'),
    ('77777777-0000-0000-0000-000000000006', N'Visual: Baja visión'),
    ('77777777-0000-0000-0000-000000000007', N'Visual: Ceguera'),
    ('77777777-0000-0000-0000-000000000008', N'Psicosocial')
) AS s (Id, Name)
WHERE NOT EXISTS (
    SELECT 1 FROM Disabilities d
    WHERE d.Id = CAST(s.Id AS UNIQUEIDENTIFIER) OR d.Name = s.Name);

-- 2) Withdrawal reasons: SII "Causa de bajas", used for temporary and definitive leaves.
INSERT INTO WithdrawReason (Id, Description)
SELECT CAST(s.Id AS UNIQUEIDENTIFIER), s.Description
FROM (VALUES
    ('88888888-0000-0000-0000-000000000001', N'Causa desconocida'),
    ('88888888-0000-0000-0000-000000000002', N'Incumplimiento de expectativas'),
    ('88888888-0000-0000-0000-000000000003', N'Reprobación'),
    ('88888888-0000-0000-0000-000000000004', N'Problemas económicos'),
    ('88888888-0000-0000-0000-000000000005', N'Motivos personales'),
    ('88888888-0000-0000-0000-000000000006', N'Distancia de la Universidad'),
    ('88888888-0000-0000-0000-000000000007', N'Problemas de trabajo'),
    ('88888888-0000-0000-0000-000000000008', N'Cambio de institución'),
    ('88888888-0000-0000-0000-000000000009', N'Cambio de carrera'),
    ('88888888-0000-0000-0000-000000000010', N'Cambio de residencia'),
    ('88888888-0000-0000-0000-000000000011', N'Faltas al reglamento escolar'),
    ('88888888-0000-0000-0000-000000000012', N'Bajo rendimiento académico'),
    ('88888888-0000-0000-0000-000000000013', N'Salud'),
    ('88888888-0000-0000-0000-000000000014', N'Defunción'),
    ('88888888-0000-0000-0000-000000000015', N'Inasistencias'),
    ('88888888-0000-0000-0000-000000000016', N'Embarazo'),
    ('88888888-0000-0000-0000-000000000017', N'Mala elección de carrera'),
    ('88888888-0000-0000-0000-000000000018', N'Violencia escolar')
) AS s (Id, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM WithdrawReason w
    WHERE w.Id = CAST(s.Id AS UNIQUEIDENTIFIER) OR w.Description = s.Description);

-- 3) Scholarship programmes: SII "Base de becas".
--    SourceType follows UPZMG.Shared FinantialSource: 0 Federal, 1 Estatal, 2 Propia, 3 Privada, 4 Internacional.
INSERT INTO ScolarshipProgram (Id, OfficialName, SourceType)
SELECT CAST(s.Id AS UNIQUEIDENTIFIER), s.OfficialName, s.SourceType
FROM (VALUES
    ('99999999-0000-0000-0000-000000000001', N'CNBES',                           0),
    ('99999999-0000-0000-0000-000000000002', N'CNBBBJ',                          0),
    ('99999999-0000-0000-0000-000000000003', N'Manutención',                     0),
    ('99999999-0000-0000-0000-000000000004', N'Jóvenes Construyendo el Futuro',  0),
    ('99999999-0000-0000-0000-000000000005', N'Jóvenes Escribiendo el Futuro',   0),
    ('99999999-0000-0000-0000-000000000006', N'Proyecta 100 Mil',                0),
    ('99999999-0000-0000-0000-000000000007', N'Otra del Gobierno Federal',       0),
    ('99999999-0000-0000-0000-000000000008', N'Gobierno Estatal',                1),
    ('99999999-0000-0000-0000-000000000009', N'Otra del Gobierno Estatal',       1),
    ('99999999-0000-0000-0000-000000000010', N'Colegiatura parcial o total',     2),
    ('99999999-0000-0000-0000-000000000011', N'Excelencia',                      2),
    ('99999999-0000-0000-0000-000000000012', N'Titulación',                      2),
    ('99999999-0000-0000-0000-000000000013', N'Investigación',                   2),
    ('99999999-0000-0000-0000-000000000014', N'Alimenticia',                     2),
    ('99999999-0000-0000-0000-000000000015', N'Transporte',                      2),
    ('99999999-0000-0000-0000-000000000016', N'Artística, deportiva y cultural', 2),
    ('99999999-0000-0000-0000-000000000017', N'Trabajo',                         2),
    ('99999999-0000-0000-0000-000000000018', N'Hospedaje',                       2),
    ('99999999-0000-0000-0000-000000000019', N'Movilidad nacional',              2),
    ('99999999-0000-0000-0000-000000000020', N'Movilidad internacional',         2),
    ('99999999-0000-0000-0000-000000000021', N'Bécalos',                         3),
    ('99999999-0000-0000-0000-000000000022', N'Otra de ente privado',            3),
    ('99999999-0000-0000-0000-000000000023', N'Beca internacional',              4)
) AS s (Id, OfficialName, SourceType)
WHERE NOT EXISTS (
    SELECT 1 FROM ScolarshipProgram p
    WHERE p.Id = CAST(s.Id AS UNIQUEIDENTIFIER) OR p.OfficialName = s.OfficialName);

COMMIT TRANSACTION;

SELECT
    (SELECT COUNT(*) FROM Disabilities)      AS disabilities,
    (SELECT COUNT(*) FROM WithdrawReason)    AS withdraw_reasons,
    (SELECT COUNT(*) FROM ScolarshipProgram) AS scholarship_programs;

/* Undo (removes only the rows this script adds):
DELETE FROM Disabilities      WHERE CONVERT(CHAR(36), Id) LIKE '77777777-0000-0000-0000-%';
DELETE FROM WithdrawReason    WHERE CONVERT(CHAR(36), Id) LIKE '88888888-0000-0000-0000-%';
DELETE FROM ScolarshipProgram WHERE CONVERT(CHAR(36), Id) LIKE '99999999-0000-0000-0000-%';
*/
