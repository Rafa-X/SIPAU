DECLARE @sql NVARCHAR(MAX) = N'';

-- Desactiva temporalmente las restricciones FK.
SELECT @sql +=
    N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) +
    N' NOCHECK CONSTRAINT ALL;' + CHAR(13)
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.name <> '__EFMigrationsHistory';

EXEC sp_executesql @sql;

-- Elimina todos los registros.
SET @sql = N'';

SELECT @sql +=
    N'DELETE FROM ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N';' + CHAR(13)
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.name <> '__EFMigrationsHistory';

EXEC sp_executesql @sql;

-- Reactiva y valida restricciones.
SET @sql = N'';

SELECT @sql +=
    N'ALTER TABLE ' + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) +
    N' WITH CHECK CHECK CONSTRAINT ALL;' + CHAR(13)
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.name <> '__EFMigrationsHistory';

EXEC sp_executesql @sql;