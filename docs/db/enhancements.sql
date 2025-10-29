-- ========================================
-- FUNBOOKR DATABASE ENHANCEMENTS
-- Advanced best practices and improvements
-- ========================================

-- ========================================
-- ADDITIONAL BEST PRACTICES
-- ========================================

-- 1. ADD ROW LEVEL SECURITY (RLS)
-- Enable RLS for sensitive tables
ALTER TABLE users ENABLE ROW LEVEL SECURITY;
ALTER TABLE customer_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE activity_providers ENABLE ROW LEVEL SECURITY;
ALTER TABLE bookings ENABLE ROW LEVEL SECURITY;
ALTER TABLE payments ENABLE ROW LEVEL SECURITY;

-- Create policies for different user roles
-- Users can only see their own data
CREATE POLICY user_isolation_policy ON users
    USING (user_id = current_setting('app.current_user_id')::uuid);

-- Customers can only see their own profiles
CREATE POLICY customer_profile_policy ON customer_profiles
    USING (user_id = current_setting('app.current_user_id')::uuid);

-- Providers can only see their own business data
CREATE POLICY provider_policy ON activity_providers
    USING (user_id = current_setting('app.current_user_id')::uuid);

-- Booking access policies
CREATE POLICY booking_customer_policy ON bookings
    USING (customer_id = current_setting('app.current_user_id')::uuid);

CREATE POLICY booking_provider_policy ON bookings
    USING (activity_id IN (
        SELECT a.activity_id FROM activities a
        JOIN activity_providers ap ON a.provider_id = ap.provider_id
        WHERE ap.user_id = current_setting('app.current_user_id')::uuid
    ));

-- 2. ENHANCED AUDIT LOGGING
-- Create audit log table
CREATE TABLE audit_log (
    audit_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    table_name VARCHAR(64) NOT NULL,
    record_id UUID NOT NULL,
    action VARCHAR(10) NOT NULL, -- INSERT, UPDATE, DELETE
    old_values JSONB,
    new_values JSONB,
    changed_by UUID,
    changed_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    ip_address INET,
    user_agent TEXT
);

-- Create audit trigger function
CREATE OR REPLACE FUNCTION audit_trigger_function()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO audit_log (
        table_name,
        record_id,
        action,
        old_values,
        new_values,
        changed_by
    ) VALUES (
        TG_TABLE_NAME,
        COALESCE(NEW.user_id, OLD.user_id, NEW.booking_id, OLD.booking_id),
        TG_OP,
        CASE WHEN TG_OP = 'DELETE' THEN row_to_json(OLD) ELSE NULL END,
        CASE WHEN TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN row_to_json(NEW) ELSE NULL END,
        current_setting('app.current_user_id', true)::uuid
    );
    
    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

-- Apply audit triggers to critical tables
CREATE TRIGGER audit_users_trigger
    AFTER INSERT OR UPDATE OR DELETE ON users
    FOR EACH ROW EXECUTE FUNCTION audit_trigger_function();

CREATE TRIGGER audit_bookings_trigger
    AFTER INSERT OR UPDATE OR DELETE ON bookings
    FOR EACH ROW EXECUTE FUNCTION audit_trigger_function();

CREATE TRIGGER audit_payments_trigger
    AFTER INSERT OR UPDATE OR DELETE ON payments
    FOR EACH ROW EXECUTE FUNCTION audit_trigger_function();

-- 3. ADVANCED CONSTRAINTS AND DATA VALIDATION

-- Add check constraints for business rules
ALTER TABLE activities ADD CONSTRAINT chk_activities_price_positive 
    CHECK (price > 0);

ALTER TABLE activities ADD CONSTRAINT chk_activities_participants 
    CHECK (max_participants >= min_participants AND min_participants > 0);

ALTER TABLE activities ADD CONSTRAINT chk_activities_duration 
    CHECK (duration_minutes > 0);

ALTER TABLE bookings ADD CONSTRAINT chk_booking_future_date 
    CHECK (booking_date >= CURRENT_DATE);

ALTER TABLE bookings ADD CONSTRAINT chk_booking_participants_positive 
    CHECK (number_of_participants > 0);

ALTER TABLE coupons ADD CONSTRAINT chk_coupon_dates 
    CHECK (valid_until > valid_from);

ALTER TABLE coupons ADD CONSTRAINT chk_coupon_discount_positive 
    CHECK (discount_value > 0);

-- 4. LICENSE AND CERTIFICATION MANAGEMENT
-- Create provider licenses table
CREATE TABLE provider_licenses (
    license_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    provider_id UUID NOT NULL REFERENCES activity_providers(provider_id) ON DELETE CASCADE,
    license_type VARCHAR(100) NOT NULL, -- 'business_license', 'adventure_sports_permit', 'water_sports_license', 'tour_operator_license', etc.
    license_number VARCHAR(100) NOT NULL,
    license_name VARCHAR(200) NOT NULL,
    issuing_authority VARCHAR(200) NOT NULL, -- Kerala Tourism Board, Pollution Control Board, etc.
    issued_date DATE NOT NULL,
    expiry_date DATE NOT NULL,
    license_status VARCHAR(50) DEFAULT 'active', -- 'active', 'expired', 'suspended', 'revoked'
    document_url VARCHAR(500), -- URL to uploaded license document
    verification_status VARCHAR(50) DEFAULT 'pending', -- 'pending', 'verified', 'rejected'
    verified_by UUID REFERENCES users(user_id), -- Admin who verified
    verified_at TIMESTAMP WITH TIME ZONE,
    verification_notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(provider_id, license_type, license_number)
);

-- Create insurance details table
CREATE TABLE provider_insurance (
    insurance_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    provider_id UUID NOT NULL REFERENCES activity_providers(provider_id) ON DELETE CASCADE,
    insurance_type VARCHAR(100) NOT NULL, -- 'public_liability', 'professional_indemnity', 'equipment_insurance'
    insurance_provider VARCHAR(200) NOT NULL,
    policy_number VARCHAR(100) NOT NULL,
    coverage_amount DECIMAL(15, 2),
    currency VARCHAR(3) DEFAULT 'INR',
    policy_start_date DATE NOT NULL,
    policy_end_date DATE NOT NULL,
    policy_status VARCHAR(50) DEFAULT 'active', -- 'active', 'expired', 'cancelled'
    document_url VARCHAR(500), -- URL to uploaded policy document
    verification_status VARCHAR(50) DEFAULT 'pending',
    verified_by UUID REFERENCES users(user_id),
    verified_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(provider_id, insurance_type, policy_number)
);

-- Create safety certifications table
CREATE TABLE provider_certifications (
    certification_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    provider_id UUID NOT NULL REFERENCES activity_providers(provider_id) ON DELETE CASCADE,
    certification_type VARCHAR(100) NOT NULL, -- 'first_aid', 'scuba_instructor', 'adventure_guide', 'safety_officer'
    certification_name VARCHAR(200) NOT NULL,
    certifying_body VARCHAR(200) NOT NULL, -- PADI, NAUI, Adventure Activities Licensing Authority
    certificate_number VARCHAR(100),
    instructor_name VARCHAR(200), -- Name of certified instructor/guide
    issued_date DATE NOT NULL,
    expiry_date DATE,
    certification_level VARCHAR(100), -- 'basic', 'advanced', 'instructor', 'master'
    document_url VARCHAR(500),
    verification_status VARCHAR(50) DEFAULT 'pending',
    verified_by UUID REFERENCES users(user_id),
    verified_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create compliance requirements table (defines what licenses are required for each activity type)
CREATE TABLE compliance_requirements (
    requirement_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    category_id UUID NOT NULL REFERENCES categories(category_id),
    activity_type VARCHAR(200), -- Specific activity type (can be NULL for category-wide requirements)
    required_license_type VARCHAR(100) NOT NULL,
    is_mandatory BOOLEAN DEFAULT true,
    location_specific BOOLEAN DEFAULT false, -- Some licenses are location-specific
    description TEXT,
    regulatory_authority VARCHAR(200),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Add license compliance status to activities table
ALTER TABLE activities ADD COLUMN license_compliance_status VARCHAR(50) DEFAULT 'pending'; -- 'compliant', 'non_compliant', 'pending', 'expired'
ALTER TABLE activities ADD COLUMN compliance_checked_at TIMESTAMP WITH TIME ZONE;
ALTER TABLE activities ADD COLUMN compliance_notes TEXT;

-- 5. SOFT DELETE FUNCTIONALITY
-- Add deleted_at columns for soft delete
ALTER TABLE users ADD COLUMN deleted_at TIMESTAMP WITH TIME ZONE;
ALTER TABLE activities ADD COLUMN deleted_at TIMESTAMP WITH TIME ZONE;
ALTER TABLE activity_providers ADD COLUMN deleted_at TIMESTAMP WITH TIME ZONE;

-- Create function for soft delete
CREATE OR REPLACE FUNCTION soft_delete(table_name TEXT, record_id UUID)
RETURNS BOOLEAN AS $$
BEGIN
    EXECUTE format('UPDATE %I SET deleted_at = CURRENT_TIMESTAMP WHERE %I = $1', 
                   table_name, table_name || '_id')
    USING record_id;
    
    RETURN FOUND;
END;
$$ LANGUAGE plpgsql;

-- Update views to exclude soft-deleted records
DROP VIEW IF EXISTS activity_details;
CREATE VIEW activity_details AS
SELECT 
    a.activity_id,
    a.title,
    a.description,
    a.short_description,
    a.price,
    a.currency,
    a.duration_minutes,
    a.max_participants,
    a.min_participants,
    a.rating,
    a.total_reviews,
    a.status,
    a.featured,
    c.name as category_name,
    l.name as location_name,
    l.city,
    l.state,
    ap.business_name as provider_name,
    ap.rating as provider_rating,
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
WHERE a.status = 'active' 
  AND a.deleted_at IS NULL 
  AND ap.deleted_at IS NULL 
  AND u.deleted_at IS NULL;

-- 5. ADVANCED INDEXING FOR PERFORMANCE

-- Composite indexes for common query patterns
CREATE INDEX idx_activities_location_category ON activities(location_id, category_id) WHERE status = 'active' AND deleted_at IS NULL;
CREATE INDEX idx_activities_price_rating ON activities(price, rating DESC) WHERE status = 'active' AND deleted_at IS NULL;
CREATE INDEX idx_bookings_date_status ON bookings(booking_date, status);
CREATE INDEX idx_bookings_customer_date ON bookings(customer_id, booking_date DESC);

-- Partial indexes for better performance
CREATE INDEX idx_active_activities ON activities(created_at DESC) WHERE status = 'active' AND deleted_at IS NULL;
CREATE INDEX idx_featured_activities ON activities(rating DESC) WHERE featured = true AND status = 'active';
CREATE INDEX idx_pending_bookings ON bookings(created_at) WHERE status = 'pending';
CREATE INDEX idx_failed_payments ON payments(created_at) WHERE status = 'failed';

-- Full-text search indexes
CREATE INDEX idx_activities_search ON activities USING gin(to_tsvector('english', title || ' ' || description || ' ' || short_description));
CREATE INDEX idx_providers_search ON activity_providers USING gin(to_tsvector('english', business_name || ' ' || description));

-- Geographic indexes for location-based queries
CREATE INDEX idx_locations_geography ON locations USING gist(point(longitude, latitude));

-- 6. MATERIALIZED VIEWS FOR ANALYTICS

-- Popular activities materialized view
CREATE MATERIALIZED VIEW mv_popular_activities AS
SELECT 
    a.activity_id,
    a.title,
    a.price,
    COUNT(b.booking_id) as booking_count,
    SUM(b.number_of_participants) as total_participants,
    AVG(r.rating) as avg_rating,
    COUNT(r.review_id) as review_count,
    ap.business_name,
    l.city
FROM activities a
LEFT JOIN bookings b ON a.activity_id = b.activity_id 
    AND b.created_at >= CURRENT_DATE - INTERVAL '90 days'
LEFT JOIN reviews r ON a.activity_id = r.activity_id
JOIN activity_providers ap ON a.provider_id = ap.provider_id
JOIN locations l ON a.location_id = l.location_id
WHERE a.status = 'active' AND a.deleted_at IS NULL
GROUP BY a.activity_id, a.title, a.price, ap.business_name, l.city;

-- Create unique index on materialized view
CREATE UNIQUE INDEX idx_mv_popular_activities_id ON mv_popular_activities(activity_id);

-- Revenue analytics materialized view
CREATE MATERIALIZED VIEW mv_revenue_analytics AS
SELECT 
    DATE_TRUNC('month', b.created_at) as month,
    c.name as category,
    l.city,
    COUNT(b.booking_id) as booking_count,
    SUM(b.total_amount) as total_revenue,
    AVG(b.total_amount) as avg_booking_value,
    COUNT(DISTINCT b.customer_id) as unique_customers
FROM bookings b
JOIN activities a ON b.activity_id = a.activity_id
JOIN categories c ON a.category_id = c.category_id
JOIN locations l ON a.location_id = l.location_id
WHERE b.status IN ('confirmed', 'completed')
GROUP BY DATE_TRUNC('month', b.created_at), c.name, l.city;

-- Function to refresh materialized views
CREATE OR REPLACE FUNCTION refresh_analytics_views()
RETURNS VOID AS $$
BEGIN
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_popular_activities;
    REFRESH MATERIALIZED VIEW CONCURRENTLY mv_revenue_analytics;
END;
$$ LANGUAGE plpgsql;

-- 7. ADVANCED BUSINESS LOGIC FUNCTIONS

-- Function to check activity availability
CREATE OR REPLACE FUNCTION check_activity_availability(
    p_activity_id UUID,
    p_booking_date DATE,
    p_booking_time TIME,
    p_participants INTEGER
)
RETURNS BOOLEAN AS $$
DECLARE
    v_available_spots INTEGER;
    v_booked_spots INTEGER;
    v_day_of_week INTEGER;
BEGIN
    -- Get day of week (0=Sunday, 1=Monday, etc.)
    v_day_of_week := EXTRACT(DOW FROM p_booking_date);
    
    -- Get available spots for this time slot
    SELECT available_spots INTO v_available_spots
    FROM activity_schedules 
    WHERE activity_id = p_activity_id 
      AND start_time = p_booking_time
      AND v_day_of_week = ANY(days_of_week)
      AND is_active = true;
    
    IF v_available_spots IS NULL THEN
        RETURN false; -- No schedule found
    END IF;
    
    -- Get currently booked spots
    SELECT COALESCE(SUM(number_of_participants), 0) INTO v_booked_spots
    FROM bookings
    WHERE activity_id = p_activity_id
      AND booking_date = p_booking_date
      AND booking_time = p_booking_time
      AND status IN ('confirmed', 'completed');
    
    -- Check if enough spots available
    RETURN (v_available_spots - v_booked_spots) >= p_participants;
END;
$$ LANGUAGE plpgsql;

-- Function to check license compliance for an activity
CREATE OR REPLACE FUNCTION check_license_compliance(p_activity_id UUID)
RETURNS TABLE (
    is_compliant BOOLEAN,
    missing_licenses TEXT[],
    expired_licenses TEXT[],
    compliance_details JSONB
) AS $$
DECLARE
    v_provider_id UUID;
    v_category_id UUID;
    v_activity_title VARCHAR(200);
    v_missing_licenses TEXT[] := '{}';
    v_expired_licenses TEXT[] := '{}';
    v_is_compliant BOOLEAN := true;
    v_compliance_details JSONB := '{}';
    req RECORD;
    lic RECORD;
BEGIN
    -- Get activity details
    SELECT a.provider_id, a.category_id, a.title 
    INTO v_provider_id, v_category_id, v_activity_title
    FROM activities a 
    WHERE a.activity_id = p_activity_id;
    
    -- Check each compliance requirement
    FOR req IN 
        SELECT cr.required_license_type, cr.is_mandatory, cr.description
        FROM compliance_requirements cr 
        WHERE cr.category_id = v_category_id
          AND (cr.activity_type IS NULL OR cr.activity_type = v_activity_title)
    LOOP
        -- Check if provider has this license
        SELECT * INTO lic
        FROM provider_licenses pl
        WHERE pl.provider_id = v_provider_id
          AND pl.license_type = req.required_license_type
          AND pl.license_status = 'active'
          AND pl.verification_status = 'verified'
        ORDER BY pl.expiry_date DESC
        LIMIT 1;
        
        IF NOT FOUND THEN
            IF req.is_mandatory THEN
                v_missing_licenses := array_append(v_missing_licenses, req.required_license_type);
                v_is_compliant := false;
            END IF;
        ELSE
            -- Check if license is expired or expiring soon
            IF lic.expiry_date < CURRENT_DATE THEN
                v_expired_licenses := array_append(v_expired_licenses, req.required_license_type);
                v_is_compliant := false;
            ELSIF lic.expiry_date <= CURRENT_DATE + INTERVAL '30 days' THEN
                -- License expiring soon - add to compliance details but don't mark as non-compliant
                v_compliance_details := v_compliance_details || jsonb_build_object(
                    'expiring_soon', 
                    COALESCE(v_compliance_details->'expiring_soon', '[]'::jsonb) || to_jsonb(req.required_license_type)
                );
            END IF;
        END IF;
    END LOOP;
    
    -- Return results
    RETURN QUERY SELECT 
        v_is_compliant,
        v_missing_licenses,
        v_expired_licenses,
        v_compliance_details;
END;
$$ LANGUAGE plpgsql;

-- Function to update activity compliance status
CREATE OR REPLACE FUNCTION update_activity_compliance_status(p_activity_id UUID)
RETURNS VOID AS $$
DECLARE
    v_compliance_result RECORD;
    v_status VARCHAR(50);
    v_notes TEXT;
BEGIN
    -- Get compliance check results
    SELECT * INTO v_compliance_result
    FROM check_license_compliance(p_activity_id);
    
    -- Determine status
    IF v_compliance_result.is_compliant THEN
        v_status := 'compliant';
        v_notes := 'All required licenses are valid and verified.';
    ELSE
        v_status := 'non_compliant';
        v_notes := '';
        
        IF array_length(v_compliance_result.missing_licenses, 1) > 0 THEN
            v_notes := v_notes || 'Missing licenses: ' || array_to_string(v_compliance_result.missing_licenses, ', ') || '. ';
        END IF;
        
        IF array_length(v_compliance_result.expired_licenses, 1) > 0 THEN
            v_notes := v_notes || 'Expired licenses: ' || array_to_string(v_compliance_result.expired_licenses, ', ') || '. ';
        END IF;
    END IF;
    
    -- Update activity
    UPDATE activities 
    SET 
        license_compliance_status = v_status,
        compliance_checked_at = CURRENT_TIMESTAMP,
        compliance_notes = v_notes
    WHERE activity_id = p_activity_id;
END;
$$ LANGUAGE plpgsql;

-- Function to calculate dynamic pricing
CREATE OR REPLACE FUNCTION calculate_dynamic_price(
    p_activity_id UUID,
    p_booking_date DATE,
    p_participants INTEGER
)
RETURNS DECIMAL AS $$
DECLARE
    v_base_price DECIMAL;
    v_final_price DECIMAL;
    v_demand_multiplier DECIMAL := 1.0;
    v_booking_count INTEGER;
BEGIN
    -- Get base price
    SELECT price INTO v_base_price
    FROM activities 
    WHERE activity_id = p_activity_id;
    
    -- Calculate demand (bookings in last 7 days)
    SELECT COUNT(*) INTO v_booking_count
    FROM bookings b
    WHERE b.activity_id = p_activity_id
      AND b.created_at >= CURRENT_DATE - INTERVAL '7 days'
      AND b.status IN ('confirmed', 'completed');
    
    -- Apply demand-based pricing
    IF v_booking_count > 10 THEN
        v_demand_multiplier := 1.2; -- 20% increase for high demand
    ELSIF v_booking_count > 5 THEN
        v_demand_multiplier := 1.1; -- 10% increase for medium demand
    END IF;
    
    -- Apply group discount for large bookings
    IF p_participants >= 10 THEN
        v_demand_multiplier := v_demand_multiplier * 0.9; -- 10% group discount
    ELSIF p_participants >= 5 THEN
        v_demand_multiplier := v_demand_multiplier * 0.95; -- 5% group discount
    END IF;
    
    v_final_price := v_base_price * v_demand_multiplier * p_participants;
    
    RETURN ROUND(v_final_price, 2);
END;
$$ LANGUAGE plpgsql;

-- 8. NOTIFICATION SYSTEM ENHANCEMENTS

-- Create notification templates table
CREATE TABLE notification_templates (
    template_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    template_key VARCHAR(100) UNIQUE NOT NULL,
    title_template VARCHAR(200) NOT NULL,
    message_template TEXT NOT NULL,
    notification_type VARCHAR(50) NOT NULL,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Insert default notification templates
INSERT INTO notification_templates (template_key, title_template, message_template, notification_type) VALUES
('booking_confirmed', 'Booking Confirmed - {{activity_title}}', 'Your booking {{booking_reference}} for {{activity_title}} on {{booking_date}} has been confirmed. Get ready for an amazing experience!', 'booking'),
('booking_cancelled', 'Booking Cancelled - {{activity_title}}', 'Your booking {{booking_reference}} for {{activity_title}} has been cancelled. {{cancellation_reason}}', 'booking'),
('payment_completed', 'Payment Successful', 'Payment of {{amount}} {{currency}} for booking {{booking_reference}} has been completed successfully.', 'payment'),
('review_request', 'How was your experience?', 'We hope you enjoyed {{activity_title}}. Please take a moment to share your experience with a review.', 'review');

-- Function to send notifications
CREATE OR REPLACE FUNCTION send_notification(
    p_user_id UUID,
    p_template_key VARCHAR,
    p_variables JSONB,
    p_related_booking_id UUID DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
    v_template RECORD;
    v_title TEXT;
    v_message TEXT;
    v_notification_id UUID;
BEGIN
    -- Get template
    SELECT * INTO v_template
    FROM notification_templates 
    WHERE template_key = p_template_key AND is_active = true;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Notification template not found: %', p_template_key;
    END IF;
    
    -- Replace variables in title and message
    v_title := v_template.title_template;
    v_message := v_template.message_template;
    
    -- Simple variable replacement (in production, use a proper template engine)
    FOR key_val IN SELECT key, value FROM jsonb_each_text(p_variables) LOOP
        v_title := REPLACE(v_title, '{{' || key_val.key || '}}', key_val.value);
        v_message := REPLACE(v_message, '{{' || key_val.key || '}}', key_val.value);
    END LOOP;
    
    -- Insert notification
    INSERT INTO notifications (user_id, title, message, type, related_booking_id)
    VALUES (p_user_id, v_title, v_message, v_template.notification_type, p_related_booking_id)
    RETURNING notification_id INTO v_notification_id;
    
    RETURN v_notification_id;
END;
$$ LANGUAGE plpgsql;

-- 9. ADVANCED REPORTING VIEWS

-- Provider dashboard view
CREATE VIEW provider_dashboard AS
SELECT 
    ap.provider_id,
    ap.business_name,
    ap.rating as provider_rating,
    COUNT(DISTINCT a.activity_id) as total_activities,
    COUNT(DISTINCT CASE WHEN b.created_at >= CURRENT_DATE - INTERVAL '30 days' THEN b.booking_id END) as bookings_last_30_days,
    SUM(CASE WHEN b.created_at >= CURRENT_DATE - INTERVAL '30 days' AND b.status IN ('confirmed', 'completed') THEN b.total_amount ELSE 0 END) as revenue_last_30_days,
    COUNT(DISTINCT CASE WHEN r.created_at >= CURRENT_DATE - INTERVAL '30 days' THEN r.review_id END) as reviews_last_30_days,
    AVG(CASE WHEN r.created_at >= CURRENT_DATE - INTERVAL '30 days' THEN r.rating END) as avg_rating_last_30_days
FROM activity_providers ap
LEFT JOIN activities a ON ap.provider_id = a.provider_id AND a.deleted_at IS NULL
LEFT JOIN bookings b ON a.activity_id = b.activity_id
LEFT JOIN reviews r ON a.activity_id = r.activity_id
WHERE ap.deleted_at IS NULL
GROUP BY ap.provider_id, ap.business_name, ap.rating;

-- License compliance dashboard view
CREATE VIEW license_compliance_dashboard AS
SELECT 
    ap.provider_id,
    ap.business_name,
    ap.verification_status as provider_verification,
    COUNT(pl.license_id) as total_licenses,
    COUNT(CASE WHEN pl.license_status = 'active' AND pl.verification_status = 'verified' THEN 1 END) as active_verified_licenses,
    COUNT(CASE WHEN pl.expiry_date <= CURRENT_DATE + INTERVAL '30 days' AND pl.expiry_date > CURRENT_DATE THEN 1 END) as licenses_expiring_soon,
    COUNT(CASE WHEN pl.expiry_date < CURRENT_DATE THEN 1 END) as expired_licenses,
    COUNT(CASE WHEN pl.verification_status = 'pending' THEN 1 END) as pending_verification,
    COUNT(pi.insurance_id) as total_insurance_policies,
    COUNT(CASE WHEN pi.policy_status = 'active' AND pi.verification_status = 'verified' THEN 1 END) as active_insurance,
    COUNT(pc.certification_id) as total_certifications,
    COUNT(CASE WHEN pc.verification_status = 'verified' THEN 1 END) as verified_certifications,
    COUNT(DISTINCT a.activity_id) as total_activities,
    COUNT(DISTINCT CASE WHEN a.license_compliance_status = 'compliant' THEN a.activity_id END) as compliant_activities,
    COUNT(DISTINCT CASE WHEN a.license_compliance_status = 'non_compliant' THEN a.activity_id END) as non_compliant_activities
FROM activity_providers ap
LEFT JOIN provider_licenses pl ON ap.provider_id = pl.provider_id
LEFT JOIN provider_insurance pi ON ap.provider_id = pi.provider_id
LEFT JOIN provider_certifications pc ON ap.provider_id = pc.provider_id
LEFT JOIN activities a ON ap.provider_id = a.provider_id AND a.deleted_at IS NULL
WHERE ap.deleted_at IS NULL
GROUP BY ap.provider_id, ap.business_name, ap.verification_status;

-- License expiry tracking view
CREATE VIEW license_expiry_tracking AS
SELECT 
    pl.license_id,
    ap.business_name,
    pl.license_type,
    pl.license_name,
    pl.license_number,
    pl.issuing_authority,
    pl.expiry_date,
    pl.license_status,
    pl.verification_status,
    CASE 
        WHEN pl.expiry_date < CURRENT_DATE THEN 'Expired'
        WHEN pl.expiry_date <= CURRENT_DATE + INTERVAL '7 days' THEN 'Critical - 7 days'
        WHEN pl.expiry_date <= CURRENT_DATE + INTERVAL '30 days' THEN 'Warning - 30 days'
        WHEN pl.expiry_date <= CURRENT_DATE + INTERVAL '60 days' THEN 'Notice - 60 days'
        ELSE 'Valid'
    END as expiry_status,
    EXTRACT(DAYS FROM pl.expiry_date - CURRENT_DATE) as days_until_expiry,
    COUNT(a.activity_id) as affected_activities
FROM provider_licenses pl
JOIN activity_providers ap ON pl.provider_id = ap.provider_id
LEFT JOIN activities a ON ap.provider_id = a.provider_id 
    AND a.status = 'active' 
    AND a.deleted_at IS NULL
WHERE pl.license_status = 'active'
  AND ap.deleted_at IS NULL
GROUP BY pl.license_id, ap.business_name, pl.license_type, pl.license_name, 
         pl.license_number, pl.issuing_authority, pl.expiry_date, 
         pl.license_status, pl.verification_status
ORDER BY pl.expiry_date;

-- Activity compliance summary view
CREATE VIEW activity_compliance_summary AS
SELECT 
    a.activity_id,
    a.title,
    a.license_compliance_status,
    a.compliance_checked_at,
    a.compliance_notes,
    ap.business_name as provider_name,
    c.name as category_name,
    l.city,
    COUNT(cr.requirement_id) as total_requirements,
    COUNT(CASE WHEN cr.is_mandatory THEN 1 END) as mandatory_requirements,
    compliance_check.is_compliant,
    compliance_check.missing_licenses,
    compliance_check.expired_licenses
FROM activities a
JOIN activity_providers ap ON a.provider_id = ap.provider_id
JOIN categories c ON a.category_id = c.category_id
JOIN locations l ON a.location_id = l.location_id
LEFT JOIN compliance_requirements cr ON c.category_id = cr.category_id
LEFT JOIN LATERAL check_license_compliance(a.activity_id) compliance_check ON true
WHERE a.status = 'active' 
  AND a.deleted_at IS NULL
  AND ap.deleted_at IS NULL
GROUP BY a.activity_id, a.title, a.license_compliance_status, a.compliance_checked_at,
         a.compliance_notes, ap.business_name, c.name, l.city,
         compliance_check.is_compliant, compliance_check.missing_licenses,
         compliance_check.expired_licenses
ORDER BY a.license_compliance_status, a.title;

-- Customer insights view
CREATE VIEW customer_insights AS
SELECT 
    u.user_id,
    u.first_name || ' ' || u.last_name as customer_name,
    u.email,
    COUNT(b.booking_id) as total_bookings,
    SUM(b.total_amount) as lifetime_value,
    AVG(b.total_amount) as avg_booking_value,
    MIN(b.created_at) as first_booking_date,
    MAX(b.created_at) as last_booking_date,
    EXTRACT(DAYS FROM CURRENT_DATE - MAX(b.created_at)) as days_since_last_booking,
    COUNT(DISTINCT a.category_id) as categories_tried,
    ARRAY_AGG(DISTINCT c.name) as favorite_categories
FROM users u
LEFT JOIN bookings b ON u.user_id = b.customer_id
LEFT JOIN activities a ON b.activity_id = a.activity_id
LEFT JOIN categories c ON a.category_id = c.category_id
WHERE u.role = 'customer' AND u.deleted_at IS NULL
GROUP BY u.user_id, u.first_name, u.last_name, u.email;

-- 10. DATABASE MAINTENANCE FUNCTIONS

-- Function to clean up old audit logs
CREATE OR REPLACE FUNCTION cleanup_old_audit_logs(retention_days INTEGER DEFAULT 365)
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM audit_log 
    WHERE changed_at < CURRENT_DATE - INTERVAL '1 day' * retention_days;
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

-- Function to check and update license expiry status
CREATE OR REPLACE FUNCTION update_license_expiry_status()
RETURNS TABLE (
    updated_licenses INTEGER,
    expired_licenses INTEGER,
    affected_activities INTEGER
) AS $$
DECLARE
    v_updated_licenses INTEGER := 0;
    v_expired_licenses INTEGER := 0;
    v_affected_activities INTEGER := 0;
BEGIN
    -- Update expired licenses
    UPDATE provider_licenses 
    SET license_status = 'expired'
    WHERE expiry_date < CURRENT_DATE 
      AND license_status = 'active';
    
    GET DIAGNOSTICS v_expired_licenses = ROW_COUNT;
    
    -- Update insurance policies
    UPDATE provider_insurance 
    SET policy_status = 'expired'
    WHERE policy_end_date < CURRENT_DATE 
      AND policy_status = 'active';
    
    GET DIAGNOSTICS v_updated_licenses = ROW_COUNT;
    v_updated_licenses := v_updated_licenses + v_expired_licenses;
    
    -- Update activity compliance status for affected activities
    WITH affected_providers AS (
        SELECT DISTINCT provider_id 
        FROM provider_licenses 
        WHERE license_status = 'expired' 
           OR expiry_date <= CURRENT_DATE + INTERVAL '30 days'
    )
    UPDATE activities 
    SET license_compliance_status = 'pending'
    WHERE provider_id IN (SELECT provider_id FROM affected_providers)
      AND license_compliance_status != 'non_compliant';
    
    GET DIAGNOSTICS v_affected_activities = ROW_COUNT;
    
    -- Return results
    RETURN QUERY SELECT v_updated_licenses, v_expired_licenses, v_affected_activities;
END;
$$ LANGUAGE plpgsql;

-- Function to send license expiry notifications
CREATE OR REPLACE FUNCTION send_license_expiry_notifications()
RETURNS INTEGER AS $$
DECLARE
    notification_count INTEGER := 0;
    license_record RECORD;
BEGIN
    -- Send notifications for licenses expiring in 30 days
    FOR license_record IN 
        SELECT 
            pl.provider_id,
            ap.business_name,
            u.user_id,
            pl.license_type,
            pl.license_name,
            pl.expiry_date
        FROM provider_licenses pl
        JOIN activity_providers ap ON pl.provider_id = ap.provider_id
        JOIN users u ON ap.user_id = u.user_id
        WHERE pl.expiry_date BETWEEN CURRENT_DATE AND CURRENT_DATE + INTERVAL '30 days'
          AND pl.license_status = 'active'
          AND pl.verification_status = 'verified'
    LOOP
        -- Send notification using the notification function
        PERFORM send_notification(
            license_record.user_id,
            'license_expiry_warning',
            jsonb_build_object(
                'business_name', license_record.business_name,
                'license_type', license_record.license_type,
                'license_name', license_record.license_name,
                'expiry_date', license_record.expiry_date::text
            )
        );
        
        notification_count := notification_count + 1;
    END LOOP;
    
    RETURN notification_count;
END;
$$ LANGUAGE plpgsql;

-- Function to update all activity compliance status
CREATE OR REPLACE FUNCTION update_all_activity_compliance()
RETURNS INTEGER AS $$
DECLARE
    activity_record RECORD;
    updated_count INTEGER := 0;
BEGIN
    FOR activity_record IN 
        SELECT activity_id 
        FROM activities 
        WHERE status = 'active' AND deleted_at IS NULL
    LOOP
        PERFORM update_activity_compliance_status(activity_record.activity_id);
        updated_count := updated_count + 1;
    END LOOP;
    
    RETURN updated_count;
END;
$$ LANGUAGE plpgsql;

-- Function to update activity and provider ratings (can be run periodically)
CREATE OR REPLACE FUNCTION recalculate_all_ratings()
RETURNS VOID AS $$
BEGIN
    -- Update activity ratings
    UPDATE activities 
    SET rating = COALESCE(subq.avg_rating, 0),
        total_reviews = COALESCE(subq.review_count, 0)
    FROM (
        SELECT 
            activity_id,
            AVG(rating::DECIMAL) as avg_rating,
            COUNT(*) as review_count
        FROM reviews 
        GROUP BY activity_id
    ) subq
    WHERE activities.activity_id = subq.activity_id;
    
    -- Update provider ratings
    UPDATE activity_providers 
    SET rating = COALESCE(subq.avg_rating, 0),
        total_reviews = COALESCE(subq.review_count, 0)
    FROM (
        SELECT 
            provider_id,
            AVG(rating::DECIMAL) as avg_rating,
            COUNT(*) as review_count
        FROM reviews 
        GROUP BY provider_id
    ) subq
    WHERE activity_providers.provider_id = subq.provider_id;
END;
$$ LANGUAGE plpgsql;

-- ========================================
-- ADDITIONAL INDEXES FOR NEW FEATURES
-- ========================================

CREATE INDEX idx_audit_log_table_record ON audit_log(table_name, record_id);
CREATE INDEX idx_audit_log_changed_at ON audit_log(changed_at);
CREATE INDEX idx_notification_templates_key ON notification_templates(template_key);
CREATE INDEX idx_notifications_user_unread ON notifications(user_id, is_read, created_at);

-- License management indexes
CREATE INDEX idx_provider_licenses_provider ON provider_licenses(provider_id);
CREATE INDEX idx_provider_licenses_type ON provider_licenses(license_type);
CREATE INDEX idx_provider_licenses_expiry ON provider_licenses(expiry_date);
CREATE INDEX idx_provider_licenses_status ON provider_licenses(license_status, verification_status);
CREATE INDEX idx_provider_licenses_expiring ON provider_licenses(expiry_date) WHERE license_status = 'active' AND expiry_date >= CURRENT_DATE;

CREATE INDEX idx_provider_insurance_provider ON provider_insurance(provider_id);
CREATE INDEX idx_provider_insurance_expiry ON provider_insurance(policy_end_date);
CREATE INDEX idx_provider_insurance_status ON provider_insurance(policy_status, verification_status);

CREATE INDEX idx_provider_certifications_provider ON provider_certifications(provider_id);
CREATE INDEX idx_provider_certifications_type ON provider_certifications(certification_type);
CREATE INDEX idx_provider_certifications_expiry ON provider_certifications(expiry_date);

CREATE INDEX idx_compliance_requirements_category ON compliance_requirements(category_id);
CREATE INDEX idx_compliance_requirements_mandatory ON compliance_requirements(is_mandatory);

CREATE INDEX idx_activities_compliance ON activities(license_compliance_status);
CREATE INDEX idx_activities_compliance_checked ON activities(compliance_checked_at);

-- ========================================
-- SCHEDULED MAINTENANCE JOBS
-- ========================================

-- Note: These would typically be run via pg_cron or external scheduler

-- Daily maintenance
-- SELECT cleanup_old_audit_logs(365);
-- SELECT refresh_analytics_views();
-- SELECT update_license_expiry_status();
-- SELECT send_license_expiry_notifications();

-- Weekly maintenance  
-- SELECT recalculate_all_ratings();
-- SELECT update_all_activity_compliance();

-- Monthly maintenance
-- VACUUM ANALYZE; -- Full database statistics update
-- REINDEX INDEX CONCURRENTLY idx_activities_search; -- Rebuild search indexes

-- ========================================
-- LICENSE NOTIFICATION TEMPLATES
-- ========================================

-- Add additional notification templates for license management
INSERT INTO notification_templates (template_key, title_template, message_template, notification_type) VALUES
('license_expiry_warning', 'License Expiring Soon - {{license_type}}', 'Your {{license_type}} ({{license_name}}) for {{business_name}} will expire on {{expiry_date}}. Please renew to avoid service disruption.', 'license'),
('license_expired', 'License Expired - {{license_type}}', 'Your {{license_type}} ({{license_name}}) for {{business_name}} has expired. Activities may be suspended until renewal.', 'license'),
('compliance_check_failed', 'Activity Compliance Issue', 'Activity "{{activity_title}}" has compliance issues. Please check your license status in the provider dashboard.', 'compliance'),
('license_verification_approved', 'License Verified', 'Your {{license_type}} has been successfully verified and approved.', 'license'),
('license_verification_rejected', 'License Verification Failed', 'Your {{license_type}} verification was rejected. Reason: {{rejection_reason}}', 'license');

-- ========================================
-- COMPLIANCE REQUIREMENTS SETUP
-- ========================================

-- Insert common compliance requirements for Kerala activity providers
INSERT INTO compliance_requirements (category_id, activity_type, required_license_type, is_mandatory, description, regulatory_authority) 
SELECT 
    c.category_id,
    NULL, -- Category-wide requirement
    'business_license',
    true,
    'Valid business registration license',
    'Kerala Government - Industries Department'
FROM categories c
WHERE c.name IN ('Adventure', 'Event Organizers', 'Leisure', 'Skill Training');

-- Adventure sports specific requirements
INSERT INTO compliance_requirements (category_id, activity_type, required_license_type, is_mandatory, description, regulatory_authority)
SELECT 
    c.category_id,
    'Scuba Diving',
    'water_sports_license',
    true,
    'Water sports operation license for scuba diving activities',
    'Kerala Tourism Department'
FROM categories c
WHERE c.name = 'Adventure';

INSERT INTO compliance_requirements (category_id, activity_type, required_license_type, is_mandatory, description, regulatory_authority)
SELECT 
    c.category_id,
    'Surfing',
    'water_sports_license',
    true,
    'Water sports operation license for surfing activities',
    'Kerala Tourism Department'
FROM categories c
WHERE c.name = 'Adventure';

INSERT INTO compliance_requirements (category_id, activity_type, required_license_type, is_mandatory, description, regulatory_authority)
SELECT 
    c.category_id,
    NULL,
    'tour_operator_license',
    true,
    'Tour operator license for adventure activities',
    'Kerala Tourism Department'
FROM categories c
WHERE c.name = 'Adventure';

-- Event organizer requirements
INSERT INTO compliance_requirements (category_id, activity_type, required_license_type, is_mandatory, description, regulatory_authority)
SELECT 
    c.category_id,
    NULL,
    'event_permit',
    true,
    'Event organization permit',
    'Local Municipality/Corporation'
FROM categories c
WHERE c.name = 'Event Organizers';

-- Safety certifications for adventure activities
INSERT INTO compliance_requirements (category_id, activity_type, required_license_type, is_mandatory, description, regulatory_authority)
SELECT 
    c.category_id,
    NULL,
    'public_liability_insurance',
    true,
    'Public liability insurance coverage',
    'Insurance Regulatory Authority'
FROM categories c
WHERE c.name IN ('Adventure', 'Event Organizers');

-- ========================================
-- SUMMARY OF ENHANCEMENTS
-- ========================================

/*
ENHANCEMENTS ADDED:

1. ROW LEVEL SECURITY (RLS) - Data isolation between users
2. COMPREHENSIVE AUDIT LOGGING - Track all changes with JSON
3. ADVANCED CONSTRAINTS - Business rule enforcement at DB level
4. LICENSE & CERTIFICATION MANAGEMENT - Complete compliance tracking system:
   - provider_licenses: Business licenses, permits, and regulatory approvals
   - provider_insurance: Insurance policies and coverage tracking
   - provider_certifications: Safety certifications and instructor qualifications
   - compliance_requirements: Configurable requirements by activity category
   - Automated compliance checking and status updates
   - License expiry tracking and notifications
   - Compliance dashboard and reporting views
5. SOFT DELETE - Maintain data integrity while "deleting"
6. PERFORMANCE INDEXING - Composite, partial, and FTS indexes
7. MATERIALIZED VIEWS - Pre-computed analytics for performance
8. BUSINESS LOGIC FUNCTIONS - Availability checking, dynamic pricing, compliance validation
9. NOTIFICATION SYSTEM - Template-based notifications including license alerts
10. REPORTING VIEWS - Ready-made dashboards for providers/customers/compliance
11. MAINTENANCE FUNCTIONS - Automated cleanup, recalculation, and compliance updates

KEY LICENSE MANAGEMENT FEATURES:
- Multi-type license tracking (business, water sports, tour operator, etc.)
- Insurance policy management with coverage amounts
- Safety certifications for instructors and guides
- Automated compliance status checking for activities
- License expiry warnings and notifications
- Comprehensive compliance dashboard for admins
- Regulatory authority tracking
- Document upload support for verification
- Activity suspension for non-compliance

PRODUCTION CONSIDERATIONS:
- Set up monitoring for slow queries
- Implement connection pooling (PgBouncer)
- Configure automated backups with point-in-time recovery
- Set up replication for high availability
- Monitor index usage and optimize regularly
- Implement rate limiting at application level
- Use prepared statements to prevent SQL injection
- Set up alerting for failed payments and system errors
- Configure daily license expiry checks and notifications
- Implement document storage for license verification
- Set up compliance reporting for regulatory authorities
- Create admin workflows for license verification
*/