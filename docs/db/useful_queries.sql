-- ========================================
-- FUNBOOKR DATABASE - USEFUL QUERIES
-- Common queries for the Funbookr platform
-- ========================================

-- ========================================
-- USER MANAGEMENT QUERIES
-- ========================================

-- Get all activity providers with their business details
SELECT 
    u.user_id,
    u.email,
    u.first_name,
    u.last_name,
    ap.business_name,
    ap.description,
    ap.instagram_handle,
    ap.rating,
    ap.total_reviews,
    ap.verification_status,
    l.name as location,
    l.city,
    l.state
FROM users u
JOIN activity_providers ap ON u.user_id = ap.user_id
JOIN locations l ON ap.location_id = l.location_id
WHERE u.role = 'activity_provider'
ORDER BY ap.rating DESC, ap.total_reviews DESC;

-- Get customer profile with booking history
SELECT 
    u.user_id,
    u.email,
    u.first_name,
    u.last_name,
    cp.date_of_birth,
    cp.gender,
    COUNT(b.booking_id) as total_bookings,
    SUM(b.total_amount) as total_spent,
    MAX(b.created_at) as last_booking_date
FROM users u
LEFT JOIN customer_profiles cp ON u.user_id = cp.user_id
LEFT JOIN bookings b ON u.user_id = b.customer_id
WHERE u.role = 'customer'
GROUP BY u.user_id, u.email, u.first_name, u.last_name, cp.date_of_birth, cp.gender
ORDER BY total_spent DESC NULLS LAST;

-- ========================================
-- ACTIVITY SEARCH AND FILTERING QUERIES
-- ========================================

-- Search activities by location and category
SELECT 
    a.activity_id,
    a.title,
    a.short_description,
    a.price,
    a.currency,
    a.duration_minutes,
    a.rating,
    a.total_reviews,
    c.name as category,
    l.name as location,
    l.city,
    ap.business_name as provider_name
FROM activities a
JOIN categories c ON a.category_id = c.category_id
JOIN locations l ON a.location_id = l.location_id
JOIN activity_providers ap ON a.provider_id = ap.provider_id
WHERE a.status = 'active'
  AND l.city = 'Thiruvananthapuram'  -- Filter by city
  AND c.name = 'Adventure'           -- Filter by category
ORDER BY a.featured DESC, a.rating DESC, a.total_reviews DESC;

-- Get activities with availability for a specific date
SELECT 
    a.activity_id,
    a.title,
    a.price,
    a.max_participants,
    asched.start_time,
    asched.end_time,
    asched.available_spots,
    COALESCE(asched.available_spots - booked.total_booked, asched.available_spots) as remaining_spots
FROM activities a
JOIN activity_schedules asched ON a.activity_id = asched.activity_id
LEFT JOIN (
    SELECT 
        activity_id,
        booking_date,
        booking_time,
        SUM(number_of_participants) as total_booked
    FROM bookings 
    WHERE status IN ('confirmed', 'completed')
      AND booking_date = '2024-11-01'  -- Specific date
    GROUP BY activity_id, booking_date, booking_time
) booked ON a.activity_id = booked.activity_id 
         AND asched.start_time = booked.booking_time
WHERE a.status = 'active'
  AND asched.is_active = true
  AND EXTRACT(DOW FROM DATE '2024-11-01') = ANY(asched.days_of_week)
  AND COALESCE(asched.available_spots - booked.total_booked, asched.available_spots) > 0
ORDER BY asched.start_time;

-- Search activities by tags
SELECT DISTINCT
    a.activity_id,
    a.title,
    a.price,
    a.rating,
    ap.business_name
FROM activities a
JOIN activity_tags at ON a.activity_id = at.activity_id
JOIN activity_providers ap ON a.provider_id = ap.provider_id
WHERE a.status = 'active'
  AND at.tag IN ('scuba', 'diving', 'water', 'adventure')  -- Search by tags
ORDER BY a.rating DESC;

-- Get featured activities with images
SELECT 
    a.activity_id,
    a.title,
    a.short_description,
    a.price,
    a.rating,
    ap.business_name,
    l.name as location,
    ai.image_url as primary_image
FROM activities a
JOIN activity_providers ap ON a.provider_id = ap.provider_id
JOIN locations l ON a.location_id = l.location_id
LEFT JOIN activity_images ai ON a.activity_id = ai.activity_id AND ai.is_primary = true
WHERE a.featured = true AND a.status = 'active'
ORDER BY a.rating DESC;

-- ========================================
-- BOOKING MANAGEMENT QUERIES
-- ========================================

-- Get booking details with customer and activity info
SELECT 
    b.booking_id,
    b.booking_reference,
    b.booking_date,
    b.booking_time,
    b.number_of_participants,
    b.total_amount,
    b.status,
    u.first_name || ' ' || u.last_name as customer_name,
    u.email as customer_email,
    u.phone as customer_phone,
    a.title as activity_title,
    ap.business_name as provider_name,
    l.name as location
FROM bookings b
JOIN users u ON b.customer_id = u.user_id
JOIN activities a ON b.activity_id = a.activity_id
JOIN activity_providers ap ON a.provider_id = ap.provider_id
JOIN locations l ON a.location_id = l.location_id
WHERE b.booking_date >= CURRENT_DATE
ORDER BY b.booking_date, b.booking_time;

-- Get provider's upcoming bookings
SELECT 
    b.booking_id,
    b.booking_reference,
    b.booking_date,
    b.booking_time,
    b.number_of_participants,
    b.status,
    u.first_name || ' ' || u.last_name as customer_name,
    u.phone as customer_phone,
    a.title as activity_title,
    b.special_requests
FROM bookings b
JOIN users u ON b.customer_id = u.user_id
JOIN activities a ON b.activity_id = a.activity_id
JOIN activity_providers ap ON a.provider_id = ap.provider_id
JOIN users pu ON ap.user_id = pu.user_id
WHERE pu.email = 'enquiry@bondsafarikovalam.com'  -- Provider email
  AND b.booking_date >= CURRENT_DATE
  AND b.status IN ('confirmed', 'pending')
ORDER BY b.booking_date, b.booking_time;

-- Daily booking summary for providers
SELECT 
    DATE(b.booking_date) as booking_date,
    COUNT(*) as total_bookings,
    SUM(b.number_of_participants) as total_participants,
    SUM(b.total_amount) as total_revenue,
    COUNT(CASE WHEN b.status = 'confirmed' THEN 1 END) as confirmed_bookings,
    COUNT(CASE WHEN b.status = 'pending' THEN 1 END) as pending_bookings,
    COUNT(CASE WHEN b.status = 'cancelled' THEN 1 END) as cancelled_bookings
FROM bookings b
JOIN activities a ON b.activity_id = a.activity_id
JOIN activity_providers ap ON a.provider_id = ap.provider_id
JOIN users u ON ap.user_id = u.user_id
WHERE u.email = 'enquiry@bondsafarikovalam.com'  -- Provider email
  AND b.booking_date >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY DATE(b.booking_date)
ORDER BY booking_date DESC;

-- ========================================
-- PAYMENT AND REVENUE QUERIES
-- ========================================

-- Revenue analysis by category
SELECT 
    c.name as category,
    COUNT(b.booking_id) as total_bookings,
    SUM(b.total_amount) as total_revenue,
    AVG(b.total_amount) as avg_booking_value,
    SUM(CASE WHEN p.status = 'completed' THEN b.total_amount ELSE 0 END) as confirmed_revenue
FROM bookings b
JOIN activities a ON b.activity_id = a.activity_id
JOIN categories c ON a.category_id = c.category_id
LEFT JOIN payments p ON b.booking_id = p.booking_id
WHERE b.created_at >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY c.name
ORDER BY total_revenue DESC;

-- Payment status summary
SELECT 
    p.status,
    COUNT(*) as payment_count,
    SUM(p.amount) as total_amount,
    AVG(p.amount) as avg_amount
FROM payments p
WHERE p.created_at >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY p.status
ORDER BY total_amount DESC;

-- Failed payments that need attention
SELECT 
    b.booking_reference,
    u.first_name || ' ' || u.last_name as customer_name,
    u.email,
    a.title as activity_title,
    p.amount,
    p.payment_method,
    p.created_at,
    p.gateway_transaction_id
FROM payments p
JOIN bookings b ON p.booking_id = b.booking_id
JOIN users u ON b.customer_id = u.user_id
JOIN activities a ON b.activity_id = a.activity_id
WHERE p.status = 'failed'
  AND p.created_at >= CURRENT_DATE - INTERVAL '7 days'
ORDER BY p.created_at DESC;

-- ========================================
-- REVIEW AND RATING QUERIES
-- ========================================

-- Recent reviews for an activity
SELECT 
    r.rating,
    r.title,
    r.review_text,
    u.first_name as reviewer_name,
    r.created_at
FROM reviews r
JOIN users u ON r.customer_id = u.user_id
JOIN activities a ON r.activity_id = a.activity_id
WHERE a.title = 'Scuba Diving Experience'
ORDER BY r.created_at DESC
LIMIT 10;

-- Average ratings by provider
SELECT 
    ap.business_name,
    ap.rating as provider_rating,
    ap.total_reviews,
    AVG(r.rating) as calculated_avg_rating,
    COUNT(r.review_id) as review_count
FROM activity_providers ap
LEFT JOIN reviews r ON ap.provider_id = r.provider_id
GROUP BY ap.provider_id, ap.business_name, ap.rating, ap.total_reviews
ORDER BY provider_rating DESC;

-- Top rated activities
SELECT 
    a.title,
    a.rating,
    a.total_reviews,
    ap.business_name as provider,
    c.name as category,
    l.city
FROM activities a
JOIN activity_providers ap ON a.provider_id = ap.provider_id
JOIN categories c ON a.category_id = c.category_id
JOIN locations l ON a.location_id = l.location_id
WHERE a.status = 'active'
  AND a.total_reviews >= 5  -- Minimum reviews for reliability
ORDER BY a.rating DESC, a.total_reviews DESC
LIMIT 20;

-- ========================================
-- ANALYTICS AND REPORTING QUERIES
-- ========================================

-- Monthly booking trends
SELECT 
    DATE_TRUNC('month', b.created_at) as month,
    COUNT(*) as total_bookings,
    COUNT(DISTINCT b.customer_id) as unique_customers,
    SUM(b.total_amount) as total_revenue,
    AVG(b.total_amount) as avg_booking_value
FROM bookings b
WHERE b.created_at >= CURRENT_DATE - INTERVAL '12 months'
GROUP BY DATE_TRUNC('month', b.created_at)
ORDER BY month;

-- Popular activities by booking count
SELECT 
    a.title,
    COUNT(b.booking_id) as booking_count,
    SUM(b.number_of_participants) as total_participants,
    ap.business_name as provider
FROM activities a
JOIN bookings b ON a.activity_id = b.activity_id
JOIN activity_providers ap ON a.provider_id = ap.provider_id
WHERE b.created_at >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY a.activity_id, a.title, ap.business_name
ORDER BY booking_count DESC
LIMIT 10;

-- Customer lifetime value
SELECT 
    u.user_id,
    u.first_name || ' ' || u.last_name as customer_name,
    u.email,
    COUNT(b.booking_id) as total_bookings,
    SUM(b.total_amount) as lifetime_value,
    AVG(b.total_amount) as avg_booking_value,
    MIN(b.created_at) as first_booking,
    MAX(b.created_at) as last_booking
FROM users u
JOIN bookings b ON u.user_id = b.customer_id
WHERE u.role = 'customer'
GROUP BY u.user_id, u.first_name, u.last_name, u.email
HAVING COUNT(b.booking_id) > 1  -- Repeat customers
ORDER BY lifetime_value DESC;

-- Provider performance metrics
SELECT 
    ap.business_name,
    COUNT(DISTINCT a.activity_id) as total_activities,
    COUNT(b.booking_id) as total_bookings,
    SUM(b.total_amount) as total_revenue,
    ap.rating as provider_rating,
    ap.total_reviews,
    AVG(a.rating) as avg_activity_rating
FROM activity_providers ap
LEFT JOIN activities a ON ap.provider_id = a.provider_id
LEFT JOIN bookings b ON a.activity_id = b.activity_id
GROUP BY ap.provider_id, ap.business_name, ap.rating, ap.total_reviews
ORDER BY total_revenue DESC NULLS LAST;

-- ========================================
-- OPERATIONAL QUERIES
-- ========================================

-- Activities that need attention (low ratings or no bookings)
SELECT 
    a.title,
    a.rating,
    a.total_reviews,
    COUNT(b.booking_id) as recent_bookings,
    ap.business_name as provider,
    a.created_at
FROM activities a
JOIN activity_providers ap ON a.provider_id = ap.provider_id
LEFT JOIN bookings b ON a.activity_id = b.activity_id 
    AND b.created_at >= CURRENT_DATE - INTERVAL '30 days'
WHERE a.status = 'active'
GROUP BY a.activity_id, a.title, a.rating, a.total_reviews, ap.business_name, a.created_at
HAVING COUNT(b.booking_id) = 0 OR a.rating < 3.0
ORDER BY a.rating, recent_bookings;

-- Upcoming activities with low availability
SELECT 
    a.title,
    asched.start_time,
    asched.available_spots,
    COALESCE(booked.total_booked, 0) as booked_spots,
    asched.available_spots - COALESCE(booked.total_booked, 0) as remaining_spots,
    ap.business_name
FROM activities a
JOIN activity_schedules asched ON a.activity_id = asched.activity_id
JOIN activity_providers ap ON a.provider_id = ap.provider_id
LEFT JOIN (
    SELECT 
        activity_id,
        booking_time,
        SUM(number_of_participants) as total_booked
    FROM bookings 
    WHERE status IN ('confirmed', 'completed')
      AND booking_date = CURRENT_DATE + INTERVAL '1 day'  -- Tomorrow
    GROUP BY activity_id, booking_time
) booked ON a.activity_id = booked.activity_id 
         AND asched.start_time = booked.booking_time
WHERE asched.is_active = true
  AND EXTRACT(DOW FROM CURRENT_DATE + INTERVAL '1 day') = ANY(asched.days_of_week)
  AND (asched.available_spots - COALESCE(booked.total_booked, 0)) <= 2  -- Low availability
ORDER BY remaining_spots;

-- Cancelled bookings analysis
SELECT 
    DATE_TRUNC('week', b.cancelled_at) as week,
    COUNT(*) as cancellation_count,
    SUM(b.total_amount) as lost_revenue,
    string_agg(DISTINCT b.cancellation_reason, '; ') as common_reasons
FROM bookings b
WHERE b.status = 'cancelled'
  AND b.cancelled_at >= CURRENT_DATE - INTERVAL '8 weeks'
GROUP BY DATE_TRUNC('week', b.cancelled_at)
ORDER BY week DESC;

-- ========================================
-- ENHANCED QUERIES WITH NEW FEATURES
-- ========================================

-- Full-text search for activities
SELECT 
    a.activity_id,
    a.title,
    a.short_description,
    a.price,
    ap.business_name,
    ts_rank_cd(to_tsvector('english', a.title || ' ' || a.description), query) as relevance
FROM activities a
JOIN activity_providers ap ON a.provider_id = ap.provider_id,
     plainto_tsquery('english', 'scuba diving adventure') as query
WHERE to_tsvector('english', a.title || ' ' || a.description) @@ query
  AND a.status = 'active'
  AND a.deleted_at IS NULL
ORDER BY relevance DESC, a.rating DESC;

-- Geographic search - activities within radius
SELECT 
    a.title,
    ap.business_name,
    l.name as location,
    l.city,
    ST_Distance(
        ST_Point(l.longitude, l.latitude)::geography,
        ST_Point(76.9366, 8.5241)::geography  -- Kovalam coordinates
    ) / 1000 as distance_km
FROM activities a
JOIN activity_providers ap ON a.provider_id = ap.provider_id
JOIN locations l ON a.location_id = l.location_id
WHERE a.status = 'active'
  AND a.deleted_at IS NULL
  AND ST_DWithin(
        ST_Point(l.longitude, l.latitude)::geography,
        ST_Point(76.9366, 8.5241)::geography,
        50000  -- 50km radius
      )
ORDER BY distance_km;

-- Dynamic pricing calculation
SELECT 
    a.title,
    a.price as base_price,
    calculate_dynamic_price(a.activity_id, CURRENT_DATE + INTERVAL '7 days', 2) as dynamic_price_2_pax,
    calculate_dynamic_price(a.activity_id, CURRENT_DATE + INTERVAL '7 days', 8) as dynamic_price_8_pax,
    ap.business_name
FROM activities a
JOIN activity_providers ap ON a.provider_id = ap.provider_id
WHERE a.status = 'active'
  AND a.deleted_at IS NULL
ORDER BY a.title;

-- Availability check for specific activities
SELECT 
    a.title,
    a.max_participants,
    asched.start_time,
    asched.available_spots,
    check_activity_availability(a.activity_id, CURRENT_DATE + INTERVAL '3 days', asched.start_time, 2) as available_for_2_people,
    check_activity_availability(a.activity_id, CURRENT_DATE + INTERVAL '3 days', asched.start_time, 5) as available_for_5_people
FROM activities a
JOIN activity_schedules asched ON a.activity_id = asched.activity_id
WHERE a.status = 'active'
  AND a.deleted_at IS NULL
  AND asched.is_active = true
  AND EXTRACT(DOW FROM CURRENT_DATE + INTERVAL '3 days') = ANY(asched.days_of_week)
ORDER BY a.title, asched.start_time;

-- Popular activities from materialized view
SELECT 
    title,
    booking_count,
    total_participants,
    avg_rating,
    business_name,
    city
FROM mv_popular_activities
WHERE booking_count > 0
ORDER BY booking_count DESC, avg_rating DESC
LIMIT 10;

-- Provider dashboard insights
SELECT 
    business_name,
    provider_rating,
    total_activities,
    bookings_last_30_days,
    revenue_last_30_days,
    reviews_last_30_days,
    ROUND(avg_rating_last_30_days, 2) as recent_avg_rating
FROM provider_dashboard
WHERE total_activities > 0
ORDER BY revenue_last_30_days DESC;

-- Customer segmentation analysis
SELECT 
    CASE 
        WHEN lifetime_value >= 10000 THEN 'VIP'
        WHEN lifetime_value >= 5000 THEN 'Premium'
        WHEN lifetime_value >= 1000 THEN 'Regular'
        ELSE 'New'
    END as customer_segment,
    COUNT(*) as customer_count,
    AVG(lifetime_value) as avg_lifetime_value,
    AVG(total_bookings) as avg_total_bookings,
    AVG(days_since_last_booking) as avg_days_since_last_booking
FROM customer_insights
WHERE total_bookings > 0
GROUP BY customer_segment
ORDER BY avg_lifetime_value DESC;

-- Audit trail for suspicious activities
SELECT 
    al.table_name,
    al.action,
    al.changed_at,
    u.email as changed_by_email,
    al.ip_address,
    al.old_values,
    al.new_values
FROM audit_log al
LEFT JOIN users u ON al.changed_by = u.user_id
WHERE al.table_name = 'payments'
  AND al.action = 'UPDATE'
  AND al.old_values->>'status' = 'completed'
  AND al.new_values->>'status' = 'refunded'
  AND al.changed_at >= CURRENT_DATE - INTERVAL '7 days'
ORDER BY al.changed_at DESC;

-- Notification delivery tracking
SELECT 
    nt.template_key,
    nt.notification_type,
    COUNT(n.notification_id) as notifications_sent,
    COUNT(CASE WHEN n.is_read THEN 1 END) as notifications_read,
    ROUND(
        COUNT(CASE WHEN n.is_read THEN 1 END)::DECIMAL / 
        COUNT(n.notification_id) * 100, 2
    ) as read_rate_percentage
FROM notification_templates nt
LEFT JOIN notifications n ON n.type = nt.notification_type
WHERE n.created_at >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY nt.template_key, nt.notification_type
ORDER BY notifications_sent DESC;

-- Revenue analytics from materialized view
SELECT 
    month,
    category,
    city,
    booking_count,
    total_revenue,
    avg_booking_value,
    unique_customers,
    ROUND(total_revenue / LAG(total_revenue) OVER (
        PARTITION BY category, city 
        ORDER BY month
    ) - 1, 2) as revenue_growth_rate
FROM mv_revenue_analytics
WHERE month >= CURRENT_DATE - INTERVAL '12 months'
ORDER BY month DESC, total_revenue DESC;

-- Activities needing attention (low performance indicators)
SELECT 
    a.title,
    a.rating,
    a.total_reviews,
    a.price,
    recent_stats.booking_count_30d,
    recent_stats.revenue_30d,
    recent_stats.last_booking_date,
    ap.business_name,
    CASE 
        WHEN a.rating < 3.0 THEN 'Low Rating'
        WHEN recent_stats.booking_count_30d = 0 THEN 'No Recent Bookings'
        WHEN a.total_reviews < 3 THEN 'Few Reviews'
        WHEN recent_stats.last_booking_date < CURRENT_DATE - INTERVAL '60 days' THEN 'Inactive'
        ELSE 'Other'
    END as attention_reason
FROM activities a
JOIN activity_providers ap ON a.provider_id = ap.provider_id
LEFT JOIN (
    SELECT 
        activity_id,
        COUNT(*) as booking_count_30d,
        SUM(total_amount) as revenue_30d,
        MAX(booking_date) as last_booking_date
    FROM bookings 
    WHERE created_at >= CURRENT_DATE - INTERVAL '30 days'
      AND status IN ('confirmed', 'completed')
    GROUP BY activity_id
) recent_stats ON a.activity_id = recent_stats.activity_id
WHERE a.status = 'active' 
  AND a.deleted_at IS NULL
  AND (
    a.rating < 3.0 OR 
    COALESCE(recent_stats.booking_count_30d, 0) = 0 OR 
    a.total_reviews < 3 OR 
    recent_stats.last_booking_date < CURRENT_DATE - INTERVAL '60 days'
  )
ORDER BY a.rating, recent_stats.booking_count_30d NULLS FIRST;

-- ========================================
-- LICENSE MANAGEMENT QUERIES
-- ========================================

-- License compliance overview by provider
SELECT 
    ap.business_name,
    ap.verification_status as provider_status,
    COUNT(DISTINCT pl.license_id) as total_licenses,
    COUNT(DISTINCT CASE WHEN pl.license_status = 'active' AND pl.verification_status = 'verified' THEN pl.license_id END) as verified_licenses,
    COUNT(DISTINCT CASE WHEN pl.expiry_date <= CURRENT_DATE + INTERVAL '30 days' AND pl.expiry_date > CURRENT_DATE THEN pl.license_id END) as expiring_soon,
    COUNT(DISTINCT CASE WHEN pl.expiry_date < CURRENT_DATE THEN pl.license_id END) as expired_licenses,
    COUNT(DISTINCT a.activity_id) as total_activities,
    COUNT(DISTINCT CASE WHEN a.license_compliance_status = 'compliant' THEN a.activity_id END) as compliant_activities
FROM activity_providers ap
LEFT JOIN provider_licenses pl ON ap.provider_id = pl.provider_id
LEFT JOIN activities a ON ap.provider_id = a.provider_id AND a.deleted_at IS NULL
WHERE ap.deleted_at IS NULL
GROUP BY ap.provider_id, ap.business_name, ap.verification_status
ORDER BY ap.business_name;

-- Licenses expiring in the next 60 days
SELECT 
    ap.business_name,
    pl.license_type,
    pl.license_name,
    pl.license_number,
    pl.issuing_authority,
    pl.expiry_date,
    EXTRACT(DAYS FROM pl.expiry_date - CURRENT_DATE) as days_until_expiry,
    CASE 
        WHEN pl.expiry_date < CURRENT_DATE THEN 'EXPIRED'
        WHEN pl.expiry_date <= CURRENT_DATE + INTERVAL '7 days' THEN 'CRITICAL'
        WHEN pl.expiry_date <= CURRENT_DATE + INTERVAL '30 days' THEN 'WARNING'
        ELSE 'NOTICE'
    END as urgency_level,
    COUNT(a.activity_id) as affected_activities
FROM provider_licenses pl
JOIN activity_providers ap ON pl.provider_id = ap.provider_id
LEFT JOIN activities a ON ap.provider_id = a.provider_id 
    AND a.status = 'active' 
    AND a.deleted_at IS NULL
WHERE pl.expiry_date <= CURRENT_DATE + INTERVAL '60 days'
  AND pl.license_status = 'active'
  AND ap.deleted_at IS NULL
GROUP BY ap.business_name, pl.license_type, pl.license_name, pl.license_number, 
         pl.issuing_authority, pl.expiry_date
ORDER BY pl.expiry_date, ap.business_name;

-- Activity compliance status with detailed breakdown
SELECT 
    a.title,
    ap.business_name,
    c.name as category,
    a.license_compliance_status,
    a.compliance_checked_at,
    compliance_result.is_compliant,
    CASE 
        WHEN array_length(compliance_result.missing_licenses, 1) > 0 
        THEN 'Missing: ' || array_to_string(compliance_result.missing_licenses, ', ')
        ELSE 'All required licenses present'
    END as missing_licenses_info,
    CASE 
        WHEN array_length(compliance_result.expired_licenses, 1) > 0 
        THEN 'Expired: ' || array_to_string(compliance_result.expired_licenses, ', ')
        ELSE 'No expired licenses'
    END as expired_licenses_info,
    COUNT(b.booking_id) as recent_bookings
FROM activities a
JOIN activity_providers ap ON a.provider_id = ap.provider_id
JOIN categories c ON a.category_id = c.category_id
LEFT JOIN LATERAL check_license_compliance(a.activity_id) compliance_result ON true
LEFT JOIN bookings b ON a.activity_id = b.activity_id 
    AND b.created_at >= CURRENT_DATE - INTERVAL '30 days'
    AND b.status IN ('confirmed', 'completed')
WHERE a.status = 'active' 
  AND a.deleted_at IS NULL
  AND ap.deleted_at IS NULL
GROUP BY a.activity_id, a.title, ap.business_name, c.name, a.license_compliance_status,
         a.compliance_checked_at, compliance_result.is_compliant, 
         compliance_result.missing_licenses, compliance_result.expired_licenses
ORDER BY a.license_compliance_status, recent_bookings DESC;

-- Provider insurance coverage summary
SELECT 
    ap.business_name,
    pi.insurance_type,
    pi.insurance_provider,
    pi.policy_number,
    pi.coverage_amount,
    pi.currency,
    pi.policy_start_date,
    pi.policy_end_date,
    EXTRACT(DAYS FROM pi.policy_end_date - CURRENT_DATE) as days_until_expiry,
    pi.policy_status,
    pi.verification_status
FROM provider_insurance pi
JOIN activity_providers ap ON pi.provider_id = ap.provider_id
WHERE ap.deleted_at IS NULL
  AND pi.policy_status = 'active'
ORDER BY ap.business_name, pi.insurance_type, pi.policy_end_date;

-- Safety certifications by provider
SELECT 
    ap.business_name,
    pc.certification_type,
    pc.certification_name,
    pc.certifying_body,
    pc.instructor_name,
    pc.certification_level,
    pc.issued_date,
    pc.expiry_date,
    CASE 
        WHEN pc.expiry_date IS NULL THEN 'No Expiry'
        WHEN pc.expiry_date < CURRENT_DATE THEN 'Expired'
        WHEN pc.expiry_date <= CURRENT_DATE + INTERVAL '90 days' THEN 'Expiring Soon'
        ELSE 'Valid'
    END as certification_status,
    pc.verification_status
FROM provider_certifications pc
JOIN activity_providers ap ON pc.provider_id = ap.provider_id
WHERE ap.deleted_at IS NULL
ORDER BY ap.business_name, pc.certification_type, pc.issued_date DESC;

-- Compliance requirements vs actual licenses
SELECT 
    c.name as category,
    cr.required_license_type,
    cr.is_mandatory,
    cr.description,
    cr.regulatory_authority,
    COUNT(DISTINCT ap.provider_id) as total_providers_in_category,
    COUNT(DISTINCT CASE WHEN pl.license_status = 'active' AND pl.verification_status = 'verified' 
                       THEN pl.provider_id END) as providers_with_license,
    ROUND(
        COUNT(DISTINCT CASE WHEN pl.license_status = 'active' AND pl.verification_status = 'verified' 
                           THEN pl.provider_id END)::DECIMAL / 
        NULLIF(COUNT(DISTINCT ap.provider_id), 0) * 100, 2
    ) as compliance_percentage
FROM compliance_requirements cr
JOIN categories c ON cr.category_id = c.category_id
LEFT JOIN activities a ON c.category_id = a.category_id AND a.deleted_at IS NULL
LEFT JOIN activity_providers ap ON a.provider_id = ap.provider_id AND ap.deleted_at IS NULL
LEFT JOIN provider_licenses pl ON ap.provider_id = pl.provider_id 
    AND pl.license_type = cr.required_license_type
GROUP BY c.category_id, c.name, cr.required_license_type, cr.is_mandatory, 
         cr.description, cr.regulatory_authority
ORDER BY c.name, cr.is_mandatory DESC, compliance_percentage;

-- License verification workload for admins
SELECT 
    pl.license_type,
    pl.verification_status,
    COUNT(*) as count,
    AVG(EXTRACT(DAYS FROM CURRENT_DATE - pl.created_at)) as avg_days_pending,
    MIN(pl.created_at) as oldest_pending_date,
    MAX(pl.created_at) as newest_pending_date
FROM provider_licenses pl
JOIN activity_providers ap ON pl.provider_id = ap.provider_id
WHERE ap.deleted_at IS NULL
GROUP BY pl.license_type, pl.verification_status
ORDER BY pl.license_type, 
         CASE pl.verification_status 
            WHEN 'pending' THEN 1 
            WHEN 'verified' THEN 2 
            WHEN 'rejected' THEN 3 
         END;

-- Provider risk assessment based on compliance
SELECT 
    ap.business_name,
    ap.rating as provider_rating,
    COUNT(DISTINCT a.activity_id) as total_activities,
    COUNT(DISTINCT CASE WHEN a.license_compliance_status = 'compliant' THEN a.activity_id END) as compliant_activities,
    COUNT(DISTINCT CASE WHEN a.license_compliance_status = 'non_compliant' THEN a.activity_id END) as non_compliant_activities,
    COUNT(DISTINCT pl.license_id) as total_licenses,
    COUNT(DISTINCT CASE WHEN pl.expiry_date < CURRENT_DATE THEN pl.license_id END) as expired_licenses,
    COUNT(DISTINCT pi.insurance_id) as insurance_policies,
    COUNT(DISTINCT CASE WHEN pi.policy_end_date < CURRENT_DATE THEN pi.insurance_id END) as expired_policies,
    CASE 
        WHEN COUNT(DISTINCT CASE WHEN a.license_compliance_status = 'non_compliant' THEN a.activity_id END) > 0 
             OR COUNT(DISTINCT CASE WHEN pl.expiry_date < CURRENT_DATE THEN pl.license_id END) > 0
             OR COUNT(DISTINCT CASE WHEN pi.policy_end_date < CURRENT_DATE THEN pi.insurance_id END) > 0
        THEN 'HIGH RISK'
        WHEN COUNT(DISTINCT CASE WHEN pl.expiry_date <= CURRENT_DATE + INTERVAL '30 days' THEN pl.license_id END) > 0
             OR COUNT(DISTINCT CASE WHEN pi.policy_end_date <= CURRENT_DATE + INTERVAL '30 days' THEN pi.insurance_id END) > 0
        THEN 'MEDIUM RISK'
        ELSE 'LOW RISK'
    END as risk_level
FROM activity_providers ap
LEFT JOIN activities a ON ap.provider_id = a.provider_id AND a.deleted_at IS NULL
LEFT JOIN provider_licenses pl ON ap.provider_id = pl.provider_id
LEFT JOIN provider_insurance pi ON ap.provider_id = pi.provider_id
WHERE ap.deleted_at IS NULL
GROUP BY ap.provider_id, ap.business_name, ap.rating
ORDER BY 
    CASE 
        WHEN COUNT(DISTINCT CASE WHEN a.license_compliance_status = 'non_compliant' THEN a.activity_id END) > 0 
             OR COUNT(DISTINCT CASE WHEN pl.expiry_date < CURRENT_DATE THEN pl.license_id END) > 0
             OR COUNT(DISTINCT CASE WHEN pi.policy_end_date < CURRENT_DATE THEN pi.insurance_id END) > 0
        THEN 1
        WHEN COUNT(DISTINCT CASE WHEN pl.expiry_date <= CURRENT_DATE + INTERVAL '30 days' THEN pl.license_id END) > 0
             OR COUNT(DISTINCT CASE WHEN pi.policy_end_date <= CURRENT_DATE + INTERVAL '30 days' THEN pi.insurance_id END) > 0
        THEN 2
        ELSE 3
    END,
    ap.business_name;