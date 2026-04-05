# SecurityPatrol — DRH Security Walk Management System

## Overview
A security patrol management system for tracking officer walks, QR code scanning, scheduling, and audit reporting.

## Roles
| Role | Access |
|------|--------|
| Administrator | Full access: site admin, users, schedules, routes, messages, QR codes, reports |
| Security Officer | Dashboard, start/complete walks, scan QR codes, view own history |
| Auditor | Walk history review, missed location alerts, reports |

## Default Login
- **URL:** `http://localhost:8080`
- **Email:** `admin@drhsecurity.com`
- **Password:** `Admin@123`

---

## Local Development (Docker Compose)

```bash
cd /path/to/SecurityPatrol
docker compose up --build
```

App runs at `http://localhost:8080`

---

## Kubernetes Deployment

### Prerequisites
- Kubernetes cluster with NGINX Ingress Controller
- `kubectl` configured

### 1. Update Secrets
Edit `k8s/secret.yaml` and replace the base64-encoded values:

```bash
# Encode your SA password
echo -n 'YourStrongPassword@123' | base64

# Encode connection string
echo -n 'Server=sqlserver;Database=SecurityPatrol;User Id=sa;Password=YourStrongPassword@123;TrustServerCertificate=True;' | base64
```

### 2. Build and Push Docker Image

```bash
docker build -t your-registry/security-patrol-web:latest -f src/SecurityPatrol.Web/Dockerfile .
docker push your-registry/security-patrol-web:latest
```

Update `k8s/app-deployment.yaml` image field to match.

### 3. Deploy

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/secret.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/sqlserver-pvc.yaml
kubectl apply -f k8s/sqlserver-deployment.yaml
kubectl apply -f k8s/sqlserver-service.yaml
kubectl apply -f k8s/app-deployment.yaml
kubectl apply -f k8s/app-service.yaml
kubectl apply -f k8s/app-ingress.yaml
```

### 4. Access
Add to `/etc/hosts`:
```
<INGRESS_IP> security-patrol.local
```
Then browse to `http://security-patrol.local`

---

## Generating EF Core Migrations (Required before first run without Docker)

```bash
cd src/SecurityPatrol.Web
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Then update `Program.cs` to call `db.Database.Migrate()` instead of `EnsureCreated()`.

---

## QR Code Workflow
1. Admin creates Buildings → Floors → Locations in Site Administration
2. Admin creates Walk Routes selecting ordered locations
3. Admin generates and prints QR codes from Admin → Print QR Codes
4. Print and affix QR codes at each physical location (label includes: QR code, location name, "Do Not Remove", "DRH Security")
5. Officers scan QR codes during walks using the mobile-friendly scan interface

## Tech Stack
- **Framework:** ASP.NET Core 8 MVC
- **Database:** Microsoft SQL Server 2022
- **ORM:** Entity Framework Core 8
- **Auth:** ASP.NET Core Identity (local) — SSO/OIDC integration ready
- **QR Codes:** QRCoder library
- **UI:** Bootstrap 5, Font Awesome
- **Container:** Docker
- **Orchestration:** Kubernetes
