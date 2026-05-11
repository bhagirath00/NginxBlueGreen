#!/bin/bash
# Professional Blue-Green Deployment Script

IS_WINDOWS=false
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" ]]; then IS_WINDOWS=true; fi

log() { echo -e "\033[0;34m[$(date '+%H:%M:%S')]\033[0m $1"; }
success() { echo -e "\033[0;32m[$(date '+%H:%M:%S')] ✅ $1\033[0m"; }
error() { echo -e "\033[0;31m[$(date '+%H:%M:%S')] ❌ $1\033[0m"; exit 1; }

PRIMARY_DIR="/var/www/green"
TEMP_DIR="/var/www/blue"
BUILD_PATH=${1:-./publish/}

if [ "$IS_WINDOWS" = true ]; then
    PRIMARY_DIR="./www/green"
    TEMP_DIR="./www/blue"
    systemctl() { log "SIMULATION: systemctl $*"; }
else
    if [ "$EUID" -ne 0 ]; then error "Please run as root (sudo)"; fi
fi

log "Starting Blue-Green Deployment (2001 -> 2002 -> 2001)"
log "----------------------------------------------------"

# 1. Start lifeboat
log "Starting Port 2002..."
systemctl start app-blue
success "Lifeboat is ready."

# 2. Update Primary
log "Updating Port 2001..."
systemctl stop app-green
cp -r "$BUILD_PATH"* "$PRIMARY_DIR/"
systemctl start app-green
success "Port 2001 is updated."

# 3. Cleanup
systemctl stop app-blue
success "Deployment Successful."
