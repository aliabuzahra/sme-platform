-- UC-007: users inside a tenant, one-time codes for phone sign-in, and the modules a tenant may use.

-- Module entitlements end up in the sign-in token. Billing owns them later; 'core' is the default.
ALTER TABLE identity.tenants ADD COLUMN modules text[] NOT NULL DEFAULT ARRAY['core'];

CREATE TABLE identity.users (
    id          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   uuid        NOT NULL REFERENCES identity.tenants (id),
    phone       text        NOT NULL CHECK (phone ~ '^\+[1-9][0-9]{7,14}$'),
    name        text        NOT NULL DEFAULT '' CHECK (length(name) <= 120),
    role        text        NOT NULL DEFAULT 'staff' CHECK (role IN ('owner', 'staff')),
    created_at  timestamptz NOT NULL DEFAULT now(),
    UNIQUE (tenant_id, phone)
);

CREATE INDEX users_phone_idx ON identity.users (phone);

GRANT SELECT, INSERT, UPDATE, DELETE ON identity.users TO sme_app;

ALTER TABLE identity.users ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation ON identity.users
    USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid)
    WITH CHECK (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);

-- One-time codes are platform data (sign-in happens before a tenant is known), so no policy and
-- no grant to sme_app: only the platform role touches this table. Codes are stored hashed.
CREATE TABLE identity.otp_codes (
    id           uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    phone        text        NOT NULL,
    code_hash    text        NOT NULL,
    expires_at   timestamptz NOT NULL,
    attempts     int         NOT NULL DEFAULT 0,
    consumed_at  timestamptz,
    created_at   timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX otp_codes_phone_idx ON identity.otp_codes (phone, created_at DESC);
