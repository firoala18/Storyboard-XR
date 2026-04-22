# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["ProjectsWebApp.sln", "./"]
COPY ["ProjectsWebApp/ProjectsWebApp.csproj", "ProjectsWebApp/"]
COPY ["ProjectsWebApp.DataAccsess/ProjectsWebApp.DataAccsess.csproj", "ProjectsWebApp.DataAccsess/"]
COPY ["ProjectsWebApp.Models/ProjectsWebApp.Models.csproj", "ProjectsWebApp.Models/"]
COPY ["ProjectsWebApp.Utility/ProjectsWebApp.Utility.csproj", "ProjectsWebApp.Utility/"]
COPY ["Dto/Dto.csproj", "Dto/"]

# Restore dependencies
RUN dotnet restore "ProjectsWebApp.sln"

# Copy everything else
COPY . .

# Build the application
WORKDIR "/src/ProjectsWebApp"
RUN dotnet build "ProjectsWebApp.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ProjectsWebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser

# Copy published files
COPY --from=publish /app/publish .

# Create directories for uploaded files and data protection keys
RUN mkdir -p /app/wwwroot/images/uploads \
    && mkdir -p /app/wwwroot/videos/Projects \
    && mkdir -p /var/aspnet-dpkeys/storyboard \
    && chown -R appuser:appuser /app \
    && chown -R appuser:appuser /var/aspnet-dpkeys

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

# Entry point
ENTRYPOINT ["dotnet", "ProjectsWebApp.dll"]
