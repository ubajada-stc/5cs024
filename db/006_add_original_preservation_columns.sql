ALTER TABLE ReportAttachments
    ADD OriginalStoragePath NVARCHAR(500) NULL,
        OriginalPlaintextHash VARBINARY(32) NULL;
