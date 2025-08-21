#!/bin/bash

# Payment Service - Environment Teardown Script
# This script tears down the payment-service development environment

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVICE_DIR="$(dirname "$SCRIPT_DIR")"
SERVICE_NAME="payment-service"

# Function to print colored output
print_status() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

print_status $BLUE "🧹 Starting $SERVICE_NAME teardown for development environment..."

# Check if docker-compose file exists
COMPOSE_FILE="$SERVICE_DIR/docker-compose.yml"
if [ -f "$COMPOSE_FILE" ]; then
    print_status $BLUE "📦 Found docker-compose.yml, stopping services..."
    
    cd "$SERVICE_DIR"
    
    # Stop and remove containers
    if docker-compose down; then
        print_status $GREEN "✅ Containers stopped and removed"
    else
        print_status $YELLOW "⚠️  Some issues stopping containers (they may not be running)"
    fi
    
    # Remove volumes if they exist
    print_status $BLUE "🗂️  Cleaning up volumes..."
    docker volume rm payment-service_sqlserver_data 2>/dev/null || print_status $BLUE "No payment service volumes to remove"
    
else
    print_status $YELLOW "⚠️  No docker-compose.yml found, attempting manual cleanup..."
    
    # Try to remove containers with service name pattern
    CONTAINERS=$(docker ps -aq --filter "name=payment-service" 2>/dev/null || true)
    if [ -n "$CONTAINERS" ]; then
        print_status $BLUE "Stopping containers..."
        docker stop $CONTAINERS >/dev/null 2>&1 || true
        docker rm $CONTAINERS >/dev/null 2>&1 || true
        print_status $GREEN "✅ Manual container cleanup completed"
    else
        print_status $BLUE "No containers found matching payment-service"
    fi
fi

print_status $GREEN "🧹 Payment service teardown completed for development environment"
