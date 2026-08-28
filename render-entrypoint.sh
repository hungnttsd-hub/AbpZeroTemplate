#!/bin/sh
set -eu

echo "Running database migrations..."
cd /app/migrator
dotnet WebHoanTien.DbMigrator.dll

echo "Starting CatBack web service..."
cd /app/web
exec dotnet WebHoanTien.Web.dll
