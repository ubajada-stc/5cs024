-- Database application user with limited privileges
-- Created to replace root/sa access for the website

USE master
GO
-- Password removed for security
CREATE LOGIN WebAppUser
WITH PASSWORD = 'SET_SECURE_PASSWORD',
DEFAULT_DATABASE = Whistleblowing
GO

USE Whistleblowing
GO

CREATE USER WebAppUser
FOR LOGIN WebAppUser
WITH DEFAULT_SCHEMA = dbo
GO

ALTER ROLE db_datareader ADD MEMBER WebAppUser
GO

ALTER ROLE db_datawriter ADD MEMBER WebAppUser
GO