# FunBookr CRM - Complete API Documentation

## 📋 Table of Contents

1. [Authentication APIs](#authentication-apis)
2. [User Management APIs](#user-management-apis)
3. [Activity Provider APIs](#activity-provider-apis)
4. [Category APIs](#category-apis)
5. [Location APIs](#location-apis)
6. [Activity APIs](#activity-apis)
7. [Booking APIs](#booking-apis)
8. [Payment APIs](#payment-apis)
9. [Review APIs](#review-apis)
10. [Notification APIs](#notification-apis)
11. [Coupon APIs](#coupon-apis)
12. [Wishlist APIs](#wishlist-apis)
13. [Dashboard & Analytics APIs](#dashboard--analytics-apis)
14. [Admin APIs](#admin-apis)
15. [Common Response Models](#common-response-models)
16. [Error Codes](#error-codes)

## 🔐 Authentication

### Base URL
```
Production: https://api.funbookr.com/v1
Development: https://localhost:5001/api
```

### Authentication Header
```
Authorization: Bearer <jwt-token>
```

---

## 1. Authentication APIs

### POST /auth/register
Register a new user account. A 6-digit OTP verification code will be sent to the provided email address.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+919876543210",
  "role": "Customer" // Customer, ActivityProvider
}
```

**Response (201):**
```json
{
  "success": true,
  "message": "User registered successfully",
  "data": {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "Customer",
    "isEmailVerified": false
  }
}
```

### POST /auth/login
Authenticate user and get access token.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "rememberMe": false
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
    "expiresIn": 3600,
    "user": {
      "userId": "550e8400-e29b-41d4-a716-446655440000",
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "role": "Customer",
      "isEmailVerified": true
    }
  }
}
```

### POST /auth/refresh
Refresh access token using refresh token.

**Request:**
```json
{
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
  "rememberMe": false
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Token refreshed successfully",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "550e8400-e29b-41d4-a716-446655440001",
    "expiresIn": 3600
  }
}
```

### POST /auth/logout
Logout and invalidate tokens.

**Headers:** `Authorization: Bearer <token>`

**Request:**
```json
{
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Logged out successfully",
  "data": {
    "message": "You have been successfully logged out",
    "loggedOutAt": "2025-10-26T12:30:00Z"
  }
}
```

### POST /auth/forgot-password
Request password reset via email.

**Request:**
```json
{
  "email": "user@example.com"
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Password reset instructions sent",
  "data": {
    "message": "If an account with that email exists, we've sent password reset instructions to it.",
    "requestedAt": "2025-10-26T12:30:00Z",
    "maskedEmail": "u***@example.com"
  }
}
```

### POST /auth/reset-password
Reset password using token from email.

**Request:**
```json
{
  "resetToken": "base64-encoded-token-from-email",
  "newPassword": "NewSecurePassword123!",
  "confirmPassword": "NewSecurePassword123!"
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Password reset successful",
  "data": {
    "message": "Your password has been reset successfully. You can now log in with your new password.",
    "resetAt": "2025-10-26T12:45:00Z",
    "success": true
  }
}
```

**Error Response (400):**
```json
{
  "success": false,
  "message": "Invalid or expired reset token",
  "errorCode": "RESET_PASSWORD_FAILED"
}
```

### GET /auth/me
Get current user profile.

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "success": true,
  "data": {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+919876543210",
    "role": "Customer",
    "isEmailVerified": true,
    "isActive": true,
    "lastLoginAt": "2025-10-26T10:30:00Z"
  }
}
```

### POST /auth/verify-email
Verify user email address using 6-digit OTP code.

**Request:**
```json
{
  "email": "user@example.com",
  "token": "123456"
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Email verified successfully! Your account is now active.",
  "data": {
    "success": true,
    "message": "Email verified successfully! Your account is now active."
  }
}
```

**Error Responses:**
```json
// Invalid or expired OTP
{
  "success": false,
  "message": "Invalid or expired verification code",
  "errorCode": "EMAIL_VERIFICATION_FAILED"
}

// Too many failed attempts (rate limited)
{
  "success": false,
  "message": "Too many verification attempts. Please wait 15 minutes before trying again.",
  "errorCode": "EMAIL_VERIFICATION_FAILED"
}

// Email already verified
{
  "success": false,
  "message": "Email is already verified",
  "errorCode": "EMAIL_VERIFICATION_FAILED"
}
```

### POST /auth/resend-verification
Resend 6-digit OTP verification code to user's email. Rate limited to 5 emails per day and 1 minute between requests.

**Request:**
```json
{
  "email": "user@example.com"
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Verification code has been sent to your email",
  "data": {
    "success": true,
    "message": "Verification code has been sent to your email"
  }
}
```

**Error Responses:**
```json
// Rate limited (too frequent requests)
{
  "success": false,
  "message": "Please wait 45 seconds before requesting another verification code",
  "errorCode": "RESEND_VERIFICATION_FAILED"
}

// Daily limit exceeded
{
  "success": false,
  "message": "Daily limit reached. You can request up to 5 verification codes per day.",
  "errorCode": "RESEND_VERIFICATION_FAILED"
}

// Email already verified
{
  "success": false,
  "message": "Email is already verified",
  "errorCode": "RESEND_VERIFICATION_FAILED"
}
```

---

## 2. User Management APIs

### GET /users/profile
Get user profile details.

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "success": true,
  "data": {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "phoneNumber": "+919876543210",
    "role": "Customer",
    "isEmailVerified": true,
    "customerProfile": {
      "dateOfBirth": "1990-01-15",
      "gender": "Male",
      "emergencyContact": "Jane Doe",
      "emergencyPhone": "+919876543211",
      "dietaryRestrictions": "Vegetarian",
      "medicalConditions": "None",
      "preferredLanguage": "English"
    }
  }
}
```

### PUT /users/profile
Update user profile.

**Headers:** `Authorization: Bearer <token>`

**Request:**
```json
{
  "firstName": "John",
  "lastName": "Doe",
  "phoneNumber": "+919876543210",
  "customerProfile": {
    "dateOfBirth": "1990-01-15",
    "gender": "Male",
    "emergencyContact": "Jane Doe",
    "emergencyPhone": "+919876543211",
    "dietaryRestrictions": "Vegetarian",
    "preferredLanguage": "English"
  }
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Profile updated successfully"
}
```

### POST /users/change-password
Change user password.

**Headers:** `Authorization: Bearer <token>`

**Request:**
```json
{
  "currentPassword": "CurrentPassword123!",
  "newPassword": "NewPassword123!"
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Password changed successfully"
}
```

---

## 3. Activity Provider APIs

### GET /providers
Get list of activity providers.

**Query Parameters:**
- `page` (int): Page number (default: 1)
- `pageSize` (int): Items per page (default: 10, max: 50)
- `search` (string): Search by business name
- `locationId` (guid): Filter by location
- `isVerified` (bool): Filter by verification status
- `sortBy` (string): Sort field (rating, name, createdAt)
- `sortOrder` (string): asc/desc

**Response (200):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "providerId": "550e8400-e29b-41d4-a716-446655440000",
        "businessName": "Adventure Tours India",
        "description": "Premium adventure activities",
        "logoUrl": "https://example.com/logo.jpg",
        "location": {
          "locationId": "550e8400-e29b-41d4-a716-446655440001",
          "name": "Goa",
          "city": "Panaji",
          "state": "Goa"
        },
        "averageRating": 4.8,
        "totalReviews": 127,
        "totalBookings": 456,
        "isVerified": true,
        "isActive": true
      }
    ],
    "totalCount": 50,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 5
  }
}
```

### GET /providers/{id}
Get provider details by ID.

**Response (200):**
```json
{
  "success": true,
  "data": {
    "providerId": "550e8400-e29b-41d4-a716-446655440000",
    "user": {
      "firstName": "John",
      "lastName": "Smith",
      "email": "john@adventuretours.com"
    },
    "businessName": "Adventure Tours India",
    "businessEmail": "info@adventuretours.com",
    "businessPhone": "+919876543210",
    "description": "Premium adventure activities with 10+ years experience",
    "website": "https://adventuretours.com",
    "logoUrl": "https://example.com/logo.jpg",
    "instagramHandle": "@adventuretours",
    "facebookUrl": "https://facebook.com/adventuretours",
    "location": {
      "locationId": "550e8400-e29b-41d4-a716-446655440001",
      "name": "Goa",
      "city": "Panaji",
      "state": "Goa"
    },
    "address": {
      "addressLine1": "123 Beach Road",
      "city": "Panaji",
      "state": "Goa",
      "country": "India",
      "postalCode": "403001"
    },
    "isVerified": true,
    "verifiedAt": "2025-01-15T10:30:00Z",
    "averageRating": 4.8,
    "totalReviews": 127,
    "totalBookings": 456,
    "isActive": true,
    "createdAt": "2024-12-01T09:00:00Z"
  }
}
```

### POST /providers
Create provider profile (Customer upgrading to Provider).

**Headers:** `Authorization: Bearer <token>`

**Request:**
```json
{
  "businessName": "Adventure Tours India",
  "businessEmail": "info@adventuretours.com",
  "businessPhone": "+919876543210",
  "description": "Premium adventure activities",
  "website": "https://adventuretours.com",
  "locationId": "550e8400-e29b-41d4-a716-446655440001",
  "address": {
    "addressLine1": "123 Beach Road",
    "city": "Panaji",
    "state": "Goa",
    "country": "India",
    "postalCode": "403001"
  }
}
```

**Response (201):**
```json
{
  "success": true,
  "message": "Provider profile created successfully",
  "data": {
    "providerId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

### PUT /providers/{id}
Update provider profile.

**Headers:** `Authorization: Bearer <token>`

**Request:**
```json
{
  "businessName": "Adventure Tours India",
  "businessEmail": "info@adventuretours.com",
  "businessPhone": "+919876543210",
  "description": "Premium adventure activities with 15+ years experience",
  "website": "https://adventuretours.com",
  "instagramHandle": "@adventuretours",
  "facebookUrl": "https://facebook.com/adventuretours"
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Provider profile updated successfully"
}
```

---

## 4. Category APIs

### GET /categories
Get all activity categories.

**Query Parameters:**
- `includeInactive` (bool): Include inactive categories (default: false)
- `parentOnly` (bool): Only top-level categories (default: false)

**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "categoryId": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Water Sports",
      "slug": "water-sports",
      "description": "Exciting water-based activities",
      "iconUrl": "https://example.com/icons/water-sports.svg",
      "displayOrder": 1,
      "isActive": true,
      "parentCategoryId": null,
      "subCategories": [
        {
          "categoryId": "550e8400-e29b-41d4-a716-446655440001",
          "name": "Scuba Diving",
          "slug": "scuba-diving",
          "displayOrder": 1
        }
      ]
    }
  ]
}
```

### GET /categories/{id}
Get category by ID.

**Response (200):**
```json
{
  "success": true,
  "data": {
    "categoryId": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Water Sports",
    "slug": "water-sports",
    "description": "Exciting water-based activities",
    "iconUrl": "https://example.com/icons/water-sports.svg",
    "displayOrder": 1,
    "isActive": true,
    "parentCategory": null,
    "subCategories": [
      {
        "categoryId": "550e8400-e29b-41d4-a716-446655440001",
        "name": "Scuba Diving",
        "slug": "scuba-diving"
      }
    ],
    "totalActivities": 25
  }
}
```

### POST /categories
Create new category (Admin only).

**Headers:** `Authorization: Bearer <token>`

**Request:**
```json
{
  "name": "Water Sports",
  "slug": "water-sports",
  "description": "Exciting water-based activities",
  "iconUrl": "https://example.com/icons/water-sports.svg",
  "displayOrder": 1,
  "parentCategoryId": null
}
```

**Response (201):**
```json
{
  "success": true,
  "message": "Category created successfully",
  "data": {
    "categoryId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

---

## 5. Location APIs

### GET /locations
Get all locations.

**Query Parameters:**
- `search` (string): Search by name or city
- `country` (string): Filter by country
- `state` (string): Filter by state

**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "locationId": "550e8400-e29b-41d4-a716-446655440000",
      "name": "Goa",
      "city": "Panaji",
      "state": "Goa",
      "country": "India",
      "latitude": 15.4909,
      "longitude": 73.8278,
      "totalActivities": 45,
      "totalProviders": 12
    }
  ]
}
```

---

## 6. Activity APIs

### GET /activities
Get activities with filters and pagination.

**Query Parameters:**
- `page` (int): Page number (default: 1)
- `pageSize` (int): Items per page (default: 10, max: 50)
- `search` (string): Search in title/description
- `categoryId` (guid): Filter by category
- `locationId` (guid): Filter by location
- `providerId` (guid): Filter by provider
- `minPrice` (decimal): Minimum price filter
- `maxPrice` (decimal): Maximum price filter
- `minRating` (decimal): Minimum rating filter
- `difficultyLevel` (string): beginner/intermediate/advanced
- `sortBy` (string): price/rating/popularity/newest
- `sortOrder` (string): asc/desc
- `featured` (bool): Featured activities only

**Response (200):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "activityId": "550e8400-e29b-41d4-a716-446655440000",
        "title": "Scuba Diving Adventure",
        "slug": "scuba-diving-adventure",
        "shortDescription": "Explore underwater world",
        "coverImageUrl": "https://example.com/images/scuba.jpg",
        "price": 3500.00,
        "discountedPrice": 2800.00,
        "currency": "INR",
        "hasActiveDiscount": true,
        "durationMinutes": 180,
        "maxParticipants": 8,
        "averageRating": 4.8,
        "totalReviews": 42,
        "difficultyLevel": "beginner",
        "category": {
          "categoryId": "550e8400-e29b-41d4-a716-446655440001",
          "name": "Water Sports"
        },
        "location": {
          "locationId": "550e8400-e29b-41d4-a716-446655440002",
          "name": "Goa",
          "city": "Panaji"
        },
        "provider": {
          "providerId": "550e8400-e29b-41d4-a716-446655440003",
          "businessName": "Adventure Tours India",
          "averageRating": 4.7,
          "isVerified": true
        },
        "isFeatured": true,
        "isActive": true
      }
    ],
    "totalCount": 100,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 10
  }
}
```

### GET /activities/{id}
Get activity details by ID.

**Response (200):**
```json
{
  "success": true,
  "data": {
    "activityId": "550e8400-e29b-41d4-a716-446655440000",
    "title": "Scuba Diving Adventure",
    "slug": "scuba-diving-adventure",
    "description": "Comprehensive scuba diving experience...",
    "shortDescription": "Explore underwater world",
    "coverImageUrl": "https://example.com/images/scuba.jpg",
    "price": 3500.00,
    "discountedPrice": 2800.00,
    "currency": "INR",
    "hasActiveDiscount": true,
    "discountValidUntil": "2025-11-30T23:59:59Z",
    "minParticipants": 2,
    "maxParticipants": 8,
    "durationMinutes": 180,
    "minAge": 12,
    "maxAge": 65,
    "difficultyLevel": "beginner",
    "ageRequirement": "12+ years with adult supervision",
    "skillLevel": "No prior experience required",
    "requiredEquipment": "Swimming costume, towel",
    "providedEquipment": "Mask, fins, oxygen tank, wetsuit",
    "whatToBring": "Swimming costume, towel, sunscreen",
    "meetingPoint": "Baga Beach, Goa",
    "safetyInstructions": "Listen to instructor at all times...",
    "cancellationPolicy": "Free cancellation up to 24 hours before",
    "refundPolicy": "100% refund for cancellations 24+ hours before",
    "averageRating": 4.8,
    "totalReviews": 42,
    "totalBookings": 156,
    "viewCount": 1247,
    "category": {
      "categoryId": "550e8400-e29b-41d4-a716-446655440001",
      "name": "Water Sports"
    },
    "location": {
      "locationId": "550e8400-e29b-41d4-a716-446655440002",
      "name": "Goa",
      "city": "Panaji",
      "state": "Goa"
    },
    "provider": {
      "providerId": "550e8400-e29b-41d4-a716-446655440003",
      "businessName": "Adventure Tours India",
      "description": "Premium adventure activities",
      "averageRating": 4.7,
      "totalReviews": 127,
      "isVerified": true,
      "businessPhone": "+919876543210",
      "businessEmail": "info@adventuretours.com"
    },
    "images": [
      {
        "imageId": "550e8400-e29b-41d4-a716-446655440010",
        "imageUrl": "https://example.com/images/scuba1.jpg",
        "caption": "Underwater exploration",
        "isPrimary": true,
        "sortOrder": 1
      }
    ],
    "schedules": [
      {
        "scheduleId": "550e8400-e29b-41d4-a716-446655440020",
        "startTime": "09:00:00",
        "endTime": "12:00:00",
        "daysOfWeek": ["Monday", "Wednesday", "Friday"],
        "availableSpots": 6,
        "isActive": true
      }
    ],
    "tags": ["adventure", "underwater", "beginner-friendly"],
    "isFeatured": true,
    "isActive": true,
    "status": "Active",
    "publishedAt": "2025-01-15T10:30:00Z",
    "createdAt": "2025-01-10T09:00:00Z"
  }
}
```

### POST /activities
Create new activity (Provider only).

**Headers:** `Authorization: Bearer <token>`

**Request:**
```json
{
  "categoryId": "550e8400-e29b-41d4-a716-446655440001",
  "locationId": "550e8400-e29b-41d4-a716-446655440002",
  "title": "Scuba Diving Adventure",
  "slug": "scuba-diving-adventure",
  "description": "Comprehensive scuba diving experience for beginners and intermediates...",
  "shortDescription": "Explore underwater world",
  "price": 3500.00,
  "currency": "INR",
  "maxParticipants": 8,
  "durationMinutes": 180,
  "minAge": 12,
  "maxAge": 65,
  "difficultyLevel": "beginner",
  "whatToBring": "Swimming costume, towel, sunscreen",
  "meetingPoint": "Baga Beach, Goa",
  "cancellationPolicy": "Free cancellation up to 24 hours before"
}
```

**Response (201):**
```json
{
  "success": true,
  "message": "Activity created successfully",
  "data": {
    "activityId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

### PUT /activities/{id}
Update activity (Provider only).

**Headers:** `Authorization: Bearer <token>`

### PUT /activities/{id}/publish
Publish activity (make it available for booking).

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "success": true,
  "message": "Activity published successfully"
}
```

### PUT /activities/{id}/unpublish
Unpublish activity.

### DELETE /activities/{id}
Archive activity.

---

## 7. Booking APIs

### GET /bookings
Get user's bookings.

**Headers:** `Authorization: Bearer <token>`

**Query Parameters:**
- `page` (int): Page number (default: 1)
- `pageSize` (int): Items per page (default: 10)
- `status` (string): Filter by status (Pending/Confirmed/Completed/Cancelled)
- `fromDate` (date): Filter bookings from date
- `toDate` (date): Filter bookings to date
- `activityId` (guid): Filter by activity

**Response (200):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "bookingId": "550e8400-e29b-41d4-a716-446655440000",
        "bookingReference": "BK20251026ABC123",
        "bookingDate": "2025-11-15",
        "bookingTime": "09:00:00",
        "numberOfParticipants": 2,
        "status": "Confirmed",
        "totalAmount": 5600.00,
        "currency": "INR",
        "specialRequests": "Vegetarian lunch required",
        "activity": {
          "activityId": "550e8400-e29b-41d4-a716-446655440001",
          "title": "Scuba Diving Adventure",
          "coverImageUrl": "https://example.com/images/scuba.jpg",
          "durationMinutes": 180,
          "location": {
            "name": "Goa",
            "city": "Panaji"
          },
          "provider": {
            "businessName": "Adventure Tours India",
            "businessPhone": "+919876543210"
          }
        },
        "payment": {
          "paymentId": "550e8400-e29b-41d4-a716-446655440010",
          "status": "Completed",
          "paidAt": "2025-10-26T14:30:00Z"
        },
        "canBeCancelled": true,
        "isPaid": true,
        "confirmedAt": "2025-10-26T14:35:00Z",
        "createdAt": "2025-10-26T14:25:00Z"
      }
    ],
    "totalCount": 15,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 2
  }
}
```

### GET /bookings/{id}
Get booking details by ID.

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "success": true,
  "data": {
    "bookingId": "550e8400-e29b-41d4-a716-446655440000",
    "bookingReference": "BK20251026ABC123",
    "bookingDate": "2025-11-15",
    "bookingTime": "09:00:00",
    "numberOfParticipants": 2,
    "status": "Confirmed",
    "pricePerParticipant": 2800.00,
    "subtotalAmount": 5600.00,
    "discountAmount": 0.00,
    "taxAmount": 0.00,
    "totalAmount": 5600.00,
    "currency": "INR",
    "couponCode": null,
    "specialRequests": "Vegetarian lunch required",
    "participantNames": "John Doe, Jane Doe",
    "customerNotes": "First time diving",
    "activity": {
      "activityId": "550e8400-e29b-41d4-a716-446655440001",
      "title": "Scuba Diving Adventure",
      "description": "Comprehensive scuba diving experience...",
      "coverImageUrl": "https://example.com/images/scuba.jpg",
      "durationMinutes": 180,
      "meetingPoint": "Baga Beach, Goa",
      "whatToBring": "Swimming costume, towel, sunscreen",
      "location": {
        "name": "Goa",
        "city": "Panaji"
      },
      "provider": {
        "businessName": "Adventure Tours India",
        "businessPhone": "+919876543210",
        "businessEmail": "info@adventuretours.com"
      }
    },
    "customer": {
      "firstName": "John",
      "lastName": "Doe",
      "email": "john@example.com",
      "phoneNumber": "+919876543210"
    },
    "payment": {
      "paymentId": "550e8400-e29b-41d4-a716-446655440010",
      "paymentReference": "PAY20251026XYZ789",
      "amount": 5600.00,
      "status": "Completed",
      "paymentMethod": "UPI",
      "paidAt": "2025-10-26T14:30:00Z"
    },
    "participants": [
      {
        "participantId": "550e8400-e29b-41d4-a716-446655440020",
        "name": "John Doe",
        "age": 28,
        "gender": "Male"
      },
      {
        "participantId": "550e8400-e29b-41d4-a716-446655440021",
        "name": "Jane Doe",
        "age": 26,
        "gender": "Female"
      }
    ],
    "canBeCancelled": true,
    "isPaid": true,
    "isUpcoming": true,
    "confirmedAt": "2025-10-26T14:35:00Z",
    "createdAt": "2025-10-26T14:25:00Z"
  }
}
```

### POST /bookings
Create new booking.

**Headers:** `Authorization: Bearer <token>`

**Request:**
```json
{
  "activityId": "550e8400-e29b-41d4-a716-446655440001",
  "bookingDate": "2025-11-15",
  "bookingTime": "09:00:00",
  "numberOfParticipants": 2,
  "specialRequests": "Vegetarian lunch required",
  "participantNames": "John Doe, Jane Doe",
  "customerNotes": "First time diving",
  "participants": [
    {
      "name": "John Doe",
      "age": 28,
      "gender": "Male",
      "contactPhone": "+919876543210"
    },
    {
      "name": "Jane Doe",
      "age": 26,
      "gender": "Female"
    }
  ],
  "couponCode": "FIRSTTIME20"
}
```

**Response (201):**
```json
{
  "success": true,
  "message": "Booking created successfully",
  "data": {
    "bookingId": "550e8400-e29b-41d4-a716-446655440000",
    "bookingReference": "BK20251026ABC123",
    "totalAmount": 5600.00,
    "paymentRequired": true
  }
}
```

### PUT /bookings/{id}/cancel
Cancel booking.

**Headers:** `Authorization: Bearer <token>`

**Request:**
```json
{
  "reason": "Change of plans"
}
```

**Response (200):**
```json
{
  "success": true,
  "message": "Booking cancelled successfully",
  "data": {
    "refundAmount": 5600.00,
    "refundStatus": "Processing"
  }
}
```

### PUT /bookings/{id}/confirm
Confirm booking (Provider only).

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "success": true,
  "message": "Booking confirmed successfully"
}
```

### PUT /bookings/{id}/complete
Mark booking as completed (Provider only).

### PUT /bookings/{id}/checkin
Check-in customer (Provider only).

---

## 8. Payment APIs

### GET /payments/methods
Get available payment methods.

**Response (200):**
```json
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
    },
    {
      "method": "NetBanking",
      "displayName": "Net Banking",
      "isActive": true,
      "processingFee": 1.5
    }
  ]
}
```

### POST /payments/initiate
Initiate payment for booking.

**Headers:** `Authorization: Bearer <token>`

**Request:**
```json
{
  "bookingId": "550e8400-e29b-41d4-a716-446655440000",
  "paymentGateway": "Razorpay"
}
```

**Response (200):**
```json
{
  "success": true,
  "data": {
    "paymentId": "550e8400-e29b-41d4-a716-446655440010",
    "paymentReference": "PAY20251026XYZ789",
    "amount": 5600.00,
    "currency": "INR",
    "gatewayOrderId": "order_razorpay_123456",
    "gatewayKey": "rzp_live_xxxxxxxxxx",
    "callbackUrl": "https://api.funbookr.com/v1/payments/callback"
  }
}
```

### POST /payments/callback
Payment gateway callback (webhook).

### GET /payments/{id}
Get payment details.

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "success": true,
  "data": {
    "paymentId": "550e8400-e29b-41d4-a716-446655440010",
    "paymentReference": "PAY20251026XYZ789",
    "bookingId": "550e8400-e29b-41d4-a716-446655440000",
    "amount": 5600.00,
    "currency": "INR",
    "status": "Completed",
    "paymentGateway": "Razorpay",
    "paymentMethod": "UPI",
    "gatewayTransactionId": "txn_razorpay_789123",
    "paidAt": "2025-10-26T14:30:00Z",
    "refundedAmount": 0.00,
    "canBeRefunded": true,
    "createdAt": "2025-10-26T14:25:00Z"
  }
}
```

---

## 9. Review APIs

### GET /reviews
Get reviews with filters.

**Query Parameters:**
- `activityId` (guid): Filter by activity
- `providerId` (guid): Filter by provider
- `customerId` (guid): Filter by customer
- `rating` (int): Filter by rating (1-5)
- `isVerified` (bool): Filter verified reviews
- `page` (int): Page number
- `pageSize` (int): Items per page

**Response (200):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "reviewId": "550e8400-e29b-41d4-a716-446655440000",
        "rating": 5,
        "title": "Amazing Experience!",
        "reviewText": "Had an incredible time scuba diving...",
        "isVerified": true,
        "helpfulCount": 15,
        "customer": {
          "firstName": "John",
          "lastName": "D."
        },
        "activity": {
          "activityId": "550e8400-e29b-41d4-a716-446655440001",
          "title": "Scuba Diving Adventure"
        },
        "createdAt": "2025-10-20T16:45:00Z"
      }
    ],
    "totalCount": 50,
    "averageRating": 4.6,
    "ratingDistribution": {
      "5": 30,
      "4": 15,
      "3": 3,
      "2": 1,
      "1": 1
    }
  }
}
```

### POST /reviews
Create review for completed booking.

**Headers:** `Authorization: Bearer <token>`

**Request:**
```json
{
  "bookingId": "550e8400-e29b-41d4-a716-446655440000",
  "rating": 5,
  "title": "Amazing Experience!",
  "reviewText": "Had an incredible time scuba diving. The instructor was very professional and made us feel safe throughout the experience."
}
```

**Response (201):**
```json
{
  "success": true,
  "message": "Review submitted successfully",
  "data": {
    "reviewId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

### PUT /reviews/{id}/helpful
Mark review as helpful.

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "success": true,
  "message": "Review marked as helpful"
}
```

---

## 10. Notification APIs

### GET /notifications
Get user notifications.

**Headers:** `Authorization: Bearer <token>`

**Query Parameters:**
- `isRead` (bool): Filter by read status
- `type` (string): Filter by notification type
- `page` (int): Page number

**Response (200):**
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "notificationId": "550e8400-e29b-41d4-a716-446655440000",
        "title": "Booking Confirmed",
        "message": "Your booking for Scuba Diving Adventure has been confirmed",
        "type": "BookingConfirmed",
        "isRead": false,
        "relatedBookingId": "550e8400-e29b-41d4-a716-446655440001",
        "createdAt": "2025-10-26T14:35:00Z"
      }
    ],
    "totalCount": 25,
    "unreadCount": 8
  }
}
```

### PUT /notifications/{id}/read
Mark notification as read.

### PUT /notifications/mark-all-read
Mark all notifications as read.

---

## 11. Coupon APIs

### GET /coupons/validate/{code}
Validate coupon code.

**Headers:** `Authorization: Bearer <token>`

**Query Parameters:**
- `activityId` (guid): Activity to apply coupon to
- `orderAmount` (decimal): Order amount to validate against

**Response (200):**
```json
{
  "success": true,
  "data": {
    "couponId": "550e8400-e29b-41d4-a716-446655440000",
    "code": "FIRSTTIME20",
    "description": "20% off for first-time users",
    "discountType": "Percentage",
    "discountValue": 20.0,
    "maxDiscount": 1000.0,
    "isValid": true,
    "discountAmount": 560.0,
    "finalAmount": 5040.0
  }
}
```

### GET /coupons/my-coupons
Get user's available coupons.

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "couponId": "550e8400-e29b-41d4-a716-446655440000",
      "code": "RETURN10",
      "description": "10% off for returning customers",
      "discountType": "Percentage",
      "discountValue": 10.0,
      "validFrom": "2025-10-01T00:00:00Z",
      "validUntil": "2025-12-31T23:59:59Z",
      "minOrderAmount": 1000.0,
      "isActive": true,
      "usageLimit": 1,
      "usedCount": 0,
      "canUse": true
    }
  ]
}
```

---

## 12. Wishlist APIs

### GET /wishlist
Get user's wishlist.

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "success": true,
  "data": [
    {
      "wishlistId": "550e8400-e29b-41d4-a716-446655440000",
      "activity": {
        "activityId": "550e8400-e29b-41d4-a716-446655440001",
        "title": "Scuba Diving Adventure",
        "coverImageUrl": "https://example.com/images/scuba.jpg",
        "price": 3500.00,
        "discountedPrice": 2800.00,
        "averageRating": 4.8,
        "location": {
          "name": "Goa"
        },
        "provider": {
          "businessName": "Adventure Tours India"
        }
      },
      "addedAt": "2025-10-25T10:15:00Z"
    }
  ]
}
```

### POST /wishlist
Add activity to wishlist.

**Headers:** `Authorization: Bearer <token>`

**Request:**
```json
{
  "activityId": "550e8400-e29b-41d4-a716-446655440001"
}
```

**Response (201):**
```json
{
  "success": true,
  "message": "Activity added to wishlist"
}
```

### DELETE /wishlist/{activityId}
Remove activity from wishlist.

**Headers:** `Authorization: Bearer <token>`

**Response (200):**
```json
{
  "success": true,
  "message": "Activity removed from wishlist"
}
```

---

## 13. Dashboard & Analytics APIs

### GET /dashboard/stats
Get dashboard statistics (role-based).

**Headers:** `Authorization: Bearer <token>`

**Customer Response (200):**
```json
{
  "success": true,
  "data": {
    "totalBookings": 15,
    "upcomingBookings": 3,
    "completedBookings": 10,
    "totalSpent": 45600.00,
    "currency": "INR",
    "favoriteActivities": [
      {
        "activityId": "550e8400-e29b-41d4-a716-446655440001",
        "title": "Scuba Diving Adventure",
        "bookingCount": 3
      }
    ],
    "recentBookings": [
      {
        "bookingId": "550e8400-e29b-41d4-a716-446655440000",
        "activityTitle": "Scuba Diving Adventure",
        "bookingDate": "2025-11-15",
        "status": "Confirmed"
      }
    ]
  }
}
```

**Provider Response (200):**
```json
{
  "success": true,
  "data": {
    "totalActivities": 12,
    "activeActivities": 10,
    "totalBookings": 456,
    "monthlyBookings": 45,
    "totalRevenue": 1250000.00,
    "monthlyRevenue": 125000.00,
    "averageRating": 4.7,
    "totalReviews": 234,
    "topActivities": [
      {
        "activityId": "550e8400-e29b-41d4-a716-446655440001",
        "title": "Scuba Diving Adventure",
        "bookingCount": 156,
        "revenue": 435000.00
      }
    ],
    "recentBookings": [
      {
        "bookingId": "550e8400-e29b-41d4-a716-446655440000",
        "customerName": "John Doe",
        "activityTitle": "Scuba Diving Adventure",
        "bookingDate": "2025-11-15",
        "status": "Confirmed",
        "amount": 5600.00
      }
    ]
  }
}
```

### GET /dashboard/revenue
Get revenue analytics (Provider only).

**Headers:** `Authorization: Bearer <token>`

**Query Parameters:**
- `period` (string): daily/weekly/monthly/yearly
- `fromDate` (date): Start date
- `toDate` (date): End date

**Response (200):**
```json
{
  "success": true,
  "data": {
    "totalRevenue": 1250000.00,
    "currency": "INR",
    "period": "monthly",
    "chartData": [
      {
        "date": "2025-01",
        "revenue": 98000.00,
        "bookings": 35
      },
      {
        "date": "2025-02",
        "revenue": 125000.00,
        "bookings": 45
      }
    ],
    "topActivities": [
      {
        "activityId": "550e8400-e29b-41d4-a716-446655440001",
        "title": "Scuba Diving Adventure",
        "revenue": 435000.00,
        "percentage": 34.8
      }
    ]
  }
}
```

---

## 14. Admin APIs

### GET /admin/users
Get all users (Admin only).

### GET /admin/providers/pending
Get pending provider verifications.

### PUT /admin/providers/{id}/verify
Verify provider.

### GET /admin/bookings
Get all bookings with filters.

### GET /admin/analytics/overview
Get platform analytics.

---

## 15. Common Response Models

### Success Response
```json
{
  "success": true,
  "message": "Operation successful",
  "data": { /* response data */ }
}
```

### Error Response
```json
{
  "success": false,
  "message": "Error message",
  "errors": [
    {
      "field": "email",
      "message": "Email is required"
    }
  ],
  "errorCode": "VALIDATION_ERROR"
}
```

### Paginated Response
```json
{
  "success": true,
  "data": {
    "items": [/* array of items */],
    "totalCount": 100,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 10,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

---

## 16. Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `VALIDATION_ERROR` | 400 | Request validation failed |
| `UNAUTHORIZED` | 401 | Authentication required |
| `FORBIDDEN` | 403 | Insufficient permissions |
| `NOT_FOUND` | 404 | Resource not found |
| `CONFLICT` | 409 | Resource already exists |
| `BUSINESS_RULE_VIOLATION` | 422 | Business rule violation |
| `RATE_LIMIT_EXCEEDED` | 429 | Too many requests |
| `INTERNAL_ERROR` | 500 | Internal server error |
| `SERVICE_UNAVAILABLE` | 503 | Service temporarily unavailable |

---

## 📝 Notes

1. All timestamps are in UTC ISO 8601 format
2. All monetary amounts are in the specified currency (default INR)
3. File uploads use multipart/form-data
4. Rate limiting: 100 requests per minute per user
5. API versioning via URL path (`/v1/`)
6. CORS enabled for web applications
7. Request/Response compression supported (gzip)

---

**Last Updated:** October 26, 2025  
**API Version:** v1.0  
**Documentation Version:** 1.0