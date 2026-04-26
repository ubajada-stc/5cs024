-- Migration 008: Widen Reports.Status check constraint to allow ReferredToCourt (3) and Closed (4)

ALTER TABLE dbo.Reports DROP CONSTRAINT CK_Reports_Status;

ALTER TABLE dbo.Reports
    ADD CONSTRAINT CK_Reports_Status CHECK (Status BETWEEN 0 AND 4);

UPDATE dbo.Reports SET Status = 4 WHERE Status = 3;
