-- =========================================
-- Permission catalogue and role defaults
-- =========================================
-- Authorisation is checked against permission names, not role names, so this catalogue is
-- what the administration screen edits. The defaults reproduce exactly the access each area
-- had when authorisation was hard-coded, so loading this seed changes nothing until somebody
-- decides to change it.
--
-- Generated from UPZMG.Implementations/Security/AppPermissions.cs: edit the catalogue there.
-- Requires: migrations and test-data.sql (roles).
-- Run:
--   docker exec -i upzmg_db sh -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U dev_user -P "$MSSQL_DEV_PASSWORD" -C -d upzmg -b -I' < seed-permissions.sql
--
-- Idempotent: permissions are matched by name and each role's assignments are rebuilt.

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
BEGIN TRANSACTION;

-- The catalogue.
DECLARE @Catalogue TABLE (Name NVARCHAR(50) PRIMARY KEY, DisplayName NVARCHAR(80), Description NVARCHAR(200));
INSERT INTO @Catalogue (Name, DisplayName, Description) VALUES
    (N'alumnos.capturar', N'Capturar alumnos', N'Registrar, consultar y corregir alumnos.'),
    (N'personal.capturar', N'Capturar personal', N'Registrar, consultar y corregir docentes y empleados.'),
    (N'grupos.capturar', N'Capturar grupos de clase', N'Registrar, consultar y corregir grupos y sus horarios.'),
    (N'becas.capturar', N'Capturar becas', N'Asignar becas y consultar el padrón.'),
    (N'titulacion.capturar', N'Capturar titulación', N'Registrar procesos de titulación de egresados.'),
    (N'infraestructura.capturar', N'Capturar infraestructura', N'Dar de alta espacios físicos.'),
    (N'infraestructura.consultar', N'Consultar infraestructura', N'Ver los espacios registrados y su uso.'),
    (N'vinculacion.capturar', N'Capturar vinculación', N'Registrar convenios y proyectos de vinculación.'),
    (N'finanzas.capturar', N'Capturar finanzas', N'Registrar el resumen financiero anual.'),
    (N'formatos.capturar', N'Capturar formatos oficiales', N'Llenar y enviar los formatos 911 y SII.'),
    (N'formatos.publicar', N'Publicar formatos', N'Crear, editar y retirar plantillas de formatos.'),
    (N'informe.consultar', N'Consultar el informe de Junta', N'Ver el informe consolidado de un ciclo.'),
    (N'informe.redactar', N'Redactar el informe de Junta', N'Escribir los bloques narrativos y enviar el informe.'),
    (N'indicadores.consultar', N'Consultar indicadores', N'Ver el tablero, las gráficas y las series institucionales.'),
    (N'riesgo.consultar', N'Consultar riesgo de deserción', N'Ver el modelo y los alumnos señalados.'),
    (N'riesgo.reentrenar', N'Reentrenar el modelo de riesgo', N'Forzar un nuevo entrenamiento.'),
    (N'usuarios.administrar', N'Administrar usuarios y roles', N'Altas, bajas, roles y permisos del sistema.'),
    (N'periodos.administrar', N'Administrar periodos', N'Crear periodos y definir cuál está activo.');

UPDATE p SET DisplayName = c.DisplayName, Description = c.Description
FROM permissions p JOIN @Catalogue c ON c.Name = p.Name;

INSERT INTO permissions (Id, Name, DisplayName, Description)
SELECT NEWID(), c.Name, c.DisplayName, c.Description
FROM @Catalogue c
WHERE NOT EXISTS (SELECT 1 FROM permissions p WHERE p.Name = c.Name);

-- Defaults per role.
DECLARE @Defaults TABLE (RoleName NVARCHAR(100), PermissionName NVARCHAR(50));
INSERT INTO @Defaults (RoleName, PermissionName) VALUES
    (N'Admin', N'alumnos.capturar'),
    (N'Admin', N'personal.capturar'),
    (N'Admin', N'grupos.capturar'),
    (N'Admin', N'becas.capturar'),
    (N'Admin', N'titulacion.capturar'),
    (N'Admin', N'infraestructura.capturar'),
    (N'Admin', N'infraestructura.consultar'),
    (N'Admin', N'vinculacion.capturar'),
    (N'Admin', N'finanzas.capturar'),
    (N'Admin', N'formatos.capturar'),
    (N'Admin', N'formatos.publicar'),
    (N'Admin', N'informe.consultar'),
    (N'Admin', N'informe.redactar'),
    (N'Admin', N'indicadores.consultar'),
    (N'Admin', N'riesgo.consultar'),
    (N'Admin', N'riesgo.reentrenar'),
    (N'Admin', N'usuarios.administrar'),
    (N'Admin', N'periodos.administrar'),
    (N'Control Escolar', N'alumnos.capturar'),
    (N'Control Escolar', N'titulacion.capturar'),
    (N'Control Escolar', N'formatos.capturar'),
    (N'Control Escolar', N'informe.consultar'),
    (N'Control Escolar', N'indicadores.consultar'),
    (N'Control Escolar', N'riesgo.consultar'),
    (N'Servicios Estudiantiles', N'alumnos.capturar'),
    (N'Servicios Estudiantiles', N'becas.capturar'),
    (N'Servicios Estudiantiles', N'vinculacion.capturar'),
    (N'Servicios Estudiantiles', N'formatos.capturar'),
    (N'Servicios Estudiantiles', N'informe.consultar'),
    (N'Servicios Estudiantiles', N'indicadores.consultar'),
    (N'Servicios Estudiantiles', N'riesgo.consultar'),
    (N'Recursos Humanos', N'personal.capturar'),
    (N'Recursos Humanos', N'formatos.capturar'),
    (N'Recursos Humanos', N'informe.consultar'),
    (N'Recursos Humanos', N'indicadores.consultar'),
    (N'Direccion Academica', N'grupos.capturar'),
    (N'Direccion Academica', N'infraestructura.consultar'),
    (N'Direccion Academica', N'vinculacion.capturar'),
    (N'Direccion Academica', N'formatos.capturar'),
    (N'Direccion Academica', N'informe.consultar'),
    (N'Direccion Academica', N'indicadores.consultar'),
    (N'Direccion Academica', N'riesgo.consultar'),
    (N'Recursos Materiales y Servicios Generales', N'infraestructura.capturar'),
    (N'Recursos Materiales y Servicios Generales', N'infraestructura.consultar'),
    (N'Recursos Materiales y Servicios Generales', N'formatos.capturar'),
    (N'Recursos Materiales y Servicios Generales', N'informe.consultar'),
    (N'Recursos Materiales y Servicios Generales', N'indicadores.consultar');

-- Rebuilt per role, so a rerun restores the defaults without duplicating rows.
DELETE rp FROM rolePermissions rp
JOIN roles r ON r.Id = rp.RoleId
WHERE r.Name IN (SELECT DISTINCT RoleName FROM @Defaults);

INSERT INTO rolePermissions (Id, RoleId, PermissionId)
SELECT NEWID(), r.Id, p.Id
FROM @Defaults d
JOIN roles r ON r.Name = d.RoleName
JOIN permissions p ON p.Name = d.PermissionName;

-- The first seed's FullAccess and ServiceAccess grant nothing: no code reads them. Once the
-- rebuild above has unassigned them they only add two meaningless boxes to the screen.
DELETE FROM permissions
WHERE Name IN (N'FullAccess', N'ServiceAccess')
  AND NOT EXISTS (SELECT 1 FROM rolePermissions rp WHERE rp.PermissionId = permissions.Id);

-- Nobody may be left able to grant permissions.
IF NOT EXISTS (
    SELECT 1 FROM rolePermissions rp
    JOIN permissions p ON p.Id = rp.PermissionId
    WHERE p.Name = N'usuarios.administrar')
    THROW 51000, 'No role would be able to administer users.', 1;

COMMIT TRANSACTION;

PRINT '=== Permissions loaded ===';
SELECT r.Name AS rol, COUNT(*) AS permisos
FROM rolePermissions rp JOIN roles r ON r.Id = rp.RoleId
GROUP BY r.Name ORDER BY r.Name;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH
