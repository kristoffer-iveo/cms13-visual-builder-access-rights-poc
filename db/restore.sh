#!/bin/sh
# Restores db/Cms13Poc.bak into the SQL Server container started by `docker compose up -d`.
set -e
cd "$(dirname "$0")"
sql() { docker exec cms13-poc-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'Qwerty12345!' -C -b -Q "$1"; }

echo "Waiting for SQL Server..."
until sql "SELECT 1" >/dev/null 2>&1; do sleep 2; done

docker cp Cms13Poc.bak cms13-poc-db:/var/opt/mssql/data/Cms13Poc.bak
docker exec -u root cms13-poc-db chmod 644 /var/opt/mssql/data/Cms13Poc.bak
sql "IF DB_ID('Cms13Poc') IS NOT NULL ALTER DATABASE Cms13Poc SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
     RESTORE DATABASE Cms13Poc FROM DISK='/var/opt/mssql/data/Cms13Poc.bak' WITH REPLACE;
     ALTER DATABASE Cms13Poc SET MULTI_USER;"
echo "Done. Start the site with: dotnet run"
