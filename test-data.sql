-- =========================================
-- Create roles + users + user-role relations
-- =========================================

--Email: admin@upzmg.local
--Password: --Admin123!
--Email: teacher@upzmg.local
--Password: --Teacher123!

-- NOTE:
-- The passwords are hashed using the default ASP.NET Core Identity password hasher.
-- Run:  dotnet run --project .\tests\CreatePasswordHash\CreatePasswordHash.csproj -- {PASSWORD}  to generate a new password hash.

DECLARE @AdminPasswordHash  NVARCHAR(200) = 'AQAAAAIAAYagAAAAELFEBuqkjC8mw6wGU/ZOPvIl/gtdxfihT8R93uN65idh4ZUs24vObHCaa/a+fNJ8jg==';
DECLARE @TeacherPasswordHash NVARCHAR(200) = 'AQAAAAIAAYagAAAAEM46AAHF8ktFbnKXnMtgdZQe2yO+QlEi4ymS1gdR7EQ5Xxltt06oz5n84Wdv2wCD+w==';

DECLARE @AdminRoleId   UNIQUEIDENTIFIER;  
DECLARE @TeacherRoleId UNIQUEIDENTIFIER;

DECLARE @AdminEmployeeId   UNIQUEIDENTIFIER;
DECLARE @TeacherEmployeeId UNIQUEIDENTIFIER;

DECLARE @AdminUserId   UNIQUEIDENTIFIER;
DECLARE @TeacherUserId UNIQUEIDENTIFIER;

DECLARE @FullAccessPermissionId    UNIQUEIDENTIFIER;
DECLARE @ServiceAccessPermissionId UNIQUEIDENTIFIER;

-- 1) Roles
BEGIN
    INSERT INTO roles (Id, Name)
    VALUES (NEWID(), 'Admin');
END;

BEGIN
    INSERT INTO roles (Id, Name)
    VALUES (NEWID(), 'Teacher');
END;

SELECT @AdminRoleId = Id   FROM roles WHERE Name = 'Admin';
SELECT @TeacherRoleId = Id FROM roles WHERE Name = 'Teacher';

-- 2) Employees
BEGIN
    SET @AdminEmployeeId = NEWID();

    INSERT INTO employees
        (Id, EmployeeNumber, FullName, Email, Rfc, Curp, Gender, BirthDate, HighestEducationLevel, TitleStatus, HasDisability, ExternalXPYears, FirstHiredDate, IsTeacher)
    VALUES
        (@AdminEmployeeId, 1001, 'Administrador del Sistema', 'admin@upzmg.local', 'ADMJ800101AA1', 'ADMJ800101HDFRRN01', 'H', '1980-01-01', 4, 0, 0, 12, '2010-01-15', 0);
END;

BEGIN
    SET @TeacherEmployeeId = NEWID();

    INSERT INTO employees
        (Id, EmployeeNumber, FullName, Email, Rfc, Curp, Gender, BirthDate, HighestEducationLevel, TitleStatus, HasDisability, ExternalXPYears, FirstHiredDate, IsTeacher)
    VALUES
        (@TeacherEmployeeId, 1002, 'Docente de Prueba', 'teacher@upzmg.local', 'DOCJ850202BB2', 'DOCJ850202HDFRRS02', 'M', '1985-02-02', 3, 0, 0, 8, '2016-08-01', 1);
END;

-- 3) System users
BEGIN
    SET @AdminUserId = NEWID();

    INSERT INTO systemUser (Id, EmployeeId, UserName, Email, Password, IsActive, LastLogin)
    VALUES (@AdminUserId, @AdminEmployeeId, 'Admin', 'admin@upzmg.local', @AdminPasswordHash, 1, GETUTCDATE());
END

BEGIN
    SET @TeacherUserId = NEWID();

    INSERT INTO systemUser (Id, EmployeeId, UserName, Email, Password, IsActive, LastLogin)
    VALUES (@TeacherUserId, @TeacherEmployeeId, 'Teacher', 'teacher@upzmg.local', @TeacherPasswordHash, 1, GETUTCDATE());
END

-- 4) UserRole relations
BEGIN
    INSERT INTO userRoles (Id, UserId, RoleId)
    VALUES (NEWID(), @AdminUserId, @AdminRoleId);
END;

BEGIN
    INSERT INTO userRoles (Id, UserId, RoleId)
    VALUES (NEWID(), @TeacherUserId, @TeacherRoleId);
END;

-- 5) Permissions
BEGIN
    SET @FullAccessPermissionId = NEWID();

    INSERT INTO permissions (Id, Name)
    VALUES (@FullAccessPermissionId, 'FullAccess');
END;

BEGIN
    SET @ServiceAccessPermissionId = NEWID();

    INSERT INTO permissions (Id, Name)
    VALUES (@ServiceAccessPermissionId, 'ServiceAccess');
END;

SELECT @FullAccessPermissionId = Id    FROM permissions WHERE Name = 'FullAccess';
SELECT @ServiceAccessPermissionId = Id FROM permissions WHERE Name = 'ServiceAccess';

-- 6) RolePermission relations
BEGIN
    INSERT INTO rolePermissions (Id, RoleId, PermissionId)
    VALUES (NEWID(), @AdminRoleId, @FullAccessPermissionId);
END;

BEGIN
    INSERT INTO rolePermissions (Id, RoleId, PermissionId)
    VALUES (NEWID(), @TeacherRoleId, @ServiceAccessPermissionId);
END;

-- 7) Indigenous Languages
DECLARE @Language1 UNIQUEIDENTIFIER; SET @Language1 = NEWID();
DECLARE @Language2 UNIQUEIDENTIFIER; SET @Language2 = NEWID();
DECLARE @Language3 UNIQUEIDENTIFIER; SET @Language3 = NEWID();
DECLARE @Language4 UNIQUEIDENTIFIER; SET @Language4 = NEWID();
BEGIN
    INSERT INTO IndigenousLanguages (Id, Language)
    VALUES 
        (@Language1, 'Nahuatl'),
        (@Language2, 'Maya Yucateco'),
        (@Language3, 'Zapoteco'),
        (@Language4, 'Mixteco');
END;

-- 8) Careers
DECLARE @CareerComputing UNIQUEIDENTIFIER; SET @CareerComputing = '11111111-1111-1111-1111-111111111111';
DECLARE @CareerBiomedic UNIQUEIDENTIFIER; SET @CareerBiomedic = '11111111-1111-1111-1111-111111111112';
DECLARE @CareerIndustrial UNIQUEIDENTIFIER; SET @CareerIndustrial = '11111111-1111-1111-1111-111111111113';

BEGIN
    INSERT INTO careers
        (Id, Name, SEPCode, EducationalLevel, Modality, PeriodType, DurationInPeriods, DurationInYears, TotalCredits, PlanCreadtedDate, LastUpdatedDate)
    VALUES
        (@CareerComputing, 'Ingeniería en Sistemas Computacionales', 'COMP-001', 4, 0, 0, 15, 5, 240, '2024-01-01', '2024-01-01'),
        (@CareerBiomedic, 'Ingeniería en Biotecnología', 'BIOM-001', 4, 0, 0, 15, 5, 240, '2024-01-01', '2024-01-01'),
        (@CareerIndustrial, 'Ingeniería Industrial', 'IND-001', 4, 0, 0, 15, 5, 240, '2024-01-01', '2024-01-01');
END;

-- 9) Internal Areas
DECLARE @RectoryAreaId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333301';
DECLARE @AcademicAreaId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333302';
DECLARE @SchoolServicesAreaId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333303';
DECLARE @HumanResourcesAreaId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333304';
DECLARE @PlanningAreaId UNIQUEIDENTIFIER = '33333333-3333-3333-3333-333333333305';

BEGIN
    INSERT INTO internalAreas (Id, Name, ParentAreaId, Type)
    VALUES
        (@RectoryAreaId, 'Rectoría', NULL, 1),
        (@AcademicAreaId, 'Dirección Académica', @RectoryAreaId, 0),
        (@SchoolServicesAreaId, 'Servicios Escolares', @AcademicAreaId, 0),
        (@HumanResourcesAreaId, 'Recursos Humanos', @RectoryAreaId, 1),
        (@PlanningAreaId, 'Planeación', @RectoryAreaId, 1);
END;

    -- 10) Maximum Degrees
    BEGIN
        INSERT INTO maxDegrees (Id, Name)
        VALUES
        ('44444444-4444-4444-4444-444444444401', 'Bachillerato'),
        ('44444444-4444-4444-4444-444444444402', 'Técnico Superior Universitario'),
        ('44444444-4444-4444-4444-444444444403', 'Licenciatura'),
        ('44444444-4444-4444-4444-444444444404', 'Maestría'),
        ('44444444-4444-4444-4444-444444444405', 'Doctorado');
    END;

-- 11) Contract Types
BEGIN
    INSERT INTO contractTypes (Id, Name)
    VALUES
        ('55555555-5555-5555-5555-555555555501', 'Tiempo completo'),
        ('55555555-5555-5555-5555-555555555502', 'Medio tiempo'),
        ('55555555-5555-5555-5555-555555555503', 'Asignatura / Honorarios');
END;

-- 12) Periods for testing
DECLARE @Period2025A UNIQUEIDENTIFIER; SET @Period2025A = '22222222-2222-2222-2222-222222222201';
DECLARE @Period2025B UNIQUEIDENTIFIER; SET @Period2025B = '22222222-2222-2222-2222-222222222202';
DECLARE @Period2025C UNIQUEIDENTIFIER; SET @Period2025C = '22222222-2222-2222-2222-222222222203';

BEGIN
    INSERT INTO periods
        (Id, IdCode, SchoolCycle, SEP_Period, StartDate, EndDate, IsActive)
    VALUES
        (@Period2025A, '2026-EA', '2026', 'Ene-Abr', '2026-01-01', '2026-04-30', 1),
        (@Period2025B, '2026-MA', '2026', 'May-Ago', '2026-05-01', '2026-08-31', 0),
        (@Period2025C, '2026-SD', '2026', 'Sep-Dic', '2026-09-01', '2026-12-31', 0);
END;

-- 13) Class-group dependencies for testing
DECLARE @FacilityClassroom UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666666601';
DECLARE @FacilityLaboratory UNIQUEIDENTIFIER = '66666666-6666-6666-6666-666666666602';

BEGIN
    INSERT INTO SchoolFacility
        (Id, Name, Type, Capacity, InUse, HasInternet, HasComputers, SupportDisabilities, Problems)
    VALUES
        (@FacilityClassroom, 'Aula 101', 11, 35, 1, 1, 0, 1, '[]'),
        (@FacilityLaboratory, 'Laboratorio de Cómputo 1', 9, 30, 1, 1, 1, 1, '[]');
END;

DECLARE @AsignatureProgramming UNIQUEIDENTIFIER = '77777777-7777-7777-7777-777777777701';
DECLARE @AsignatureMathematics UNIQUEIDENTIFIER = '77777777-7777-7777-7777-777777777702';
DECLARE @AsignatureDatabases UNIQUEIDENTIFIER = '77777777-7777-7777-7777-777777777703';
DECLARE @AsignatureBiology UNIQUEIDENTIFIER = '77777777-7777-7777-7777-777777777704';
DECLARE @AsignaturePhysics UNIQUEIDENTIFIER = '77777777-7777-7777-7777-777777777705';
DECLARE @AsignatureIndustrialSafety UNIQUEIDENTIFIER = '77777777-7777-7777-7777-777777777706';

BEGIN
    INSERT INTO asignatures
        (Id, CareerId, AsignatureCode, AsignatureName, Credits, Cuatrimestre, IsMandatory)
    VALUES
        (@AsignatureProgramming, @CareerComputing, '88888888-8888-8888-8888-888888888701', 'Programación Orientada a Objetos', 8, 3, 1),
        (@AsignatureDatabases, @CareerComputing, '88888888-8888-8888-8888-888888888703', 'Bases de Datos', 8, 4, 1),
        (@AsignatureBiology, @CareerBiomedic, '88888888-8888-8888-8888-888888888704', 'Biología Celular', 8, 1, 1),
        (@AsignaturePhysics, @CareerBiomedic, '88888888-8888-8888-8888-888888888705', 'Física Aplicada', 8, 2, 1),
        (@AsignatureMathematics, @CareerIndustrial, '88888888-8888-8888-8888-888888888702', 'Cálculo Diferencial', 8, 1, 1),
        (@AsignatureIndustrialSafety, @CareerIndustrial, '88888888-8888-8888-8888-888888888706', 'Seguridad Industrial', 8, 2, 1);
END;

DECLARE @ClassGroupProgramming UNIQUEIDENTIFIER = '99999999-9999-9999-9999-999999999701';
DECLARE @ClassGroupMathematics UNIQUEIDENTIFIER = '99999999-9999-9999-9999-999999999702';

BEGIN
    INSERT INTO classGroups
        (Id, FacilityId, Schedule, Capacity, Enrolled, Weekhours,
         PeriodId, CareerId, TeacherId, AsignatureId, IsTheory)
    VALUES
        (@ClassGroupProgramming, @FacilityLaboratory,
         '[{"Day":"Lunes y miércoles","Time":"11:00-13:00 hrs"}]',
         30, 1, 4, @Period2025A, @CareerComputing, @TeacherEmployeeId, @AsignatureProgramming, 0),
        (@ClassGroupMathematics, @FacilityClassroom,
         '[{"Day":"Martes y jueves","Time":"09:00-11:00 hrs"}]',
         35, 1, 4, @Period2025A, @CareerIndustrial, @TeacherEmployeeId, @AsignatureMathematics, 1);
END;

-- 14) Students for testing
DECLARE @StudentOneId UNIQUEIDENTIFIER; SET @StudentOneId = NEWID();
DECLARE @StudentTwoId UNIQUEIDENTIFIER; SET @StudentTwoId = NEWID();

BEGIN
    INSERT INTO students
        (Id, EnrollmentNumber, CURP, Name, MiddleName, LastName, BirthDate, Gender, CareerId, PeriodIdStart,
         InscriptionType, Status,
         IndigenousLanguages, PostalCode, EnglishLevel,
         MaritalStatus, NumberOfChildren, Works, BirthCountry, BirthState,
         Address)
    VALUES
        (@StudentOneId, '20250001', 'ALOC950101HDFLRA09', 'Andrea', 'Lucia', 'Lopez', '1995-01-01', 'M', @CareerComputing, @Period2025A, 0, 0, @Language1, '97000', 0, 0, 0, 0, 'México', 'Chiapas', 'Calle 60 #123, Mérida, Yucatán'),
        (@StudentTwoId, '20250002', 'ROGC960202HDFNTA10', 'Roberto', 'Carlos', 'García', '1996-02-02', 'H', @CareerIndustrial, @Period2025B, 0, 0, @Language2, '97010', 0, 0, 0, 1, 'México', 'Yucatán', 'Calle 20 #45, Valladolid, Yucatán');
END;

BEGIN
    INSERT INTO IndigenousLanguagesSpeakers (Id, SpeakerId, LanguageId)
    VALUES
        (NEWID(), @StudentOneId, @Language1),
        (NEWID(), @StudentTwoId, @Language2);
END;