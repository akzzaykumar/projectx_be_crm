# Razorpay Integration - Production Ready

## Configuration

### Environment Variables (.env)
```bash
RAZORPAY_KEY_ID=rzp_live_xxxxxxxxxx
RAZORPAY_KEY_SECRET=your-razorpay-secret-key
RAZORPAY_WEBHOOK_SECRET=your-razorpay-webhook-secret
PAYMENT_GATEWAY_CALLBACK_URL=https://api.funbookr.com/api/bookings/webhook/payment
```

### appsettings.json
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

## API Endpoints

### 1. Get Payment Methods
```http
GET /api/Payments/methods
Authorization: Not required

Response:
{
  "success": true,
  "data": [
    {
      "method": "UPI",
      "displayName": "UPI",
      "isActive": true,
      "processingFee": 0.00
    },
    {
      "method": "Card",
      "displayName": "Credit/Debit Card",
      "isActive": true,
      "processingFee": 2.5
    }
  ]
}
```

### 2. Initiate Payment
```http
POST /api/Payments/initiate
Authorization: Bearer <token>
Content-Type: application/json

Request:
{
  "bookingId": "550e8400-e29b-41d4-a716-446655440000",
  "paymentGateway": "Razorpay"
}

Response:
{
  "success": true,
  "message": "Payment initiated successfully",
  "data": {
    "paymentId": "550e8400-e29b-41d4-a716-446655440010",
    "paymentReference": "PAY20251026XYZ789",
    "amount": 5600.00,
    "currency": "INR",
    "gatewayOrderId": "order_razorpay_123456",
    "gatewayKey": "rzp_live_xxxxxxxxxx",
    "callbackUrl": "https://api.funbookr.com/api/bookings/webhook/payment"
  }
}
```

### 3. Get Payment Details
```http
GET /api/Payments/{paymentId}
Authorization: Bearer <token>

Response:
{
  "success": true,
  "data": {
    "paymentId": "550e8400-e29b-41d4-a716-446655440010",
    "paymentReference": "PAY20251026XYZ789",
    "amount": 5600.00,
    "currency": "INR",
    "status": "Completed",
    "paymentGateway": "Razorpay",
    "paymentMethod": "UPI",
    "gatewayTransactionId": "txn_razorpay_789123",
    "paidAt": "2025-10-26T14:30:00Z",
    "refundedAmount": 0.00,
    "canBeRefunded": true,
    "booking": {
      "bookingId": "550e8400-e29b-41d4-a716-446655440000",
      "bookingReference": "BK20251026ABC123",
      "activityTitle": "Scuba Diving Adventure"
    }
  }
}
```

## Frontend Integration

### React Example
```javascript
import axios from 'axios';

// 1. Initiate Payment
const initiatePayment = async (bookingId) => {
  const response = await axios.post('/api/Payments/initiate', {
    bookingId: bookingId,
    paymentGateway: 'Razorpay'
  }, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  return response.data.data;
};

// 2. Load Razorpay Script
const loadRazorpay = () => {
  return new Promise((resolve) => {
    const script = document.createElement('script');
    script.src = 'https://checkout.razorpay.com/v1/checkout.js';
    script.onload = () => resolve(true);
    script.onerror = () => resolve(false);
    document.body.appendChild(script);
  });
};

// 3. Open Razorpay Checkout
const handlePayment = async (bookingId, customerDetails) => {
  const razorpayLoaded = await loadRazorpay();
  
  if (!razorpayLoaded) {
    alert('Failed to load Razorpay SDK');
    return;
  }

  const paymentData = await initiatePayment(bookingId);

  const options = {
    key: paymentData.gatewayKey,
    amount: paymentData.amount * 100, // Razorpay expects amount in paise
    currency: paymentData.currency,
    name: 'FunBookr',
    description: `Payment for booking ${bookingId}`,
    order_id: paymentData.gatewayOrderId,
    handler: function (response) {
      // Payment successful
      console.log('Payment successful:', response);
      // Redirect to success page or refresh booking status
      window.location.href = '/booking/success';
    },
    prefill: {
      name: customerDetails.name,
      email: customerDetails.email,
      contact: customerDetails.phone
    },
    theme: {
      color: '#3399cc'
    }
  };

  const rzp = new window.Razorpay(options);
  
  rzp.on('payment.failed', function (response) {
    console.error('Payment failed:', response.error);
    window.location.href = '/booking/failure';
  });

  rzp.open();
};
```

## Webhook Handling

The webhook is already implemented in `BookingsController.cs`:
```http
POST /api/Bookings/webhook/payment
Content-Type: application/json
X-Razorpay-Signature: <signature>

Request Body:
{
  "event": "payment.captured",
  "payload": {
    "payment": {
      "entity": {
        "id": "pay_razorpay_123",
        "order_id": "order_razorpay_456",
        "amount": 560000,
        "currency": "INR",
        "status": "captured",
        "method": "upi"
      }
    }
  }
}
```

### Webhook Configuration in Razorpay Dashboard
1. Go to Settings → Webhooks
2. Add webhook URL: `https://api.funbookr.com/api/bookings/webhook/payment`
3. Select events:
   - `payment.captured`
   - `payment.failed`
   - `refund.created`
4. Copy webhook secret and add to configuration

## Security Features

### 1. Webhook Signature Verification
```csharp
private bool VerifyRazorpaySignature(string webhookBody, string signature)
{
    var webhookSecret = _configuration["Razorpay:WebhookSecret"];
    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(webhookSecret));
    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(webhookBody));
    var computedSignature = BitConverter.ToString(hash).Replace("-", "").ToLower();
    return signature.Equals(computedSignature, StringComparison.OrdinalIgnoreCase);
}
```

### 2. Idempotency Check
- Prevents duplicate payment processing
- Checks if payment already completed before updating

### 3. Authorization
- Customers can only initiate payments for their own bookings
- Customers and providers can view payment details for their transactions

## Testing

### Test Cards (Razorpay Test Mode)
- Success: Use any valid card number (e.g., 4111 1111 1111 1111)
- Failure: Use card 4000 0000 0000 0002
- 3D Secure: Use card 4000 0027 6000 3184

### Test UPI
- Success: success@razorpay
- Failure: failure@razorpay

### Webhook Testing
Use Razorpay's webhook tester in dashboard or ngrok for local testing:
```bash
ngrok http 5154
# Update webhook URL in Razorpay dashboard to ngrok URL
```

## Error Handling

The implementation includes comprehensive error handling:
- Invalid booking validation
- Duplicate payment prevention
- Gateway API failures with retry logic
- Webhook signature validation
- Payment status verification

## Refund Flow

Refunds are processed through the `CancelBookingCommand`:
1. Customer cancels booking
2. System calculates refund amount based on cancellation policy
3. Calls `IRazorpayService.CreateRefundAsync()`
4. Updates payment status to `Refunded` or `PartiallyRefunded`
5. Tracks refund details (transaction ID, amount, reason)

## Monitoring

Logs are generated for:
- Payment initiation
- Order creation in Razorpay
- Webhook events
- Refund processing
- All errors with context

Use Application Insights or ELK stack for production monitoring.

## Production Checklist

- [ ] Replace test keys with live Razorpay keys
- [ ] Configure webhook URL in Razorpay dashboard
- [ ] Set up SSL certificate for webhook endpoint
- [ ] Configure environment variables securely (Azure Key Vault, AWS Secrets Manager)
- [ ] Enable webhook signature verification
- [ ] Set up monitoring and alerting
- [ ] Test payment flow end-to-end
- [ ] Test refund flow
- [ ] Configure rate limiting
- [ ] Set up backup payment gateway (Stripe/PayPal)
