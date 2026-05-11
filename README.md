# .NET Blue-Green Deployment Master Controller

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![ASP.NET Core](https://img.shields.io/badge/.NET-Core-512BD4?logo=dotnet&logoColor=white)
![Nginx](https://img.shields.io/badge/Nginx-OpenSource-2695BA?logo=nginx&logoColor=white)

A production-ready deployment strategy for ASP.NET Core that solves the .NET DLL file-locking issue using Nginx and a "Bounce" deployment flow.

## Operational Flow

```mermaid
graph LR
    %% Node Styles
    classDef green fill:#e8f5e9,stroke:#2e7d32,stroke-width:2px,color:#1b5e20;
    classDef blue fill:#e3f2fd,stroke:#1565c0,stroke-width:2px,color:#0d47a1;
    classDef orange fill:#fff3e0,stroke:#e65100,stroke-width:2px,color:#bf360c;

    subgraph "PROD"
        A[Port 2001: LIVE]:::green
    end

    A --> B[Start Port 2002]:::blue
    B --> C[Stop Port 2001]:::orange
    C --> D{UNLOCK}:::orange
    D --> E[Update DLLs]:::orange
    E --> F[Restart Port 2001]:::green
    F --> G[Stop Port 2002]:::blue
    G --> H[Port 2001: LIVE]:::green
```

---

## The Problem: DLL Locking
In traditional .NET deployments, the runtime locks `.dll` files while the application is running. If you try to overwrite these files during an update, the deployment fails. If you stop the service to overwrite them, your website goes down.

## The Solution: Blue-Green "Bounce"
This system solves the issue by using a reverse proxy (Nginx) to shift traffic between two ports:
1.  **Green (2001)**: The primary production port.
2.  **Blue (2002)**: The temporary failover port.

**How it prevents downtime:**
*   **Traffic Shifting**: Before updating, we start a "Blue" instance and tell Nginx to send all users there.
*   **Safe Unlocking**: We then stop the "Green" service. Since no traffic is hitting it, the DLLs are released and can be replaced safely.
*   **Zero-Downtime Return**: Once updated, we restart "Green" and switch Nginx back. Users never see an error page.

---

## Project Structure
*   `src/` — ASP.NET Core source code with integrated switch logic.
*   `scripts/` — Bash automation for Linux deployment.
*   `nginx/` — Nginx reverse proxy configuration.
*   `www/` — Environment folders (green/blue) for local proof.
*   `publish/` — Staging area for build artifacts.
