-- Migration 005: Seed default platform settings
-- Requires an Admin row to exist (run 004 first).
-- Uses the first active admin as UpdatedBy.

DECLARE @AdminId UNIQUEIDENTIFIER = (SELECT TOP 1 AdminId FROM Admins WHERE IsActive = 1);

INSERT INTO PlatformSettings (SettingKey, SettingValue, Description, UpdatedAt, UpdatedBy)
VALUES
    ('RetentionDays',                '365',  'Days to retain closed cases before permanent deletion', GETUTCDATE(), @AdminId),
    ('AcknowledgementDeadlineDays',  '7',    'EU Directive: days to acknowledge a new report',        GETUTCDATE(), @AdminId),
    ('FeedbackDeadlineDays',         '90',   'EU Directive: days to provide feedback to whistleblower', GETUTCDATE(), @AdminId),
    ('MaxConcurrentSanitizations',   '2',    'Maximum files sanitized in parallel by background service', GETUTCDATE(), @AdminId);
