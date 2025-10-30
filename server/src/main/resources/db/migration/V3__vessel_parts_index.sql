ALTER TABLE vessel ADD COLUMN parts TEXT[];
CREATE INDEX idx_vessel_parts_gin ON vessel USING GIN (parts);