CREATE TABLE HandHistory
(
    HandId VARCHAR(128) NOT NULL,
    HeroPlayerId VARCHAR(255) NOT NULL,
    HeroHoleCardsJson JSON NOT NULL,
    Timestamp DATETIME(6) NOT NULL,

    PRIMARY KEY (HandId)
);

CREATE TABLE HandHistoryPlayer
(
    HandId VARCHAR(128) NOT NULL,
    PlayerId VARCHAR(255) NOT NULL,
    Position INT NOT NULL,
    StartingStackBB DECIMAL(18, 6) NOT NULL,

    PRIMARY KEY (HandId, PlayerId),
    CONSTRAINT FK_HandHistoryPlayer_HandHistory
        FOREIGN KEY (HandId) REFERENCES HandHistory (HandId)
        ON DELETE CASCADE
);

CREATE TABLE HandHistoryEvent
(
    HandId VARCHAR(128) NOT NULL,
    Sequence INT NOT NULL,
    EventType VARCHAR(64) NOT NULL,
    PayloadJson JSON NOT NULL,

    PRIMARY KEY (HandId, Sequence),
    CONSTRAINT FK_HandHistoryEvent_HandHistory
        FOREIGN KEY (HandId) REFERENCES HandHistory (HandId)
        ON DELETE CASCADE
);
