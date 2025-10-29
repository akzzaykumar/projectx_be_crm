# Payment Integration - Production Ready Setup

## ✅ Implemented Features

### 1. **Razorpay Service** (`RazorpayService.cs`)
- Complete API integration with Razorpay v1
- Order creation for payment initiation
- Payment verification and signature validation
- Refund processing (full and partial)
- Webhook signature verification (HMAC-SHA256)
- Comprehensive error handling and logging

### 2. **Payment APIs**
- **GET /api/Payments/methods** - List available payment methods
- **POST /api/Payments/initiate** - Create Razorpay order for booking
- **GET /api/Payments/{id}** - Get payment details with authorization
- **POST /api/Bookings/webhook/payment** - Webhook handler (already implemented)

### 3. **Configuration Management**
- Environment variable support (.env)
- appsettings.json configuration
- Secure credential management
- Multiple environment support (Dev/Staging/Prod)

## 📁 Files Created/Modified

### New Files:
1. `.env.example` - Environment variables template
2. `IRazorpayService.cs` - Razorpay service interface
3. `RazorpayService.cs` - Production-ready Razorpay integration
4. `GetPaymentMethods` - Query for payment methods
5. `InitiatePayment` - Command to create payment orders
6. `GetPaymentById` - Query for payment details
7. `PaymentsController.cs` - REST API endpoints
8. `RAZORPAY_INTEGRATION.md` - Complete integration guide

### Modified Files:
1. `DependencyInjection.cs` - Registered Razorpay service with HttpClient
2. `InitiatePaymentCommandHandler.cs` - Real Razorpay API integration
3. `CancelBookingCommandHandler.cs` - Real refund processing
4. `appsettings.Development.json` - Added Razorpay configuration

## 🔧 Setup Instructions

### 1. Environment Configuration

Create `.env` file (copy from `.env.example`):
```bash
# Razorpay Test Keys (from Razorpay Dashboard)
RAZORPAY_KEY_ID=rzp_test_xxxxxxxxxx
RAZORPAY_KEY_SECRET=your_test_secret_key
RAZORPAY_WEBHOOK_SECRET=your_webhook_secret

# URLs
PAYMENT_GATEWAY_CALLBACK_URL=http://localhost:5154/api/bookings/webhook/payment
```

### 2. appsettings.json Configuration

```json
{
  "Razorpay": {
    "KeyId": "rzp_test_xxxxxxxxxx",
    "KeySecret": "your-razorpay-secret-key",
    "WebhookSecret": "your-razorpay-webhook-secret"
  },
  "PaymentGateway": {
    "CallbackUrl": "https://api.funbookr.com/api/bookings/webhook/payment",
    "SuccessUrl": "https://funbookr.com/booking/success",
    "FailureUrl": "https://funbookr.com/booking/failure"
  }
}
```

### 3. Get Razorpay Credentials

1. Sign up at https://razorpay.com
2. Go to Settings → API Keys
3. Generate Test/Live keys
4. Go to Settings → Webhooks
5. Add webhook URL: `https://your-domain.com/api/bookings/webhook/payment`
6. Select events: `payment.captured`, `payment.failed`, `refund.created`
7. Copy webhook secret

## 🚀 Payment Flow

### Customer Flow:
```
1. Customer creates booking → Booking (Pending)
2. Customer calls /payments/initiate
   ↓
3. Backend creates Razorpay order via API
   ↓
4. Returns order details (orderId, key, amount)
   ↓
5. Frontend shows Razorpay checkout
   ↓
6. Customer completes payment
   ↓
7. Razorpay webhook → /bookings/webhook/payment
   ↓
8. Backend verifies signature
   ↓
9. Updates Payment status → Completed
   ↓
10. Updates Booking status → Confirmed
```

### Refund Flow:
```
1. Customer cancels booking
   ↓
2. System calculates refund (48+ hrs = 100%, 24-48 hrs = 50%)
   ↓
3. Calls RazorpayService.CreateRefundAsync()
   ↓
4. Razorpay processes refund
   ↓
5. Updates Payment with refund details
   ↓
6. Booking marked as Cancelled
```

## 🔒 Security Features

### 1. Webhook Signature Verification
```csharp
// HMAC-SHA256 signature verification
var webhookSecret = _configuration["Razorpay:WebhookSecret"];
using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
var computedSignature = hmac.ComputeHash(Encoding.UTF8.GetBytes(webhookBody));
```

### 2. Authorization Checks
- Customers can only initiate payments for their own bookings
- Ownership verification before payment initiation
- Duplicate payment prevention

### 3. Idempotency
- Checks if payment already completed
- Prevents duplicate webhook processing
- Safe retry mechanisms

## 📊 API Examples

### Initiate Payment
```bash
curl -X POST https://api.funbookr.com/api/Payments/initiate \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "bookingId": "550e8400-e29b-41d4-a716-446655440000",
    "paymentGateway": "Razorpay"
  }'
```

Response:
```json
{
  "success": true,
  "data": {
    "paymentId": "guid",
    "paymentReference": "PAY20251026ABC123",
    "amount": 5600.00,
    "currency": "INR",
    "gatewayOrderId": "order_razorpay_123456",
    "gatewayKey": "rzp_live_xxxxxxxxxx",
    "callbackUrl": "https://api.funbookr.com/api/bookings/webhook/payment"
  }
}
```

## 🧪 Testing

### Test Mode
Razorpay provides test cards:
- **Success**: 4111 1111 1111 1111 (any CVV, future expiry)
- **Failure**: 4000 0000 0000 0002
- **3D Secure**: 4000 0027 6000 3184

### Test UPI
- **Success**: success@razorpay
- **Failure**: failure@razorpay

### Local Webhook Testing
Use ngrok for local development:
```bash
ngrok http 5154
# Update Razorpay webhook URL to ngrok URL
https://xxxx.ngrok.io/api/bookings/webhook/payment
```

## 📝 Production Checklist

- [ ] Replace test keys with live Razorpay keys
- [ ] Set environment variables in hosting platform
- [ ] Configure webhook URL in Razorpay dashboard (production)
- [ ] Enable SSL certificate for API
- [ ] Set up monitoring (Application Insights/Serilog)
- [ ] Configure rate limiting
- [ ] Test payment flow end-to-end
- [ ] Test refund flow
- [ ] Set up alerts for payment failures
- [ ] Document runbooks for payment issues
- [ ] Set up backup payment gateway (optional)

## 🔍 Monitoring & Logging

All payment operations are logged:
- Payment initiation (booking ID, amount)
- Razorpay order creation (order ID, payment ID)
- Webhook events (signature verification, event type)
- Refund processing (refund ID, amount, reason)
- All errors with context

Log levels:
- **Information**: Normal operations
- **Warning**: Signature verification failures, missing transaction IDs
- **Error**: API failures, refund failures

## 🛠️ Troubleshooting

### Payment Initiation Fails
- Check Razorpay credentials in configuration
- Verify API keys are correct (KeyId and KeySecret)
- Check logs for API error responses
- Ensure amount is > 0

### Webhook Not Received
- Verify webhook URL is publicly accessible
- Check webhook secret matches configuration
- Review Razorpay dashboard webhook logs
- Ensure endpoint returns 200 OK

### Refund Fails
- Verify payment was captured (status = Completed)
- Check payment has gateway transaction ID
- Ensure refund amount ≤ remaining amount
- Review Razorpay dashboard for refund status

## 💡 Best Practices

1. **Always use environment variables** for secrets in production
2. **Verify webhook signatures** to prevent fraudulent requests
3. **Log all payment operations** for audit trail
4. **Handle errors gracefully** and provide clear messages
5. **Test refund flow thoroughly** before going live
6. **Monitor payment success rates** and set up alerts
7. **Keep backup of payment records** for compliance
8. **Use idempotency keys** for payment operations

## 📚 Additional Resources

- [Razorpay API Documentation](https://razorpay.com/docs/api/)
- [Razorpay Webhook Guide](https://razorpay.com/docs/webhooks/)
- [Razorpay Test Cards](https://razorpay.com/docs/payments/payments/test-card-details/)
- [RAZORPAY_INTEGRATION.md](./RAZORPAY_INTEGRATION.md) - Detailed integration guide

## 🆘 Support

For issues:
1. Check application logs
2. Review Razorpay dashboard
3. Verify configuration values
4. Test with Razorpay test mode first
5. Contact Razorpay support if API issues persist

---

**Last Updated**: October 26, 2025
**Version**: 1.0
**Status**: Production Ready ✅
