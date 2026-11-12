-- UC-006: the tenants table and the row-level security that isolates tenants (ADR-0007).
-- The Identity service owns the "identity" schema (ADR-0008, proposed).

-- Application role: no login, no BYPASSRLS, not the table owner, so policies apply to it.
-- The service connects as the owner and switches with SET ROLE for tenant work.
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'sme_app') THEN
        CREATE ROLE sme_app NOLOGIN NOBYPASSRLS;
    END IF;
END
$$;

GRANT sme_app TO CURRENT_USER;
GRANT USAGE ON SCHEMA identity TO sme_app;

CREATE TABLE identity.tenants (
    id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    name        text        NOT NULL CHECK (length(btrim(name)) BETWEEN 1 AND 120),
    country     char(2)     NOT NULL DEFAULT 'JO',
    status      text        NOT NULL DEFAULT 'active' CHECK (status IN ('active', 'suspended')),
    created_at  timestamptz NOT NULL DEFAULT now()
);

GRANT SELECT, INSERT, UPDATE ON identity.tenants TO sme_app;

-- A session sees only the tenant named by app.tenant_id. Unset means nothing is visible.
ALTER TABLE identity.tenants ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON identity.tenants
    USING (id = NULLIF(current_setting('app.tenant_id', true), '')::uuid)
    WITH CHECK (id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
