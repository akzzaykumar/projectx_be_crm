-- ========================================
-- FUNBOOKR SAMPLE DATA INSERTION SCRIPT
-- Populating database with provided activity data
-- ========================================

-- Insert categories based on the provided data
INSERT INTO categories (name, description) VALUES
('Event Organizers', 'Companies organizing events and nightlife activities'),
('Adventure', 'Outdoor adventure activities and water sports'),
('Community', 'Community-based activities and tournaments'),
('NGO', 'Non-profit organization activities'),
('Leisure', 'Relaxation and leisure activities'),
('Game', 'Gaming and entertainment activities'),
('Theatre', 'Cultural performances and traditional arts'),
('Theme Park', 'Theme park and entertainment venues'),
('Skill Training', 'Learning and skill development activities'),
('Museum and Planetarium', 'Educational and science-related visits'),
('Mall', 'Shopping and entertainment complexes'),
('Music and Dance', 'Musical and dance workshops'),
('Dance and Fitness', 'Fitness and dance classes'),
('Cultural Complex', 'Cultural programs and events');

-- Insert locations from the provided data
INSERT INTO locations (name, city, state, country) VALUES
('Kulathoor', 'Thiruvananthapuram', 'Kerala', 'India'),
('Pattom', 'Thiruvananthapuram', 'Kerala', 'India'),
('Kovalam', 'Thiruvananthapuram', 'Kerala', 'India'),
('Thiruvananthapuram', 'Thiruvananthapuram', 'Kerala', 'India'),
('Neyyattinkara', 'Thiruvananthapuram', 'Kerala', 'India'),
('Poovar', 'Thiruvananthapuram', 'Kerala', 'India'),
('Vellayambalam', 'Thiruvananthapuram', 'Kerala', 'India'),
('Kowdiar', 'Thiruvananthapuram', 'Kerala', 'India'),
('Varkala', 'Thiruvananthapuram', 'Kerala', 'India'),
('Chadayamangalam', 'Kollam', 'Kerala', 'India'),
('Fort', 'Thiruvananthapuram', 'Kerala', 'India'),
('Kazhakuttam', 'Thiruvananthapuram', 'Kerala', 'India'),
('Vilayil vilakom, Kidangil Nemom', 'Thiruvananthapuram', 'Kerala', 'India'),
('Munroe Island', 'Kollam', 'Kerala', 'India'),
('Paravur', 'Kollam', 'Kerala', 'India'),
('Edava', 'Thiruvananthapuram', 'Kerala', 'India'),
('Vakkom', 'Thiruvananthapuram', 'Kerala', 'India'),
('Kollam', 'Kollam', 'Kerala', 'India'),
('Nedungolam', 'Kollam', 'Kerala', 'India'),
('Nanthancode', 'Thiruvananthapuram', 'Kerala', 'India'),
('Vikasbhavan', 'Thiruvananthapuram', 'Kerala', 'India'),
('Nanthancodu', 'Thiruvananthapuram', 'Kerala', 'India'),
('Akkulam Bridge', 'Thiruvananthapuram', 'Kerala', 'India'),
('Cotton Hill', 'Thiruvananthapuram', 'Kerala', 'India'),
('Chempukkavu', 'Thiruvananthapuram', 'Kerala', 'India'),
('Sreekaryam', 'Thiruvananthapuram', 'Kerala', 'India'),
('Shasthamangalam', 'Thiruvananthapuram', 'Kerala', 'India');

-- Create admin user
INSERT INTO users (email, password_hash, role, first_name, last_name, phone, is_active, email_verified) VALUES
('admin@funbookr.com', crypt('admin123', gen_salt('bf')), 'admin', 'Admin', 'User', '+919876543210', true, true);

-- Create sample customer users
INSERT INTO users (email, password_hash, role, first_name, last_name, phone, is_active, email_verified) VALUES
('customer1@example.com', crypt('password123', gen_salt('bf')), 'customer', 'John', 'Doe', '+919876543211', true, true),
('customer2@example.com', crypt('password123', gen_salt('bf')), 'customer', 'Jane', 'Smith', '+919876543212', true, true);

-- Create provider users (based on the provided data)
INSERT INTO users (email, password_hash, role, first_name, last_name, phone, is_active, email_verified) VALUES
('amigozhub@gmail.com', crypt('provider123', gen_salt('bf')), 'activity_provider', 'Amigoz', 'Hub', '04712414900', true, true),
('reservations@pattomroyal.com', crypt('provider123', gen_salt('bf')), 'activity_provider', 'Starlight', 'Team', '04714228888', true, true),
('enquiry@bondsafarikovalam.com', crypt('provider123', gen_salt('bf')), 'activity_provider', 'Bond', 'Safari', '8448448846', true, true),
('playfest.in@gmail.com', crypt('provider123', gen_salt('bf')), 'activity_provider', 'Playfest', 'Team', '3562008555', true, true),
('adventureamas@gmail.com', crypt('provider123', gen_salt('bf')), 'activity_provider', 'AMAS', 'Kerala', '9446101056', true, true),
('kovalamsurfclub@gmail.com', crypt('provider123', gen_salt('bf')), 'activity_provider', 'Kovalam', 'Surf Club', '9847347367', true, true),
('universaladventures.in', crypt('provider123', gen_salt('bf')), 'activity_provider', 'Universal', 'Adventures', '8533812266', true, true),
('oceanautadventures@gmail.com', crypt('provider123', gen_salt('bf')), 'activity_provider', 'Oceanaut', 'Adventures', '9747670729', true, true),
('kelakscuba@gmail.com', crypt('provider123', gen_salt('bf')), 'activity_provider', 'Ke-Lak', 'Adventures', '6238331016', true, true),
('info@poovarbackwatercruise.com', crypt('provider123', gen_salt('bf')), 'activity_provider', 'Poovar', 'Backwater', '9947067526', true, true);

-- Create activity providers based on the data
WITH provider_data AS (
    SELECT 
        u.user_id,
        'Amigoz Hub' as business_name,
        'Event organizers specializing in nightclub events' as description,
        'amigoz_hub' as instagram_handle,
        l.location_id
    FROM users u, locations l
    WHERE u.email = 'amigozhub@gmail.com' AND l.name = 'Kulathoor'
    
    UNION ALL
    
    SELECT 
        u.user_id,
        'Starlight' as business_name,
        'Premium nightclub and event venue' as description,
        'starlightbypattomroyal' as instagram_handle,
        l.location_id
    FROM users u, locations l
    WHERE u.email = 'reservations@pattomroyal.com' AND l.name = 'Pattom'
    
    UNION ALL
    
    SELECT 
        u.user_id,
        'Bond Safari' as business_name,
        'Adventure water sports including scuba diving, kayaking, snorkeling, surfing, and parasailing' as description,
        'bond_safari_kovalam' as instagram_handle,
        l.location_id
    FROM users u, locations l
    WHERE u.email = 'enquiry@bondsafarikovalam.com' AND l.name = 'Kovalam'
    
    UNION ALL
    
    SELECT 
        u.user_id,
        'Playfest' as business_name,
        'Community gaming tournaments and events' as description,
        'playfestindia' as instagram_handle,
        l.location_id
    FROM users u, locations l
    WHERE u.email = 'playfest.in@gmail.com' AND l.name = 'Thiruvananthapuram'
    
    UNION ALL
    
    SELECT 
        u.user_id,
        'AMAS Kerala' as business_name,
        'NGO focused on adventure activities and trekking' as description,
        'adventureamas' as instagram_handle,
        l.location_id
    FROM users u, locations l
    WHERE u.email = 'adventureamas@gmail.com' AND l.name = 'Neyyattinkara'
    
    UNION ALL
    
    SELECT 
        u.user_id,
        'Kovalam Surf Club' as business_name,
        'Premier surfing club and training center' as description,
        'kovalam_surf_club' as instagram_handle,
        l.location_id
    FROM users u, locations l
    WHERE u.email = 'kovalamsurfclub@gmail.com' AND l.name = 'Kovalam'
    
    UNION ALL
    
    SELECT 
        u.user_id,
        'Universal Adventures' as business_name,
        'Stand up paddling and kayaking adventures' as description,
        'universaladventuresindia' as instagram_handle,
        l.location_id
    FROM users u, locations l
    WHERE u.email = 'universaladventures.in' AND l.name = 'Kovalam'
    
    UNION ALL
    
    SELECT 
        u.user_id,
        'Oceanaut Adventures' as business_name,
        'Professional scuba diving, snorkeling, and freediving center' as description,
        'oceanautadventures' as instagram_handle,
        l.location_id
    FROM users u, locations l
    WHERE u.email = 'oceanautadventures@gmail.com' AND l.name = 'Kovalam'
    
    UNION ALL
    
    SELECT 
        u.user_id,
        'Ke-Lak Adventures' as business_name,
        'Specialized scuba diving experiences' as description,
        'kelak_adventures' as instagram_handle,
        l.location_id
    FROM users u, locations l
    WHERE u.email = 'kelakscuba@gmail.com' AND l.name = 'Kovalam'
    
    UNION ALL
    
    SELECT 
        u.user_id,
        'Poovar Backwater Cruise' as business_name,
        'Scenic backwater cruise experiences' as description,
        'poovarbackwatercruise' as instagram_handle,
        l.location_id
    FROM users u, locations l
    WHERE u.email = 'info@poovarbackwatercruise.com' AND l.name = 'Poovar'
)
INSERT INTO activity_providers (user_id, business_name, description, instagram_handle, location_id, verification_status)
SELECT user_id, business_name, description, instagram_handle, location_id, 'verified'
FROM provider_data;

-- Add contact information for providers
INSERT INTO provider_contacts (provider_id, contact_type, contact_value, is_primary)
SELECT 
    ap.provider_id,
    'email' as contact_type,
    u.email as contact_value,
    true as is_primary
FROM activity_providers ap
JOIN users u ON ap.user_id = u.user_id;

INSERT INTO provider_contacts (provider_id, contact_type, contact_value, is_primary)
SELECT 
    ap.provider_id,
    'phone' as contact_type,
    u.phone as contact_value,
    false as is_primary
FROM activity_providers ap
JOIN users u ON ap.user_id = u.user_id
WHERE u.phone IS NOT NULL;

-- Create sample activities
INSERT INTO activities (provider_id, category_id, location_id, title, description, short_description, duration_minutes, max_participants, price, difficulty_level)
SELECT 
    ap.provider_id,
    c.category_id,
    ap.location_id,
    'Nightclub Event Experience' as title,
    'Experience the best nightlife in Thiruvananthapuram with our exclusive nightclub events featuring top DJs and premium ambiance.' as description,
    'Premium nightclub experience with top entertainment' as short_description,
    300 as duration_minutes,
    50 as max_participants,
    1500.00 as price,
    'beginner' as difficulty_level
FROM activity_providers ap
JOIN categories c ON c.name = 'Event Organizers'
WHERE ap.business_name = 'Amigoz Hub';

INSERT INTO activities (provider_id, category_id, location_id, title, description, short_description, duration_minutes, max_participants, price, difficulty_level)
SELECT 
    ap.provider_id,
    c.category_id,
    ap.location_id,
    'Scuba Diving Experience' as title,
    'Explore the underwater world of Kovalam with professional scuba diving instruction and equipment. Perfect for beginners and experienced divers.' as description,
    'Professional scuba diving with full equipment and instruction' as short_description,
    180 as duration_minutes,
    8 as max_participants,
    3500.00 as price,
    'intermediate' as difficulty_level
FROM activity_providers ap
JOIN categories c ON c.name = 'Adventure'
WHERE ap.business_name = 'Bond Safari';

INSERT INTO activities (provider_id, category_id, location_id, title, description, short_description, duration_minutes, max_participants, price, difficulty_level)
SELECT 
    ap.provider_id,
    c.category_id,
    ap.location_id,
    'Kayaking Adventure' as title,
    'Paddle through the beautiful waters of Kovalam with our guided kayaking tours. Suitable for all skill levels.' as description,
    'Guided kayaking tours through scenic waters' as short_description,
    120 as duration_minutes,
    12 as max_participants,
    1200.00 as price,
    'beginner' as difficulty_level
FROM activity_providers ap
JOIN categories c ON c.name = 'Adventure'
WHERE ap.business_name = 'Bond Safari';

INSERT INTO activities (provider_id, category_id, location_id, title, description, short_description, duration_minutes, max_participants, price, difficulty_level)
SELECT 
    ap.provider_id,
    c.category_id,
    ap.location_id,
    'Gaming Tournament' as title,
    'Join competitive gaming tournaments with prizes and community interaction. Various games available.' as description,
    'Competitive gaming tournaments with prizes' as short_description,
    240 as duration_minutes,
    32 as max_participants,
    500.00 as price,
    'beginner' as difficulty_level
FROM activity_providers ap
JOIN categories c ON c.name = 'Community'
WHERE ap.business_name = 'Playfest';

INSERT INTO activities (provider_id, category_id, location_id, title, description, short_description, duration_minutes, max_participants, price, difficulty_level)
SELECT 
    ap.provider_id,
    c.category_id,
    ap.location_id,
    'Trekking Expedition' as title,
    'Adventure trekking through the scenic landscapes of Kerala with experienced guides and safety equipment.' as description,
    'Guided trekking expedition through Kerala landscapes' as short_description,
    480 as duration_minutes,
    15 as max_participants,
    800.00 as price,
    'intermediate' as difficulty_level
FROM activity_providers ap
JOIN categories c ON c.name = 'NGO'
WHERE ap.business_name = 'AMAS Kerala';

INSERT INTO activities (provider_id, category_id, location_id, title, description, short_description, duration_minutes, max_participants, price, difficulty_level)
SELECT 
    ap.provider_id,
    c.category_id,
    ap.location_id,
    'Surfing Lessons' as title,
    'Learn to surf with professional instructors at Kovalam beach. All equipment provided, suitable for beginners.' as description,
    'Professional surfing lessons with equipment included' as short_description,
    150 as duration_minutes,
    6 as max_participants,
    2000.00 as price,
    'beginner' as difficulty_level
FROM activity_providers ap
JOIN categories c ON c.name = 'Adventure'
WHERE ap.business_name = 'Kovalam Surf Club';

INSERT INTO activities (provider_id, category_id, location_id, title, description, short_description, duration_minutes, max_participants, price, difficulty_level)
SELECT 
    ap.provider_id,
    c.category_id,
    ap.location_id,
    'Stand Up Paddling' as title,
    'Experience the calm waters of Kovalam with stand up paddleboarding. Great for fitness and relaxation.' as description,
    'Stand up paddleboarding experience in calm waters' as short_description,
    90 as duration_minutes,
    10 as max_participants,
    1000.00 as price,
    'beginner' as difficulty_level
FROM activity_providers ap
JOIN categories c ON c.name = 'Adventure'
WHERE ap.business_name = 'Universal Adventures';

INSERT INTO activities (provider_id, category_id, location_id, title, description, short_description, duration_minutes, max_participants, price, difficulty_level)
SELECT 
    ap.provider_id,
    c.category_id,
    ap.location_id,
    'Backwater Cruise' as title,
    'Enjoy a peaceful cruise through the scenic backwaters of Poovar with traditional boat and local guide.' as description,
    'Scenic backwater cruise with traditional boat' as short_description,
    120 as duration_minutes,
    20 as max_participants,
    800.00 as price,
    'beginner' as difficulty_level
FROM activity_providers ap
JOIN categories c ON c.name = 'Leisure'
WHERE ap.business_name = 'Poovar Backwater Cruise';

-- Add activity tags for better searchability
INSERT INTO activity_tags (activity_id, tag)
SELECT a.activity_id, unnest(ARRAY['nightlife', 'entertainment', 'club', 'music'])
FROM activities a
WHERE a.title = 'Nightclub Event Experience';

INSERT INTO activity_tags (activity_id, tag)
SELECT a.activity_id, unnest(ARRAY['scuba', 'diving', 'underwater', 'adventure', 'water'])
FROM activities a
WHERE a.title = 'Scuba Diving Experience';

INSERT INTO activity_tags (activity_id, tag)
SELECT a.activity_id, unnest(ARRAY['kayaking', 'paddle', 'water', 'adventure', 'outdoor'])
FROM activities a
WHERE a.title = 'Kayaking Adventure';

INSERT INTO activity_tags (activity_id, tag)
SELECT a.activity_id, unnest(ARRAY['gaming', 'tournament', 'competition', 'esports', 'community'])
FROM activities a
WHERE a.title = 'Gaming Tournament';

INSERT INTO activity_tags (activity_id, tag)
SELECT a.activity_id, unnest(ARRAY['trekking', 'hiking', 'nature', 'outdoor', 'adventure'])
FROM activities a
WHERE a.title = 'Trekking Expedition';

INSERT INTO activity_tags (activity_id, tag)
SELECT a.activity_id, unnest(ARRAY['surfing', 'waves', 'beach', 'water', 'lessons'])
FROM activities a
WHERE a.title = 'Surfing Lessons';

INSERT INTO activity_tags (activity_id, tag)
SELECT a.activity_id, unnest(ARRAY['paddling', 'sup', 'water', 'fitness', 'balance'])
FROM activities a
WHERE a.title = 'Stand Up Paddling';

INSERT INTO activity_tags (activity_id, tag)
SELECT a.activity_id, unnest(ARRAY['cruise', 'backwater', 'boat', 'scenic', 'relaxing'])
FROM activities a
WHERE a.title = 'Backwater Cruise';

-- Create activity schedules
INSERT INTO activity_schedules (activity_id, start_time, end_time, days_of_week, available_spots)
SELECT 
    a.activity_id,
    '20:00'::TIME as start_time,
    '02:00'::TIME as end_time,
    ARRAY[5,6] as days_of_week, -- Friday and Saturday
    50 as available_spots
FROM activities a
WHERE a.title = 'Nightclub Event Experience';

INSERT INTO activity_schedules (activity_id, start_time, end_time, days_of_week, available_spots)
SELECT 
    a.activity_id,
    '09:00'::TIME as start_time,
    '12:00'::TIME as end_time,
    ARRAY[1,2,3,4,5,6,0] as days_of_week, -- All days
    8 as available_spots
FROM activities a
WHERE a.title = 'Scuba Diving Experience';

INSERT INTO activity_schedules (activity_id, start_time, end_time, days_of_week, available_spots)
SELECT 
    a.activity_id,
    '07:00'::TIME as start_time,
    '09:00'::TIME as end_time,
    ARRAY[1,2,3,4,5,6,0] as days_of_week, -- All days
    12 as available_spots
FROM activities a
WHERE a.title = 'Kayaking Adventure';

-- Add some sample customer profiles
INSERT INTO customer_profiles (user_id, date_of_birth, gender, emergency_contact_name, emergency_contact_phone)
SELECT 
    u.user_id,
    '1990-05-15'::DATE as date_of_birth,
    'Male' as gender,
    'Emergency Contact' as emergency_contact_name,
    '+919876543213' as emergency_contact_phone
FROM users u
WHERE u.email = 'customer1@example.com';

INSERT INTO customer_profiles (user_id, date_of_birth, gender, emergency_contact_name, emergency_contact_phone)
SELECT 
    u.user_id,
    '1992-08-22'::DATE as date_of_birth,
    'Female' as gender,
    'Emergency Contact' as emergency_contact_name,
    '+919876543214' as emergency_contact_phone
FROM users u
WHERE u.email = 'customer2@example.com';

-- Create some sample bookings
INSERT INTO bookings (customer_id, activity_id, booking_reference, booking_date, booking_time, number_of_participants, total_amount, status)
SELECT 
    u.user_id as customer_id,
    a.activity_id,
    generate_booking_reference() as booking_reference,
    CURRENT_DATE + INTERVAL '7 days' as booking_date,
    '09:00'::TIME as booking_time,
    2 as number_of_participants,
    7000.00 as total_amount,
    'confirmed' as status
FROM users u, activities a
WHERE u.email = 'customer1@example.com' AND a.title = 'Scuba Diving Experience';

INSERT INTO bookings (customer_id, activity_id, booking_reference, booking_date, booking_time, number_of_participants, total_amount, status)
SELECT 
    u.user_id as customer_id,
    a.activity_id,
    generate_booking_reference() as booking_reference,
    CURRENT_DATE + INTERVAL '10 days' as booking_date,
    '07:00'::TIME as booking_time,
    1 as number_of_participants,
    1200.00 as total_amount,
    'confirmed' as status
FROM users u, activities a
WHERE u.email = 'customer2@example.com' AND a.title = 'Kayaking Adventure';

-- Create sample payments for bookings
INSERT INTO payments (booking_id, payment_reference, amount, payment_method, payment_gateway, status, processed_at)
SELECT 
    b.booking_id,
    'PAY' || TO_CHAR(NOW(), 'YYYYMMDDHHMMSS') || LPAD(FLOOR(RANDOM() * 1000)::TEXT, 3, '0') as payment_reference,
    b.total_amount,
    'card' as payment_method,
    'razorpay' as payment_gateway,
    'completed' as status,
    CURRENT_TIMESTAMP as processed_at
FROM bookings b
WHERE b.status = 'confirmed';

-- Create some sample reviews
INSERT INTO reviews (booking_id, customer_id, activity_id, provider_id, rating, title, review_text)
SELECT 
    b.booking_id,
    b.customer_id,
    b.activity_id,
    a.provider_id,
    5 as rating,
    'Amazing Experience!' as title,
    'Had an incredible time with the scuba diving experience. The instructors were professional and the underwater views were breathtaking. Highly recommended!' as review_text
FROM bookings b
JOIN activities a ON b.activity_id = a.activity_id
WHERE a.title = 'Scuba Diving Experience'
LIMIT 1;

-- Add some system settings
INSERT INTO system_settings (key, value, description) VALUES
('platform_commission_rate', '0.10', 'Platform commission rate (10%)'),
('default_currency', 'INR', 'Default currency for the platform'),
('booking_cancellation_window', '24', 'Minimum hours before activity to allow cancellation'),
('max_participants_per_booking', '20', 'Maximum participants allowed per booking'),
('review_window_days', '30', 'Days after activity completion to allow reviews');

-- Create a sample coupon
INSERT INTO coupons (code, description, discount_type, discount_value, min_order_amount, valid_from, valid_until, usage_limit)
VALUES 
('WELCOME10', 'Welcome discount for new users', 'percentage', 10.00, 500.00, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '30 days', 100);

COMMIT;