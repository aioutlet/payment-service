# Payment Service Dapr Configuration

This directory contains Dapr configuration files for the Payment Service.

## Files

- `components/pubsub.yaml` - Redis pub/sub component configuration
- `components/secretstore.yaml` - Local file-based secret store configuration
- `components/secrets.json` - Encrypted secrets (JWT, database, payment providers)
- `config.yaml` - Dapr runtime configuration (tracing, metrics, access control)

## Running with Dapr

### Windows

```bash
run-with-dapr.bat
```

### Linux/Mac

```bash
./run-with-dapr.sh
```

## Configuration Details

### Pub/Sub Component

- **Name**: payment-pubsub
- **Type**: Redis
- **Host**: localhost:6379
- **Consumer ID**: payment-service

### Runtime Ports

- **App Port**: 8004
- **Dapr HTTP Port**: 3504
- **Dapr gRPC Port**: 50004

## Prerequisites

1. Dapr CLI installed
2. Redis running (localhost:6379)
3. .NET 8.0 SDK
4. Payment service dependencies (dotnet restore)
