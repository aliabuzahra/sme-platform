-- UC-008: branches. Each tenant can run several locations, each with its own opening hours.

CREATE TABLE identity.branches (
    id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   uuid        NOT NULL REFERENCES identity.tenants (id),
    name        text        NOT NULL CHECK (length(btrim(name)) BETWEEN 1 AND 120),
    -- {"mon":[{"open":"09:00","close":"18:00"}], ...}; a missing day means closed. Validated by the service.
    hours       jsonb       NOT NULL DEFAULT '{}'::jsonb,
    created_at  timestamptz NOT NULL DEFAULT now(),
    UNIQUE (tenant_id, name)
);

CREATE INDEX branches_tenant_id_idx ON identity.branches (tenant_id);

GRANT SELECT, INSERT, UPDATE, DELETE ON identity.branches TO sme_app;

-- Same rule as every tenant-owned table (ADR-0007): a session sees and writes only its tenant's rows.
ALTER TABLE identity.branches ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON identity.branches
    USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid)
    WITH CHECK (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
