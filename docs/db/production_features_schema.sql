-- ========================================
-- FUNBOOKR PRODUCTION-READY FEATURES
-- Additional Tables & Enums for Business Logic
-- ========================================

-- ========================================
-- ADDITIONAL ENUMS
-- ========================================

-- Dynamic pricing rule types
CREATE TYPE pricing_rule_type AS ENUM (
    'peak_hours', 
    'early_bird', 
    'last_minute', 
    'group_discount', 
    'seasonal', 
    'day_of_week',
    'advance_booking'
);

-- Gift card status
CREATE TYPE gift_card_status AS ENUM ('active', 'redeemed', 'expired', 'cancelled');

-- Loyalty tier types
CREATE TYPE loyalty_tier AS ENUM ('bronze', 'silver', 'gold', 'platinum');

-- Provider subscription plans
CREATE TYPE subscription_plan AS ENUM ('starter', 'growth', 'premium');

-- Weather conditions
CREATE TYPE weather_condition AS ENUM ('sunny', 'cloudy', 'rainy', 'stormy', 'snowy', 'foggy');

-- Customer photo approval status
CREATE TYPE photo_approval_status AS ENUM ('pending', 'approved', 'rejected');

-- Recommendation types
CREATE TYPE recommendation_type AS ENUM ('personalized', 'similar', 'trending', 'popular', 'nearby');

-- ========================================
-- DYNAMIC PRICING TABLES
-- ========================================

-- Pricing rules for activities
CREATE TABLE pricing_rules (
    rule_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    activity_id UUID NOT NULL REFERENCES activities(activity_id) ON DELETE CASCADE,
    rule_type pricing_rule_type NOT NULL,
    rule_name VARCHAR(100) NOT NULL,
    description TEXT,
    condition_json JSONB NOT NULL, -- Flexible conditions
    discount_percentage DECIMAL(5,2), -- For discounts
    markup_percentage DECIMAL(5,2), -- For peak pricing
    priority INTEGER DEFAULT 0, -- Higher priority rules apply first
    is_active BOOLEAN DEFAULT true,
    valid_from TIMESTAMP WITH TIME ZONE,
    valid_until TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Price history for analytics
CREATE TABLE price_history (
    history_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    activity_id UUID NOT NULL REFERENCES activities(activity_id) ON DELETE CASCADE,
    booking_id UUID REFERENCES bookings(booking_id),
    base_price DECIMAL(10, 2) NOT NULL,
    final_price DECIMAL(10, 2) NOT NULL,
    applied_rules JSONB, -- Array of applied rule details
    pricing_factors JSONB, -- Additional factors (occupancy rate, demand, etc.)
    calculated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- GIFT CARDS & VOUCHERS
-- ========================================

CREATE TABLE gift_cards (
    gift_card_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code VARCHAR(16) UNIQUE NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'INR',
    balance DECIMAL(10,2) NOT NULL,
    purchased_by UUID REFERENCES users(user_id),
    recipient_email VARCHAR(255),
    recipient_name VARCHAR(200),
    message TEXT,
    status gift_card_status DEFAULT 'active',
    expires_at TIMESTAMP WITH TIME ZONE,
    purchased_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    redeemed_at TIMESTAMP WITH TIME ZONE,
    redeemed_by UUID REFERENCES users(user_id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE gift_card_transactions (
    transaction_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    gift_card_id UUID NOT NULL REFERENCES gift_cards(gift_card_id) ON DELETE CASCADE,
    booking_id UUID REFERENCES bookings(booking_id),
    amount_used DECIMAL(10,2) NOT NULL,
    balance_after DECIMAL(10,2) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- LOYALTY & REWARDS PROGRAM
-- ========================================

CREATE TABLE loyalty_points (
    point_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    points INTEGER NOT NULL,
    transaction_type VARCHAR(50) NOT NULL, -- 'earned', 'redeemed', 'expired', 'bonus'
    reference_type VARCHAR(50), -- 'booking', 'review', 'referral', 'signup'
    reference_id UUID, -- booking_id, review_id, etc.
    description TEXT,
    expiry_date DATE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE loyalty_tiers (
    tier_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name loyalty_tier UNIQUE NOT NULL,
    min_points INTEGER NOT NULL,
    discount_percentage DECIMAL(5,2) NOT NULL,
    benefits JSONB, -- JSON array of benefits
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE user_loyalty_status (
    status_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID UNIQUE NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    current_tier loyalty_tier DEFAULT 'bronze',
    total_points INTEGER DEFAULT 0,
    available_points INTEGER DEFAULT 0,
    lifetime_points INTEGER DEFAULT 0,
    tier_upgraded_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- PROVIDER SUBSCRIPTIONS
-- ========================================

CREATE TABLE provider_subscriptions (
    subscription_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    provider_id UUID NOT NULL REFERENCES activity_providers(provider_id) ON DELETE CASCADE,
    plan subscription_plan NOT NULL,
    monthly_fee DECIMAL(10,2) NOT NULL,
    commission_percentage DECIMAL(5,2) NOT NULL,
    max_listings INTEGER,
    max_photos_per_activity INTEGER,
    featured_listings_per_month INTEGER DEFAULT 0,
    has_priority_support BOOLEAN DEFAULT false,
    has_analytics BOOLEAN DEFAULT false,
    has_api_access BOOLEAN DEFAULT false,
    has_instant_booking BOOLEAN DEFAULT false,
    start_date TIMESTAMP WITH TIME ZONE NOT NULL,
    end_date TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT true,
    auto_renew BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE subscription_payments (
    payment_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    subscription_id UUID NOT NULL REFERENCES provider_subscriptions(subscription_id) ON DELETE CASCADE,
    amount DECIMAL(10,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'INR',
    payment_date TIMESTAMP WITH TIME ZONE NOT NULL,
    billing_period_start DATE NOT NULL,
    billing_period_end DATE NOT NULL,
    payment_method VARCHAR(50),
    transaction_id VARCHAR(200),
    status payment_status DEFAULT 'pending',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- INSTANT BOOKING & APPROVAL SETTINGS
-- ========================================

-- Add columns to activities table (via ALTER in migration)
-- allow_instant_booking BOOLEAN DEFAULT false
-- require_provider_approval BOOLEAN DEFAULT true
-- auto_confirm_threshold_hours INTEGER DEFAULT 24

-- ========================================
-- FLEXIBLE CANCELLATION & RESCHEDULING
-- ========================================

CREATE TABLE booking_reschedule_history (
    reschedule_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_id UUID NOT NULL REFERENCES bookings(booking_id) ON DELETE CASCADE,
    original_date DATE NOT NULL,
    original_time TIME NOT NULL,
    new_date DATE NOT NULL,
    new_time TIME NOT NULL,
    reason TEXT,
    rescheduled_by UUID NOT NULL REFERENCES users(user_id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Add columns to activities (via ALTER)
-- allow_free_reschedule BOOLEAN DEFAULT true
-- max_reschedules INTEGER DEFAULT 2

-- Add columns to bookings (via ALTER)
-- original_booking_date DATE
-- rescheduled_count INTEGER DEFAULT 0

-- ========================================
-- QR CODE CHECK-IN SYSTEM
-- ========================================

CREATE TABLE qr_code_tokens (
    token_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_id UUID UNIQUE NOT NULL REFERENCES bookings(booking_id) ON DELETE CASCADE,
    qr_code_data TEXT UNIQUE NOT NULL, -- Encrypted booking data
    signature TEXT NOT NULL, -- HMAC signature for verification
    generated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL, -- Short expiry for security
    is_used BOOLEAN DEFAULT false,
    used_at TIMESTAMP WITH TIME ZONE
);

-- ========================================
-- WEATHER INTEGRATION
-- ========================================

CREATE TABLE weather_forecasts (
    forecast_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    location_id UUID NOT NULL REFERENCES locations(location_id),
    forecast_date DATE NOT NULL,
    condition weather_condition NOT NULL,
    temperature_high DECIMAL(5,2),
    temperature_low DECIMAL(5,2),
    rain_probability INTEGER, -- Percentage
    wind_speed DECIMAL(5,2), -- km/h
    visibility DECIMAL(5,2), -- km
    is_safe_for_outdoor BOOLEAN DEFAULT true,
    fetched_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(location_id, forecast_date)
);

CREATE TABLE weather_cancellations (
    cancellation_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_id UUID NOT NULL REFERENCES bookings(booking_id) ON DELETE CASCADE,
    forecast_id UUID REFERENCES weather_forecasts(forecast_id),
    reason TEXT NOT NULL,
    automatic BOOLEAN DEFAULT true,
    refund_processed BOOLEAN DEFAULT false,
    customer_notified BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- CUSTOMER PHOTOS & SOCIAL PROOF
-- ========================================

CREATE TABLE customer_photos (
    photo_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_id UUID NOT NULL REFERENCES bookings(booking_id) ON DELETE CASCADE,
    customer_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    activity_id UUID NOT NULL REFERENCES activities(activity_id),
    photo_url VARCHAR(500) NOT NULL,
    caption TEXT,
    status photo_approval_status DEFAULT 'pending',
    helpful_count INTEGER DEFAULT 0,
    reward_points_given INTEGER DEFAULT 0,
    approved_by UUID REFERENCES users(user_id),
    approved_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE photo_helpful_votes (
    vote_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    photo_id UUID NOT NULL REFERENCES customer_photos(photo_id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(photo_id, user_id)
);

-- ========================================
-- REAL-TIME ACTIVITY FEED
-- ========================================

CREATE TABLE activity_feed (
    feed_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    activity_id UUID NOT NULL REFERENCES activities(activity_id) ON DELETE CASCADE,
    event_type VARCHAR(50) NOT NULL, -- 'booking', 'review', 'view'
    user_id UUID REFERENCES users(user_id),
    user_location VARCHAR(100), -- City name
    reference_id UUID, -- booking_id or review_id
    metadata JSONB, -- Additional event data
    is_public BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- SEARCH & RECOMMENDATION ENGINE
-- ========================================

CREATE TABLE search_queries (
    query_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES users(user_id),
    query_text TEXT NOT NULL,
    filters JSONB, -- Search filters applied
    results_count INTEGER,
    clicked_activity_id UUID REFERENCES activities(activity_id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE user_activity_interactions (
    interaction_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    activity_id UUID NOT NULL REFERENCES activities(activity_id) ON DELETE CASCADE,
    interaction_type VARCHAR(50) NOT NULL, -- 'view', 'click', 'wishlist', 'booking'
    duration_seconds INTEGER, -- Time spent viewing
    source VARCHAR(100), -- 'search', 'recommendation', 'browse'
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE recommendation_cache (
    cache_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    recommendation_type recommendation_type NOT NULL,
    activity_ids UUID[] NOT NULL,
    score DECIMAL(5,4), -- Recommendation confidence
    generated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    UNIQUE(user_id, recommendation_type)
);

-- ========================================
-- PROVIDER DASHBOARD ANALYTICS
-- ========================================

CREATE TABLE provider_analytics (
    analytics_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    provider_id UUID NOT NULL REFERENCES activity_providers(provider_id) ON DELETE CASCADE,
    date DATE NOT NULL,
    total_views INTEGER DEFAULT 0,
    total_bookings INTEGER DEFAULT 0,
    total_revenue DECIMAL(12,2) DEFAULT 0.00,
    total_commission DECIMAL(12,2) DEFAULT 0.00,
    avg_rating DECIMAL(3,2),
    new_reviews INTEGER DEFAULT 0,
    cancellations INTEGER DEFAULT 0,
    no_shows INTEGER DEFAULT 0,
    unique_customers INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(provider_id, date)
);

CREATE TABLE activity_performance (
    performance_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    activity_id UUID NOT NULL REFERENCES activities(activity_id) ON DELETE CASCADE,
    date DATE NOT NULL,
    views INTEGER DEFAULT 0,
    clicks INTEGER DEFAULT 0,
    bookings INTEGER DEFAULT 0,
    revenue DECIMAL(10,2) DEFAULT 0.00,
    avg_rating DECIMAL(3,2),
    conversion_rate DECIMAL(5,4), -- bookings/views
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(activity_id, date)
);

-- ========================================
-- REFERRAL PROGRAM
-- ========================================

CREATE TABLE referrals (
    referral_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    referrer_user_id UUID NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    referred_user_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    referral_code VARCHAR(20) UNIQUE NOT NULL,
    referred_email VARCHAR(255),
    status VARCHAR(50) DEFAULT 'pending', -- 'pending', 'completed', 'rewarded'
    referrer_reward_points INTEGER,
    referred_reward_points INTEGER,
    referrer_rewarded_at TIMESTAMP WITH TIME ZONE,
    referred_rewarded_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- ABANDONED CART TRACKING
-- ========================================

CREATE TABLE abandoned_bookings (
    abandoned_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID REFERENCES users(user_id) ON DELETE CASCADE,
    session_id VARCHAR(100), -- For anonymous users
    activity_id UUID NOT NULL REFERENCES activities(activity_id) ON DELETE CASCADE,
    booking_date DATE,
    booking_time TIME,
    participants INTEGER,
    incomplete_step VARCHAR(50), -- 'details', 'payment', etc.
    reminder_sent BOOLEAN DEFAULT false,
    reminder_sent_at TIMESTAMP WITH TIME ZONE,
    recovery_code VARCHAR(50) UNIQUE, -- Discount code for recovery
    recovered BOOLEAN DEFAULT false,
    recovered_booking_id UUID REFERENCES bookings(booking_id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- PROVIDER PAYOUTS
-- ========================================

CREATE TABLE payout_schedules (
    schedule_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    provider_id UUID NOT NULL REFERENCES activity_providers(provider_id) ON DELETE CASCADE,
    period_start DATE NOT NULL,
    period_end DATE NOT NULL,
    total_bookings INTEGER NOT NULL,
    gross_revenue DECIMAL(12,2) NOT NULL,
    commission_amount DECIMAL(12,2) NOT NULL,
    subscription_fee DECIMAL(10,2) DEFAULT 0.00,
    net_payout DECIMAL(12,2) NOT NULL,
    status VARCHAR(50) DEFAULT 'pending', -- 'pending', 'processing', 'completed', 'failed'
    requested_at TIMESTAMP WITH TIME ZONE,
    processed_at TIMESTAMP WITH TIME ZONE,
    transaction_id VARCHAR(200),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- ADD-ON SERVICES MARKETPLACE
-- ========================================

CREATE TABLE activity_addons (
    addon_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    activity_id UUID NOT NULL REFERENCES activities(activity_id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    price DECIMAL(10,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'INR',
    commission_percentage DECIMAL(5,2) DEFAULT 20.00,
    is_required BOOLEAN DEFAULT false,
    max_quantity INTEGER DEFAULT 10,
    image_url VARCHAR(500),
    display_order INTEGER DEFAULT 0,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE booking_addons (
    booking_addon_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_id UUID NOT NULL REFERENCES bookings(booking_id) ON DELETE CASCADE,
    addon_id UUID NOT NULL REFERENCES activity_addons(addon_id),
    quantity INTEGER NOT NULL DEFAULT 1,
    price_per_unit DECIMAL(10,2) NOT NULL,
    total_amount DECIMAL(10,2) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- REVIEW RESPONSES (Provider replies)
-- ========================================

CREATE TABLE review_responses (
    response_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    review_id UUID UNIQUE NOT NULL REFERENCES reviews(review_id) ON DELETE CASCADE,
    provider_id UUID NOT NULL REFERENCES activity_providers(provider_id) ON DELETE CASCADE,
    response_text TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- TRUST BADGES & CERTIFICATES
-- ========================================

CREATE TABLE provider_certifications (
    certification_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    provider_id UUID NOT NULL REFERENCES activity_providers(provider_id) ON DELETE CASCADE,
    certification_name VARCHAR(200) NOT NULL,
    certification_type VARCHAR(100), -- 'safety', 'quality', 'environmental', etc.
    issuing_authority VARCHAR(200),
    certificate_number VARCHAR(100),
    issued_date DATE,
    expiry_date DATE,
    document_url VARCHAR(500),
    is_verified BOOLEAN DEFAULT false,
    verified_by UUID REFERENCES users(user_id),
    verified_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- INDEXES FOR NEW TABLES
-- ========================================

-- Pricing rules indexes
CREATE INDEX idx_pricing_rules_activity ON pricing_rules(activity_id);
CREATE INDEX idx_pricing_rules_type ON pricing_rules(rule_type);
CREATE INDEX idx_pricing_rules_active ON pricing_rules(is_active);
CREATE INDEX idx_pricing_rules_valid_dates ON pricing_rules(valid_from, valid_until);

-- Gift cards indexes
CREATE INDEX idx_gift_cards_code ON gift_cards(code);
CREATE INDEX idx_gift_cards_status ON gift_cards(status);
CREATE INDEX idx_gift_cards_recipient_email ON gift_cards(recipient_email);
CREATE INDEX idx_gift_cards_purchased_by ON gift_cards(purchased_by);

-- Loyalty indexes
CREATE INDEX idx_loyalty_points_user ON loyalty_points(user_id);
CREATE INDEX idx_loyalty_points_expiry ON loyalty_points(expiry_date);
CREATE INDEX idx_user_loyalty_status_user ON user_loyalty_status(user_id);
CREATE INDEX idx_user_loyalty_status_tier ON user_loyalty_status(current_tier);

-- Provider subscription indexes
CREATE INDEX idx_provider_subscriptions_provider ON provider_subscriptions(provider_id);
CREATE INDEX idx_provider_subscriptions_plan ON provider_subscriptions(plan);
CREATE INDEX idx_provider_subscriptions_active ON provider_subscriptions(is_active);

-- Weather indexes
CREATE INDEX idx_weather_forecasts_location_date ON weather_forecasts(location_id, forecast_date);
CREATE INDEX idx_weather_forecasts_safe ON weather_forecasts(is_safe_for_outdoor);

-- Customer photos indexes
CREATE INDEX idx_customer_photos_activity ON customer_photos(activity_id);
CREATE INDEX idx_customer_photos_status ON customer_photos(status);
CREATE INDEX idx_customer_photos_helpful ON customer_photos(helpful_count);

-- Activity feed indexes
CREATE INDEX idx_activity_feed_activity ON activity_feed(activity_id);
CREATE INDEX idx_activity_feed_event_type ON activity_feed(event_type);
CREATE INDEX idx_activity_feed_created_at ON activity_feed(created_at DESC);

-- Search indexes
CREATE INDEX idx_search_queries_user ON search_queries(user_id);
CREATE INDEX idx_user_interactions_user ON user_activity_interactions(user_id);
CREATE INDEX idx_user_interactions_activity ON user_activity_interactions(activity_id);

-- Analytics indexes
CREATE INDEX idx_provider_analytics_provider_date ON provider_analytics(provider_id, date);
CREATE INDEX idx_activity_performance_activity_date ON activity_performance(activity_id, date);

-- Referral indexes
CREATE INDEX idx_referrals_referrer ON referrals(referrer_user_id);
CREATE INDEX idx_referrals_referred ON referrals(referred_user_id);
CREATE INDEX idx_referrals_code ON referrals(referral_code);

-- Abandoned bookings indexes
CREATE INDEX idx_abandoned_bookings_user ON abandoned_bookings(user_id);
CREATE INDEX idx_abandoned_bookings_reminder ON abandoned_bookings(reminder_sent);

-- Addon indexes
CREATE INDEX idx_activity_addons_activity ON activity_addons(activity_id);
CREATE INDEX idx_booking_addons_booking ON booking_addons(booking_id);

-- ========================================
-- TRIGGERS FOR NEW TABLES
-- ========================================

CREATE TRIGGER update_gift_cards_updated_at BEFORE UPDATE ON gift_cards 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_loyalty_tiers_updated_at BEFORE UPDATE ON loyalty_tiers 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_user_loyalty_status_updated_at BEFORE UPDATE ON user_loyalty_status 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_provider_subscriptions_updated_at BEFORE UPDATE ON provider_subscriptions 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_customer_photos_updated_at BEFORE UPDATE ON customer_photos 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_referrals_updated_at BEFORE UPDATE ON referrals 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_abandoned_bookings_updated_at BEFORE UPDATE ON abandoned_bookings 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_payout_schedules_updated_at BEFORE UPDATE ON payout_schedules 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_activity_addons_updated_at BEFORE UPDATE ON activity_addons 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_review_responses_updated_at BEFORE UPDATE ON review_responses 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_provider_certifications_updated_at BEFORE UPDATE ON provider_certifications 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_pricing_rules_updated_at BEFORE UPDATE ON pricing_rules 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ========================================
-- BUSINESS LOGIC FUNCTIONS
-- ========================================

-- Function to calculate dynamic price
CREATE OR REPLACE FUNCTION calculate_dynamic_price(
    p_activity_id UUID,
    p_booking_date DATE,
    p_booking_time TIME,
    p_participants INTEGER
) RETURNS DECIMAL(10,2) AS $$
DECLARE
    v_base_price DECIMAL(10,2);
    v_final_price DECIMAL(10,2);
    v_multiplier DECIMAL(5,4) := 1.0;
    v_rule RECORD;
BEGIN
    -- Get base price
    SELECT price INTO v_base_price
    FROM activities
    WHERE activity_id = p_activity_id;
    
    -- Apply pricing rules in priority order
    FOR v_rule IN 
        SELECT * FROM pricing_rules
        WHERE activity_id = p_activity_id
          AND is_active = true
          AND (valid_from IS NULL OR valid_from <= NOW())
          AND (valid_until IS NULL OR valid_until >= NOW())
        ORDER BY priority DESC
    LOOP
        -- Apply rule logic based on conditions in condition_json
        -- This would be extended with specific rule evaluation logic
        IF v_rule.discount_percentage IS NOT NULL THEN
            v_multiplier := v_multiplier * (1 - v_rule.discount_percentage / 100.0);
        ELSIF v_rule.markup_percentage IS NOT NULL THEN
            v_multiplier := v_multiplier * (1 + v_rule.markup_percentage / 100.0);
        END IF;
    END LOOP;
    
    v_final_price := v_base_price * v_multiplier;
    
    RETURN ROUND(v_final_price, 2);
END;
$$ LANGUAGE plpgsql;

-- Function to update loyalty points
CREATE OR REPLACE FUNCTION update_loyalty_points_on_booking()
RETURNS TRIGGER AS $$
DECLARE
    v_points INTEGER;
BEGIN
    IF NEW.status = 'completed' AND OLD.status != 'completed' THEN
        -- Award points: 1 point per 100 INR spent
        v_points := FLOOR(NEW.total_amount / 100);
        
        INSERT INTO loyalty_points (user_id, points, transaction_type, reference_type, reference_id, description)
        VALUES (NEW.customer_id, v_points, 'earned', 'booking', NEW.booking_id, 
                'Points earned from booking ' || NEW.booking_reference);
        
        -- Update user loyalty status
        UPDATE user_loyalty_status
        SET total_points = total_points + v_points,
            available_points = available_points + v_points,
            lifetime_points = lifetime_points + v_points
        WHERE user_id = NEW.customer_id;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER award_loyalty_points_trigger
    AFTER UPDATE ON bookings
    FOR EACH ROW EXECUTE FUNCTION update_loyalty_points_on_booking();

-- Function to generate referral code
CREATE OR REPLACE FUNCTION generate_referral_code(p_user_id UUID)
RETURNS VARCHAR(20) AS $$
DECLARE
    v_code VARCHAR(20);
    v_exists BOOLEAN;
BEGIN
    LOOP
        v_code := 'REF' || UPPER(SUBSTRING(MD5(p_user_id::TEXT || NOW()::TEXT) FROM 1 FOR 8));
        
        SELECT EXISTS(SELECT 1 FROM referrals WHERE referral_code = v_code) INTO v_exists;
        
        EXIT WHEN NOT v_exists;
    END LOOP;
    
    RETURN v_code;
END;
$$ LANGUAGE plpgsql;

-- Function to check and upgrade loyalty tier
CREATE OR REPLACE FUNCTION check_and_upgrade_loyalty_tier()
RETURNS TRIGGER AS $$
DECLARE
    v_new_tier loyalty_tier;
BEGIN
    -- Determine new tier based on total points
    IF NEW.total_points >= 50000 THEN
        v_new_tier := 'platinum';
    ELSIF NEW.total_points >= 20000 THEN
        v_new_tier := 'gold';
    ELSIF NEW.total_points >= 5000 THEN
        v_new_tier := 'silver';
    ELSE
        v_new_tier := 'bronze';
    END IF;
    
    -- Update tier if changed
    IF v_new_tier != NEW.current_tier THEN
        NEW.current_tier := v_new_tier;
        NEW.tier_upgraded_at := CURRENT_TIMESTAMP;
        
        -- Send notification (would be handled by application layer)
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER check_loyalty_tier_trigger
    BEFORE UPDATE ON user_loyalty_status
    FOR EACH ROW EXECUTE FUNCTION check_and_upgrade_loyalty_tier();

-- ========================================
-- ALTER EXISTING TABLES
-- ========================================

-- Add instant booking columns to activities
ALTER TABLE activities 
ADD COLUMN IF NOT EXISTS allow_instant_booking BOOLEAN DEFAULT false,
ADD COLUMN IF NOT EXISTS require_provider_approval BOOLEAN DEFAULT true,
ADD COLUMN IF NOT EXISTS auto_confirm_threshold_hours INTEGER DEFAULT 24,
ADD COLUMN IF NOT EXISTS is_outdoor BOOLEAN DEFAULT false;

-- Add rescheduling columns to activities
ALTER TABLE activities
ADD COLUMN IF NOT EXISTS allow_free_reschedule BOOLEAN DEFAULT true,
ADD COLUMN IF NOT EXISTS max_reschedules INTEGER DEFAULT 2;

-- Add rescheduling columns to bookings
ALTER TABLE bookings
ADD COLUMN IF NOT EXISTS original_booking_date DATE,
ADD COLUMN IF NOT EXISTS rescheduled_count INTEGER DEFAULT 0;

-- Add commission override for specific activities
ALTER TABLE activities
ADD COLUMN IF NOT EXISTS custom_commission_percentage DECIMAL(5,2);

-- ========================================
-- INITIAL DATA
-- ========================================

-- Insert default loyalty tiers
INSERT INTO loyalty_tiers (name, min_points, discount_percentage, benefits) VALUES
('bronze', 0, 0, '["Access to basic features", "Earn points on bookings"]'),
('silver', 5000, 5, '["5% discount on all bookings", "Priority customer support", "Early access to new activities"]'),
('gold', 20000, 10, '["10% discount on all bookings", "Free cancellation", "Exclusive member events", "Birthday rewards"]'),
('platinum', 50000, 15, '["15% discount on all bookings", "Dedicated concierge", "VIP experiences", "Free upgrades"]')
ON CONFLICT (name) DO NOTHING;

-- ========================================
-- COMMENTS ON TABLES
-- ========================================

COMMENT ON TABLE pricing_rules IS 'Dynamic pricing rules for activities based on various conditions';
COMMENT ON TABLE gift_cards IS 'Gift cards and vouchers for purchasing and gifting experiences';
COMMENT ON TABLE loyalty_points IS 'Customer loyalty points tracking for rewards program';
COMMENT ON TABLE user_loyalty_status IS 'Current loyalty tier and points balance for each user';
COMMENT ON TABLE provider_subscriptions IS 'Provider subscription plans with different features and pricing';
COMMENT ON TABLE weather_forecasts IS 'Weather forecasts for outdoor activity planning and auto-cancellation';
COMMENT ON TABLE customer_photos IS 'User-generated photos from bookings for social proof';
COMMENT ON TABLE activity_feed IS 'Real-time activity feed showing bookings, reviews, and views';
COMMENT ON TABLE recommendation_cache IS 'Cached personalized recommendations for users';
COMMENT ON TABLE provider_analytics IS 'Daily analytics for provider dashboard';
COMMENT ON TABLE referrals IS 'Referral program tracking for customer acquisition';
COMMENT ON TABLE abandoned_bookings IS 'Cart abandonment tracking for recovery campaigns';
COMMENT ON TABLE activity_addons IS 'Add-on services and products for activities';
COMMENT ON TABLE review_responses IS 'Provider responses to customer reviews';
