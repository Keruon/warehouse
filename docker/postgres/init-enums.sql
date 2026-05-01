DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'zone_type_enum') THEN
        CREATE TYPE zone_type_enum AS ENUM ('Storage', 'Production', 'Shipping', 'Returns', 'Maintenance');
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'component_type_enum') THEN
        CREATE TYPE component_type_enum AS ENUM ('SMD', 'ThroughHole', 'QFP', 'SOIC', 'DIP', 'Other');
    END IF;
END $$;
