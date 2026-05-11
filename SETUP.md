# Project Setup & Deployment Guide

This guide provides the complete setup instructions for both local testing (Windows) and production deployment (Ubuntu Linux).

## Requirements

*   **.NET SDK 8.0** (or newer)
*   **Nginx** (For Production)
*   **Ubuntu 22.04+** (Recommended for Production)

---

## Production Setup (Ubuntu Linux)

### **1. Install .NET 8 Runtime**
```bash
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

### **2. Configure Nginx**
```nginx
upstream dotnet_app {
    server 127.0.0.1:2001;
}

server {
    listen 80;
    location / {
        proxy_pass http://dotnet_app;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

## Commands Used

### **1. Initial Setup and Run**
```powershell
dotnet publish src/BlueGreenApp.csproj -c Release -o ./publish
mkdir -p ./www/green, ./www/blue
cp -r ./publish/* ./www/green/
$env:APP_COLOR="Green"; $env:ASPNETCORE_URLS="http://localhost:2001"; dotnet ./www/green/BlueGreenApp.dll
```

### **2. Update Deployment (Simulate)**
```powershell
dotnet publish src/BlueGreenApp.csproj -c Release -o ./publish
```