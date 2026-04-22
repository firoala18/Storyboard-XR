#!/bin/bash
set -e

echo "Installing dotnet-ef tool..."
dotnet tool install --global dotnet-ef --version 8.0.10

export PATH="$PATH:/root/.dotnet/tools"

echo "Running database migrations..."
dotnet ef database update \
    --project ProjectsWebApp.DataAccsess/ProjectsWebApp.DataAccsess.csproj \
    --startup-project ProjectsWebApp/ProjectsWebApp.csproj

echo "Migrations completed successfully!"
