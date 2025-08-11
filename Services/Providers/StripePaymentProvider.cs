using Microsoft.Extensions.Options;
using Stripe;
using PaymentService.Configuration;
using PaymentService.Models.DTOs;
using PaymentService.Models.Entities;

namespace PaymentService.Services.Providers;

/// <summary>
/// Stripe payment provider implementation
/// </summary>
public class StripePaymentProvider : IPaymentProvider
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentProvider> _logger;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly PaymentMethodService _paymentMethodService;
    private readonly RefundService _refundService;

    public string ProviderName => "stripe";
    public List<string> SupportedPaymentMethods => _settings.SupportedMethods;
    public bool IsEnabled => _settings.IsEnabled;

    public StripePaymentProvider(
        IOptions<PaymentProvidersSettings> paymentProvidersSettings,
        ILogger<StripePaymentProvider> logger)
    {
        _settings = paymentProvidersSettings.Value.Stripe;
        _logger = logger;

        // Configure Stripe
        StripeConfiguration.ApiKey = _settings.SecretKey;

        _paymentIntentService = new PaymentIntentService();
        _paymentMethodService = new PaymentMethodService();
        _refundService = new RefundService();

        _logger.LogInformation("Stripe payment provider initialized. Enabled: {IsEnabled}", IsEnabled);
    }

    public async Task<PaymentProviderResult> ProcessPaymentAsync(ProcessPaymentDto request, string correlationId)
    {
        try
        {
            _logger.LogInformation("Processing Stripe payment for order {OrderId} [CorrelationId: {CorrelationId}]", 
                request.OrderId, correlationId);

            var paymentIntentOptions = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100), // Stripe uses cents
                Currency = request.Currency.ToLower(),
                Description = request.Description ?? $"Payment for order {request.OrderId}",
                Metadata = new Dictionary<string, string>
                {
                    ["order_id"] = request.OrderId,
                    ["customer_id"] = request.CustomerId,
                    ["correlation_id"] = correlationId
                }
            };

            // Handle payment method
            if (request.PaymentMethodDetails?.TokenId != null)
            {
                // Use saved payment method
                paymentIntentOptions.PaymentMethod = request.PaymentMethodDetails.TokenId;
                paymentIntentOptions.ConfirmationMethod = "automatic";
                paymentIntentOptions.Confirm = true;
            }
            else if (request.PaymentMethodDetails?.Card != null)
            {
                // Create new payment method from card details
                var cardOptions = new PaymentMethodCreateOptions
                {
                    Type = "card",
                    Card = new PaymentMethodCardOptions
                    {
                        Number = request.PaymentMethodDetails.Card.Number,
                        ExpMonth = request.PaymentMethodDetails.Card.ExpiryMonth,
                        ExpYear = request.PaymentMethodDetails.Card.ExpiryYear,
                        Cvc = request.PaymentMethodDetails.Card.Cvc
                    }
                };

                if (!string.IsNullOrEmpty(request.PaymentMethodDetails.Card.HolderName))
                {
                    cardOptions.BillingDetails = new PaymentMethodBillingDetailsOptions
                    {
                        Name = request.PaymentMethodDetails.Card.HolderName
                    };
                }

                var paymentMethod = await _paymentMethodService.CreateAsync(cardOptions);
                paymentIntentOptions.PaymentMethod = paymentMethod.Id;
                paymentIntentOptions.ConfirmationMethod = "automatic";
                paymentIntentOptions.Confirm = true;
            }

            var paymentIntent = await _paymentIntentService.CreateAsync(paymentIntentOptions);

            var result = new PaymentProviderResult
            {
                TransactionId = paymentIntent.Id,
                ProviderTransactionId = paymentIntent.Id,
                Metadata = new Dictionary<string, object>
                {
                    ["stripe_payment_intent_id"] = paymentIntent.Id,
                    ["stripe_client_secret"] = paymentIntent.ClientSecret ?? ""
                }
            };

            switch (paymentIntent.Status)
            {
                case "succeeded":
                    result.IsSuccess = true;
                    result.Status = PaymentStatus.Succeeded;
                    _logger.LogInformation("Stripe payment succeeded for order {OrderId} [CorrelationId: {CorrelationId}]", 
                        request.OrderId, correlationId);
                    break;
                    
                case "processing":
                    result.IsSuccess = true;
                    result.Status = PaymentStatus.Processing;
                    _logger.LogInformation("Stripe payment processing for order {OrderId} [CorrelationId: {CorrelationId}]", 
                        request.OrderId, correlationId);
                    break;
                    
                case "requires_action":
                case "requires_confirmation":
                    result.IsSuccess = false;
                    result.Status = PaymentStatus.Pending;
                    result.FailureReason = "Payment requires additional action";
                    _logger.LogWarning("Stripe payment requires action for order {OrderId} [CorrelationId: {CorrelationId}]", 
                        request.OrderId, correlationId);
                    break;
                    
                default:
                    result.IsSuccess = false;
                    result.Status = PaymentStatus.Failed;
                    result.FailureReason = $"Payment failed with status: {paymentIntent.Status}";
                    _logger.LogError("Stripe payment failed for order {OrderId} with status {Status} [CorrelationId: {CorrelationId}]", 
                        request.OrderId, paymentIntent.Status, correlationId);
                    break;
            }

            return result;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error processing payment for order {OrderId} [CorrelationId: {CorrelationId}]", 
                request.OrderId, correlationId);
                
            return new PaymentProviderResult
            {
                IsSuccess = false,
                Status = PaymentStatus.Failed,
                FailureReason = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Stripe payment for order {OrderId} [CorrelationId: {CorrelationId}]", 
                request.OrderId, correlationId);
                
            return new PaymentProviderResult
            {
                IsSuccess = false,
                Status = PaymentStatus.Failed,
                FailureReason = "An unexpected error occurred"
            };
        }
    }

    public async Task<RefundProviderResult> ProcessRefundAsync(Payment payment, decimal amount, string reason, string correlationId)
    {
        try
        {
            _logger.LogInformation("Processing Stripe refund for payment {PaymentId}, amount {Amount} [CorrelationId: {CorrelationId}]", 
                payment.Id, amount, correlationId);

            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = payment.ProviderTransactionId,
                Amount = (long)(amount * 100), // Stripe uses cents
                Reason = "requested_by_customer",
                Metadata = new Dictionary<string, string>
                {
                    ["payment_id"] = payment.Id.ToString(),
                    ["order_id"] = payment.OrderId,
                    ["correlation_id"] = correlationId,
                    ["reason"] = reason ?? ""
                }
            };

            var refund = await _refundService.CreateAsync(refundOptions);

            var result = new RefundProviderResult
            {
                RefundId = refund.Id,
                ProviderRefundId = refund.Id,
                Metadata = new Dictionary<string, object>
                {
                    ["stripe_refund_id"] = refund.Id
                }
            };

            switch (refund.Status)
            {
                case "succeeded":
                    result.IsSuccess = true;
                    result.Status = RefundStatus.Succeeded;
                    _logger.LogInformation("Stripe refund succeeded for payment {PaymentId} [CorrelationId: {CorrelationId}]", 
                        payment.Id, correlationId);
                    break;
                    
                case "pending":
                    result.IsSuccess = true;
                    result.Status = RefundStatus.Processing;
                    _logger.LogInformation("Stripe refund pending for payment {PaymentId} [CorrelationId: {CorrelationId}]", 
                        payment.Id, correlationId);
                    break;
                    
                default:
                    result.IsSuccess = false;
                    result.Status = RefundStatus.Failed;
                    result.FailureReason = $"Refund failed with status: {refund.Status}";
                    _logger.LogError("Stripe refund failed for payment {PaymentId} with status {Status} [CorrelationId: {CorrelationId}]", 
                        payment.Id, refund.Status, correlationId);
                    break;
            }

            return result;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error processing refund for payment {PaymentId} [CorrelationId: {CorrelationId}]", 
                payment.Id, correlationId);
                
            return new RefundProviderResult
            {
                IsSuccess = false,
                Status = RefundStatus.Failed,
                FailureReason = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Stripe refund for payment {PaymentId} [CorrelationId: {CorrelationId}]", 
                payment.Id, correlationId);
                
            return new RefundProviderResult
            {
                IsSuccess = false,
                Status = RefundStatus.Failed,
                FailureReason = "An unexpected error occurred"
            };
        }
    }

    public async Task<PaymentMethodResult> SavePaymentMethodAsync(SavePaymentMethodDto request, string correlationId)
    {
        try
        {
            _logger.LogInformation("Saving Stripe payment method for customer {CustomerId} [CorrelationId: {CorrelationId}]", 
                request.CustomerId, correlationId);

            if (request.PaymentMethodDetails?.Card == null)
            {
                return new PaymentMethodResult
                {
                    IsSuccess = false,
                    FailureReason = "Card details are required for Stripe payment methods"
                };
            }

            var paymentMethodOptions = new PaymentMethodCreateOptions
            {
                Type = "card",
                Card = new PaymentMethodCardOptions
                {
                    Number = request.PaymentMethodDetails.Card.Number,
                    ExpMonth = request.PaymentMethodDetails.Card.ExpiryMonth,
                    ExpYear = request.PaymentMethodDetails.Card.ExpiryYear,
                    Cvc = request.PaymentMethodDetails.Card.Cvc
                }
            };

            if (!string.IsNullOrEmpty(request.PaymentMethodDetails.Card.HolderName))
            {
                paymentMethodOptions.BillingDetails = new PaymentMethodBillingDetailsOptions
                {
                    Name = request.PaymentMethodDetails.Card.HolderName
                };
            }

            var paymentMethod = await _paymentMethodService.CreateAsync(paymentMethodOptions);

            _logger.LogInformation("Stripe payment method saved for customer {CustomerId} [CorrelationId: {CorrelationId}]", 
                request.CustomerId, correlationId);

            return new PaymentMethodResult
            {
                IsSuccess = true,
                ProviderTokenId = paymentMethod.Id,
                Last4Digits = paymentMethod.Card?.Last4,
                Brand = paymentMethod.Card?.Brand,
                ExpiryMonth = (int?)paymentMethod.Card?.ExpMonth,
                ExpiryYear = (int?)paymentMethod.Card?.ExpYear,
                Metadata = new Dictionary<string, object>
                {
                    ["stripe_payment_method_id"] = paymentMethod.Id
                }
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error saving payment method for customer {CustomerId} [CorrelationId: {CorrelationId}]", 
                request.CustomerId, correlationId);
                
            return new PaymentMethodResult
            {
                IsSuccess = false,
                FailureReason = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error saving Stripe payment method for customer {CustomerId} [CorrelationId: {CorrelationId}]", 
                request.CustomerId, correlationId);
                
            return new PaymentMethodResult
            {
                IsSuccess = false,
                FailureReason = "An unexpected error occurred"
            };
        }
    }

    public async Task<bool> DeletePaymentMethodAsync(string providerTokenId, string correlationId)
    {
        try
        {
            _logger.LogInformation("Deleting Stripe payment method {TokenId} [CorrelationId: {CorrelationId}]", 
                providerTokenId, correlationId);

            await _paymentMethodService.DetachAsync(providerTokenId);

            _logger.LogInformation("Stripe payment method deleted {TokenId} [CorrelationId: {CorrelationId}]", 
                providerTokenId, correlationId);

            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error deleting payment method {TokenId} [CorrelationId: {CorrelationId}]", 
                providerTokenId, correlationId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting Stripe payment method {TokenId} [CorrelationId: {CorrelationId}]", 
                providerTokenId, correlationId);
            return false;
        }
    }
}
