CREATE TABLE HandFileMetadata
(
    FileId VARCHAR(36) NOT NULL,
    FileName VARCHAR(255) NOT NULL,
    Status VARCHAR(64) NOT NULL,
    CreatedAt DATETIME(6) NOT NULL,

    PRIMARY KEY (FileId)
);