$ErrorActionPreference = 'Stop'

docker compose down -v
docker compose up -d --wait --wait-timeout 5 db
docker compose exec -T db /bin/sh -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U dev_user -P "$MSSQL_DEV_PASSWORD" -d upzmg -C -Q "SELECT 1"'

dotnet ef migrations add ConsolidatedInitial --project UPZMG.Persistence --startup-project UPZMG.API
dotnet ef database update --project UPZMG.Persistence --startup-project UPZMG.API
