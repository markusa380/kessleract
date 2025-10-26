CREATE TABLE vessel (
    body_id INTEGER NOT NULL,
    vessel_hash INTEGER NOT NULL,
    vessel_spec BYTEA NOT NULL,
    PRIMARY KEY (body_id, vessel_hash)
);