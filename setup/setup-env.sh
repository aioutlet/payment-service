#!/bin/bash

# Payment Service Environment Setup (.NET Core)
# This script sets up the .NET payment service for any environment using appsettings.json files

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

# Default environment
ENV_NAME="development"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--env)
            ENV_NAME="$2"
            shift 2
            ;;
        -h|--help)
            echo "Usage: $0 [options]"
            echo "Options:"
            echo "  -e, --env ENV_NAME    Environment name (default: development)"
            echo "                        Uses appsettings.ENV_NAME.json file"
            echo "  -h, --help           Show this help message"
            echo ""
            echo "Examples:"
            echo "  $0                   # Uses appsettings.Development.json"
            echo "  $0 -e production     # Uses appsettings.Production.json"
            echo "  $0 -e staging        # Uses appsettings.Staging.json"
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use -h or --help for usage information"
            exit 1
            ;;
    esac
done

echo "🚀 Setting up $SERVICE_NAME (.NET) for $ENV_NAME environment..."

# Set up environment file path based on environment name
if [ "$ENV_NAME" = "development" ]; then
    APPSETTINGS_FILE="$SERVICE_PATH/appsettings.Development.json"
    ASPNET_ENVIRONMENT="Development"
else
    # Capitalize first letter for ASP.NET Core convention
    ASPNET_ENVIRONMENT=$(echo "$ENV_NAME" | sed 's/.*/\L&/; s/[a-z]/\U&/')
    APPSETTINGS_FILE="$SERVICE_PATH/appsettings.$ASPNET_ENVIRONMENT.json"
fi

echo "📂 Using appsettings file: $APPSETTINGS_FILE"
echo "🌍 ASP.NET Core Environment: $ASPNET_ENVIRONMENT"

# Check if .NET SDK is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK is not installed or not in PATH"
    echo "Please install .NET 8.0 SDK or later from https://dotnet.microsoft.com/download"
    exit 1
fi

# Check if appsettings file exists
if [ ! -f "$APPSETTINGS_FILE" ]; then
    echo "❌ Error: Configuration file not found: $APPSETTINGS_FILE"
    echo "Please create the appsettings file for the $ENV_NAME environment"
    exit 1
fi

echo "✅ Found appsettings file: $APPSETTINGS_FILE"

# Validate JSON syntax
if command -v jq &> /dev/null; then
    echo "🔍 Validating JSON syntax..."
    if jq empty "$APPSETTINGS_FILE" 2>/dev/null; then
        echo "✅ JSON syntax is valid"
    else
        echo "❌ Error: Invalid JSON syntax in $APPSETTINGS_FILE"
        exit 1
    fi
else
    echo "⚠️  Warning: jq not found, skipping JSON validation"
fi

# Check if required configuration sections exist
echo "🔍 Checking configuration sections..."

# Check for required sections using jq if available
if command -v jq &> /dev/null; then
    # Check for ConnectionStrings section
    if jq -e '.ConnectionStrings' "$APPSETTINGS_FILE" &>/dev/null; then
        echo "✅ ConnectionStrings section found"
        
        # Check for DefaultConnection
        if jq -e '.ConnectionStrings.DefaultConnection' "$APPSETTINGS_FILE" &>/dev/null; then
            echo "✅ DefaultConnection string found"
        else
            echo "⚠️  Warning: DefaultConnection string not found in ConnectionStrings"
        fi
    else
        echo "⚠️  Warning: ConnectionStrings section not found"
    fi

    # Check for Logging section
    if jq -e '.Logging' "$APPSETTINGS_FILE" &>/dev/null; then
        echo "✅ Logging section found"
    else
        echo "⚠️  Warning: Logging section not found"
    fi
else
    echo "⚠️  Cannot validate configuration sections without jq"
fi

# Set ASPNETCORE_ENVIRONMENT for the current session
export ASPNETCORE_ENVIRONMENT="$ASPNET_ENVIRONMENT"
echo "🌍 Set ASPNETCORE_ENVIRONMENT=$ASPNET_ENVIRONMENT"

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to detect OS
detect_os() {
    if [[ "$OSTYPE" == "darwin"* ]]; then
        echo "macos"
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        echo "linux"
    elif [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]]; then
        echo "windows"
    else
        echo "unknown"
    fi
}

# Check for .NET CLI
check_dotnet() {
    echo "🔍 Checking .NET installation..."
    
    if command_exists dotnet; then
        DOTNET_VERSION=$(dotnet --version)
        echo "✅ .NET $DOTNET_VERSION is installed"
        
        # Check if version is 8.0 or higher
        MAJOR_VERSION=$(echo "$DOTNET_VERSION" | cut -d. -f1)
        if [[ $MAJOR_VERSION -lt 8 ]]; then
            echo "⚠️  Warning: .NET 8.0+ is recommended. Current version: $DOTNET_VERSION"
        fi
    else
        echo "❌ Error: .NET CLI is not installed. Please install .NET 8.0 or later"
        exit 1
    fi
}

# Check for PostgreSQL (optional for validation)
check_postgresql() {
    echo "🔍 Checking PostgreSQL installation..."
    
    if command_exists psql; then
        POSTGRES_VERSION=$(psql --version | awk '{print $3}' | sed 's/,.*//g')
        echo "✅ PostgreSQL $POSTGRES_VERSION is installed"
    else
        echo "ℹ️  PostgreSQL client not found (optional for development)"
    fi
}

# Setup .NET project
setup_dotnet_project() {
    echo "🔍 Setting up .NET project..."
    
    cd "$SERVICE_PATH"
    
    # Find the project file
    local project_file
    project_file=$(find . -name "*.csproj" | head -1)
    
    if [[ -n "$project_file" ]]; then
        echo "📦 Found project file: $(basename "$project_file")"
        
        # Restore NuGet packages
        echo "📦 Restoring .NET dependencies..."
        if dotnet restore; then
            echo "✅ Dependencies restored successfully"
        else
            echo "⚠️  Warning: Dependency restore failed"
        fi
        
        # Build the project
        echo "🔨 Building .NET project..."
        if dotnet build --no-restore; then
            echo "✅ Project built successfully"
        else
            echo "❌ Project build failed"
            exit 1
        fi
    else
        echo "⚠️  Warning: No .csproj file found in project directory"
    fi
}

# Check for PostgreSQL
check_postgresql() {
    log_info "Checking PostgreSQL installation..."
    
    if command_exists psql; then
        POSTGRES_VERSION=$(psql --version | awk '{print $3}' | sed 's/,.*//g')
        log_success "PostgreSQL $POSTGRES_VERSION is installed"
    else
        log_error "PostgreSQL is not installed. Please install PostgreSQL 12+"
        exit 1
    fi
}

# Setup .NET project
setup_dotnet_project() {
    log_info "Setting up .NET project..."
    
    cd "$SERVICE_PATH"
    
    # Restore NuGet packages
    if [ -f "*.csproj" ] || [ -f "PaymentService.csproj" ]; then
        log_info "Restoring .NET dependencies..."
        dotnet restore
        log_success "Dependencies restored successfully"
    else
        log_warning "No .csproj file found, skipping dependency restore"
    fi
    
    # Build the project
    if [ -f "*.csproj" ] || [ -f "PaymentService.csproj" ]; then
        log_info "Building .NET project..."
        dotnet build --no-restore
        log_success "Project built successfully"
    fi
}

# Setup database
setup_database() {
    echo "🔍 Setting up database configuration..."
    
    # Get database configuration from appsettings file if jq is available
    if command -v jq &> /dev/null; then
        local connection_string
        connection_string=$(jq -r '.ConnectionStrings.DefaultConnection // empty' "$APPSETTINGS_FILE")
        
        if [[ -n "$connection_string" ]]; then
            echo "✅ Database connection string found in appsettings"
            echo "ℹ️  Connection configured via appsettings.json"
        else
            echo "⚠️  Warning: No DefaultConnection found in appsettings"
            echo "Please ensure your appsettings file contains a ConnectionStrings section"
        fi
    else
        echo "⚠️  Cannot read database configuration without jq"
        echo "Please ensure your appsettings file contains proper ConnectionStrings section"
    fi
}

# Run database migrations
run_database_migrations() {
    echo "🔍 Checking for database migrations..."
    cd "$SERVICE_PATH"
    
    if command_exists dotnet; then
        # Check if Entity Framework tools are available
        if dotnet tool list -g | grep -q "dotnet-ef"; then
            echo "🚀 Running Entity Framework migrations..."
            if dotnet ef database update; then
                echo "✅ Database migrations completed successfully"
            else
                echo "⚠️  Warning: Database migration failed or no migrations found"
            fi
        else
            echo "ℹ️  Entity Framework tools not found"
            echo "   Install with: dotnet tool install --global dotnet-ef"
        fi
    fi
}

# Validate setup
validate_setup() {
    echo "🔍 Validating setup..."
    
    # Check if appsettings file is valid JSON and contains required sections
    if command -v jq &> /dev/null; then
        if jq empty "$APPSETTINGS_FILE" 2>/dev/null; then
            echo "✅ Configuration file is valid JSON"
        else
            echo "❌ Configuration file has invalid JSON syntax"
            return 1
        fi
    fi
    
    # Check if .NET project builds
    local project_file
    project_file=$(find "$SERVICE_PATH" -name "*.csproj" | head -1)
    
    if [[ -n "$project_file" ]]; then
        cd "$SERVICE_PATH"
        echo "🔨 Testing project build..."
        if dotnet build --no-restore --verbosity quiet > /dev/null 2>&1; then
            echo "✅ .NET project builds successfully"
        else
            echo "❌ .NET project build failed"
            return 1
        fi
    else
        echo "⚠️  No .csproj file found, skipping build validation"
    fi
    
    return 0
}

# Main execution
main() {
    echo "=========================================="
    echo "💳 Payment Service Setup (.NET)"
    echo "=========================================="
    
    OS=$(detect_os)
    echo "ℹ️  Detected OS: $OS"
    echo "ℹ️  Target Environment: $ENV_NAME"
    echo "ℹ️  ASP.NET Core Environment: $ASPNET_ENVIRONMENT"
    
    # Check prerequisites
    check_dotnet
    check_postgresql
    
    # Setup project
    setup_dotnet_project
    
    # Setup database
    setup_database
    
    # Run migrations
    run_database_migrations
    
    # Validate setup
    if validate_setup; then
        echo "=========================================="
        echo "✅ Payment Service setup completed successfully!"
        echo "=========================================="
        echo ""
        echo "💳 Setup Summary:"
        echo "  • Environment: $ASPNET_ENVIRONMENT"
        echo "  • Configuration: $(basename "$APPSETTINGS_FILE")"
        echo "  • Service URL: http://localhost:3004"
        echo "  • Health Check: http://localhost:3004/health"
        echo ""
        echo "🔒 Payment Features:"
        echo "  • Stripe Integration"
        echo "  • PayPal Integration"
        echo "  • PCI DSS Compliance"
        echo "  • Secure Payment Processing"
        echo "  • Refund Management"
        echo "  • Webhook Support"
        echo ""
        echo "🚀 Next Steps:"
        echo "  1. Configure payment gateway credentials in $APPSETTINGS_FILE"
        echo "  2. Set up webhook endpoints for payment providers"
        echo "  3. Start the service: dotnet run"
        echo "  4. Run tests: dotnet test"
        echo "  5. Check health: curl http://localhost:3004/health"
        echo ""
        echo "📝 Configuration Note:"
        echo "  This .NET service uses appsettings.json files for configuration."
        echo "  Environment-specific settings are in appsettings.\$ENVIRONMENT.json"
        echo "  Set ASPNETCORE_ENVIRONMENT=$ASPNET_ENVIRONMENT when running the service"
        echo ""
    else
        echo "❌ Setup validation failed"
        exit 1
    fi
}

# Run main function
main "$@"
