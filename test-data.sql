-- =========================================
-- Create roles + users + user-role relations
-- =========================================

--Email: admin@upzmg.local
--Password: --Admin123!
--Email: docente_01@upzmg.local
--Password: --Teacher123!
--Email: control.escolar@upzmg.local / servicios.estudiantiles@upzmg.local / recursos.humanos@upzmg.local / recursos.materiales@upzmg.local
--Password: --User123!

-- NOTE:
-- The passwords are hashed using the default ASP.NET Core Identity password hasher.
-- Run:  dotnet run --project .\tests\CreatePasswordHash\CreatePasswordHash.csproj -- {PASSWORD}  to generate a new password hash.

DECLARE @AdminPasswordHash  NVARCHAR(200) = 'AQAAAAIAAYagAAAAELFEBuqkjC8mw6wGU/ZOPvIl/gtdxfihT8R93uN65idh4ZUs24vObHCaa/a+fNJ8jg==';
DECLARE @TeacherPasswordHash NVARCHAR(200) = 'AQAAAAIAAYagAAAAEM46AAHF8ktFbnKXnMtgdZQe2yO+QlEi4ymS1gdR7EQ5Xxltt06oz5n84Wdv2wCD+w==';
DECLARE @UserPasswordHash NVARCHAR(200) = 'AQAAAAIAAYagAAAAEN2rm3dyOJ5a9z7t8t8uA3xYli3upMY6sedID0sERHBMQK22V9Qj5XFcZDKKxTvDFw==';

DECLARE @AdminRoleId                UNIQUEIDENTIFIER;
DECLARE @ControlEscolarRoleId       UNIQUEIDENTIFIER;
DECLARE @ServiciosEstudiantilesRoleId UNIQUEIDENTIFIER;
DECLARE @RecursosHumanosRoleId      UNIQUEIDENTIFIER;
DECLARE @DireccionAcademicaRoleId   UNIQUEIDENTIFIER;
DECLARE @RecursosMaterialesRoleId   UNIQUEIDENTIFIER;

DECLARE @AdminEmployeeId   UNIQUEIDENTIFIER;
DECLARE @TeacherEmployeeId UNIQUEIDENTIFIER;
DECLARE @TeacherTwoEmployeeId UNIQUEIDENTIFIER;
DECLARE @TeacherThreeEmployeeId UNIQUEIDENTIFIER;
DECLARE @TeacherFourEmployeeId UNIQUEIDENTIFIER;
DECLARE @ControlEscolarEmployeeId       UNIQUEIDENTIFIER;
DECLARE @ServiciosEstudiantilesEmployeeId UNIQUEIDENTIFIER;
DECLARE @RecursosHumanosEmployeeId  UNIQUEIDENTIFIER;
DECLARE @RecursosMaterialesEmployeeId UNIQUEIDENTIFIER;

DECLARE @AdminUserId   UNIQUEIDENTIFIER;
DECLARE @TeacherUserId UNIQUEIDENTIFIER;
DECLARE @ControlEscolarUserId       UNIQUEIDENTIFIER;
DECLARE @ServiciosEstudiantilesUserId UNIQUEIDENTIFIER;
DECLARE @RecursosHumanosUserId      UNIQUEIDENTIFIER;
DECLARE @RecursosMaterialesUserId   UNIQUEIDENTIFIER;

DECLARE @FullAccessPermissionId    UNIQUEIDENTIFIER;
DECLARE @ServiceAccessPermissionId UNIQUEIDENTIFIER;

-- 1) Roles (Admin kept as-is; the rest mirror the real organizational areas)
BEGIN
    INSERT INTO roles (Id, Name)
    VALUES (NEWID(), 'Admin');
END;

BEGIN
    INSERT INTO roles (Id, Name)
    VALUES
        (NEWID(), 'Control Escolar'),
        (NEWID(), 'Servicios Estudiantiles'),
        (NEWID(), 'Recursos Humanos'),
        (NEWID(), 'Direccion Academica'),
        (NEWID(), 'Recursos Materiales y Servicios Generales');
END;

SELECT @AdminRoleId = Id                  FROM roles WHERE Name = 'Admin';
SELECT @ControlEscolarRoleId = Id         FROM roles WHERE Name = 'Control Escolar';
SELECT @ServiciosEstudiantilesRoleId = Id FROM roles WHERE Name = 'Servicios Estudiantiles';
SELECT @RecursosHumanosRoleId = Id        FROM roles WHERE Name = 'Recursos Humanos';
SELECT @DireccionAcademicaRoleId = Id     FROM roles WHERE Name = 'Direccion Academica';
SELECT @RecursosMaterialesRoleId = Id     FROM roles WHERE Name = 'Recursos Materiales y Servicios Generales';

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
        (@TeacherEmployeeId, 2001, 'Mtra. Laura Mendoza Ruiz', 'docente_01@upzmg.local', 'MERL800312AB1', 'MERL800312MJCNZR01', 'M', '1980-03-12', 7, 0, 0, 8, '2015-08-17', 1);
END;

BEGIN
    SET @TeacherTwoEmployeeId = NEWID();

    INSERT INTO employees
        (Id, EmployeeNumber, FullName, Email, Rfc, Curp, Gender, BirthDate, HighestEducationLevel, TitleStatus, HasDisability, ExternalXPYears, FirstHiredDate, IsTeacher)
    VALUES
        (@TeacherTwoEmployeeId, 2002, 'Dr. Ricardo Salinas Ortega', 'docente_02@upzmg.local', 'SAOR750921CD2', 'SAOR750921HJCLRC02', 'H', '1975-09-21', 9, 0, 0, 12, '2012-01-09', 1);
END;

BEGIN
    SET @TeacherThreeEmployeeId = NEWID();

    INSERT INTO employees
        (Id, EmployeeNumber, FullName, Email, Rfc, Curp, Gender, BirthDate, HighestEducationLevel, TitleStatus, HasDisability, ExternalXPYears, FirstHiredDate, IsTeacher)
    VALUES
        (@TeacherThreeEmployeeId, 2003, 'Ing. Pablo Navarro Cruz', 'docente_03@upzmg.local', 'NACP880105EF3', 'NACP880105HJCVRB03', 'H', '1988-01-05', 4, 0, 0, 4, '2019-08-19', 1);
END;

BEGIN
    SET @TeacherFourEmployeeId = NEWID();

    INSERT INTO employees
        (Id, EmployeeNumber, FullName, Email, Rfc, Curp, Gender, BirthDate, HighestEducationLevel, TitleStatus, HasDisability, ExternalXPYears, FirstHiredDate, IsTeacher)
    VALUES
        (@TeacherFourEmployeeId, 2004, 'Mtra. Elena Castillo Vega', 'docente_04@upzmg.local', 'CAVE850614GH4', 'CAVE850614MJCSGL04', 'M', '1985-06-14', 7, 0, 0, 6, '2017-01-16', 1);
END;

BEGIN
    SET @ControlEscolarEmployeeId = NEWID();

    INSERT INTO employees
        (Id, EmployeeNumber, FullName, Email, Rfc, Curp, Gender, BirthDate, HighestEducationLevel, TitleStatus, HasDisability, ExternalXPYears, FirstHiredDate, IsTeacher)
    VALUES
        (@ControlEscolarEmployeeId, 1003, 'Encargado de Control Escolar', 'control.escolar@upzmg.local', 'CEOJ870303CC3', 'CEOJ870303MDFRRN03', 'M', '1987-03-03', 3, 0, 0, 6, '2018-03-01', 0);
END;

BEGIN
    SET @ServiciosEstudiantilesEmployeeId = NEWID();

    INSERT INTO employees
        (Id, EmployeeNumber, FullName, Email, Rfc, Curp, Gender, BirthDate, HighestEducationLevel, TitleStatus, HasDisability, ExternalXPYears, FirstHiredDate, IsTeacher)
    VALUES
        (@ServiciosEstudiantilesEmployeeId, 1004, 'Encargado de Servicios Estudiantiles', 'servicios.estudiantiles@upzmg.local', 'SEOJ880404DD4', 'SEOJ880404HDFRRN04', 'H', '1988-04-04', 3, 0, 0, 5, '2019-05-01', 0);
END;

BEGIN
    SET @RecursosHumanosEmployeeId = NEWID();

    INSERT INTO employees
        (Id, EmployeeNumber, FullName, Email, Rfc, Curp, Gender, BirthDate, HighestEducationLevel, TitleStatus, HasDisability, ExternalXPYears, FirstHiredDate, IsTeacher)
    VALUES
        (@RecursosHumanosEmployeeId, 1005, 'Encargado de Recursos Humanos', 'recursos.humanos@upzmg.local', 'RHOJ890505EE5', 'RHOJ890505MDFRRN05', 'M', '1989-05-05', 3, 0, 0, 7, '2017-02-01', 0);
END;

BEGIN
    SET @RecursosMaterialesEmployeeId = NEWID();

    INSERT INTO employees
        (Id, EmployeeNumber, FullName, Email, Rfc, Curp, Gender, BirthDate, HighestEducationLevel, TitleStatus, HasDisability, ExternalXPYears, FirstHiredDate, IsTeacher)
    VALUES
        (@RecursosMaterialesEmployeeId, 1006, 'Encargado de Recursos Materiales y Servicios Generales', 'recursos.materiales@upzmg.local', 'RMOJ900606FF6', 'RMOJ900606HDFRRN06', 'H', '1990-06-06', 3, 0, 0, 4, '2020-01-15', 0);
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
    VALUES (@TeacherUserId, @TeacherEmployeeId, 'LauraMendoza', 'docente_01@upzmg.local', @TeacherPasswordHash, 1, GETUTCDATE());
END

BEGIN
    SET @ControlEscolarUserId = NEWID();

    INSERT INTO systemUser (Id, EmployeeId, UserName, Email, Password, IsActive, LastLogin)
    VALUES (@ControlEscolarUserId, @ControlEscolarEmployeeId, 'ControlEscolar', 'control.escolar@upzmg.local', @UserPasswordHash, 1, GETUTCDATE());
END

BEGIN
    SET @ServiciosEstudiantilesUserId = NEWID();

    INSERT INTO systemUser (Id, EmployeeId, UserName, Email, Password, IsActive, LastLogin)
    VALUES (@ServiciosEstudiantilesUserId, @ServiciosEstudiantilesEmployeeId, 'ServiciosEstudiantiles', 'servicios.estudiantiles@upzmg.local', @UserPasswordHash, 1, GETUTCDATE());
END

BEGIN
    SET @RecursosHumanosUserId = NEWID();

    INSERT INTO systemUser (Id, EmployeeId, UserName, Email, Password, IsActive, LastLogin)
    VALUES (@RecursosHumanosUserId, @RecursosHumanosEmployeeId, 'RecursosHumanos', 'recursos.humanos@upzmg.local', @UserPasswordHash, 1, GETUTCDATE());
END

BEGIN
    SET @RecursosMaterialesUserId = NEWID();

    INSERT INTO systemUser (Id, EmployeeId, UserName, Email, Password, IsActive, LastLogin)
    VALUES (@RecursosMaterialesUserId, @RecursosMaterialesEmployeeId, 'RecursosMateriales', 'recursos.materiales@upzmg.local', @UserPasswordHash, 1, GETUTCDATE());
END

-- 4) UserRole relations
BEGIN
    INSERT INTO userRoles (Id, UserId, RoleId)
    VALUES (NEWID(), @AdminUserId, @AdminRoleId);
END;

-- "Mtra. Laura Mendoza Ruiz" reports under Dirección Académica.
BEGIN
    INSERT INTO userRoles (Id, UserId, RoleId)
    VALUES (NEWID(), @TeacherUserId, @DireccionAcademicaRoleId);
END;

BEGIN
    INSERT INTO userRoles (Id, UserId, RoleId)
    VALUES
        (NEWID(), @ControlEscolarUserId, @ControlEscolarRoleId),
        (NEWID(), @ServiciosEstudiantilesUserId, @ServiciosEstudiantilesRoleId),
        (NEWID(), @RecursosHumanosUserId, @RecursosHumanosRoleId),
        (NEWID(), @RecursosMaterialesUserId, @RecursosMaterialesRoleId);
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
    VALUES
        (NEWID(), @ControlEscolarRoleId, @ServiceAccessPermissionId),
        (NEWID(), @ServiciosEstudiantilesRoleId, @ServiceAccessPermissionId),
        (NEWID(), @RecursosHumanosRoleId, @ServiceAccessPermissionId),
        (NEWID(), @DireccionAcademicaRoleId, @ServiceAccessPermissionId),
        (NEWID(), @RecursosMaterialesRoleId, @ServiceAccessPermissionId);
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
DECLARE @CareerRenewableEnergy UNIQUEIDENTIFIER; SET @CareerRenewableEnergy = '11111111-1111-1111-1111-111111111114';

BEGIN
    INSERT INTO careers
        (Id, Alias, Name, SEPCode, EducationalLevel, Modality, PeriodType, DurationInPeriods, DurationInYears, TotalCredits, PlanCreadtedDate, LastUpdatedDate)
    VALUES
        (@CareerComputing, 'INCO', 'Ingeniería en Sistemas Computacionales', 'COMP-001', 4, 0, 0, 15, 5, 240, '2024-01-01', '2024-01-01'),
        (@CareerBiomedic, 'IBT', 'Ingeniería en Biotecnología', 'BIOM-001', 4, 0, 0, 15, 5, 240, '2024-01-01', '2024-01-01'),
        (@CareerIndustrial, 'IIND', 'Ingeniería Industrial', 'IND-001', 4, 0, 0, 15, 5, 240, '2024-01-01', '2024-01-01'),
        (@CareerRenewableEnergy, 'IER', 'Ingeniería en Energías Renovables', 'ENER-001', 4, 0, 0, 15, 5, 240, '2024-01-01', '2024-01-01');
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
DECLARE @ClassGroupBiology UNIQUEIDENTIFIER = '99999999-9999-9999-9999-999999999703';
DECLARE @ClassGroupPhysics UNIQUEIDENTIFIER = '99999999-9999-9999-9999-999999999704';
DECLARE @ClassGroupDatabases UNIQUEIDENTIFIER = '99999999-9999-9999-9999-999999999705';
DECLARE @ClassGroupIndustrialSafety UNIQUEIDENTIFIER = '99999999-9999-9999-9999-999999999706';

BEGIN
    INSERT INTO classGroups
        (Id, FacilityId, Schedule, Capacity, Enrolled, Weekhours,
         PeriodId, CareerId, TeacherId, AsignatureId, IsTheory)
    VALUES
        (@ClassGroupProgramming, @FacilityLaboratory,
         '[{"Day":"Lunes y miércoles","Time":"11:00-13:00 hrs"}]',
         30, 2, 4, @Period2025A, @CareerComputing, @TeacherEmployeeId, @AsignatureProgramming, 0),
        (@ClassGroupMathematics, @FacilityClassroom,
         '[{"Day":"Martes y jueves","Time":"09:00-11:00 hrs"}]',
         35, 2, 4, @Period2025A, @CareerIndustrial, @TeacherTwoEmployeeId, @AsignatureMathematics, 1),
        (@ClassGroupBiology, @FacilityLaboratory,
         '[{"Day":"Lunes y miércoles","Time":"13:00-15:00 hrs"}]',
         30, 2, 4, @Period2025A, @CareerBiomedic, @TeacherThreeEmployeeId, @AsignatureBiology, 0),
        (@ClassGroupPhysics, @FacilityClassroom,
         '[{"Day":"Martes y jueves","Time":"11:00-13:00 hrs"}]',
         35, 2, 4, @Period2025A, @CareerBiomedic, @TeacherThreeEmployeeId, @AsignaturePhysics, 1),
        (@ClassGroupDatabases, @FacilityLaboratory,
         '[{"Day":"Viernes","Time":"09:00-13:00 hrs"}]',
         30, 2, 4, @Period2025A, @CareerComputing, @TeacherFourEmployeeId, @AsignatureDatabases, 0),
        (@ClassGroupIndustrialSafety, @FacilityClassroom,
         '[{"Day":"Viernes","Time":"11:00-13:00 hrs"}]',
         35, 2, 2, @Period2025A, @CareerIndustrial, @TeacherTwoEmployeeId, @AsignatureIndustrialSafety, 1);
END;

-- 14) Students for testing
DECLARE @StudentOneId UNIQUEIDENTIFIER; SET @StudentOneId = NEWID();
DECLARE @StudentTwoId UNIQUEIDENTIFIER; SET @StudentTwoId = NEWID();
DECLARE @StudentThreeId UNIQUEIDENTIFIER; SET @StudentThreeId = NEWID();
DECLARE @StudentFourId UNIQUEIDENTIFIER; SET @StudentFourId = NEWID();
DECLARE @StudentFiveId UNIQUEIDENTIFIER; SET @StudentFiveId = NEWID();
DECLARE @StudentSixId UNIQUEIDENTIFIER; SET @StudentSixId = NEWID();
DECLARE @StudentSevenId UNIQUEIDENTIFIER; SET @StudentSevenId = NEWID();
DECLARE @StudentEightId UNIQUEIDENTIFIER; SET @StudentEightId = NEWID();
DECLARE @StudentNineId UNIQUEIDENTIFIER; SET @StudentNineId = NEWID();
DECLARE @StudentTenId UNIQUEIDENTIFIER; SET @StudentTenId = NEWID();

BEGIN
    INSERT INTO students
        (Id, EnrollmentNumber, CURP, Name, MiddleName, LastName, BirthDate, Gender, CareerId, PeriodIdStart,
         InscriptionType, Status,
         IndigenousLanguages, PostalCode, EnglishLevel,
         MaritalStatus, NumberOfChildren, Works, BirthCountry, BirthState,
         Address)
    VALUES
        (@StudentOneId, '20250001', 'ALOC950101HDFLRA09', 'Andrea', 'Lucia', 'Lopez', '1995-01-01', 'M', @CareerComputing, @Period2025A, 0, 0, @Language1, '97000', 0, 0, 0, 0, 'México', 'Chiapas', 'Calle 60 #123, Mérida, Yucatán'),
        (@StudentTwoId, '20250002', 'ROGC960202HDFNTA10', 'Roberto', 'Carlos', 'García', '1996-02-02', 'H', @CareerIndustrial, @Period2025B, 0, 0, @Language2, '97010', 0, 0, 0, 1, 'México', 'Yucatán', 'Calle 20 #45, Valladolid, Yucatán'),
        (@StudentThreeId, '20250003', 'ANRM970303MDFLRA11', 'Ana', 'Rosa', 'Martinez', '1997-03-03', 'M', @CareerBiomedic, @Period2025A, 0, 0, @Language3, '97020', 1, 0, 0, 0, 'Mexico', 'Jalisco', 'Calle 10 #12, Guadalajara'),
        (@StudentFourId, '20250004', 'JPLM980404HDFNTA12', 'Juan', 'Pablo', 'Lopez', '1998-04-04', 'H', @CareerComputing, @Period2025A, 0, 0, @Language4, '97030', 2, 0, 0, 1, 'Mexico', 'Jalisco', 'Calle 11 #13, Zapopan'),
        (@StudentFiveId, '20250005', 'MCRS990505MDFLRA13', 'Maria', 'Cristina', 'Ramos', '1999-05-05', 'M', @CareerIndustrial, @Period2025A, 0, 0, @Language1, '97040', 0, 0, 0, 0, 'Mexico', 'Puebla', 'Calle 12 #14, Puebla'),
        (@StudentSixId, '20250006', 'DAGS000606HDFNTA14', 'Diego', 'Alberto', 'Gomez', '2000-06-06', 'H', @CareerBiomedic, @Period2025A, 0, 0, @Language2, '97050', 1, 0, 0, 1, 'Mexico', 'Puebla', 'Calle 13 #15, Puebla'),
        (@StudentSevenId, '20250007', 'LFHS010707MDFLRA15', 'Laura', 'Fernanda', 'Hernandez', '2001-07-07', 'M', @CareerComputing, @Period2025A, 0, 0, @Language3, '97060', 2, 0, 0, 0, 'Mexico', 'Oaxaca', 'Calle 14 #16, Oaxaca'),
        (@StudentEightId, '20250008', 'MTRJ020808HDFNTA16', 'Mateo', 'Rafael', 'Torres', '2002-08-08', 'H', @CareerIndustrial, @Period2025A, 0, 0, @Language4, '97070', 0, 0, 0, 0, 'Mexico', 'Oaxaca', 'Calle 15 #17, Oaxaca'),
        (@StudentNineId, '20250009', 'SVPM030909MDFLRA17', 'Sofia', 'Valeria', 'Perez', '2003-09-09', 'M', @CareerBiomedic, @Period2025A, 0, 0, @Language1, '97080', 1, 0, 0, 1, 'Mexico', 'Sonora', 'Calle 16 #18, Hermosillo'),
        (@StudentTenId, '20250010', 'EMCL040410HDFNTA18', 'Emilio', 'Manuel', 'Castillo', '2004-10-10', 'H', @CareerComputing, @Period2025A, 0, 0, @Language2, '97090', 2, 0, 0, 0, 'Mexico', 'Sonora', 'Calle 17 #19, Hermosillo');
END;

BEGIN
    INSERT INTO IndigenousLanguagesSpeakers (Id, SpeakerId, LanguageId)
    VALUES
        (NEWID(), @StudentOneId, @Language1),
        (NEWID(), @StudentTwoId, @Language2);
END;

-- 15) Asignature registrations for testing
DECLARE @NoWithdrawReason UNIQUEIDENTIFIER = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
DECLARE @ClassGroupProgrammingB UNIQUEIDENTIFIER = '99999999-9999-9999-9999-999999999801';
DECLARE @ClassGroupBiologyB UNIQUEIDENTIFIER = '99999999-9999-9999-9999-999999999802';
DECLARE @ClassGroupMathematicsB UNIQUEIDENTIFIER = '99999999-9999-9999-9999-999999999803';
DECLARE @ClassGroupProgrammingC UNIQUEIDENTIFIER = '99999999-9999-9999-9999-999999999901';
DECLARE @ClassGroupBiologyC UNIQUEIDENTIFIER = '99999999-9999-9999-9999-999999999902';
DECLARE @ClassGroupIndustrialSafetyC UNIQUEIDENTIFIER = '99999999-9999-9999-9999-999999999903';

BEGIN
    INSERT INTO WithdrawReason (Id, Description)
    VALUES (@NoWithdrawReason, 'Sin baja');
END;

-- Groups must belong to the same period as their registrations.
BEGIN
    INSERT INTO classGroups
        (Id, FacilityId, Schedule, Capacity, Enrolled, Weekhours,
         PeriodId, CareerId, TeacherId, AsignatureId, IsTheory)
    VALUES
        (@ClassGroupProgrammingB, @FacilityLaboratory,
         '[{"Day":"Lunes y miércoles","Time":"11:00-13:00 hrs"}]',
         30, 3, 4, @Period2025B, @CareerComputing, @TeacherEmployeeId, @AsignatureProgramming, 0),
        (@ClassGroupBiologyB, @FacilityLaboratory,
         '[{"Day":"Martes y jueves","Time":"13:00-15:00 hrs"}]',
         30, 3, 4, @Period2025B, @CareerBiomedic, @TeacherThreeEmployeeId, @AsignatureBiology, 0),
        (@ClassGroupMathematicsB, @FacilityClassroom,
         '[{"Day":"Viernes","Time":"09:00-13:00 hrs"}]',
         35, 3, 4, @Period2025B, @CareerIndustrial, @TeacherTwoEmployeeId, @AsignatureMathematics, 1),
        (@ClassGroupProgrammingC, @FacilityLaboratory,
         '[{"Day":"Lunes y miércoles","Time":"11:00-13:00 hrs"}]',
         30, 4, 4, @Period2025C, @CareerComputing, @TeacherEmployeeId, @AsignatureProgramming, 0),
        (@ClassGroupBiologyC, @FacilityLaboratory,
         '[{"Day":"Martes y jueves","Time":"13:00-15:00 hrs"}]',
         30, 4, 4, @Period2025C, @CareerBiomedic, @TeacherThreeEmployeeId, @AsignatureBiology, 0),
        (@ClassGroupIndustrialSafetyC, @FacilityClassroom,
         '[{"Day":"Viernes","Time":"11:00-13:00 hrs"}]',
         35, 4, 2, @Period2025C, @CareerIndustrial, @TeacherTwoEmployeeId, @AsignatureIndustrialSafety, 1);
END;

BEGIN
    INSERT INTO assignatureRegistrations
        (Id, StudentId, GroupId, PeriodId, InscriptionType, FinalStatus, WithdrawReasonId)
    VALUES
        (NEWID(), @StudentOneId, @ClassGroupProgramming, @Period2025A, 0, 0, @NoWithdrawReason),
        (NEWID(), @StudentOneId, @ClassGroupDatabases, @Period2025A, 0, 0, @NoWithdrawReason),
        (NEWID(), @StudentTwoId, @ClassGroupMathematics, @Period2025A, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentTwoId, @ClassGroupIndustrialSafety, @Period2025A, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentThreeId, @ClassGroupBiology, @Period2025A, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentThreeId, @ClassGroupPhysics, @Period2025A, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentFourId, @ClassGroupProgramming, @Period2025A, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentFourId, @ClassGroupDatabases, @Period2025A, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentFiveId, @ClassGroupMathematics, @Period2025A, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentFiveId, @ClassGroupIndustrialSafety, @Period2025A, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentSixId, @ClassGroupBiology, @Period2025A, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentSixId, @ClassGroupPhysics, @Period2025A, 1, 0, @NoWithdrawReason),

        -- May-Ago: 6 distinct students.
        (NEWID(), @StudentOneId, @ClassGroupProgrammingB, @Period2025B, 0, 0, @NoWithdrawReason),
        (NEWID(), @StudentTwoId, @ClassGroupMathematicsB, @Period2025B, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentThreeId, @ClassGroupBiologyB, @Period2025B, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentFourId, @ClassGroupProgrammingB, @Period2025B, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentFiveId, @ClassGroupMathematicsB, @Period2025B, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentSixId, @ClassGroupBiologyB, @Period2025B, 1, 0, @NoWithdrawReason),

        -- Sep-Dic: 8 distinct students, providing a third observed point.
        (NEWID(), @StudentOneId, @ClassGroupProgrammingC, @Period2025C, 0, 0, @NoWithdrawReason),
        (NEWID(), @StudentTwoId, @ClassGroupIndustrialSafetyC, @Period2025C, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentThreeId, @ClassGroupBiologyC, @Period2025C, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentFourId, @ClassGroupProgrammingC, @Period2025C, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentFiveId, @ClassGroupIndustrialSafetyC, @Period2025C, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentSixId, @ClassGroupBiologyC, @Period2025C, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentSevenId, @ClassGroupProgrammingC, @Period2025C, 1, 0, @NoWithdrawReason),
        (NEWID(), @StudentEightId, @ClassGroupIndustrialSafetyC, @Period2025C, 1, 0, @NoWithdrawReason);
END;