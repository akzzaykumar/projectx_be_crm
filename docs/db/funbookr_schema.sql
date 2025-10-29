-- ========================================
-- FUNBOOKR ACTIVITY BOOKING SAAS DATABASE
-- PostgreSQL Schema Design
-- ========================================

-- Enable necessary extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ========================================
-- ENUMS AND TYPES
-- ========================================

-- User role types
CREATE TYPE user_role AS ENUM ('admin', 'customer', 'activity_provider');

-- Booking status types
CREATE TYPE booking_status AS ENUM ('pending', 'confirmed', 'cancelled', 'completed', 'refunded');

-- Activity status types
CREATE TYPE activity_status AS ENUM ('active', 'inactive', 'draft', 'archived');

-- Payment status types
CREATE TYPE payment_status AS ENUM ('pending', 'completed', 'failed', 'refunded', 'partially_refunded');

-- ========================================
-- CORE TABLES
-- ========================================

-- Users table (handles all three roles)
CREATE TABLE users (
    user_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role user_role NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    phone VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    email_verified BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP WITH TIME ZONE
);

-- Categories table (with hierarchical support)
CREATE TABLE categories (
    category_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) UNIQUE NOT NULL,
    slug VARCHAR(150) UNIQUE NOT NULL,
    description TEXT,
    icon_url VARCHAR(500),
    display_order INTEGER DEFAULT 0,
    parent_category_id UUID REFERENCES categories(category_id) ON DELETE RESTRICT,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Locations table
CREATE TABLE locations (
    location_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(100) NOT NULL,
    city VARCHAR(100) NOT NULL,
    state VARCHAR(100) NOT NULL,
    country VARCHAR(100) DEFAULT 'India',
    latitude DECIMAL(10, 8),
    longitude DECIMAL(11, 8),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Activity providers table (extended profile for providers)
CREATE TABLE activity_providers (
    provider_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID UNIQUE NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    business_name VARCHAR(200) NOT NULL,
    business_registration_number VARCHAR(100),
    tax_identification_number VARCHAR(100),
    business_email VARCHAR(255),
    business_phone VARCHAR(20),
    website_url VARCHAR(500),
    description TEXT,
    logo_url VARCHAR(500),
    instagram_handle VARCHAR(100),
    facebook_url VARCHAR(500),
    location_id UUID REFERENCES locations(location_id),
    address_line1 VARCHAR(255),
    address_line2 VARCHAR(255),
    city VARCHAR(100),
    state VARCHAR(100),
    country VARCHAR(100),
    postal_code VARCHAR(20),
    bank_account_name VARCHAR(200),
    bank_account_number VARCHAR(100),
    bank_name VARCHAR(200),
    bank_branch_code VARCHAR(50),
    payment_gateway_id VARCHAR(200),
    is_verified BOOLEAN DEFAULT false,
    verified_at TIMESTAMP WITH TIME ZONE,
    verified_by UUID REFERENCES users(user_id),
    is_active BOOLEAN DEFAULT true,
    rejection_reason VARCHAR(500),
    average_rating DECIMAL(3, 2) DEFAULT 0.00,
    total_reviews INTEGER DEFAULT 0,
    total_bookings INTEGER DEFAULT 0,
    is_featured BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Provider contact information
CREATE TABLE provider_contacts (
    contact_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    provider_id UUID NOT NULL REFERENCES activity_providers(provider_id) ON DELETE CASCADE,
    contact_type VARCHAR(50) NOT NULL, -- 'phone', 'email', 'whatsapp'
    contact_value VARCHAR(255) NOT NULL,
    is_primary BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Activities table
CREATE TABLE activities (
    activity_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    provider_id UUID NOT NULL REFERENCES activity_providers(provider_id) ON DELETE CASCADE,
    category_id UUID NOT NULL REFERENCES categories(category_id),
    location_id UUID NOT NULL REFERENCES locations(location_id),
    title VARCHAR(200) NOT NULL,
    slug VARCHAR(250) NOT NULL,
    description TEXT,
    short_description VARCHAR(500),
    cover_image_url VARCHAR(500),
    price DECIMAL(10, 2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'INR',
    discounted_price DECIMAL(10, 2),
    discount_valid_until TIMESTAMP WITH TIME ZONE,
    min_participants INTEGER DEFAULT 1,
    max_participants INTEGER,
    duration_minutes INTEGER,
    preparation_time_minutes INTEGER,
    cleanup_time_minutes INTEGER,
    is_scheduled BOOLEAN DEFAULT true,
    advance_booking_days INTEGER,
    cancellation_hours INTEGER,
    status activity_status DEFAULT 'draft',
    is_active BOOLEAN DEFAULT false,
    is_featured BOOLEAN DEFAULT false,
    published_at TIMESTAMP WITH TIME ZONE,
    min_age INTEGER,
    max_age INTEGER,
    difficulty_level VARCHAR(50), -- 'beginner', 'intermediate', 'advanced'
    age_requirement VARCHAR(100),
    skill_level VARCHAR(100),
    required_equipment TEXT,
    provided_equipment TEXT,
    what_to_bring TEXT,
    meeting_point TEXT,
    safety_instructions TEXT,
    cancellation_policy TEXT,
    refund_policy TEXT,
    average_rating DECIMAL(3, 2) DEFAULT 0.00,
    total_reviews INTEGER DEFAULT 0,
    total_bookings INTEGER DEFAULT 0,
    view_count INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Activity images
CREATE TABLE activity_images (
    image_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    activity_id UUID NOT NULL REFERENCES activities(activity_id) ON DELETE CASCADE,
    image_url VARCHAR(500) NOT NULL,
    caption VARCHAR(255),
    is_primary BOOLEAN DEFAULT false,
    sort_order INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Activity availability/schedules
CREATE TABLE activity_schedules (
    schedule_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    activity_id UUID NOT NULL REFERENCES activities(activity_id) ON DELETE CASCADE,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    days_of_week INTEGER[] NOT NULL, -- Array of days (0=Sunday, 1=Monday, etc.)
    available_spots INTEGER NOT NULL,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Activity tags (for better searchability)
CREATE TABLE activity_tags (
    tag_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    activity_id UUID NOT NULL REFERENCES activities(activity_id) ON DELETE CASCADE,
    tag VARCHAR(50) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(activity_id, tag)
);

-- Customer profiles (extended profile for customers)
CREATE TABLE customer_profiles (
    profile_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID UNIQUE NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    date_of_birth DATE,
    gender VARCHAR(20),
    profile_picture_url VARCHAR(500),
    bio VARCHAR(1000),
    dietary_restrictions TEXT,
    medical_conditions TEXT,
    address_line1 VARCHAR(255),
    address_line2 VARCHAR(255),
    city VARCHAR(100),
    state VARCHAR(100),
    country VARCHAR(100),
    postal_code VARCHAR(20),
    emergency_contact_name VARCHAR(100),
    emergency_contact_phone VARCHAR(20),
    emergency_contact_relationship VARCHAR(100),
    preferred_language VARCHAR(50),
    preferred_currency VARCHAR(3),
    email_notifications BOOLEAN DEFAULT true,
    sms_notifications BOOLEAN DEFAULT false,
    marketing_emails BOOLEAN DEFAULT false,
    total_bookings INTEGER DEFAULT 0,
    total_spent DECIMAL(18, 2) DEFAULT 0.00,
    last_booking_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Bookings table
CREATE TABLE bookings (
    booking_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_id UUID NOT NULL REFERENCES users(user_id),
    activity_id UUID NOT NULL REFERENCES activities(activity_id),
    booking_reference VARCHAR(20) UNIQUE NOT NULL,
    booking_date DATE NOT NULL,
    booking_time TIME NOT NULL,
    number_of_participants INTEGER NOT NULL DEFAULT 1,
    price_per_participant DECIMAL(10, 2) NOT NULL,
    subtotal_amount DECIMAL(10, 2) NOT NULL,
    discount_amount DECIMAL(10, 2) DEFAULT 0.00,
    tax_amount DECIMAL(10, 2) DEFAULT 0.00,
    total_amount DECIMAL(10, 2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'INR',
    coupon_code VARCHAR(50),
    coupon_discount_percentage DECIMAL(5, 2),
    status booking_status DEFAULT 'pending',
    special_requests TEXT,
    participant_names TEXT,
    customer_notes TEXT,
    provider_notes TEXT,
    confirmed_at TIMESTAMP WITH TIME ZONE,
    confirmed_by UUID REFERENCES users(user_id),
    completed_at TIMESTAMP WITH TIME ZONE,
    cancelled_at TIMESTAMP WITH TIME ZONE,
    cancelled_by UUID REFERENCES users(user_id),
    cancellation_reason TEXT,
    refund_amount DECIMAL(10, 2) DEFAULT 0.00,
    checked_in_at TIMESTAMP WITH TIME ZONE,
    no_show BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Booking participants (for group bookings)
CREATE TABLE booking_participants (
    participant_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_id UUID NOT NULL REFERENCES bookings(booking_id) ON DELETE CASCADE,
    name VARCHAR(100) NOT NULL,
    age INTEGER,
    gender VARCHAR(20),
    contact_phone VARCHAR(20),
    dietary_restrictions TEXT,
    medical_conditions TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Payments table
CREATE TABLE payments (
    payment_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_id UUID UNIQUE NOT NULL REFERENCES bookings(booking_id),
    payment_reference VARCHAR(100) UNIQUE NOT NULL,
    amount DECIMAL(10, 2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'INR',
    payment_method VARCHAR(50), -- 'card', 'upi', 'netbanking', 'wallet'
    payment_gateway VARCHAR(50), -- 'razorpay', 'stripe', 'paytm'
    payment_gateway_order_id VARCHAR(200),
    gateway_transaction_id VARCHAR(100),
    status payment_status DEFAULT 'pending',
    failure_reason TEXT,
    retry_count INTEGER DEFAULT 0,
    refunded_amount DECIMAL(10, 2) DEFAULT 0.00,
    paid_at TIMESTAMP WITH TIME ZONE,
    refunded_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Reviews and ratings
CREATE TABLE reviews (
    review_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    booking_id UUID UNIQUE NOT NULL REFERENCES bookings(booking_id),
    customer_id UUID NOT NULL REFERENCES users(user_id),
    activity_id UUID NOT NULL REFERENCES activities(activity_id),
    provider_id UUID NOT NULL REFERENCES activity_providers(provider_id),
    rating INTEGER CHECK (rating >= 1 AND rating <= 5),
    title VARCHAR(200),
    review_text TEXT,
    is_verified BOOLEAN DEFAULT true,
    is_featured BOOLEAN DEFAULT false,
    helpful_count INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Wishlist/Favorites
CREATE TABLE wishlists (
    wishlist_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    customer_id UUID NOT NULL REFERENCES users(user_id),
    activity_id UUID NOT NULL REFERENCES activities(activity_id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(customer_id, activity_id)
);

-- Notifications
CREATE TABLE notifications (
    notification_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES users(user_id),
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    type VARCHAR(50) NOT NULL, -- 'booking', 'payment', 'review', 'promotion'
    is_read BOOLEAN DEFAULT false,
    related_booking_id UUID REFERENCES bookings(booking_id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Coupons and discounts
CREATE TABLE coupons (
    coupon_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    code VARCHAR(50) UNIQUE NOT NULL,
    description TEXT,
    discount_type VARCHAR(20) NOT NULL, -- 'percentage', 'fixed'
    discount_value DECIMAL(10, 2) NOT NULL,
    min_order_amount DECIMAL(10, 2),
    max_discount_amount DECIMAL(10, 2),
    valid_from TIMESTAMP WITH TIME ZONE NOT NULL,
    valid_until TIMESTAMP WITH TIME ZONE NOT NULL,
    usage_limit INTEGER,
    used_count INTEGER DEFAULT 0,
    is_active BOOLEAN DEFAULT true,
    applicable_categories UUID[] REFERENCES categories(category_id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Coupon usage tracking
CREATE TABLE coupon_usage (
    usage_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    coupon_id UUID NOT NULL REFERENCES coupons(coupon_id),
    booking_id UUID NOT NULL REFERENCES bookings(booking_id),
    user_id UUID NOT NULL REFERENCES users(user_id),
    discount_amount DECIMAL(10, 2) NOT NULL,
    used_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- System settings/configuration
CREATE TABLE system_settings (
    setting_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key VARCHAR(100) UNIQUE NOT NULL,
    value TEXT,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ========================================
-- INDEXES FOR PERFORMANCE
-- ========================================

-- User indexes
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_role ON users(role);
CREATE INDEX idx_users_active ON users(is_active);

-- Category indexes
CREATE INDEX idx_categories_slug ON categories(slug);
CREATE INDEX idx_categories_name ON categories(name);
CREATE INDEX idx_categories_parent ON categories(parent_category_id);
CREATE INDEX idx_categories_display_order ON categories(display_order);

-- Activity indexes
CREATE INDEX idx_activities_provider ON activities(provider_id);
CREATE INDEX idx_activities_category ON activities(category_id);
CREATE INDEX idx_activities_location ON activities(location_id);
CREATE INDEX idx_activities_slug ON activities(slug);
CREATE INDEX idx_activities_status ON activities(status);
CREATE INDEX idx_activities_is_active ON activities(is_active);
CREATE INDEX idx_activities_is_featured ON activities(is_featured);
CREATE INDEX idx_activities_price ON activities(price);
CREATE INDEX idx_activities_average_rating ON activities(average_rating);
CREATE INDEX idx_activities_published_at ON activities(published_at);

-- Booking indexes
CREATE INDEX idx_bookings_customer ON bookings(customer_id);
CREATE INDEX idx_bookings_activity ON bookings(activity_id);
CREATE INDEX idx_bookings_date ON bookings(booking_date);
CREATE INDEX idx_bookings_status ON bookings(status);
CREATE INDEX idx_bookings_reference ON bookings(booking_reference);

-- Review indexes
CREATE INDEX idx_reviews_activity ON reviews(activity_id);
CREATE INDEX idx_reviews_provider ON reviews(provider_id);
CREATE INDEX idx_reviews_rating ON reviews(rating);

-- Location indexes
CREATE INDEX idx_locations_city ON locations(city);
CREATE INDEX idx_locations_state ON locations(state);

-- Provider indexes
CREATE INDEX idx_providers_user_id ON activity_providers(user_id);
CREATE INDEX idx_providers_business_name ON activity_providers(business_name);
CREATE INDEX idx_providers_is_verified ON activity_providers(is_verified);
CREATE INDEX idx_providers_is_active ON activity_providers(is_active);
CREATE INDEX idx_providers_average_rating ON activity_providers(average_rating);
CREATE INDEX idx_providers_is_featured ON activity_providers(is_featured);

-- Customer profile indexes
CREATE INDEX idx_customer_profiles_user_id ON customer_profiles(user_id);
CREATE INDEX idx_customer_profiles_total_bookings ON customer_profiles(total_bookings);
CREATE INDEX idx_customer_profiles_last_booking_at ON customer_profiles(last_booking_at);

-- Payment indexes
CREATE INDEX idx_payments_booking_id ON payments(booking_id);
CREATE INDEX idx_payments_payment_reference ON payments(payment_reference);
CREATE INDEX idx_payments_status ON payments(status);
CREATE INDEX idx_payments_gateway_transaction_id ON payments(gateway_transaction_id);
CREATE INDEX idx_payments_paid_at ON payments(paid_at);

-- ========================================
-- TRIGGERS FOR UPDATED_AT
-- ========================================

CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply trigger to relevant tables
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_categories_updated_at BEFORE UPDATE ON categories FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_locations_updated_at BEFORE UPDATE ON locations FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_activity_providers_updated_at BEFORE UPDATE ON activity_providers FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_activities_updated_at BEFORE UPDATE ON activities FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_activity_schedules_updated_at BEFORE UPDATE ON activity_schedules FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_customer_profiles_updated_at BEFORE UPDATE ON customer_profiles FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_bookings_updated_at BEFORE UPDATE ON bookings FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_payments_updated_at BEFORE UPDATE ON payments FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_reviews_updated_at BEFORE UPDATE ON reviews FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_coupons_updated_at BEFORE UPDATE ON coupons FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_system_settings_updated_at BEFORE UPDATE ON system_settings FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ========================================
-- FUNCTIONS FOR BUSINESS LOGIC
-- ========================================

-- Function to generate booking reference
CREATE OR REPLACE FUNCTION generate_booking_reference()
RETURNS TEXT AS $$
BEGIN
    RETURN 'FB' || TO_CHAR(NOW(), 'YYYYMMDD') || LPAD(FLOOR(RANDOM() * 10000)::TEXT, 4, '0');
END;
$$ LANGUAGE plpgsql;

-- Function to update activity rating
CREATE OR REPLACE FUNCTION update_activity_rating()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE activities 
    SET average_rating = (
        SELECT COALESCE(AVG(rating::DECIMAL), 0) 
        FROM reviews 
        WHERE activity_id = NEW.activity_id
    ),
    total_reviews = (
        SELECT COUNT(*) 
        FROM reviews 
        WHERE activity_id = NEW.activity_id
    )
    WHERE activity_id = NEW.activity_id;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Function to update provider rating
CREATE OR REPLACE FUNCTION update_provider_rating()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE activity_providers 
    SET average_rating = (
        SELECT COALESCE(AVG(rating::DECIMAL), 0) 
        FROM reviews 
        WHERE provider_id = NEW.provider_id
    ),
    total_reviews = (
        SELECT COUNT(*) 
        FROM reviews 
        WHERE provider_id = NEW.provider_id
    )
    WHERE provider_id = NEW.provider_id;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Triggers for rating updates
CREATE TRIGGER update_activity_rating_trigger
    AFTER INSERT OR UPDATE OR DELETE ON reviews
    FOR EACH ROW EXECUTE FUNCTION update_activity_rating();

CREATE TRIGGER update_provider_rating_trigger
    AFTER INSERT OR UPDATE OR DELETE ON reviews
    FOR EACH ROW EXECUTE FUNCTION update_provider_rating();

-- ========================================
-- VIEWS FOR COMMON QUERIES
-- ========================================

-- Activity details view with provider and location info
CREATE VIEW activity_details AS
SELECT 
    a.activity_id,
    a.title,
    a.slug,
    a.description,
    a.short_description,
    a.price,
    a.discounted_price,
    a.currency,
    a.duration_minutes,
    a.max_participants,
    a.min_participants,
    a.average_rating,
    a.total_reviews,
    a.status,
    a.is_active,
    a.is_featured,
    c.name as category_name,
    c.slug as category_slug,
    l.name as location_name,
    l.city,
    l.state,
    ap.business_name as provider_name,
    ap.average_rating as provider_rating,
    u.first_name as provider_first_name,
    u.last_name as provider_last_name,
    u.email as provider_email,
    a.created_at,
    a.updated_at
FROM activities a
JOIN categories c ON a.category_id = c.category_id
JOIN locations l ON a.location_id = l.location_id
JOIN activity_providers ap ON a.provider_id = ap.provider_id
JOIN users u ON ap.user_id = u.user_id
WHERE a.status = 'active' AND a.is_active = true;

-- Booking details view
CREATE VIEW booking_details AS
SELECT 
    b.booking_id,
    b.booking_reference,
    b.booking_date,
    b.booking_time,
    b.number_of_participants,
    b.total_amount,
    b.status,
    u.first_name as customer_first_name,
    u.last_name as customer_last_name,
    u.email as customer_email,
    u.phone as customer_phone,
    a.title as activity_title,
    ap.business_name as provider_name,
    l.name as location_name,
    l.city,
    b.created_at,
    b.updated_at
FROM bookings b
JOIN users u ON b.customer_id = u.user_id
JOIN activities a ON b.activity_id = a.activity_id
JOIN activity_providers ap ON a.provider_id = ap.provider_id
JOIN locations l ON a.location_id = l.location_id;