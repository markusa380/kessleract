CREATE TABLE vessel_votes (
    ip TEXT NOT NULL,
    vessel_hash INTEGER NOT NULL,
    body INTEGER NOT NULL,
    vote INTEGER NOT NULL,
    PRIMARY KEY (ip, vessel_hash, body)
);