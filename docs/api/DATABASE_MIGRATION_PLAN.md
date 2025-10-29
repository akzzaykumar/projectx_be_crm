# FUNBOOKR DATABASE MIGRATION PLAN
## Complete Prioritized Task List

---

## **PHASE 1: FOUNDATION SETUP**
**Priority: CRITICAL - Must execute first**

### 1.1 PostgreSQL Extensions
```sql
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
```
**Dependencies:** None  
**Status:** ✅ Required for UUID generation

### 1.2 Enums and Custom Types
```sql
CREATE TYPE user_role AS ENUM ('admin', 'customer', 'activity_provider');
CREATE TYPE booking_status AS ENUM ('pending', 'confirmed', 'cancelled', 'completed', 'refunded');
CREATE TYPE activity_status AS ENUM ('active', 'inactive', 'suspended');
CREATE TYPE payment_status AS ENUM ('pending', 'completed', 'failed', 'refunded');
```
**Dependencies:** Extensions only  
**Status:** ✅ Required before table creation

---

## **PHASE 2: CORE TABLES (No Foreign Keys)**
**Priority: HIGH - Independent tables**

### 2.1 Users Table
```sql
CREATE TABLE users (...)
```
**Dependencies:** user_role enum  
**Reasoning:** Base table for authentication, referenced by multiple tables

### 2.2 Categories Table
```sql
CREATE TABLE categories (...)
```
**Dependencies:** None  
**Reasoning:** Lookup table for activity classification

### 2.3 Locations Table
```sql
CREATE TABLE locations (...)
```
**Dependencies:** None  
**Reasoning:** Lookup table for geographic data

### 2.4 System Settings Table
```sql
CREATE TABLE system_settings (...)
```
**Dependencies:** None  
**Reasoning:** Application configuration storage

---

## **PHASE 3: PROFILE TABLES**
**Priority: HIGH - Depends on users**

### 3.1 Activity Providers Table
```sql
CREATE TABLE activity_providers (...)
```
**Dependencies:** users, locations  
**Reasoning:** Extended profile for providers

### 3.2 Provider Contacts Table
```sql
CREATE TABLE provider_contacts (...)
```
**Dependencies:** activity_providers  
**Reasoning:** Provider contact information

### 3.3 Customer Profiles Table
```sql
CREATE TABLE customer_profiles (...)
```
**Dependencies:** users  
**Reasoning:** Extended profile for customers

---

## **PHASE 4: ACTIVITY ECOSYSTEM**
**Priority: HIGH - Core business tables**

### 4.1 Activities Table
```sql
CREATE TABLE activities (...)
```
**Dependencies:** activity_providers, categories, locations  
**Reasoning:** Central table for offerings

### 4.2 Activity Images Table
```sql
CREATE TABLE activity_images (...)
```
**Dependencies:** activities  
**Reasoning:** Media assets for activities

### 4.3 Activity Schedules Table
```sql
CREATE TABLE activity_schedules (...)
```
**Dependencies:** activities  
**Reasoning:** Availability management

### 4.4 Activity Tags Table
```sql
CREATE TABLE activity_tags (...)
```
**Dependencies:** activities  
**Reasoning:** Search optimization

---

## **PHASE 5: BOOKING & PAYMENT FLOW**
**Priority: HIGH - Core transaction tables**

### 5.1 Bookings Table
```sql
CREATE TABLE bookings (...)
```
**Dependencies:** users, activities  
**Reasoning:** Core booking records

### 5.2 Booking Participants Table
```sql
CREATE TABLE booking_participants (...)
```
**Dependencies:** bookings  
**Reasoning:** Group booking details

### 5.3 Payments Table
```sql
CREATE TABLE payments (...)
```
**Dependencies:** bookings  
**Reasoning:** Financial transactions

---

## **PHASE 6: ENGAGEMENT & MARKETING**
**Priority: MEDIUM - User engagement features**

### 6.1 Reviews Table
```sql
CREATE TABLE reviews (...)
```
**Dependencies:** bookings, users, activities, activity_providers  
**Reasoning:** User feedback and ratings

### 6.2 Wishlists Table
```sql
CREATE TABLE wishlists (...)
```
**Dependencies:** users, activities  
**Reasoning:** Save favorites

### 6.3 Coupons Table
```sql
CREATE TABLE coupons (...)
```
**Dependencies:** categories  
**Reasoning:** Discount management

### 6.4 Coupon Usage Table
```sql
CREATE TABLE coupon_usage (...)
```
**Dependencies:** coupons, bookings, users  
**Reasoning:** Track coupon redemptions

### 6.5 Notifications Table
```sql
CREATE TABLE notifications (...)
```
**Dependencies:** users, bookings  
**Reasoning:** User communication

---

## **PHASE 7: ENHANCEMENT TABLES (New Features)**
**Priority: MEDIUM - Advanced features from enhancements.sql**

### 7.1 Audit Log Table
```sql
CREATE TABLE audit_log (...)
```
**Dependencies:** None (standalone)  
**Reasoning:** System-wide audit trail

### 7.2 Notification Templates Table
```sql
CREATE TABLE notification_templates (...)
```
**Dependencies:** None  
**Reasoning:** Reusable notification messages

### 7.3 Provider Licenses Table
```sql
CREATE TABLE provider_licenses (...)
```
**Dependencies:** activity_providers, users  
**Reasoning:** Regulatory compliance tracking

### 7.4 Provider Insurance Table
```sql
CREATE TABLE provider_insurance (...)
```
**Dependencies:** activity_providers, users  
**Reasoning:** Insurance policy management

### 7.5 Provider Certifications Table
```sql
CREATE TABLE provider_certifications (...)
```
**Dependencies:** activity_providers, users  
**Reasoning:** Safety certifications

### 7.6 Compliance Requirements Table
```sql
CREATE TABLE compliance_requirements (...)
```
**Dependencies:** categories  
**Reasoning:** Define regulatory requirements

---

## **PHASE 8: TABLE MODIFICATIONS**
**Priority: MEDIUM - Extend existing tables**

### 8.1 Add Soft Delete Columns
```sql
ALTER TABLE users ADD COLUMN deleted_at TIMESTAMP WITH TIME ZONE;
ALTER TABLE activities ADD COLUMN deleted_at TIMESTAMP WITH TIME ZONE;
ALTER TABLE activity_providers ADD COLUMN deleted_at TIMESTAMP WITH TIME ZONE;
```
**Dependencies:** Base tables exist  
**Reasoning:** Enable soft delete functionality

### 8.2 Add Compliance Tracking to Activities
```sql
ALTER TABLE activities ADD COLUMN license_compliance_status VARCHAR(50) DEFAULT 'pending';
ALTER TABLE activities ADD COLUMN compliance_checked_at TIMESTAMP WITH TIME ZONE;
ALTER TABLE activities ADD COLUMN compliance_notes TEXT;
```
**Dependencies:** activities table exists  
**Reasoning:** Track license compliance status

---

## **PHASE 9: CONSTRAINTS & VALIDATIONS**
**Priority: MEDIUM - Data integrity**

### 9.1 Check Constraints on Activities
```sql
ALTER TABLE activities ADD CONSTRAINT chk_activities_price_positive CHECK (price > 0);
ALTER TABLE activities ADD CONSTRAINT chk_activities_participants CHECK (max_participants >= min_participants AND min_participants > 0);
ALTER TABLE activities ADD CONSTRAINT chk_activities_duration CHECK (duration_minutes > 0);
```

### 9.2 Check Constraints on Bookings
```sql
ALTER TABLE bookings ADD CONSTRAINT chk_booking_future_date CHECK (booking_date >= CURRENT_DATE);
ALTER TABLE bookings ADD CONSTRAINT chk_booking_participants_positive CHECK (number_of_participants > 0);
```

### 9.3 Check Constraints on Coupons
```sql
ALTER TABLE coupons ADD CONSTRAINT chk_coupon_dates CHECK (valid_until > valid_from);
ALTER TABLE coupons ADD CONSTRAINT chk_coupon_discount_positive CHECK (discount_value > 0);
```

---

## **PHASE 10: FUNCTIONS & STORED PROCEDURES**
**Priority: MEDIUM - Business logic**

### 10.1 Core Utility Functions
- `update_updated_at_column()` - Auto-update timestamps
- `generate_booking_reference()` - Generate unique booking IDs
- `update_activity_rating()` - Recalculate activity ratings
- `update_provider_rating()` - Recalculate provider ratings

### 10.2 Enhancement Functions
- `audit_trigger_function()` - Audit logging
- `soft_delete()` - Soft delete handler
- `check_activity_availability()` - Availability checker
- `check_license_compliance()` - Compliance validation
- `update_activity_compliance_status()` - Update compliance
- `calculate_dynamic_price()` - Dynamic pricing
- `send_notification()` - Notification sender
- `cleanup_old_audit_logs()` - Maintenance
- `update_license_expiry_status()` - License monitoring
- `send_license_expiry_notifications()` - Alert system
- `update_all_activity_compliance()` - Batch compliance update
- `recalculate_all_ratings()` - Ratings maintenance

---

## **PHASE 11: TRIGGERS**
**Priority: MEDIUM - Automated actions**

### 11.1 Updated_at Triggers (12 tables)
Apply to: users, categories, locations, activity_providers, activities, activity_schedules, customer_profiles, bookings, payments, reviews, coupons, system_settings

### 11.2 Rating Update Triggers
- `update_activity_rating_trigger` on reviews
- `update_provider_rating_trigger` on reviews

### 11.3 Audit Triggers
- `audit_users_trigger` on users
- `audit_bookings_trigger` on bookings
- `audit_payments_trigger` on payments

---

## **PHASE 12: INDEXES (Performance Optimization)**
**Priority: LOW-MEDIUM - Can be added after data migration**

### 12.1 Core Indexes (from base schema)
- User indexes: email, role, active status
- Activity indexes: provider, category, location, status, price, rating, featured
- Booking indexes: customer, activity, date, status, reference
- Review indexes: activity, provider, rating
- Location indexes: city, state
- Provider indexes: verification, rating, featured

### 12.2 Enhancement Indexes
- Audit log indexes
- Notification indexes
- License management indexes (20+ indexes)
- Composite indexes for complex queries
- Partial indexes for filtered queries
- Full-text search indexes
- Geographic indexes (GiST)

---

## **PHASE 13: ROW LEVEL SECURITY (RLS)**
**Priority: LOW - Security hardening**

### 13.1 Enable RLS on Tables
- users, customer_profiles, activity_providers, bookings, payments

### 13.2 Create RLS Policies
- User isolation policy
- Customer profile policy
- Provider policy
- Booking customer policy
- Booking provider policy

---

## **PHASE 14: VIEWS**
**Priority: LOW - Query optimization**

### 14.1 Base Views
- `activity_details` - Comprehensive activity info
- `booking_details` - Complete booking info

### 14.2 Enhancement Views
- `provider_dashboard` - Provider metrics
- `license_compliance_dashboard` - Compliance overview
- `license_expiry_tracking` - Expiry monitoring
- `activity_compliance_summary` - Activity compliance status
- `customer_insights` - Customer analytics

---

## **PHASE 15: MATERIALIZED VIEWS**
**Priority: LOW - Analytics performance**

### 15.1 Analytics Views
- `mv_popular_activities` - Popular activity rankings
- `mv_revenue_analytics` - Revenue analysis

### 15.2 Refresh Function
- `refresh_analytics_views()` - Update materialized views

---

## **PHASE 16: SEED DATA & INITIAL SETUP**
**Priority: LOW - Post-deployment**

### 16.1 Notification Templates
```sql
INSERT INTO notification_templates (...)
VALUES ('booking_confirmed', ...), ('payment_completed', ...), etc.
```

### 16.2 Compliance Requirements
```sql
INSERT INTO compliance_requirements (...)
-- Kerala-specific requirements
```

### 16.3 System Settings
```sql
INSERT INTO system_settings (...)
-- Application configuration
```

---

## **EXECUTION STRATEGY**

### Option A: Monolithic Migration
Execute all phases sequentially in a single transaction

**Pros:** Atomic, all-or-nothing  
**Cons:** High risk, difficult to debug

### Option B: Incremental Migration (RECOMMENDED)
Create separate migration files for each phase:

```
migrations/
├── 001_extensions_and_types.sql
├── 002_core_tables.sql
├── 003_profile_tables.sql
├── 004_activity_ecosystem.sql
├── 005_booking_payment.sql
├── 006_engagement_marketing.sql
├── 007_enhancement_tables.sql
├── 008_table_modifications.sql
├── 009_constraints.sql
├── 010_functions.sql
├── 011_triggers.sql
├── 012_indexes.sql
├── 013_row_level_security.sql
├── 014_views.sql
├── 015_materialized_views.sql
├── 016_seed_data.sql
```

**Pros:** Safe, testable, rollback-friendly  
**Cons:** More files to manage

---

## **RISK ASSESSMENT**

### High Risk Items
- ⚠️ RLS policies (can lock out users if misconfigured)
- ⚠️ Audit triggers (can impact performance)
- ⚠️ Check constraints (can prevent data insertion)

### Medium Risk Items
- ⚠️ Functions with business logic (bugs can affect operations)
- ⚠️ Materialized views (can be resource-intensive)

### Low Risk Items
- ✅ Indexes (can be added/removed easily)
- ✅ Views (don't affect data)
- ✅ Seed data (can be modified)

---

## **ROLLBACK STRATEGY**

Each migration should have a corresponding rollback script:

```
migrations/
├── 001_extensions_and_types.sql
├── 001_extensions_and_types.rollback.sql
├── 002_core_tables.sql
├── 002_core_tables.rollback.sql
...
```

---

## **TESTING CHECKLIST**

### After Each Phase:
- [ ] Verify all objects created successfully
- [ ] Check foreign key constraints
- [ ] Test triggers fire correctly
- [ ] Validate functions return expected results
- [ ] Confirm indexes exist
- [ ] Test RLS policies with different user contexts
- [ ] Verify views return correct data
- [ ] Check for naming conflicts
- [ ] Monitor database size and performance

---

## **ESTIMATED TIMELINE**

| Phase | Complexity | Time Estimate |
|-------|-----------|---------------|
| 1-6 (Core Schema) | Medium | 2-4 hours |
| 7-8 (Enhancements) | Low | 1-2 hours |
| 9-11 (Logic Layer) | Medium | 2-3 hours |
| 12 (Indexes) | Low | 1 hour |
| 13 (Security) | High | 2-3 hours |
| 14-15 (Views) | Low | 1 hour |
| 16 (Seed Data) | Low | 1 hour |
| **Total** | | **10-16 hours** |

---

## **RECOMMENDATION**

**START WITH:** Phases 1-6 (Core functionality)  
**DEFER:** Phases 13-15 (Security hardening and analytics) until after initial deployment  
**PRIORITIZE:** Getting the MVP (Minimum Viable Product) running first

Core MVP includes:
- Users, authentication
- Activities, bookings, payments
- Basic reviews and notifications
- Core indexes and triggers

Advanced features (licenses, compliance, RLS, analytics) can be added incrementally after the core system is stable and tested.
