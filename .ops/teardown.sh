#!/bin/bash

# Payment Service - Environment Teardown Script
# This script tears down the payment-service for any environment

set -e  # Exit on any error

# Environment is mandatory
TARGET_ENV=""

# Teardown options
REMOVE_VOLUMES=false
REMOVE_NETWORKS=false
FORCE_REMOVE=false
REMOVE_IMAGES=false

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

# Function to show help
show_help() {
    echo -e "${YELLOW}Usage: $0 -e ENV_NAME [OPTIONS]${NC}"
    echo ""
    echo -e "${YELLOW}Required:${NC}"
    echo "  -e, --env ENV_NAME    Target environment (development, production, staging, testing)"
    echo ""
    echo -e "${YELLOW}Options:${NC}"
    echo "  -v, --volumes         Remove volumes (⚠️  DATA LOSS!)"
    echo "  -n, --networks        Remove networks"
    echo "  -i, --images          Remove Docker images"
    echo "  -f, --force           Force removal without confirmation"
    echo "  -h, --help           Show this help message"
    echo ""
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--env)
            TARGET_ENV="$2"
            shift 2
            ;;
        -v|--volumes)
            REMOVE_VOLUMES=true
            shift
            ;;
        -n|--networks)
            REMOVE_NETWORKS=true
            shift
            ;;
        -i|--images)
            REMOVE_IMAGES=true
            shift
            ;;
        -f|--force)
            FORCE_REMOVE=true
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            echo -e "${RED}❌ Error: Unknown option: $1${NC}"
            show_help
            exit 1
            ;;
    esac
done

# Validate required parameters
if [ -z "$TARGET_ENV" ]; then
    echo -e "${RED}❌ Error: Environment parameter is required${NC}"
    show_help
    exit 1
fi

# Validate environment value
case $TARGET_ENV in
    development|staging|production|testing)
        ;;
    *)
        echo -e "${RED}❌ Error: Invalid environment: $TARGET_ENV${NC}"
        echo -e "${YELLOW}Valid environments: development, staging, production, testing${NC}"
        exit 1
        ;;
esac

print_status $BLUE "🧹 Starting $SERVICE_NAME teardown for $TARGET_ENV environment..."

# Check if docker-compose file exists
COMPOSE_FILE="$SERVICE_DIR/docker-compose.yml"
if [ -f "$COMPOSE_FILE" ]; then
    print_status $BLUE "📦 Found docker-compose.yml, stopping services..."
    
    cd "$SERVICE_DIR"
    
    # Set environment-specific project name
    export COMPOSE_PROJECT_NAME="${SERVICE_NAME}-${TARGET_ENV}"
    
    # Stop and remove containers
    if docker-compose down; then
        print_status $GREEN "✅ Containers stopped and removed"
    else
        print_status $YELLOW "⚠️  Some issues stopping containers (they may not be running)"
    fi
    
    # Remove volumes if requested
    if [ "$REMOVE_VOLUMES" = true ]; then
        print_status $BLUE "🗂️  Removing volumes..."
        docker-compose down -v || print_status $YELLOW "⚠️  Some issues removing volumes"
    fi
    
    # Remove networks if requested
    if [ "$REMOVE_NETWORKS" = true ]; then
        print_status $BLUE "🌐 Removing networks..."
        # Remove custom networks (default networks are handled by docker-compose down)
        docker network ls --filter name="${COMPOSE_PROJECT_NAME}" -q | xargs -r docker network rm || true
    fi
    
    # Remove images if requested
    if [ "$REMOVE_IMAGES" = true ]; then
        print_status $BLUE "📦 Removing images..."
        docker-compose down --rmi all || print_status $YELLOW "⚠️  Some issues removing images"
    fi
else
    print_status $YELLOW "⚠️  No docker-compose.yml found, attempting manual cleanup..."
    
    # Try to remove containers with service name pattern
    CONTAINERS=$(docker ps -aq --filter "name=${SERVICE_NAME}-${TARGET_ENV}" 2>/dev/null || true)
    if [ -n "$CONTAINERS" ]; then
        print_status $BLUE "Stopping containers..."
        docker stop $CONTAINERS >/dev/null 2>&1 || true
        docker rm $CONTAINERS >/dev/null 2>&1 || true
        print_status $GREEN "✅ Manual container cleanup completed"
    else
        print_status $BLUE "No containers found matching ${SERVICE_NAME}-${TARGET_ENV}"
    fi
fi

print_status $GREEN "🧹 $SERVICE_NAME teardown completed for $TARGET_ENV environment"
