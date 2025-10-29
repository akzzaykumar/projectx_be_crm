# Funbookr Database Schema Diagram

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│     USERS       │     │   CATEGORIES    │     │   LOCATIONS     │
├─────────────────┤     ├─────────────────┤     ├─────────────────┤
│ user_id (PK)    │     │ category_id(PK) │     │ location_id(PK) │
│ email (UQ)      │     │ name (UQ)       │     │ name            │
│ password_hash   │     │ description     │     │ city            │
│ role (ENUM)     │     │ icon_url        │     │ state           │
│ first_name      │     │ is_active       │     │ country         │
│ last_name       │     │ created_at      │     │ latitude        │
│ phone           │     │ updated_at      │     │ longitude       │
│ is_active       │     └─────────────────┘     │ created_at      │
│ email_verified  │                             │ updated_at      │
│ created_at      │                             └─────────────────┘
│ updated_at      │                                       │
│ last_login      │                                       │
└─────────────────┘                                       │
         │                                                │
         │                                                │
    ┌────┴────┐                                          │
    │         │                                          │
    ▼         ▼                                          │
┌─────────────────┐     ┌─────────────────┐              │
│CUSTOMER_PROFILES│     │ACTIVITY_PROVIDERS│             │
├─────────────────┤     ├─────────────────┤              │
│ profile_id (PK) │     │ provider_id(PK) │              │
│ user_id (FK)    │     │ user_id (FK)    │              │
│ date_of_birth   │     │ business_name   │              │
│ gender          │     │ description     │              │
│ emergency_contact│     │ website_url     │              │
│ emergency_phone │     │ instagram_handle│              │
│ dietary_restrict│     │ facebook_url    │              │
│ medical_cond    │     │ location_id(FK) │──────────────┘
│ preferred_lang  │     │ address         │
│ created_at      │     │ verification    │
│ updated_at      │     │ rating          │
└─────────────────┘     │ total_reviews   │
                        │ is_featured     │
                        │ created_at      │
                        │ updated_at      │
                        └─────────────────┘
                                 │
                                 │
                        ┌────────┴────────┐
                        │                 │
                        ▼                 ▼
                ┌─────────────────┐ ┌─────────────────┐
                │PROVIDER_CONTACTS│ │   ACTIVITIES    │
                ├─────────────────┤ ├─────────────────┤
                │ contact_id (PK) │ │ activity_id(PK) │
                │ provider_id(FK) │ │ provider_id(FK) │
                │ contact_type    │ │ category_id(FK) │──────┐
                │ contact_value   │ │ location_id(FK) │──────┼─────────┐
                │ is_primary      │ │ title           │      │         │
                │ created_at      │ │ description     │      │         │
                └─────────────────┘ │ short_desc      │      │         │
                                    │ duration_min    │      │         │
                                    │ max_participants│      │         │
                                    │ min_participants│      │         │
                                    │ price           │      │         │
                                    │ currency        │      │         │
                                    │ status (ENUM)   │      │         │
                                    │ rating          │      │         │
                                    │ total_reviews   │      │         │
                                    │ featured        │      │         │
                                    │ cancellation    │      │         │
                                    │ what_to_bring   │      │         │
                                    │ meeting_point   │      │         │
                                    │ difficulty      │      │         │
                                    │ age_requirement │      │         │
                                    │license_compliance│      │         │
                                    │compliance_checked│      │         │
                                    │ created_at      │      │         │
                                    │ updated_at      │      │         │
                                    └─────────────────┘      │         │
                                             │               │         │
                        ┌────────────────────┼───────────────┘         │
                        │                    │                         │
                        │         ┌──────────┴──────────┐              │
                        │         │                     │              │
                        ▼         ▼                     ▼              ▼
                ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
                │ACTIVITY_IMAGES  │ │ACTIVITY_SCHEDULES│ │ ACTIVITY_TAGS   │ │    BOOKINGS     │
                ├─────────────────┤ ├─────────────────┤ ├─────────────────┤ ├─────────────────┤
                │ image_id (PK)   │ │ schedule_id(PK) │ │ tag_id (PK)     │ │ booking_id (PK) │
                │ activity_id(FK) │ │ activity_id(FK) │ │ activity_id(FK) │ │ customer_id(FK) │
                │ image_url       │ │ start_time      │ │ tag             │ │ activity_id(FK) │
                │ caption         │ │ end_time        │ │ created_at      │ │ booking_ref(UQ) │
                │ is_primary      │ │ days_of_week[]  │ └─────────────────┘ │ booking_date    │
                │ sort_order      │ │ available_spots │                     │ booking_time    │
                │ created_at      │ │ is_active       │                     │ participants    │
                └─────────────────┘ │ created_at      │                     │ total_amount    │
                                    │ updated_at      │                     │ currency        │
                                    └─────────────────┘                     │ status (ENUM)   │
                                                                            │ special_requests│
                                                                            │ customer_notes  │
                                                                            │ provider_notes  │
                                                                            │ cancel_reason   │
                                                                            │ cancelled_at    │
                                                                            │ confirmed_at    │
                                                                            │ completed_at    │
                                                                            │ created_at      │
                                                                            │ updated_at      │
                                                                            └─────────────────┘

┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│PROVIDER_LICENSES│     │PROVIDER_INSURANCE│     │PROVIDER_CERTIFS │
├─────────────────┤     ├─────────────────┤     ├─────────────────┤
│ license_id (PK) │     │insurance_id (PK)│     │ cert_id (PK)    │
│ provider_id(FK) │─┐   │ provider_id(FK) │─┐   │ provider_id(FK) │─┐
│ license_type    │ │   │ insurance_type  │ │   │ certification   │ │
│ license_name    │ │   │ insurance_prov  │ │   │ cert_name       │ │
│ license_number  │ │   │ policy_number   │ │   │ certifying_body │ │
│ issuing_auth    │ │   │ coverage_amount │ │   │ instructor_name │ │
│ issue_date      │ │   │ currency        │ │   │ cert_level      │ │
│ expiry_date     │ │   │ policy_start    │ │   │ issued_date     │ │
│ license_status  │ │   │ policy_end      │ │   │ expiry_date     │ │
│ verification    │ │   │ policy_status   │ │   │ verification    │ │
│ document_url    │ │   │ verification    │ │   │ document_url    │ │
│ created_at      │ │   │ document_url    │ │   │ created_at      │ │
│ updated_at      │ │   │ created_at      │ │   │ updated_at      │ │
└─────────────────┘ │   │ updated_at      │ │   └─────────────────┘ │
         ▲          │   └─────────────────┘ │            ▲          │
         │          │            ▲          │            │          │
         │          └────────────┼──────────┘            │          │
         │                       │                       │          │
         └───────────────────────┼───────────────────────┘          │
                                 │                                  │
                     ┌───────────▼──────────┐                       │
                     │ ACTIVITY_PROVIDERS   │◄──────────────────────┘
                     │     (Updated)        │
                     ├─────────────────────┤
                     │ provider_id (PK)    │
                     │ user_id (FK)        │
                     │ business_name       │
                     │ description         │
                     │ website_url         │
                     │ instagram_handle    │
                     │ facebook_url        │
                     │ location_id (FK)    │
                     │ address             │
                     │ verification_status │
                     │ rating              │
                     │ total_reviews       │
                     │ is_featured         │
                     │ created_at          │
                     │ updated_at          │
                     └─────────────────────┘

┌─────────────────┐
│COMPLIANCE_REQS  │
├─────────────────┤
│requirement_id(PK)│
│ category_id(FK) │──────────────┐
│required_license │              │
│ is_mandatory    │              │
│ description     │              ▼
│regulatory_auth  │     ┌─────────────────┐
│ created_at      │     │   CATEGORIES    │
│ updated_at      │     │   (Updated)     │
└─────────────────┘     ├─────────────────┤
                        │ category_id(PK) │
                        │ name (UQ)       │
                        │ description     │
                        │ icon_url        │
                        │ is_active       │
                        │ created_at      │
                        │ updated_at      │
                        └─────────────────┘
                                                                                     │
                                                    ┌────────────────────────────────┼────────────────────────────────┐
                                                    │                                │                                │
                                                    ▼                                ▼                                ▼
                                            ┌─────────────────┐            ┌─────────────────┐            ┌─────────────────┐
                                            │BOOKING_PARTICIP │            │    PAYMENTS     │            │    REVIEWS      │
                                            ├─────────────────┤            ├─────────────────┤            ├─────────────────┤
                                            │participant_id(PK)│            │ payment_id (PK) │            │ review_id (PK)  │
                                            │ booking_id (FK) │            │ booking_id (FK) │            │ booking_id (FK) │
                                            │ name            │            │ payment_ref(UQ) │            │ customer_id(FK) │
                                            │ age             │            │ amount          │            │ activity_id(FK) │
                                            │ gender          │            │ currency        │            │ provider_id(FK) │
                                            │ contact_phone   │            │ payment_method  │            │ rating (1-5)    │
                                            │ dietary_restrict│            │ payment_gateway │            │ title           │
                                            │ medical_cond    │            │ gateway_txn_id  │            │ review_text     │
                                            │ created_at      │            │ status (ENUM)   │            │ is_verified     │
                                            └─────────────────┘            │ processed_at    │            │ is_featured     │
                                                                           │ created_at      │            │ helpful_count   │
                                                                           │ updated_at      │            │ created_at      │
                                                                           └─────────────────┘            │ updated_at      │
                                                                                                          └─────────────────┘

┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   WISHLISTS     │     │ NOTIFICATIONS   │     │    COUPONS      │     │ SYSTEM_SETTINGS │
├─────────────────┤     ├─────────────────┤     ├─────────────────┤     ├─────────────────┤
│ wishlist_id(PK) │     │notification_id(PK)│     │ coupon_id (PK)  │     │ setting_id (PK) │
│ customer_id(FK) │     │ user_id (FK)    │     │ code (UQ)       │     │ key (UQ)        │
│ activity_id(FK) │     │ title           │     │ description     │     │ value           │
│ created_at      │     │ message         │     │ discount_type   │     │ description     │
└─────────────────┘     │ type            │     │ discount_value  │     │ created_at      │
                        │ is_read         │     │ min_order_amt   │     │ updated_at      │
                        │ related_booking │     │ max_discount    │     └─────────────────┘
                        │ created_at      │     │ valid_from      │
                        └─────────────────┘     │ valid_until     │
                                                │ usage_limit     │
                                                │ used_count      │
                                                │ is_active       │
                                                │ applicable_cats │
                                                │ created_at      │
                                                │ updated_at      │
                                                └─────────────────┘
                                                         │
                                                         │
                                                ┌────────▼────────┐
                                                │  COUPON_USAGE   │
                                                ├─────────────────┤
                                                │ usage_id (PK)   │
                                                │ coupon_id (FK)  │
                                                │ booking_id (FK) │
                                                │ user_id (FK)    │
                                                │ discount_amount │
                                                │ used_at         │
                                                └─────────────────┘

Legend:
PK = Primary Key
FK = Foreign Key
UQ = Unique Constraint
ENUM = Enumerated Type
[] = Array Type
```

## Key Relationships:

1. **Users** → One-to-One → **Customer Profiles** | **Activity Providers**
2. **Activity Providers** → One-to-Many → **Activities**
3. **Activities** → Many-to-One → **Categories**, **Locations**
4. **Activities** → One-to-Many → **Activity Images**, **Activity Schedules**, **Activity Tags**
5. **Users (Customers)** → One-to-Many → **Bookings**
6. **Activities** → One-to-Many → **Bookings**
7. **Bookings** → One-to-Many → **Booking Participants**, **Payments**
8. **Bookings** → One-to-One → **Reviews**
9. **Users (Customers)** → Many-to-Many → **Activities** (via **Wishlists**)
10. **Coupons** → Many-to-Many → **Bookings** (via **Coupon Usage**)
11. **Activity Providers** → One-to-Many → **Provider Licenses**, **Provider Insurance**, **Provider Certifications**
12. **Categories** → One-to-Many → **Compliance Requirements**
13. **Compliance Requirements** → Links activities to required licenses for regulatory compliance

## Database Views:

- **activity_details**: Aggregated view of activities with provider and location info
- **booking_details**: Comprehensive booking information for dashboards
- **license_compliance_dashboard**: Real-time compliance status for all providers
- **license_expiry_tracking**: Upcoming license expiries with urgency levels

## Key Features:

- **Role-Based Access**: Single users table with role enumeration
- **Flexible Scheduling**: Array-based day-of-week scheduling
- **Multi-Payment Gateway**: Support for various payment providers
- **Comprehensive Booking**: Full workflow from booking to completion
- **Review System**: Linked to completed bookings
- **Inventory Management**: Available spots tracking
- **Notification System**: Type-based notifications
- **Discount System**: Flexible coupon management
- **Analytics Ready**: Timestamp tracking for reporting
- **License Management**: Complete regulatory compliance tracking system
- **Automated Compliance**: Real-time checking and expiry notifications
- **Risk Assessment**: Provider risk categorization based on compliance status