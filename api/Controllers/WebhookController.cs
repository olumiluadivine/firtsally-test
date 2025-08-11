using api.Controllers.Base;
using application.Commands.Transactions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace api.Controllers;

[Route("api/[controller]")]
public class WebhookController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ILogger<WebhookController> _logger;
    private readonly IConfiguration _configuration;

    public WebhookController(
        IMediator mediator,
        ILogger<WebhookController> logger,
        IConfiguration configuration)
    {
        _mediator = mediator;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Handle Paystack webhook events
    /// </summary>
    [HttpPost("paystack")]
    public async Task<IActionResult> PaystackWebhook([FromBody] JsonElement payload)
    {
        try
        {
            // Verify webhook signature
            var signature = Request.Headers["x-paystack-signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Paystack webhook received without signature");
                return BadRequest("Missing signature");
            }

            var body = await new StreamReader(Request.Body).ReadToEndAsync();
            if (!VerifyPaystackSignature(body, signature))
            {
                _logger.LogWarning("Invalid Paystack webhook signature");
                return Unauthorized("Invalid signature");
            }

            // Parse the event
            var eventType = payload.GetProperty("event").GetString();
            var data = payload.GetProperty("data");

            _logger.LogInformation("Received Paystack webhook event: {EventType}", eventType);

            switch (eventType)
            {
                case "charge.success":
                    await HandleChargeSuccess(data);
                    break;

                case "charge.failed":
                    await HandleChargeFailed(data);
                    break;

                case "transfer.success":
                    await HandleTransferSuccess(data);
                    break;

                case "transfer.failed":
                    await HandleTransferFailed(data);
                    break;

                case "transfer.reversed":
                    await HandleTransferReversed(data);
                    break;

                default:
                    _logger.LogInformation("Unhandled Paystack webhook event: {EventType}", eventType);
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Paystack webhook");
            return StatusCode(500, "Internal server error");
        }
    }

    private async Task HandleChargeSuccess(JsonElement data)
    {
        try
        {
            var reference = data.GetProperty("reference").GetString();
            var paystackReference = data.GetProperty("reference").GetString();
            var amount = data.GetProperty("amount").GetDecimal() / 100; // Convert from kobo
            var status = data.GetProperty("status").GetString();

            _logger.LogInformation("Processing successful charge: {Reference}, Amount: {Amount}", reference, amount);

            if (status?.ToLower() == "success" && !string.IsNullOrEmpty(reference))
            {
                // Check if this is a deposit transaction
                if (reference.StartsWith("DEP_"))
                {
                    var command = new ConfirmDepositCommand(reference, paystackReference!);
                    var result = await _mediator.Send(command);

                    if (result.IsSuccessful)
                    {
                        _logger.LogInformation("Deposit confirmed via webhook: {Reference}", reference);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to confirm deposit via webhook: {Reference} - {Message}",
                            reference, result.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling charge success webhook");
        }
    }

    private async Task HandleChargeFailed(JsonElement data)
    {
        try
        {
            var reference = data.GetProperty("reference").GetString();
            var message = data.TryGetProperty("gateway_response", out var gatewayResponse)
                ? gatewayResponse.GetString()
                : "Payment failed";

            _logger.LogWarning("Payment failed for reference: {Reference} - {Message}", reference, message);

            // You could implement logic to notify the user or update transaction status
            // For now, we'll just log the failure
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling charge failed webhook");
        }
    }

    private async Task HandleTransferSuccess(JsonElement data)
    {
        try
        {
            var transferCode = data.GetProperty("transfer_code").GetString();
            var reference = data.GetProperty("reference").GetString();
            var amount = data.GetProperty("amount").GetDecimal() / 100;

            _logger.LogInformation("Transfer successful: {TransferCode}, Reference: {Reference}, Amount: {Amount}",
                transferCode, reference, amount);

            // Check if this is a withdrawal transfer
            if (!string.IsNullOrEmpty(reference) && reference.StartsWith("WTH_"))
            {
                var command = new ConfirmWithdrawalCommand(reference, transferCode!, true);
                var result = await _mediator.Send(command);

                if (result.IsSuccessful)
                {
                    _logger.LogInformation("Withdrawal confirmed via webhook: {Reference}", reference);
                }
                else
                {
                    _logger.LogWarning("Failed to confirm withdrawal via webhook: {Reference} - {Message}",
                        reference, result.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling transfer success webhook");
        }
    }

    private async Task HandleTransferFailed(JsonElement data)
    {
        try
        {
            var transferCode = data.GetProperty("transfer_code").GetString();
            var reference = data.GetProperty("reference").GetString();
            var message = data.TryGetProperty("message", out var msgProperty)
                ? msgProperty.GetString()
                : "Transfer failed";

            _logger.LogWarning("Transfer failed: {TransferCode}, Reference: {Reference} - {Message}",
                transferCode, reference, message);

            // Check if this is a withdrawal transfer
            if (!string.IsNullOrEmpty(reference) && reference.StartsWith("WTH_"))
            {
                var command = new ConfirmWithdrawalCommand(reference, transferCode!, false);
                var result = await _mediator.Send(command);

                if (!result.IsSuccessful)
                {
                    _logger.LogWarning("Failed to process failed withdrawal via webhook: {Reference} - {Message}",
                        reference, result.Message);
                }
                else
                {
                    _logger.LogInformation("Withdrawal failure processed and reversed via webhook: {Reference}", reference);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling transfer failed webhook");
        }
    }

    private async Task HandleTransferReversed(JsonElement data)
    {
        try
        {
            var transferCode = data.GetProperty("transfer_code").GetString();
            var reference = data.GetProperty("reference").GetString();
            var amount = data.GetProperty("amount").GetDecimal() / 100;

            _logger.LogInformation("Transfer reversed: {TransferCode}, Reference: {Reference}, Amount: {Amount}",
                transferCode, reference, amount);

            // Check if this is a withdrawal transfer
            if (!string.IsNullOrEmpty(reference) && reference.StartsWith("WTH_"))
            {
                var command = new ConfirmWithdrawalCommand(reference, transferCode!, false);
                var result = await _mediator.Send(command);

                if (result.IsSuccessful)
                {
                    _logger.LogInformation("Withdrawal reversal processed via webhook: {Reference}", reference);
                }
                else
                {
                    _logger.LogWarning("Failed to process withdrawal reversal via webhook: {Reference} - {Message}",
                        reference, result.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling transfer reversed webhook");
        }
    }

    private bool VerifyPaystackSignature(string body, string signature)
    {
        try
        {
            var secretKey = _configuration["Paystack:SecretKey"];
            if (string.IsNullOrEmpty(secretKey))
            {
                _logger.LogError("Paystack secret key not configured");
                return false;
            }

            var hash = ComputeHmacSha512(body, secretKey);
            return hash.Equals(signature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Paystack signature");
            return false;
        }
    }

    private static string ComputeHmacSha512(string message, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA512(keyBytes);
        var hash = hmac.ComputeHash(messageBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}