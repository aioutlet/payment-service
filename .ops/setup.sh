#!/bin/bash

# Payment Service Environment Setup (.NET Core)
# This script sets up the payment service for development environment

set -e

SERVICE_NAME="payment-service"
SERVICE_PATH="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Logging functions
log_info() {
    echo "[INFO] $1"
}
log_success() {
    echo "[SUCCESS] $1"
}
log_error() {
    echo "[ERROR] $1"
}
log_warning() {
    echo "[WARNING] $1"
}

echo "🚀 Setting up $SERVICE_NAME (.NET) for development environment..."

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    log_error ".NET SDK is not installed or not in PATH"
    echo "Please install .NET 8.0 SDK or later from https://dotnet.microsoft.com/download"
    exit 1
fi

log_success ".NET SDK is available"

# Check if Docker is running
if ! docker info >/dev/null 2>&1; then
    log_error "Docker is not running. Please start Docker Desktop."
    exit 1
fi

log_success "Docker is running"

# Check if shared infrastructure is running
if ! docker network ls | grep -q "aioutlet-network"; then
    log_error "Shared infrastructure network 'aioutlet-network' not found."
    echo "Please start the shared infrastructure first:"
    echo "  cd ../infrastructure && docker-compose up -d"
    exit 1
fi

log_success "Shared infrastructure network found"

cd "$SERVICE_PATH"

# Restore .NET packages
if [ -f "PaymentService.csproj" ]; then
    log_info "Restoring .NET dependencies..."
    if dotnet restore; then
        log_success "Dependencies restored successfully"
    else
        log_error "Failed to restore dependencies"
        exit 1
    fi
    
    # Build the project
    log_info "Building .NET project..."
    if dotnet build --no-restore; then
        log_success "Project built successfully"
    else
        log_error "Project build failed"
        exit 1
    fi
else
    log_warning "PaymentService.csproj not found, skipping .NET build"
fi

# Start services with Docker Compose
log_info "Starting services with Docker Compose..."
if docker-compose up -d; then
    log_success "Services started successfully"
    
    echo ""
    log_info "Waiting for services to be ready..."
    sleep 15
    
    # Check service health
    if docker-compose ps | grep -q "Up.*healthy\|Up"; then
        log_success "Services are healthy and ready"
    else
        log_warning "Services may still be starting up"
    fi
else
    log_error "Failed to start services with Docker Compose"
    exit 1
fi

echo ""
echo "✅ Payment Service setup completed successfully!"
echo ""
echo "💳 Service Information:"
echo "  • Service URL: http://localhost:8084"
echo "  • Health Check: http://localhost:8084/health"
echo "  • Database: SQL Server on port 1434"
echo ""
echo "🔗 Connected Services:"
echo "  • RabbitMQ: aioutlet-rabbitmq:5672"
echo "  • Redis: aioutlet-redis:6379"
echo "  • Network: aioutlet-network"
echo ""
echo "� Useful Commands:"
echo "  • View status: docker-compose ps"
echo "  • View logs: docker-compose logs -f"
echo "  • Stop services: bash .ops/teardown.sh"
echo ""
