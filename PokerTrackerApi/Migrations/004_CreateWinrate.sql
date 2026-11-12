CREATE TABLE Winrate
(
    HandId VARCHAR(128) NOT NULL,
    NetWonBB DECIMAL(18, 6) NOT NULL,
    WonWithShowdownBB DECIMAL(18, 6) NOT NULL,
    WonWithoutShowdownBB DECIMAL(18, 6) NOT NULL,
    NetWonBeforeRakeBB DECIMAL(18, 6) NOT NULL,
    WonWithShowdownBeforeRakeBB DECIMAL(18, 6) NOT NULL,
    WonWithoutShowdownBeforeRakeBB DECIMAL(18, 6) NOT NULL,

    PRIMARY KEY (HandId)
);
