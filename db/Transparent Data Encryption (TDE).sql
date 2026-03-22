-- Transparent Data Encryption (TDE)
-- Secures data at rest using AES-256 encryption

-- Create Master Key
USE master;
GO
-- Password removed for security
CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'SET_SECURE_PASSWORD';
GO

-- Create Certificate
CREATE CERTIFICATE TDE_Cert
WITH SUBJECT = 'TDE Certificate';
GO

-- Switch to Database
USE Whistleblowing;
GO

-- Create Database Encryption Key
CREATE DATABASE ENCRYPTION KEY
WITH ALGORITHM = AES_256
ENCRYPTION BY SERVER CERTIFICATE TDE_Cert;
GO

-- Enable Encryption
ALTER DATABASE Whistleblowing
SET ENCRYPTION ON;
GO

-- Verify Encryption
SELECT 
    db_name(database_id) AS DatabaseName,
    encryption_state
FROM sys.dm_database_encryption_keys;
GO
-- encryption_state = 3 means encryption is active

-- Backup Certificate
USE master;
GO
BACKUP CERTIFICATE TDE_Cert
TO FILE = 'C:\Users\User\Desktop\TDE_Cert.cer'
WITH PRIVATE KEY (
    FILE = 'C:\Users\User\Desktop\TDE_Cert_Key.pvk',
-- Password removed for security
    ENCRYPTION BY PASSWORD = 'SET_SECURE_PASSWORD'
);
GO

-- End of TDE Implementation Script