using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Models.DTOs;
using PaymentService.Services;
using System.ComponentModel.DataAnnotations;

namespace PaymentService.Controllers;

/// <summary>
/// Payment processing controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentService paymentService,
        ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Process a payment
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PaymentResultDto>> ProcessPayment([FromBody] ProcessPaymentDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _paymentService.ProcessPaymentAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId}", request.OrderId);
            return StatusCode(500, new PaymentResultDto 
            { 
                IsSuccess = false, 
                ErrorMessage = "An unexpected error occurred" 
            });
        }
    }

    /// <summary>
    /// Process a refund
    /// </summary>
    [HttpPost("{paymentId}/refund")]
    public async Task<ActionResult<RefundResultDto>> ProcessRefund(
        [FromRoute] Guid paymentId, 
        [FromBody] ProcessRefundDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Ensure the paymentId in the route matches the request
        request.PaymentId = paymentId.ToString();

        try
        {
            var result = await _paymentService.ProcessRefundAsync(request);
            
            if (result.IsSuccess)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for payment {PaymentId}", paymentId);
            return StatusCode(500, new RefundResultDto 
            { 
                IsSuccess = false, 
                ErrorMessage = "An unexpected error occurred" 
            });
        }
    }

    /// <summary>
    /// Get payment details
    /// </summary>
    [HttpGet("{paymentId}")]
    public async Task<ActionResult<PaymentDto>> GetPayment([FromRoute] Guid paymentId)
    {
        try
        {
            var payment = await _paymentService.GetPaymentAsync(paymentId);
            
            if (payment == null)
            {
                return NotFound(new { Message = "Payment not found" });
            }

            return Ok(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment {PaymentId}", paymentId);
            return StatusCode(500, new { Message = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get payments with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<PaymentDto>>> GetPayments(
        [FromQuery] string? customerId = null,
        [FromQuery] string? orderId = null,
        [FromQuery, Range(0, int.MaxValue)] int skip = 0,
        [FromQuery, Range(1, 100)] int take = 50)
    {
        try
        {
            var payments = await _paymentService.GetPaymentsAsync(customerId, orderId, skip, take);
            return Ok(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments");
            return StatusCode(500, new { Message = "An unexpected error occurred" });
        }
    }

    /// <summary>
    /// Get payment by order ID
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<ActionResult<PaymentDto>> GetPaymentByOrderId([FromRoute] string orderId)
    {
        try
        {
            var payments = await _paymentService.GetPaymentsAsync(orderId: orderId, take: 1);
            var payment = payments.FirstOrDefault();
            
            if (payment == null)
            {
                return NotFound(new { Message = "Payment not found for this order" });
            }

            return Ok(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment for order {OrderId}", orderId);
            return StatusCode(500, new { Message = "An unexpected error occurred" });
        }
    }
}
