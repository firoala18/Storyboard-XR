# Storyboard Deployment & Update Guide

## Overview

The Storyboard application is deployed as a Docker containerized ASP.NET Core 8.0 application with PostgreSQL database, running under the subdomain **https://storyboard.xr-adiqu.de**.

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    HTTPS (443)                          │
│              storyboard.xr-adiqu.de                     │
└─────────────────────┬───────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│              Apache Reverse Proxy (Plesk)               │
│                   Port 127.0.0.1:5052                   │
└─────────────────────┬───────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────┐
│                 Docker Network                          │
│               (storyboard-network)                      │
│  ┌───────────────────┐    ┌───────────────────────┐    │
│  │   storyboard-web  │───▶│    storyboard-db      │    │
│  │   (ASP.NET Core)  │    │    (PostgreSQL 16)    │    │
│  │   Port 8080       │    │    Port 5432          │    │
│  └───────────────────┘    └───────────────────────┘    │
└─────────────────────────────────────────────────────────┘
```

---

## Container Details

| Container | Image | Internal Port | External Port |
|-----------|-------|---------------|---------------|
| storyboard-web | storyboard-app-web | 8080 | 127.0.0.1:5052 |
| storyboard-db | postgres:16-alpine | 5432 | 0.0.0.0:5435 |

---

## File Structure

```
/var/www/vhosts/xr-adiqu.de/apps.xr-adiqu.de/storyboard/
├── Dockerfile              # Multi-stage Docker build
├── docker-compose.yml      # Container orchestration
├── .env                    # Environment variables
├── scripts/
│   └── migrate.sh          # Database migration script
├── ProjectsWebApp/         # Main web application
├── ProjectsWebApp.DataAccsess/
├── ProjectsWebApp.Models/
├── ProjectsWebApp.Utility/
└── Dto/
```

---

## Common Operations

### Navigate to Project Directory

```bash
cd /var/www/vhosts/xr-adiqu.de/apps.xr-adiqu.de/storyboard
```

### View Container Status

```bash
docker ps --filter "name=storyboard"
```

### View Application Logs

```bash
# View recent logs
docker logs storyboard-web

# Follow logs in real-time
docker logs -f storyboard-web

# View last 100 lines
docker logs --tail 100 storyboard-web
```

### Restart Services

```bash
# Restart web container only
docker restart storyboard-web

# Restart all services
docker compose restart
```

### Stop Services

```bash
docker compose down
```

### Start Services

```bash
docker compose up -d
```

---

## Update After Git Pull

When you pull new code changes from the repository, follow these steps:

### 1. Pull Latest Changes

```bash
cd /var/www/vhosts/xr-adiqu.de/apps.xr-adiqu.de/storyboard
git pull origin main
```

### 2. Rebuild and Restart Containers

```bash
# Rebuild the web container with new code
docker compose build --no-cache web

# Restart with the new image
docker compose up -d
```

### 3. Run Database Migrations (if needed)

If the update includes new database migrations:

```bash
# Remove old migrator container if exists
docker rm storyboard-migrator 2>/dev/null

# Run migrations
docker compose --profile migrate up migrator
```

### 4. Verify Deployment

```bash
# Check container health
docker ps --filter "name=storyboard"

# Test application
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:5052/

# Check logs for errors
docker logs --tail 50 storyboard-web
```

### Quick Update Script (All-in-One)

```bash
#!/bin/bash
cd /var/www/vhosts/xr-adiqu.de/apps.xr-adiqu.de/storyboard

echo "Pulling latest changes..."
git pull origin main

echo "Rebuilding containers..."
docker compose build --no-cache web

echo "Running migrations..."
docker rm storyboard-migrator 2>/dev/null
docker compose --profile migrate up migrator

echo "Restarting services..."
docker compose up -d

echo "Waiting for health check..."
sleep 15

echo "Checking status..."
docker ps --filter "name=storyboard" --format "table {{.Names}}\t{{.Status}}"

echo "Testing application..."
curl -s -o /dev/null -w "HTTP Status: %{http_code}\n" http://127.0.0.1:5052/
```

---

## Database Management

### Access PostgreSQL

```bash
# Connect to database container
docker exec -it storyboard-db psql -U storyboard_user -d storyboard_db
```

### Backup Database

```bash
# Create backup
docker exec storyboard-db pg_dump -U storyboard_user storyboard_db > backup_$(date +%Y%m%d_%H%M%S).sql
```

### Restore Database

```bash
# Restore from backup
cat backup_file.sql | docker exec -i storyboard-db psql -U storyboard_user -d storyboard_db
```

### Reset Database (Caution!)

```bash
# Stop web container
docker stop storyboard-web

# Drop and recreate database
docker exec storyboard-db psql -U storyboard_user -d postgres -c "DROP DATABASE storyboard_db;"
docker exec storyboard-db psql -U storyboard_user -d postgres -c "CREATE DATABASE storyboard_db OWNER storyboard_user;"

# Run migrations
docker rm storyboard-migrator 2>/dev/null
docker compose --profile migrate up migrator

# Start web container
docker start storyboard-web
```

---

## Environment Variables

The `.env` file contains the following configuration:

| Variable | Description | Default |
|----------|-------------|---------|
| POSTGRES_DB | Database name | storyboard_db |
| POSTGRES_USER | Database user | storyboard_user |
| POSTGRES_PASSWORD | Database password | StoryboardSecure2024! |
| SUPERADMIN_EMAIL | Initial admin email | admin@xr-adiqu.de |
| SUPERADMIN_PASSWORD | Initial admin password | SuperAdmin2024!! |

To modify environment variables:

1. Edit the `.env` file
2. Restart containers: `docker compose up -d`

---

## Troubleshooting

### Container Won't Start

```bash
# Check logs for errors
docker logs storyboard-web

# Check if port is in use
ss -tulpn | grep 5052

# Rebuild from scratch
docker compose down
docker compose build --no-cache
docker compose up -d
```

### Database Connection Issues

```bash
# Verify database is running
docker ps --filter "name=storyboard-db"

# Test database connection
docker exec storyboard-db pg_isready -U storyboard_user -d storyboard_db

# Check database logs
docker logs storyboard-db
```

### Application Returns 500 Error

```bash
# Check application logs
docker logs --tail 100 storyboard-web

# Verify migrations are up to date
docker rm storyboard-migrator 2>/dev/null
docker compose --profile migrate up migrator

# Restart application
docker restart storyboard-web
```

### SSL/HTTPS Issues

The SSL is managed by Plesk. If there are SSL issues:

```bash
# Reconfigure web service
sudo plesk repair web storyboard.xr-adiqu.de -y

# Check Apache configuration
cat /var/www/vhosts/system/storyboard.xr-adiqu.de/conf/vhost_ssl.conf
```

### WebSocket/SignalR Not Working

Verify the Apache proxy configuration includes WebSocket support:

```bash
cat /var/www/vhosts/system/storyboard.xr-adiqu.de/conf/vhost_ssl.conf
```

Should include:
```apache
RewriteEngine On
RewriteCond %{HTTP:Upgrade} =websocket [NC]
RewriteRule /(.*)           ws://127.0.0.1:5052/$1 [P,L]
```

---

## Maintenance

### Clean Up Docker Resources

```bash
# Remove unused images
docker image prune -f

# Remove unused volumes (careful!)
docker volume prune -f

# Full cleanup (removes all unused resources)
docker system prune -f
```

### Monitor Disk Usage

```bash
# Check Docker disk usage
docker system df

# Check volume sizes
docker volume ls
```

---

## Security Notes

1. **Database Credentials**: Change default passwords in `.env` for production
2. **SuperAdmin Account**: Update `SUPERADMIN_EMAIL` and `SUPERADMIN_PASSWORD` after initial setup
3. **Database Port**: Port 5435 is exposed externally - consider restricting access via firewall
4. **Backups**: Implement regular database backups

---

## Support

For issues with:
- **Application Code**: Check the repository issues or contact the development team
- **Server/Infrastructure**: Contact the system administrator
- **Plesk Configuration**: Use `plesk` CLI or Plesk web panel

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2025-12-27 | 1.0.0 | Initial Docker deployment |
