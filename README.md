# Payment Service

A comprehensive .NET Core 8 payment processing service that supports multiple payment providers including Stripe, PayPal, and Square. The service provides a unified API for processing payments, refunds, and managing payment methods across different providers.

## Features

### 🏗️ Architecture

- **Microservice Architecture**: Standalone payment service with REST API
- **Provider Abstraction**: Unified interface supporting multiple payment providers
- **Database Integration**: Entity Framework Core with SQL Server
- **JWT Authentication**: Secure API endpoints with user context
- **Correlation ID Tracing**: Full request tracing across distributed systems
- **Configuration-Driven**: Easy provider enabling/disabling via configuration

### 💳 Payment Providers

- **Stripe**: Full card processing, payment methods, refunds
- **PayPal**: PayPal account payments and refunds
- **Square**: Card processing with Square ecosystem
- **Extensible**: Easy to add new payment providers

### 🔒 Security

- JWT token authentication
- Secure payment method storage
- PCI compliance considerations
- Audit trails and logging

### 📊 Core Functionality

- **Payment Processing**: Process one-time and saved payment method payments
- **Refund Management**: Full and partial refunds with provider sync
- **Payment Methods**: Save, retrieve, and delete customer payment methods
- **Transaction History**: Complete payment and refund history
- **Multi-currency Support**: USD, EUR, GBP, CAD

## API Endpoints

### Payments

```http
POST   /api/payments              # Process a payment
GET    /api/payments              # Get payments with filtering
GET    /api/payments/{id}         # Get specific payment
POST   /api/payments/{id}/refund  # Process refund
GET    /api/payments/order/{orderId} # Get payment by order ID
```

### Payment Methods

```http
POST   /api/paymentmethods        # Save payment method
GET    /api/paymentmethods/customer/{customerId} # Get customer's payment methods
DELETE /api/paymentmethods/{id}   # Delete payment method
GET    /api/paymentmethods/supported-methods # Get supported payment methods
GET    /api/paymentmethods/providers # Get available providers
GET    /api/paymentmethods/providers/{name}/status # Check provider status
```

## Configuration

### appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PaymentServiceDb;..."
  },
  "Jwt": {
    "Key": "your-jwt-key",
    "Issuer": "aioutlet-payment-service",
    "Audience": "aioutlet-clients"
  },
  "PaymentService": {
    "MaxPaymentAmount": 10000.0,
    "DefaultCurrency": "USD",
    "AllowedCurrencies": ["USD", "EUR", "GBP", "CAD"]
  },
  "PaymentProviders": {
    "DefaultProvider": "stripe",
    "Stripe": {
      "IsEnabled": true,
      "PublishableKey": "pk_test_...",
      "SecretKey": "sk_test_...",
      "WebhookSecret": "whsec_...",
      "SupportedMethods": ["visa", "mastercard", "amex"]
    },
    "PayPal": {
      "IsEnabled": true,
      "ClientId": "your_paypal_client_id",
      "ClientSecret": "your_paypal_client_secret",
      "IsSandbox": true,
      "ReturnUrl": "https://localhost:7000/payment/success",
      "CancelUrl": "https://localhost:7000/payment/cancelled"
    },
    "Square": {
      "IsEnabled": false,
      "ApplicationId": "your_square_app_id",
      "AccessToken": "your_square_access_token",
      "IsSandbox": true
    }
  }
}
```

## Getting Started

### Prerequisites

- .NET Core 8 SDK
- SQL Server (LocalDB for development)
- Payment provider accounts (Stripe, PayPal, Square)

### Setup

1. **Clone and Navigate**

   ```bash
   cd payment-service
   ```

2. **Configure Connection String**
   Update `appsettings.json` with your SQL Server connection string.

3. **Configure Payment Providers**
   Add your payment provider credentials to `appsettings.json` or `appsettings.Development.json`.

4. **Install Dependencies**

   ```bash
   dotnet restore
   ```

5. **Run Database Migrations**

   ```bash
   dotnet ef database update
   ```

6. **Run the Service**

   ```bash
   dotnet run
   ```

7. **Access Swagger UI**
   Navigate to `https://localhost:7001` for API documentation.

## Database Schema

### Core Tables

- **Payments**: Main payment records with provider transaction IDs
- **PaymentRefunds**: Refund records linked to original payments
- **PaymentMethods**: Stored customer payment methods with provider tokens

### Key Features

- **Audit Fields**: CreatedAt, UpdatedAt, CreatedBy, UpdatedBy on all entities
- **Metadata Storage**: JSON columns for provider-specific data
- **Proper Indexing**: Performance optimized queries
- **Money Data Type**: Precise financial calculations

## Payment Flow Examples

### Process Payment

```json
POST /api/payments
{
  "orderId": "ORD-12345",
  "customerId": "CUST-67890",
  "amount": 99.99,
  "currency": "USD",
  "paymentMethod": "visa",
  "paymentProvider": "stripe",
  "description": "Order #12345",
  "paymentMethodDetails": {
    "card": {
      "number": "4242424242424242",
      "expiryMonth": 12,
      "expiryYear": 2025,
      "cvc": "123",
      "holderName": "John Doe"
    }
  }
}
```

### Process Refund

```json
POST /api/payments/123/refund
{
  "amount": 49.99,
  "reason": "Customer requested partial refund"
}
```

### Save Payment Method

```json
POST /api/paymentmethods
{
  "customerId": "CUST-67890",
  "paymentProvider": "stripe",
  "paymentMethodType": "card",
  "isDefault": true,
  "paymentMethodDetails": {
    "card": {
      "number": "4242424242424242",
      "expiryMonth": 12,
      "expiryYear": 2025,
      "cvc": "123",
      "holderName": "John Doe"
    }
  }
}
```

## Security Considerations

### PCI Compliance

- **Never store raw card data** - Use provider tokenization
- **HTTPS only** for all payment endpoints
- **Secure configuration** of provider credentials
- **Audit logging** of all payment operations

### Authentication

- All endpoints require valid JWT tokens
- User context extracted from JWT claims
- Correlation ID tracking for request tracing

## Monitoring and Logging

### Structured Logging

- Correlation ID in all log messages
- Payment provider specific logging
- Error tracking with contextual information
- Performance metrics and timing

### Health Checks

- Database connectivity checks
- Payment provider health status
- Available at `/health` endpoint

## Testing

### Provider Testing

- **Stripe**: Use test card numbers (`4242424242424242`)
- **PayPal**: Use sandbox environment
- **Square**: Use sandbox environment

### Integration Testing

- Payment processing flows
- Refund scenarios
- Payment method management
- Error handling cases

## Deployment

### Production Considerations

1. **Secure Configuration**: Use Azure Key Vault or similar for secrets
2. **Database**: Use production SQL Server instance
3. **HTTPS**: Ensure all traffic is encrypted
4. **Monitoring**: Set up application insights and logging
5. **Backup**: Regular database backups
6. **Provider Configuration**: Use production provider credentials

### Environment Variables

```bash
ConnectionStrings__DefaultConnection="production-connection-string"
PaymentProviders__Stripe__SecretKey="sk_live_..."
PaymentProviders__PayPal__ClientSecret="production-secret"
Jwt__Key="production-jwt-key"
```

## Contributing

1. Follow the existing code patterns
2. Add unit tests for new features
3. Update documentation
4. Ensure PCI compliance considerations
5. Test with all supported payment providers

## License

This project is part of the AI Outlet microservices architecture.
