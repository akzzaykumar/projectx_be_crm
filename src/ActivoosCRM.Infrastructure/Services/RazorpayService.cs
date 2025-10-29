using ActivoosCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ActivoosCRM.Infrastructure.Services;

/// <summary>
/// Razorpay payment gateway integration service
/// </summary>
public class RazorpayService : IRazorpayService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RazorpayService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _keyId;
    private readonly string _keySecret;

    public RazorpayService(
        IConfiguration configuration,
        ILogger<RazorpayService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("Razorpay");

        _keyId = configuration["Razorpay:KeyId"]
            ?? Environment.GetEnvironmentVariable("RAZORPAY_KEY_ID")
            ?? throw new InvalidOperationException("Razorpay Key ID not configured");

        _keySecret = configuration["Razorpay:KeySecret"]
            ?? Environment.GetEnvironmentVariable("RAZORPAY_KEY_SECRET")
            ?? throw new InvalidOperationException("Razorpay Key Secret not configured");

        // Configure HTTP client with Basic Auth
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_keyId}:{_keySecret}"));
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
        _httpClient.BaseAddress = new Uri("https://api.razorpay.com/v1/");
    }

    public async Task<RazorpayOrderResponse> CreateOrderAsync(
        decimal amount,
        string currency,
        string bookingReference,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Razorpay expects amount in paise (smallest currency unit)
            var amountInPaise = (int)(amount * 100);

            var orderData = new
            {
                amount = amountInPaise,
                currency = currency.ToUpperInvariant(),
                receipt = bookingReference,
                notes = new
                {
                    booking_reference = bookingReference,
                    created_at = DateTime.UtcNow.ToString("O")
                }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(orderData),
                Encoding.UTF8,
                "application/json");

            _logger.LogInformation("Creating Razorpay order for booking {BookingReference}, Amount: {Amount} {Currency}",
                bookingReference, amount, currency);

            var response = await _httpClient.PostAsync("orders", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Razorpay order creation failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to create Razorpay order: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var orderResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var result = new RazorpayOrderResponse
            {
                OrderId = orderResponse.GetProperty("id").GetString() ?? string.Empty,
                Amount = orderResponse.GetProperty("amount").GetInt32() / 100m,
                Currency = orderResponse.GetProperty("currency").GetString() ?? string.Empty,
                Status = orderResponse.GetProperty("status").GetString() ?? string.Empty,
                Receipt = orderResponse.GetProperty("receipt").GetString() ?? string.Empty,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(orderResponse.GetProperty("created_at").GetInt64()).UtcDateTime
            };

            _logger.LogInformation("Razorpay order created successfully: {OrderId}", result.OrderId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Razorpay order for booking {BookingReference}", bookingReference);
            throw;
        }
    }

    public bool VerifyWebhookSignature(string webhookBody, string signature, string webhookSecret)
    {
        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(webhookBody));
            var computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

            var isValid = signature.Equals(computedSignature, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
            {
                _logger.LogWarning("Webhook signature verification failed. Expected: {Expected}, Got: {Got}",
                    computedSignature, signature);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying webhook signature");
            return false;
        }
    }

    public bool VerifyPaymentSignature(string orderId, string paymentId, string signature)
    {
        try
        {
            var message = $"{orderId}|{paymentId}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_keySecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            var computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();

            return signature.Equals(computedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment signature");
            return false;
        }
    }

    public async Task<RazorpayPaymentResponse> FetchPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching payment details for {PaymentId}", paymentId);

            var response = await _httpClient.GetAsync($"payments/{paymentId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Razorpay fetch payment failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to fetch payment: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var paymentData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var result = new RazorpayPaymentResponse
            {
                PaymentId = paymentData.GetProperty("id").GetString() ?? string.Empty,
                OrderId = paymentData.GetProperty("order_id").GetString() ?? string.Empty,
                Amount = paymentData.GetProperty("amount").GetInt32() / 100m,
                Currency = paymentData.GetProperty("currency").GetString() ?? string.Empty,
                Status = paymentData.GetProperty("status").GetString() ?? string.Empty,
                Method = paymentData.GetProperty("method").GetString() ?? string.Empty,
                Email = paymentData.TryGetProperty("email", out var email) ? email.GetString() : null,
                Contact = paymentData.TryGetProperty("contact", out var contact) ? contact.GetString() : null,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(paymentData.GetProperty("created_at").GetInt64()).UtcDateTime
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching payment {PaymentId}", paymentId);
            throw;
        }
    }

    public async Task<RazorpayRefundResponse> CreateRefundAsync(
        string paymentId,
        decimal amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var amountInPaise = (int)(amount * 100);

            var refundData = new
            {
                amount = amountInPaise,
                speed = "normal",
                notes = new
                {
                    reason = reason,
                    created_at = DateTime.UtcNow.ToString("O")
                },
                receipt = $"refund_{DateTime.UtcNow:yyyyMMddHHmmss}"
            };

            var content = new StringContent(
                JsonSerializer.Serialize(refundData),
                Encoding.UTF8,
                "application/json");

            _logger.LogInformation("Creating refund for payment {PaymentId}, Amount: {Amount}, Reason: {Reason}",
                paymentId, amount, reason);

            var response = await _httpClient.PostAsync($"payments/{paymentId}/refund", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Razorpay refund creation failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to create refund: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var refundResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var result = new RazorpayRefundResponse
            {
                RefundId = refundResponse.GetProperty("id").GetString() ?? string.Empty,
                PaymentId = refundResponse.GetProperty("payment_id").GetString() ?? string.Empty,
                Amount = refundResponse.GetProperty("amount").GetInt32() / 100m,
                Currency = refundResponse.GetProperty("currency").GetString() ?? string.Empty,
                Status = refundResponse.GetProperty("status").GetString() ?? string.Empty,
                SpeedRequested = refundResponse.TryGetProperty("speed_requested", out var speedReq) ? speedReq.GetString() : null,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(refundResponse.GetProperty("created_at").GetInt64()).UtcDateTime
            };

            _logger.LogInformation("Refund created successfully: {RefundId}", result.RefundId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refund for payment {PaymentId}", paymentId);
            throw;
        }
    }

    public async Task<RazorpayRefundResponse> FetchRefundAsync(string refundId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching refund details for {RefundId}", refundId);

            var response = await _httpClient.GetAsync($"refunds/{refundId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Razorpay fetch refund failed: {StatusCode} - {Error}",
                    response.StatusCode, errorContent);
                throw new InvalidOperationException($"Failed to fetch refund: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var refundData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            var result = new RazorpayRefundResponse
            {
                RefundId = refundData.GetProperty("id").GetString() ?? string.Empty,
                PaymentId = refundData.GetProperty("payment_id").GetString() ?? string.Empty,
                Amount = refundData.GetProperty("amount").GetInt32() / 100m,
                Currency = refundData.GetProperty("currency").GetString() ?? string.Empty,
                Status = refundData.GetProperty("status").GetString() ?? string.Empty,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(refundData.GetProperty("created_at").GetInt64()).UtcDateTime
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching refund {RefundId}", refundId);
            throw;
        }
    }
}
