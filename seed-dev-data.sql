/* ============================================================================
   SIGIE — datos de desarrollo
   ----------------------------------------------------------------------------
   Crea lo mínimo para poder entrar al sistema y probar el módulo de Usuarios
   y el de Seguridad después de recrear la base de datos.

   Requiere que la migración ya esté aplicada:
       dotnet ef database update --project UPZMG.Persistence --startup-project UPZMG.API

   Ejecutar:
       sqlcmd -S localhost,1433 -U dev_user -P <password> -d upzmg -i seed-dev-data.sql

   CREDENCIALES QUE DEJA LISTAS
       admin@upzmg.edu.mx      Admin1234!     (rol Admin)
       test@upzmg.local        Test1234!      (rol Planeación)

   Los hashes son formato ASP.NET Core Identity (PasswordHasher V3), los mismos
   que verifica AccountController al iniciar sesión.

   Es idempotente, es decir, se puede volver a correr sin duplicar filas.
   ============================================================================ */

SET NOCOUNT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    /* ---------------------------------------------------------------- Roles */
    DECLARE @RoleAdmin        UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111101';
    DECLARE @RolePlaneacion   UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111102';
    DECLARE @RoleEscolares    UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111103';
    DECLARE @RoleRRHH         UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111104';
    DECLARE @RoleFinanzas     UNIQUEIDENTIFIER = '11111111-1111-1111-1111-111111111105';

    MERGE roles AS t
    USING (VALUES
        (@RoleAdmin,      N'Admin'),
        (@RolePlaneacion, N'Planeación'),
        (@RoleEscolares,  N'Control Escolar'),
        (@RoleRRHH,       N'Recursos Humanos'),
        (@RoleFinanzas,   N'Finanzas')
    ) AS s (Id, Name) ON t.Id = s.Id
    WHEN MATCHED THEN UPDATE SET t.Name = s.Name
    WHEN NOT MATCHED THEN INSERT (Id, Name) VALUES (s.Id, s.Name);

    /* ----------------------------------------------------------- Empleados */
    /* CreateUserAsync busca un empleado por correo antes de dar de alta un
       usuario del sistema, así que el empleado debe existir primero.          */
    DECLARE @EmpAdmin UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222201';
    DECLARE @EmpTest  UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222202';
    DECLARE @EmpKarla UNIQUEIDENTIFIER = '22222222-2222-2222-2222-222222222203';

    MERGE employees AS t
    USING (VALUES
        (@EmpAdmin, 1001,
         N'Francisco Subdirección', N'admin@upzmg.edu.mx',
         N'FASU800101AB1', N'FASU800101HJCRBR01', 'H', '1980-01-01', 5, 1, 0, 10, '2018-08-01', 0),

        (@EmpTest,  1002,
         N'Usuario de Pruebas', N'test@upzmg.local',
         N'PRUE900202CD2', N'PRUE900202HJCRBS02', 'H', '1990-02-02', 4, 1, 0, 5, '2020-01-15', 0),

        (@EmpKarla, 1003,
         N'Karla Asistente', N'karla.asistente@upzmg.edu.mx',
         N'KAAS950303EF3', N'KAAS950303MJCRBT03', 'M', '1995-03-03', 4, 1, 0, 3, '2021-03-01', 0)
    ) AS s (Id, EmployeeNumber, FullName, Email, Rfc, Curp, Gender, BirthDate,
            HighestEducationLevel, TitleStatus,
            HasDisability, ExternalXPYears, FirstHiredDate, IsTeacher)
    ON t.Id = s.Id
    WHEN MATCHED THEN UPDATE SET
        t.FullName = s.FullName, t.Email = s.Email, t.IsTeacher = s.IsTeacher
    WHEN NOT MATCHED THEN INSERT
        (Id, EmployeeNumber, FullName, Email, Rfc, Curp, Gender, BirthDate,
         HighestEducationLevel, TitleStatus,
         HasDisability, ExternalXPYears, FirstHiredDate, IsTeacher)
        VALUES
        (s.Id, s.EmployeeNumber, s.FullName, s.Email, s.Rfc, s.Curp, s.Gender, s.BirthDate,
         s.HighestEducationLevel, s.TitleStatus,
         s.HasDisability, s.ExternalXPYears, s.FirstHiredDate, s.IsTeacher);

    /* ------------------------------------------------ Usuarios del sistema */
    DECLARE @UsrAdmin UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444401';
    DECLARE @UsrTest  UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444402';
    DECLARE @UsrKarla UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444403';

    /* Hashes ASP.NET Core Identity (PBKDF2-HMAC-SHA256). NO editar a mano:
       para cambiar una contraseña, hazlo desde la pantalla de Usuarios.        */
    DECLARE @HashAdmin NVARCHAR(256) = N'AQAAAAIAAYagAAAAENtzRV1DLimwzY8P5wMcGgqUd01eegvbUFEiYnuuFfIneuiP/F0ydREyOItJhz9XJw==';  -- Admin1234!
    DECLARE @HashTest  NVARCHAR(256) = N'AQAAAAIAAYagAAAAECFw3YSeNUlfBJY/+HGT5rAximQfph285o99EnpLpp5tuntWc8tjyFbly/I4NQ/seg==';  -- Test1234!

    MERGE systemUser AS t
    USING (VALUES
        (@UsrAdmin, @EmpAdmin, N'Francisco Subdirección', N'admin@upzmg.edu.mx', @HashAdmin, 1),
        (@UsrTest,  @EmpTest,  N'Usuario de Pruebas',     N'test@upzmg.local',   @HashTest,  1),
        (@UsrKarla, @EmpKarla, N'Karla Asistente',        N'karla.asistente@upzmg.edu.mx', @HashTest, 1)
    ) AS s (Id, EmployeeId, UserName, Email, Password, IsActive)
    ON t.Id = s.Id
    WHEN MATCHED THEN UPDATE SET
        t.UserName = s.UserName, t.Email = s.Email,
        t.Password = s.Password, t.IsActive = s.IsActive
    WHEN NOT MATCHED THEN INSERT (Id, EmployeeId, UserName, Email, Password, IsActive, LastLogin)
        VALUES (s.Id, s.EmployeeId, s.UserName, s.Email, s.Password, s.IsActive, NULL);

    /* ------------------------------------------------- Asignación de roles */
    /* Un usuario = un rol (regla del sistema).                                */
    MERGE userRoles AS t
    USING (VALUES
        ('55555555-5555-5555-5555-555555555501', @UsrAdmin, @RoleAdmin),
        ('55555555-5555-5555-5555-555555555502', @UsrTest,  @RolePlaneacion),
        ('55555555-5555-5555-5555-555555555503', @UsrKarla, @RolePlaneacion)
    ) AS s (Id, UserId, RoleId) ON t.UserId = s.UserId
    WHEN MATCHED THEN UPDATE SET t.RoleId = s.RoleId
    WHEN NOT MATCHED THEN INSERT (Id, UserId, RoleId) VALUES (s.Id, s.UserId, s.RoleId);

    /* ----------------------------------------------------------- Permisos */
    DECLARE @Permisos TABLE (Id UNIQUEIDENTIFIER, Name NVARCHAR(100));
    INSERT INTO @Permisos VALUES
        ('66666666-6666-6666-6666-666666666601', N'FullAccess'),
        ('66666666-6666-6666-6666-666666666602', N'ServiceAccess'),
        ('66666666-6666-6666-6666-666666666603', N'GetAllUsers'),
        ('66666666-6666-6666-6666-666666666604', N'GetUserById'),
        ('66666666-6666-6666-6666-666666666605', N'CreateUser'),
        ('66666666-6666-6666-6666-666666666606', N'EditUser'),
        ('66666666-6666-6666-6666-666666666607', N'GetAllRoles'),
        ('66666666-6666-6666-6666-666666666608', N'CapturarDatos'),
        ('66666666-6666-6666-6666-666666666609', N'GenerarReportes'),
        ('66666666-6666-6666-6666-666666666610', N'FirmarActas');

    MERGE permissions AS t
    USING @Permisos AS s ON t.Id = s.Id
    WHEN MATCHED THEN UPDATE SET t.Name = s.Name
    WHEN NOT MATCHED THEN INSERT (Id, Name) VALUES (s.Id, s.Name);

    /* Admin recibe todos los permisos. Los nombres GetAllUsers / CreateUser /
       etc. no son decorativos: UsersController valida contra el nombre del DTO
       de la petición, así que deben coincidir exactamente.                     */
    MERGE rolePermissions AS t
    USING (SELECT NEWID() AS Id, @RoleAdmin AS RoleId, Id AS PermissionId FROM @Permisos) AS s
    ON t.RoleId = s.RoleId AND t.PermissionId = s.PermissionId
    WHEN NOT MATCHED THEN INSERT (Id, RoleId, PermissionId) VALUES (s.Id, s.RoleId, s.PermissionId);

    /* Planeación: consulta y captura, sin administración de usuarios. */
    MERGE rolePermissions AS t
    USING (SELECT NEWID() AS Id, @RolePlaneacion AS RoleId, Id AS PermissionId
           FROM @Permisos WHERE Name IN (N'CapturarDatos', N'GenerarReportes', N'GetAllRoles')) AS s
    ON t.RoleId = s.RoleId AND t.PermissionId = s.PermissionId
    WHEN NOT MATCHED THEN INSERT (Id, RoleId, PermissionId) VALUES (s.Id, s.RoleId, s.PermissionId);

    COMMIT TRANSACTION;

    /* ------------------------------------------------------------ Resumen */
    PRINT '=== Datos de desarrollo cargados ===';
    SELECT 'roles' AS tabla, COUNT(*) AS filas FROM roles
    UNION ALL SELECT 'employees',       COUNT(*) FROM employees
    UNION ALL SELECT 'systemUser',      COUNT(*) FROM systemUser
    UNION ALL SELECT 'userRoles',       COUNT(*) FROM userRoles
    UNION ALL SELECT 'permissions',     COUNT(*) FROM permissions
    UNION ALL SELECT 'rolePermissions', COUNT(*) FROM rolePermissions;

    PRINT 'Credenciales:';
    PRINT '  admin@upzmg.edu.mx / Admin1234!  (Admin)';
    PRINT '  test@upzmg.local   / Test1234!   (Planeacion)';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
