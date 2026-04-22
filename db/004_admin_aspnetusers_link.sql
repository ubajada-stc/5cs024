-- Migration 004: Link Admin entity to AspNetUsers
-- Run BEFORE deploying the new code.

-- 1. Add AdminId column to AspNetUsers
ALTER TABLE AspNetUsers ADD AdminId UNIQUEIDENTIFIER NULL;

-- 2. Add FK constraint
ALTER TABLE AspNetUsers
    ADD CONSTRAINT FK_AspNetUsers_Admins
    FOREIGN KEY (AdminId) REFERENCES Admins(AdminId)
    ON DELETE NO ACTION;

-- 3. Seed a default admin row (password will be set via POST /api/auth/dev/link-admin)
INSERT INTO Admins (AdminId, Email, PasswordHash, MFASecret, MFAEnabled, IsActive, CreatedAt, UpdatedAt)
VALUES (NEWID(), 'admin@test.com', '', '', 0, 1, GETUTCDATE(), GETUTCDATE());
