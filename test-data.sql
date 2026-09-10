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
        (Id, EmployeeNumber, FullName, Email, Rfc, Curp, Gender, BirthDate, HighestEducationLevel, TitleStatus, HasDisability, ExternalXPYears, FirstHiredDate)
    VALUES
        (@AdminEmployeeId, 1001, 'Administrador del Sistema', 'admin@upzmg.local', 'ADMJ800101AA1', 'ADMJ800101HDFRRN01', 'H', '1980-01-01', 4, 0, 0, 12, '2010-01-15');
END;

BEGIN
    SET @TeacherEmployeeId = NEWID();

    INSERT INTO employees
        (Id, EmployeeNumber, FullName, Email, Rfc, Curp, Gender, BirthDate, HighestEducationLevel, TitleStatus, HasDisability, ExternalXPYears, FirstHiredDate)
    VALUES
        (@TeacherEmployeeId, 1002, 'Docente de Prueba', 'teacher@upzmg.local', 'DOCJ850202BB2', 'DOCJ850202HDFRRS02', 'M', '1985-02-02', 3, 0, 0, 8, '2016-08-01');
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
        (@CareerComputing, 'Ingeniería en Sistemas Computacionales', 'COMP-001', 4, 0, 0, 10, 5, 240, '2024-01-01', '2024-01-01'),
        (@CareerBiomedic, 'Ingeniería en Biotecnología', 'BIOM-001', 4, 0, 0, 10, 5, 240, '2024-01-01', '2024-01-01'),
        (@CareerIndustrial, 'Ingeniería Industrial', 'IND-001', 4, 0, 0, 10, 5, 240, '2024-01-01', '2024-01-01');
END;

-- 9) Periods for testing
DECLARE @Period2025A UNIQUEIDENTIFIER; SET @Period2025A = '22222222-2222-2222-2222-222222222201';
DECLARE @Period2025B UNIQUEIDENTIFIER; SET @Period2025B = '22222222-2222-2222-2222-222222222202';

BEGIN
    INSERT INTO periods
        (Id, IdCode, SEP_Period, StartDate, EndDate, IsActive)
    VALUES
        (@Period2025A, '2025-A', '2025-1', '2025-01-01', '2025-06-30', 1),
        (@Period2025B, '2025-B', '2025-2', '2025-07-01', '2025-12-31', 0);
END;

-- 10) Students for testing
DECLARE @StudentOneId UNIQUEIDENTIFIER; SET @StudentOneId = NEWID();
DECLARE @StudentTwoId UNIQUEIDENTIFIER; SET @StudentTwoId = NEWID();

BEGIN
    INSERT INTO students
        (Id, EnrollmentNumber, CURP, Name, MiddleName, LastName, BirthDate, Gender, CareerId, PeriodIdStart,
         IndigenousLanguages, PostalCode, EnglishLevel,
         MaritalStatus, NumberOfChildren, Works, BirthCountry, BirthState,
         Address)
    VALUES
        (@StudentOneId, '20250001', 'ALOC950101HDFLRA09', 'Andrea', 'Lucia', 'Lopez', '1995-01-01', 'M', @CareerComputing, @Period2025A, @Language1, '97000', 0, 0, 0, 0, 'México', 'Chiapas', 'Calle 60 #123, Mérida, Yucatán'),
        (@StudentTwoId, '20250002', 'ROGC960202HDFNTA10', 'Roberto', 'Carlos', 'García', '1996-02-02', 'H', @CareerIndustrial, @Period2025B, @Language2, '97010', 0, 0, 0, 1, 'México', 'Yucatán', 'Calle 20 #45, Valladolid, Yucatán');
END;

BEGIN
    INSERT INTO IndigenousLanguagesSpeakers (Id, SpeakerId, LanguageId)
    VALUES
        (NEWID(), @StudentOneId, @Language1),
        (NEWID(), @StudentTwoId, @Language2);
END;