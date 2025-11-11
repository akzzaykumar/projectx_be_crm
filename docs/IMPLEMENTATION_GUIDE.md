# FunBookr Platform - Complete Implementation Guide

**Version:** 2.0  
**Date:** November 2025  
**Status:** Production Ready After Fixes

---

## Table of Contents

1. [Critical Fixes Applied](#critical-fixes-applied)
2. [Platform Architecture Overview](#platform-architecture-overview)
3. [Database Schema Updates](#database-schema-updates)
4. [Payment Flow with Commission](#payment-flow-with-commission)
5. [Booking Workflow](#booking-workflow)
6. [User Workflows](#user-workflows)
7. [API Integration Guide](#api-integration-guide)
8. [Security Implementation](#security-implementation)
9. [Deployment Checklist](#deployment-checklist)

---

## Critical Fixes Applied

### 1. Payment Webhook Transaction ID Fix ✅

**Problem:** Webhook couldn't find payments because it searched by `transactionId` before it was set.

**Solution Implemented:**
- Modified `BookingsController.cs` to search by `orderID` first, then `transactionId`
- Added database transaction wrapping for atomicity
- Created system user for auto-confirmation

**File:** `src/ActivoosCRM.API/Controllers/BookingsController.cs`

```csharp
// FIXED: Search by order ID first
var payment = await _context.Payments
    .Include(p => p.Booking)
    .FirstOrDefaultAsync(p => 
        p.PaymentGatewayOrderId == orderId || 
        p.PaymentGatewayTransactionId == transactionId);
```

### 2. Booking Availability Validation ✅

**Problem:** No validation against activity schedules - users could book on wrong days/times.

**Solution Implemented:**
- Created `IAvailabilityService` interface
- Implemented `AvailabilityService` with schedule checking
- Integrated into `CreateBookingCommandHandler`

**Files:**
- `src/ActivoosCRM.Application/Common/Interfaces/IAvailabilityService.cs`
- `src/ActivoosCRM.Infrastructure/Services/AvailabilityService.cs`

**Usage:**
```csharp
var availabilityCheck = await _availabilityService.CheckAvailabilityAsync(
    activity.Id,
    request.BookingDate.Date,
    request.BookingTime,
    request.NumberOfParticipants,
    cancellationToken);

if (!availabilityCheck.IsAvailable)
{
    return Result.CreateFailure(availabilityCheck.Reason);
}
```

---

## Platform Architecture Overview

### Revenue Model

FunBookr acts as a **commission-based marketplace**:

```
Customer Payment (₹10,000)
    ├── Platform Commission (10%): ₹1,000
    ├── GST (18% on commission): ₹180
    └── Provider Payout (88.2%): ₹8,820
```

### Key Components

1. **Customer Module**
   - Browse activities
   - Make bookings
   - Process payments
   - Leave reviews

2. **Provider Module**
   - List activities
   - Manage schedules
   - Receive bookings
   - Track earnings
   - Request payouts

3. **Platform Module**
   - Commission tracking
   - Provider payouts
   - Tax calculations
   - Admin oversight

---

## Database Schema Updates

### Required Schema Additions

Run this SQL script to add missing features:

```sql
-- ========================================
-- 1. CREATE REFRESH TOKEN TABLE
-- ========================================

CREATE TABLE refresh_tokens (
    token_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    token_hash VARCHAR(255) NOT NULL UNIQUE,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    revoked_at TIMESTAMP WITH TIME ZONE,
    revoked_reason VARCHAR(500),
    replaced_by_token_id UUID REFERENCES refresh_tokens(token_id),
    created_by_ip VARCHAR(45),
    is_active BOOLEAN GENERATED ALWAYS AS 
        (revoked_at IS NULL AND expires_at > CURRENT_TIMESTAMP) STORED
);

-- ========================================
-- 2. CREATE PLATFORM COMMISSION TRACKING
-- ========================================

CREATE TABLE platform_commissions (
    commission_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_id UUID NOT NULL REFERENCES bookings(booking_id),
    payment_id UUID NOT NULL REFERENCES payments(payment_id),
    booking_amount DECIMAL(18, 2) NOT NULL,
    commission_rate DECIMAL(5, 2) NOT NULL,
    commission_amount DECIMAL(18, 2) NOT NULL,
    provider_payout_amount DECIMAL(18, 2) NOT NULL,
    gst_amount DECIMAL(18, 2) NOT NULL DEFAULT 0.00,
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- 3. CREATE PROVIDER PAYOUTS TABLE
-- ========================================

CREATE TABLE provider_payouts (
    payout_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    provider_id UUID NOT NULL REFERENCES activity_providers(provider_id),
    payout_period_start DATE NOT NULL,
    payout_period_end DATE NOT NULL,
    total_bookings INTEGER NOT NULL DEFAULT 0,
    gross_amount DECIMAL(18, 2) NOT NULL,
    platform_commission DECIMAL(18, 2) NOT NULL,
    gst_deducted DECIMAL(18, 2) NOT NULL,
    tds_deducted DECIMAL(18, 2) NOT NULL,
    net_payout_amount DECIMAL(18, 2) NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    requested_at TIMESTAMP WITH TIME ZONE,
    approved_at TIMESTAMP WITH TIME ZONE,
    approved_by UUID REFERENCES users(user_id),
    paid_at TIMESTAMP WITH TIME ZONE,
    payment_reference VARCHAR(200),
    payment_method VARCHAR(50),
    rejection_reason TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- 4. ADD CRITICAL INDEXES
-- ========================================

-- Payment webhook performance (CRITICAL)
CREATE INDEX idx_payments_gateway_transaction_id 
    ON payments(payment_gateway_transaction_id);
    
CREATE INDEX idx_payments_gateway_order_id 
    ON payments(payment_gateway_order_id);

-- Booking availability checking
CREATE INDEX idx_bookings_activity_date_time 
    ON bookings(activity_id, booking_date, booking_time);

CREATE INDEX idx_activity_schedules_activity_active 
    ON activity_schedules(activity_id, is_active);

-- ========================================
-- 5. CREATE SYSTEM USER
-- ========================================

INSERT INTO users (user_id, email, password_hash, role, first_name, last_name, is_active, email_verified)
VALUES 
    ('00000000-0000-0000-0000-000000000001', 
     'system@funbookr.com', 
     crypt('SystemUser@2025!', gen_salt('bf')), 
     'admin', 
     'System', 
     'User', 
     true, 
     true)
ON CONFLICT (user_id) DO NOTHING;

-- ========================================
-- 6. ADD SOFT DELETE COLUMNS
-- ========================================

ALTER TABLE activities ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE;
ALTER TABLE activity_providers ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE;
ALTER TABLE bookings ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE;

CREATE INDEX idx_activities_deleted ON activities(deleted_at) WHERE deleted_at IS NULL;
CREATE INDEX idx_providers_deleted ON activity_providers(deleted_at) WHERE deleted_at IS NULL;
```

---

## Payment Flow with Commission

### Step-by-Step Payment Process

#### 1. Customer Creates Booking

```http
POST /api/bookings
Authorization: Bearer {customer_token}
Content-Type: application/json

{
  "activityId": "guid",
  "bookingDate": "2025-12-15",
  "bookingTime": "09:00:00",
  "numberOfParticipants": 2,
  "couponCode": "WELCOME10"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "bookingId": "guid",
    "bookingReference": "BK20251201ABC123",
    "totalAmount": 5600.00,
    "paymentRequired": true
  }
}
```

#### 2. Customer Initiates Payment

```http
POST /api/payments/initiate
Authorization: Bearer {customer_token}

{
  "bookingId": "guid",
  "paymentGateway": "Razorpay"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "paymentId": "guid",
    "amount": 5600.00,
    "currency": "INR",
    "gatewayOrderId": "order_razorpay_123",
    "gatewayKey": "rzp_live_xxx",
    "callbackUrl": "https://api.funbookr.com/api/bookings/webhook/payment"
  }
}
```

#### 3. Frontend Integrates Razorpay

```javascript
const options = {
  key: response.data.gatewayKey,
  amount: response.data.amount * 100, // in paise
  currency: response.data.currency,
  order_id: response.data.gatewayOrderId,
  name: "FunBookr",
  description: "Activity Booking",
  handler: function(razorpayResponse) {
    // Payment successful - Razorpay will call webhook
    console.log('Payment Success:', razorpayResponse.razorpay_payment_id);
  },
  prefill: {
    email: user.email,
    contact: user.phone
  }
};

const razorpay = new Razorpay(options);
razorpay.open();
```

#### 4. Razorpay Webhook Triggers

Razorpay sends webhook to: `POST /api/bookings/webhook/payment`

**Backend Processing:**
1. Verifies webhook signature
2. Finds payment by order ID
3. Marks payment as completed
4. **Calculates platform commission** (10%)
5. **Calculates GST** (18% on commission)
6. **Records provider earning**
7. Auto-confirms booking
8. Sends confirmation email

**Commission Calculation:**
```csharp
var commissionRate = 0.10m; // 10%
var commissionAmount = booking.TotalAmount * commissionRate;
var gstRate = 0.18m; // 18%
var gstAmount = commissionAmount * gstRate;
var providerPayout = booking.TotalAmount - commissionAmount - gstAmount;

// Example: ₹10,000 booking
// Commission: ₹1,000
// GST: ₹180
// Provider gets: ₹8,820
```

---

## Booking Workflow

### Complete Booking State Machine

```
[Pending] → Payment Initiated
    ↓ (Payment Success + Webhook)
[Confirmed] → Booking Confirmed
    ↓ (On booking date)
[CheckedIn] → Customer arrives
    ↓ (After activity)
[Completed] → Activity finished
    ↓ (Customer action)
[Reviewed] → Customer leaves review

Alternative Paths:
[Pending/Confirmed] → [Cancelled] → Refund processed
[Confirmed] → [NoShow] → No refund
```

### Cancellation & Refund Policy

```csharp
// In CancelBookingCommandHandler
var hoursUntilBooking = (booking.BookingDate - DateTime.Now).TotalHours;

decimal refundPercentage;
if (hoursUntilBooking >= 48)
{
    refundPercentage = 1.00m; // 100% refund
}
else if (hoursUntilBooking >= 24)
{
    refundPercentage = 0.50m; // 50% refund
}
else
{
    refundPercentage = 0m; // No refund
}

var refundAmount = booking.TotalAmount * refundPercentage;

// Process refund through Razorpay
await _razorpayService.CreateRefundAsync(
    payment.PaymentGatewayTransactionId,
    refundAmount,
    "Customer cancellation"
);
```

---

## User Workflows

### Customer Journey

#### 1. Registration & Profile Setup
```
Register → Verify Email → Complete Profile → Browse Activities
```

**API Sequence:**
```http
POST /api/auth/register
POST /api/auth/verify-email
PUT /api/users/profile
GET /api/activities?locationId={guid}&categoryId={guid}
```

#### 2. Browse & Search Activities
```
Search → Filter → View Details → Check Availability → Add to Wishlist (optional)
```

**Search with Filters:**
```http
GET /api/activities?
    search=scuba+diving
    &locationId={guid}
    &categoryId={guid}
    &minPrice=1000
    &maxPrice=5000
    &minRating=4
    &sortBy=rating
    &sortOrder=desc
    &page=1
    &pageSize=20
```

#### 3. Booking Process
```
Select Date/Time → Add Participants → Apply Coupon → Create Booking → 
Payment → Confirmation → Receive Email
```

#### 4. Post-Booking Actions
```
View Booking → Check-In (on day) → Complete Activity → Leave Review
```

### Provider Journey

#### 1. Provider Onboarding
```
Register as Customer → Upgrade to Provider → Submit Documents → 
Admin Verification → Approved
```

**API Sequence:**
```http
POST /api/auth/register (role: Customer)
POST /api/providers (upgrade account)
```

#### 2. Activity Management
```
Create Activity → Add Images → Set Schedules → Set Pricing → Publish
```

**Create Activity with Schedule:**
```http
POST /api/activities
{
  "title": "Scuba Diving Adventure",
  "categoryId": "guid",
  "locationId": "guid",
  "price": 3500.00,
  "maxParticipants": 8,
  "schedules": [
    {
      "startTime": "09:00:00",
      "endTime": "12:00:00",
      "daysOfWeek": [1, 2, 3, 4, 5], // Mon-Fri
      "availableSpots": 8
    },
    {
      "startTime": "14:00:00",
      "endTime": "17:00:00",
      "daysOfWeek": [6, 0], // Sat-Sun
      "availableSpots": 10
    }
  ]
}
```

####3. Booking Management
```
Receive Booking → Auto-Confirmed (on payment) → Check-In Customer → 
Mark Completed → Respond to Reviews
```

#### 4. Earnings & Payouts
```
View Earnings Dashboard → Request Payout → Admin Approval → 
Receive Bank Transfer
```

**View Earnings:**
```http
GET /api/providers/earnings?
    fromDate=2025-01-01
    &toDate=2025-01-31
```

**Request Payout:**
```http
POST /api/providers/payouts/request
{
  "periodStart": "2025-01-01",
  "periodEnd": "2025-01-31",
  "bankAccountId": "guid"
}
```

---

## API Integration Guide

### Authentication Flow

#### 1. Register
```http
POST /api/auth/register
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+919876543210",
  "role": "Customer"
}
```

#### 2. Verify Email (6-digit OTP)
```http
POST /api/auth/verify-email
{
  "email": "user@example.com",
  "token": "123456"
}
```

#### 3. Login
```http
POST /api/auth/login
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "550e8400-e29b-41d4...",
    "expiresIn": 3600,
    "user": {
      "userId": "guid",
      "email": "user@example.com",
      "role": "Customer"
    }
  }
}
```

#### 4. Refresh Token
```http
POST /api/auth/refresh
{
  "refreshToken": "550e8400-e29b-41d4..."
}
```

### Error Handling

**Standard Error Response:**
```json
{
  "success": false,
  "message": "Validation failed",
  "errors": [
    {
      "field": "email",
      "message": "Email is already registered"
    }
  ],
  "errorCode": "VALIDATION_ERROR"
}
```

**Error Codes:**
- `VALIDATION_ERROR` (400) - Input validation failed
- `UNAUTHORIZED` (401) - Not authenticated
- `FORBIDDEN` (403) - Insufficient permissions
- `NOT_FOUND` (404) - Resource not found
- `BUSINESS_RULE_VIOLATION` (422) - Business logic error
- `RATE_LIMIT_EXCEEDED` (429) - Too many requests
- `INTERNAL_ERROR` (500) - Server error

---

## Security Implementation

### 1. Webhook Signature Verification

**Correct Implementation:**
```csharp
public bool VerifyWebhookSignature(string webhookBody, string signature, string secret)
{
    var encoding = new UTF8Encoding();
    var keyBytes = encoding.GetBytes(secret);
    var bodyBytes = encoding.GetBytes(webhookBody);
    
    using var hmac = new HMACSHA256(keyBytes);
    var hash = hmac.ComputeHash(bodyBytes);
    var computedSignature = BitConverter.ToString(hash)
        .Replace("-", "").ToLower();
    
    return computedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
}
```

### 2. Data Encryption

**Sensitive Fields to Encrypt:**
- Bank account numbers
- Tax identification numbers
- Payment gateway credentials

**Implementation:**
```csharp
public class DataProtectionService
{
    private readonly IDataProtector _protector;
    
    public string Encrypt(string plainText)
    {
        return _protector.Protect(plainText);
    }
    
    public string Decrypt(string cipherText)
    {
        return _protector.Unprotect(cipherText);
    }
}
```

### 3. Rate Limiting

**Configure in Program.cs:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});
```

---

## Deployment Checklist

### Pre-Deployment

- [ ] Run database migration script
- [ ] Create system user
- [ ] Add all missing indexes
- [ ] Configure Razorpay credentials
- [ ] Set up email service (SendGrid/AWS SES)
- [ ] Configure SMS service (Twilio)
- [ ] Set webhook URL in Razorpay dashboard
- [ ] Enable SSL/TLS
- [ ] Configure CORS
- [ ] Set up logging (Application Insights)
- [ ] Configure backup strategy

### Environment Variables

```env
# Database
DATABASE_CONNECTION_STRING=postgresql://...

# JWT
JWT_SECRET=your-secret-key-min-32-chars
JWT_ISSUER=https://api.funbookr.com
JWT_AUDIENCE=https://funbookr.com
JWT_EXPIRY_MINUTES=60

# Razorpay
RAZORPAY_KEY_ID=rzp_live_xxx
RAZORPAY_KEY_SECRET=xxx
RAZORPAY_WEBHOOK_SECRET=xxx

# Email
SENDGRID_API_KEY=xxx
FROM_EMAIL=noreply@funbookr.com

# SMS
TWILIO_ACCOUNT_SID=xxx
TWILIO_AUTH_TOKEN=xxx
TWILIO_PHONE_NUMBER=+1234567890

# Platform Settings
PLATFORM_COMMISSION_RATE=0.10
GST_RATE=0.18
TDS_RATE=0.01
```

### Post-Deployment

- [ ] Test payment flow end-to-end
- [ ] Verify webhook signature validation
- [ ] Test booking availability checking
- [ ] Verify email/SMS notifications
- [ ] Test refund flow
- [ ] Monitor error logs
- [ ] Set up alerts for critical errors
- [ ] Test provider payout flow

---

## Support & Maintenance

### Monitoring Dashboard

**Key Metrics to Track:**
1. Payment success rate
2. Webhook processing time
3. Booking conversion rate
4. API response times
5. Error rates by endpoint
6. Provider payout delays
7. Customer satisfaction (reviews)

### Common Issues

**Issue: Webhook not received**
- Check Razorpay dashboard logs
- Verify webhook URL is accessible
- Check firewall rules

**Issue: Payment stuck in pending**
- Check payment gateway logs
- Verify webhook signature
- Check database transaction logs

**Issue: Booking on unavailable slot**
- Verify activity_schedules are configured
- Check availability service logs
- Review concurrency handling

---

## Next Steps

1. **Implement Notification Service** - Email/SMS for all events
2. **Complete Coupon System** - Full validation and application
3. **Add Provider Dashboard** - Earnings, analytics, payout requests
4. **Implement Search** - Full-text search with Elasticsearch
5. **Add Analytics** - Booking trends, revenue reports
6. **Mobile App Integration** - Push notifications
7. **Multi-language Support** - i18n implementation

---

**Document Version:** 2.0  
**Last Updated:** November 2025  
**Author:** FunBookr Development Team