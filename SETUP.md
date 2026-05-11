# Project Setup & Deployment Guide

This guide provides the complete setup instructions for both local testing (Windows) and production deployment (Ubuntu Linux).

## Requirements

*   **.NET SDK 8.0** (or newer)
*   **Nginx** (For Production)
*   **Ubuntu 22.04+** (Recommended for Production)

---

## 🐧 Production Setup (Ubuntu Linux)

### **1. Install .NET 8 Runtime**
```bash
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

### **2. Configure Nginx**
Create a new config file at `/etc/nginx/sites-available/bluegreen` and add the following:
```nginx
upstream dotnet_app {
    server 127.0.0.1:2001; # Default to Green
}

server {
    listen 80;
    server_name yourdomain.com;

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

### **3. Setup Systemd Services**
Create two services: `app-green.service` and `app-blue.service` in `/etc/systemd/system/`.
Example for Green:
```ini
[Service]
WorkingDirectory=/var/www/green
ExecStart=/usr/bin/dotnet /var/www/green/BlueGreenApp.dll
Environment=ASPNETCORE_URLS=http://localhost:2001
Environment=APP_COLOR=Green
Restart=always
```

---

## 🛠️ Local Proof (Windows)

### **1. Start the System**
Run the automated script to build and launch the primary server:
```powershell
./start.ps1
```

### **2. Individual Commands**
*   **Build**: `dotnet publish src/BlueGreenApp.csproj -c Release -o ./publish`
*   **Switch**: Visit `http://localhost:2001` and click the button.
*   **Update**: Replace DLLs in `./publish` or run the build command again.
*   **Return**: Visit `http://localhost:2002` and click the return button.
