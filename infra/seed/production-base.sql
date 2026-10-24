-- Base data for production: the institution and its campuses (registration needs an active campus).
-- Idempotent: it only adds what is missing, so it can run again after adding a campus here.
-- Run on the server:
--   docker compose -f docker-compose.prod.yml --env-file .env.prod exec -T postgres \
--     psql -v ON_ERROR_STOP=1 -U wizer -d wizer < infra/seed/production-base.sql
BEGIN;

INSERT INTO institutions (id, name, created_at)
SELECT gen_random_uuid(), 'NovaUniversitas', now()
WHERE NOT EXISTS (SELECT 1 FROM institutions WHERE name = 'NovaUniversitas');

INSERT INTO campuses (id, institution_id, name, is_active, created_at, updated_at)
SELECT gen_random_uuid(), i.id, c.name, true, now(), now()
FROM institutions i
CROSS JOIN (VALUES ('San Jacinto'), ('Nopala'), ('Juxtlahuaca')) AS c(name)
WHERE i.name = 'NovaUniversitas'
ON CONFLICT (institution_id, name) DO NOTHING;

COMMIT;
