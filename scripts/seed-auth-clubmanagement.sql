BEGIN;

-- ==============================
-- AUTH SERVICE (50 users)
-- ==============================

WITH seed_user_ids AS (
    SELECT ('00000000-0000-0000-0000-' || LPAD(gs::text, 12, '0'))::uuid AS user_uuid
    FROM generate_series(1, 50) AS gs
)
DELETE FROM auth."AspNetUserRoles" ur
USING seed_user_ids su
WHERE ur."UserId" = su.user_uuid::text;

DELETE FROM auth."AspNetUsers"
WHERE "UserName" LIKE 'seed.user%';

WITH seed_users AS (
    SELECT
        gs AS idx,
        ('00000000-0000-0000-0000-' || LPAD(gs::text, 12, '0'))::uuid AS user_uuid,
        'seed.user' || LPAD(gs::text, 2, '0') || '@myada.local' AS email,
        'seed.user' || LPAD(gs::text, 2, '0') AS username,
        CASE
            WHEN gs BETWEEN 1 AND 30 THEN 0 -- Student
            WHEN gs BETWEEN 31 AND 40 THEN 2 -- Staff
            ELSE 1 -- Faculty
        END AS user_type
    FROM generate_series(1, 50) AS gs
)
INSERT INTO auth."AspNetUsers" (
    "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
    "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
    "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd",
    "LockoutEnabled", "AccessFailedCount", "FirstName", "LastName", "ProfileImage",
    "Status", "UserType", "CreatedAt", "CreatedByUserId", "ModifiedAt", "ModifiedByUserId",
    "DeletedAt", "DeletedByUserId", "IsDeleted"
)
SELECT
    su.user_uuid::text,
    su.username,
    UPPER(su.username),
    su.email,
    UPPER(su.email),
    TRUE,
    NULL, -- password hash intentionally null; set via app flow if login is needed
    gen_random_uuid()::text,
    gen_random_uuid()::text,
    NULL,
    FALSE,
    FALSE,
    NULL,
    FALSE,
    0,
    'SeedFirst' || su.idx,
    'SeedLast' || su.idx,
    NULL,
    0, -- Active
    su.user_type,
    NOW(),
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE
FROM seed_users su;

WITH seed_users AS (
    SELECT
        gs AS idx,
        ('00000000-0000-0000-0000-' || LPAD(gs::text, 12, '0'))::uuid AS user_uuid
    FROM generate_series(1, 50) AS gs
)
INSERT INTO auth."AspNetUserRoles" ("UserId", "RoleId")
SELECT
    su.user_uuid::text,
    CASE
        WHEN su.idx BETWEEN 1 AND 30 THEN '1'  -- student
        WHEN su.idx BETWEEN 31 AND 40 THEN '3' -- student_services
        WHEN su.idx BETWEEN 41 AND 45 THEN '8' -- teacher
        WHEN su.idx BETWEEN 46 AND 48 THEN '2' -- club_rep
        ELSE '11' -- admin
    END
FROM seed_users su;

-- ==============================
-- CLUB MANAGEMENT SERVICE
-- at least 10 rows per table
-- ==============================

DELETE FROM club."JobApplications";
DELETE FROM club."JobVacancies";
DELETE FROM club."EventRegistrations";
DELETE FROM club."Subevents";
DELETE FROM club."Events";
DELETE FROM club."Clubs";
DELETE FROM club."Categories";

INSERT INTO club."Categories" (
    "Name", "CreatedAt", "CreatedByUserId", "ModifiedAt", "ModifiedByUserId", "DeletedAt", "DeletedByUserId", "IsDeleted"
)
SELECT
    category_name,
    NOW(),
    ('00000000-0000-0000-0000-' || LPAD(((row_no - 1) % 10 + 1)::text, 12, '0'))::uuid,
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE
FROM (
    VALUES
        (1, 'Technology'),
        (2, 'Arts & Culture'),
        (3, 'Business & Leadership'),
        (4, 'Sports & Wellness'),
        (5, 'Social Impact'),
        (6, 'Science'),
        (7, 'Media & Communications'),
        (8, 'Entrepreneurship'),
        (9, 'Health & Inclusion'),
        (10, 'Sustainability')
) AS src(row_no, category_name);

WITH cat AS (
    SELECT "Id", "Name", ROW_NUMBER() OVER (ORDER BY "Id") AS rn
    FROM club."Categories"
)
INSERT INTO club."Clubs" (
    "Name", "Description", "ApprovedBy", "Status", "CategoryId",
    "CreatedAt", "CreatedByUserId", "ModifiedAt", "ModifiedByUserId", "DeletedAt", "DeletedByUserId", "IsDeleted"
)
SELECT
    'Seed Club ' || rn,
    'Seeded club #' || rn || ' for integration testing.',
    ('00000000-0000-0000-0000-' || LPAD((rn)::text, 12, '0'))::uuid,
    CASE WHEN rn % 4 = 0 THEN 0 WHEN rn % 4 = 1 THEN 1 WHEN rn % 4 = 2 THEN 2 ELSE 3 END,
    c."Id",
    NOW() - ((11 - rn) || ' days')::interval,
    ('00000000-0000-0000-0000-' || LPAD((rn)::text, 12, '0'))::uuid,
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE
FROM cat c
WHERE c.rn <= 10;

INSERT INTO club."Events" (
    "Name", "Description", "StartTime", "EndTime", "Location", "SeatLimit", "Requirements", "Prerequisites", "Notes",
    "ApprovedBy", "Status", "CreatedAt", "CreatedByUserId", "ModifiedAt", "ModifiedByUserId", "DeletedAt", "DeletedByUserId", "IsDeleted"
)
SELECT
    'Seed Event ' || gs,
    'Seeded event #' || gs || ' for ClubManagement testing.',
    NOW() + (gs || ' days')::interval,
    NOW() + (gs || ' days')::interval + interval '2 hours',
    'Campus Hall ' || ((gs - 1) % 5 + 1),
    40 + gs,
    'Student ID',
    'None',
    'Auto-generated seed event',
    ('00000000-0000-0000-0000-' || LPAD(((gs - 1) % 15 + 1)::text, 12, '0'))::uuid,
    CASE WHEN gs % 4 = 0 THEN 0 WHEN gs % 4 = 1 THEN 1 WHEN gs % 4 = 2 THEN 2 ELSE 3 END,
    NOW(),
    ('00000000-0000-0000-0000-' || LPAD(((gs - 1) % 15 + 1)::text, 12, '0'))::uuid,
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE
FROM generate_series(1, 10) AS gs;

INSERT INTO club."EventRegistrations" (
    "EventId", "UserId", "CreatedAt", "CreatedByUserId", "ModifiedAt", "ModifiedByUserId", "DeletedAt", "DeletedByUserId", "IsDeleted"
)
SELECT
    gen_random_uuid(),
    ('00000000-0000-0000-0000-' || LPAD(gs::text, 12, '0'))::uuid,
    NOW(),
    ('00000000-0000-0000-0000-' || LPAD(gs::text, 12, '0'))::uuid,
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE
FROM generate_series(1, 10) AS gs;

WITH club_rows AS (
    SELECT "Id", ROW_NUMBER() OVER (ORDER BY "Id") AS rn
    FROM club."Clubs"
)
INSERT INTO club."JobVacancies" (
    "ClubId", "Title", "Description", "PostedBy",
    "CreatedAt", "CreatedByUserId", "ModifiedAt", "ModifiedByUserId", "DeletedAt", "DeletedByUserId", "IsDeleted"
)
SELECT
    cr."Id",
    'Seed Vacancy ' || cr.rn,
    'Seeded vacancy #' || cr.rn || ' for club #' || cr."Id",
    ('00000000-0000-0000-0000-' || LPAD((cr.rn + 10)::text, 12, '0'))::uuid,
    NOW(),
    ('00000000-0000-0000-0000-' || LPAD((cr.rn + 10)::text, 12, '0'))::uuid,
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE
FROM club_rows cr
WHERE cr.rn <= 10;

WITH vac_rows AS (
    SELECT "Id", ROW_NUMBER() OVER (ORDER BY "Id") AS rn
    FROM club."JobVacancies"
)
INSERT INTO club."JobApplications" (
    "JobVacancyId", "ApplicantId", "ExperienceLinks", "FileUrl", "Status",
    "CreatedAt", "CreatedByUserId", "ModifiedAt", "ModifiedByUserId", "DeletedAt", "DeletedByUserId", "IsDeleted"
)
SELECT
    vr."Id",
    ('00000000-0000-0000-0000-' || LPAD((vr.rn + 20)::text, 12, '0'))::uuid,
    'https://portfolio.local/user' || (vr.rn + 20),
    'https://files.local/cv/seed_user_' || (vr.rn + 20) || '.pdf',
    CASE WHEN vr.rn % 4 = 0 THEN 0 WHEN vr.rn % 4 = 1 THEN 1 WHEN vr.rn % 4 = 2 THEN 2 ELSE 3 END,
    NOW(),
    ('00000000-0000-0000-0000-' || LPAD((vr.rn + 20)::text, 12, '0'))::uuid,
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE
FROM vac_rows vr
WHERE vr.rn <= 10;

WITH event_rows AS (
    SELECT "Id", ROW_NUMBER() OVER (ORDER BY "Id") AS rn
    FROM club."Events"
)
INSERT INTO club."Subevents" (
    "MainEventId", "Name", "StartTime", "EndTime",
    "CreatedAt", "CreatedByUserId", "ModifiedAt", "ModifiedByUserId", "DeletedAt", "DeletedByUserId", "IsDeleted"
)
SELECT
    er."Id",
    'Seed Subevent ' || er.rn,
    NOW() + (er.rn || ' days')::interval + interval '15 minutes',
    NOW() + (er.rn || ' days')::interval + interval '75 minutes',
    NOW(),
    ('00000000-0000-0000-0000-' || LPAD((er.rn + 30)::text, 12, '0'))::uuid,
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE
FROM event_rows er
WHERE er.rn <= 10;

COMMIT;
