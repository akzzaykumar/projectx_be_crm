-- ========================================
-- LICENSE MANAGEMENT SAMPLE DATA
-- Sample license, insurance, and certification data
-- ========================================

-- Note: Run the main sample data script first, then this one

-- Sample compliance requirements (already added in enhancements.sql)

-- Sample provider licenses
INSERT INTO provider_licenses (provider_id, license_type, license_number, license_name, issuing_authority, issued_date, expiry_date, verification_status)
SELECT 
    ap.provider_id,
    'business_license',
    'BL' || LPAD(FLOOR(RANDOM() * 10000)::TEXT, 4, '0'),
    'Business Registration Certificate',
    'Kerala Government - Industries Department',
    CURRENT_DATE - INTERVAL '2 years',
    CURRENT_DATE + INTERVAL '3 years',
    'verified'
FROM activity_providers ap
WHERE ap.business_name IN ('Bond Safari', 'Kovalam Surf Club', 'Universal Adventures', 'Oceanaut Adventures', 'Ke-Lak Adventures');

-- Water sports licenses for adventure providers
INSERT INTO provider_licenses (provider_id, license_type, license_number, license_name, issuing_authority, issued_date, expiry_date, verification_status)
SELECT 
    ap.provider_id,
    'water_sports_license',
    'WS' || LPAD(FLOOR(RANDOM() * 10000)::TEXT, 4, '0'),
    'Water Sports Operation License',
    'Kerala Tourism Department',
    CURRENT_DATE - INTERVAL '1 year',
    CURRENT_DATE + INTERVAL '2 years',
    'verified'
FROM activity_providers ap
WHERE ap.business_name IN ('Bond Safari', 'Kovalam Surf Club', 'Universal Adventures', 'Oceanaut Adventures', 'Ke-Lak Adventures');

-- Tour operator licenses
INSERT INTO provider_licenses (provider_id, license_type, license_number, license_name, issuing_authority, issued_date, expiry_date, verification_status)
SELECT 
    ap.provider_id,
    'tour_operator_license',
    'TO' || LPAD(FLOOR(RANDOM() * 10000)::TEXT, 4, '0'),
    'Tour Operator License',
    'Kerala Tourism Department',
    CURRENT_DATE - INTERVAL '6 months',
    CURRENT_DATE + INTERVAL '1.5 years',
    'verified'
FROM activity_providers ap
WHERE ap.business_name IN ('Bond Safari', 'AMAS Kerala', 'Poovar Backwater Cruise');

-- Event permits for event organizers
INSERT INTO provider_licenses (provider_id, license_type, license_number, license_name, issuing_authority, issued_date, expiry_date, verification_status)
SELECT 
    ap.provider_id,
    'event_permit',
    'EP' || LPAD(FLOOR(RANDOM() * 10000)::TEXT, 4, '0'),
    'Event Organization Permit',
    'Thiruvananthapuram Corporation',
    CURRENT_DATE - INTERVAL '3 months',
    CURRENT_DATE + INTERVAL '9 months',
    'verified'
FROM activity_providers ap
WHERE ap.business_name IN ('Amigoz Hub', 'Starlight');

-- Sample license expiring soon (for testing notifications)
INSERT INTO provider_licenses (provider_id, license_type, license_number, license_name, issuing_authority, issued_date, expiry_date, verification_status)
SELECT 
    ap.provider_id,
    'safety_permit',
    'SP' || LPAD(FLOOR(RANDOM() * 10000)::TEXT, 4, '0'),
    'Adventure Safety Permit',
    'Kerala Adventure Sports Board',
    CURRENT_DATE - INTERVAL '11 months',
    CURRENT_DATE + INTERVAL '20 days', -- Expiring soon
    'verified'
FROM activity_providers ap
WHERE ap.business_name = 'Bond Safari';

-- Sample insurance policies
INSERT INTO provider_insurance (provider_id, insurance_type, insurance_provider, policy_number, coverage_amount, policy_start_date, policy_end_date, verification_status)
SELECT 
    ap.provider_id,
    'public_liability',
    'National Insurance Company',
    'PI' || LPAD(FLOOR(RANDOM() * 100000)::TEXT, 5, '0'),
    5000000.00, -- 50 Lakh coverage
    CURRENT_DATE - INTERVAL '6 months',
    CURRENT_DATE + INTERVAL '1.5 years',
    'verified'
FROM activity_providers ap
WHERE ap.business_name IN ('Bond Safari', 'Kovalam Surf Club', 'Universal Adventures', 'Oceanaut Adventures');

INSERT INTO provider_insurance (provider_id, insurance_type, insurance_provider, policy_number, coverage_amount, policy_start_date, policy_end_date, verification_status)
SELECT 
    ap.provider_id,
    'equipment_insurance',
    'Oriental Insurance Company',
    'EI' || LPAD(FLOOR(RANDOM() * 100000)::TEXT, 5, '0'),
    1000000.00, -- 10 Lakh coverage
    CURRENT_DATE - INTERVAL '8 months',
    CURRENT_DATE + INTERVAL '1.33 years',
    'verified'
FROM activity_providers ap
WHERE ap.business_name IN ('Bond Safari', 'Oceanaut Adventures', 'Ke-Lak Adventures');

-- Sample safety certifications
INSERT INTO provider_certifications (provider_id, certification_type, certification_name, certifying_body, certificate_number, instructor_name, issued_date, expiry_date, certification_level, verification_status)
SELECT 
    ap.provider_id,
    'scuba_instructor',
    'PADI Open Water Scuba Instructor',
    'Professional Association of Diving Instructors (PADI)',
    'PADI' || LPAD(FLOOR(RANDOM() * 1000000)::TEXT, 6, '0'),
    'Rajesh Kumar',
    CURRENT_DATE - INTERVAL '2 years',
    CURRENT_DATE + INTERVAL '1 year',
    'instructor',
    'verified'
FROM activity_providers ap
WHERE ap.business_name IN ('Bond Safari', 'Oceanaut Adventures', 'Ke-Lak Adventures');

INSERT INTO provider_certifications (provider_id, certification_type, certification_name, certifying_body, certificate_number, instructor_name, issued_date, expiry_date, certification_level, verification_status)
SELECT 
    ap.provider_id,
    'first_aid',
    'Emergency First Aid Certification',
    'Indian Red Cross Society',
    'FA' || LPAD(FLOOR(RANDOM() * 10000)::TEXT, 4, '0'),
    'Priya Nair',
    CURRENT_DATE - INTERVAL '1 year',
    CURRENT_DATE + INTERVAL '2 years',
    'basic',
    'verified'
FROM activity_providers ap
WHERE ap.business_name IN ('Bond Safari', 'Kovalam Surf Club', 'Universal Adventures', 'AMAS Kerala');

INSERT INTO provider_certifications (provider_id, certification_type, certification_name, certifying_body, certificate_number, instructor_name, issued_date, expiry_date, certification_level, verification_status)
SELECT 
    ap.provider_id,
    'surf_instructor',
    'International Surfing Association Level 1',
    'International Surfing Association (ISA)',
    'ISA' || LPAD(FLOOR(RANDOM() * 10000)::TEXT, 4, '0'),
    'Arjun Menon',
    CURRENT_DATE - INTERVAL '1.5 years',
    NULL, -- Some certifications don't expire
    'level_1',
    'verified'
FROM activity_providers ap
WHERE ap.business_name = 'Kovalam Surf Club';

INSERT INTO provider_certifications (provider_id, certification_type, certification_name, certifying_body, certificate_number, instructor_name, issued_date, expiry_date, certification_level, verification_status)
SELECT 
    ap.provider_id,
    'adventure_guide',
    'Certified Adventure Activity Leader',
    'Adventure Tourism Association of India',
    'ATAI' || LPAD(FLOOR(RANDOM() * 10000)::TEXT, 4, '0'),
    'Suresh Babu',
    CURRENT_DATE - INTERVAL '10 months',
    CURRENT_DATE + INTERVAL '2 years',
    'advanced',
    'verified'
FROM activity_providers ap
WHERE ap.business_name = 'AMAS Kerala';

-- Update activity compliance status for all activities
-- This will run the compliance check function for each activity
SELECT update_all_activity_compliance();

-- Display compliance summary
SELECT 
    business_name,
    total_licenses,
    active_verified_licenses,
    licenses_expiring_soon,
    expired_licenses,
    total_activities,
    compliant_activities,
    non_compliant_activities
FROM license_compliance_dashboard
ORDER BY business_name;

COMMIT;