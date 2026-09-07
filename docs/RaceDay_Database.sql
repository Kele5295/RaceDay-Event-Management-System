/*
    RaceDay Event Management System
    PROG6212 - Programming 2B
    PoE Part 1 - SQL Database Script
*/

IF DB_ID('RaceDayDB') IS NULL
BEGIN
    CREATE DATABASE RaceDayDB;
END;
GO

USE RaceDayDB;
GO

IF OBJECT_ID('dbo.Result', 'U') IS NOT NULL DROP TABLE dbo.Result;
IF OBJECT_ID('dbo.Enrolment', 'U') IS NOT NULL DROP TABLE dbo.Enrolment;
IF OBJECT_ID('dbo.Category', 'U') IS NOT NULL DROP TABLE dbo.Category;
IF OBJECT_ID('dbo.Event', 'U') IS NOT NULL DROP TABLE dbo.Event;
IF OBJECT_ID('dbo.EventType', 'U') IS NOT NULL DROP TABLE dbo.EventType;
IF OBJECT_ID('dbo.[User]', 'U') IS NOT NULL DROP TABLE dbo.[User];
GO

CREATE TABLE dbo.[User]
(
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FirstName NVARCHAR(50) NOT NULL,
    LastName NVARCHAR(50) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    PhoneNumber NVARCHAR(20) NULL,
    CONSTRAINT CK_User_Role CHECK (Role IN ('Organiser', 'Participant'))
);
GO

CREATE TABLE dbo.EventType
(
    EventTypeID INT IDENTITY(1,1) PRIMARY KEY,
    TypeName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL
);
GO

CREATE TABLE dbo.Event
(
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    EventTypeID INT NOT NULL,
    EventName NVARCHAR(120) NOT NULL,
    Description NVARCHAR(500) NULL,
    EventDate DATE NOT NULL,
    Location NVARCHAR(150) NOT NULL,
    Distance DECIMAL(6,2) NOT NULL,
    CONSTRAINT CK_Event_Distance CHECK (Distance > 0),
    CONSTRAINT FK_Event_User FOREIGN KEY (OrganiserID) REFERENCES dbo.[User](UserID),
    CONSTRAINT FK_Event_EventType FOREIGN KEY (EventTypeID) REFERENCES dbo.EventType(EventTypeID)
);
GO

CREATE TABLE dbo.Category
(
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255) NULL,
    MaximumParticipants INT NULL,
    CONSTRAINT CK_Category_MaximumParticipants CHECK (MaximumParticipants IS NULL OR MaximumParticipants > 0),
    CONSTRAINT UQ_Category_Event_CategoryName UNIQUE (EventID, CategoryName),
    CONSTRAINT FK_Category_Event FOREIGN KEY (EventID) REFERENCES dbo.Event(EventID)
);
GO

CREATE TABLE dbo.Enrolment
(
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    ParticipantID INT NOT NULL,
    EventID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATETIME2 NOT NULL CONSTRAINT DF_Enrolment_EnrolmentDate DEFAULT SYSDATETIME(),
    Status NVARCHAR(30) NOT NULL CONSTRAINT DF_Enrolment_Status DEFAULT 'Confirmed',
    CONSTRAINT CK_Enrolment_Status CHECK (Status IN ('Pending', 'Confirmed', 'Cancelled')),
    CONSTRAINT UQ_Enrolment_Participant_Event UNIQUE (ParticipantID, EventID),
    CONSTRAINT FK_Enrolment_User FOREIGN KEY (ParticipantID) REFERENCES dbo.[User](UserID),
    CONSTRAINT FK_Enrolment_Event FOREIGN KEY (EventID) REFERENCES dbo.Event(EventID),
    CONSTRAINT FK_Enrolment_Category FOREIGN KEY (CategoryID) REFERENCES dbo.Category(CategoryID)
);
GO

CREATE TABLE dbo.Result
(
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE,
    FinishTime TIME NULL,
    FinishingPosition INT NULL,
    RecordedDate DATETIME2 NOT NULL CONSTRAINT DF_Result_RecordedDate DEFAULT SYSDATETIME(),
    CONSTRAINT CK_Result_FinishingPosition CHECK (FinishingPosition IS NULL OR FinishingPosition > 0),
    CONSTRAINT FK_Result_Enrolment FOREIGN KEY (EnrolmentID) REFERENCES dbo.Enrolment(EnrolmentID)
);
GO

INSERT INTO dbo.[User] (FirstName, LastName, Email, PasswordHash, Role, PhoneNumber)
VALUES
('Thabo', 'Mokoena', 'thabo.mokoena@raceday.co.za', 'HASHED_PASSWORD_THABO_001', 'Organiser', '0825550101'),
('Naledi', 'Khumalo', 'naledi.khumalo@raceday.co.za', 'HASHED_PASSWORD_NALEDI_002', 'Organiser', '0835550102'),
('Kagiso', 'Molefe', 'kagiso.molefe@example.co.za', 'HASHED_PASSWORD_KAGISO_003', 'Participant', '0715550103'),
('Lerato', 'Dlamini', 'lerato.dlamini@example.co.za', 'HASHED_PASSWORD_LERATO_004', 'Participant', '0725550104');
GO

INSERT INTO dbo.EventType (TypeName, Description)
VALUES
('Road Running', 'Road-based running events over different distances.'),
('Walking', 'Organised walking events for recreational and competitive participants.'),
('Cycling', 'Road cycling events for individual participants.');
GO

INSERT INTO dbo.Event (OrganiserID, EventTypeID, EventName, Description, EventDate, Location, Distance)
VALUES
(1, 1, 'Pretoria Spring 10K', 'A community road running event through central Pretoria.', '2026-10-10', 'Pretoria, Gauteng', 10.00),
(2, 2, 'Centurion Wellness Walk', 'A recreational walking event promoting active lifestyles.', '2026-10-24', 'Centurion, Gauteng', 5.00),
(1, 3, 'Tshwane Cycle Challenge', 'A road cycling challenge for riders of different experience levels.', '2026-11-14', 'Tshwane, Gauteng', 40.00);
GO

INSERT INTO dbo.Category (EventID, CategoryName, Description, MaximumParticipants)
VALUES
(1, 'Open 10K', 'Open category for the Pretoria Spring 10K.', 500),
(1, 'Junior 10K', 'Junior category for eligible younger participants.', 150),
(2, 'Open Walk', 'General category for the Centurion Wellness Walk.', 400),
(2, 'Veteran Walk', 'Walking category for veteran participants.', 150),
(3, 'Open 40K', 'Open category for the Tshwane Cycle Challenge.', 300),
(3, 'Veteran 40K', 'Veteran category for the Tshwane Cycle Challenge.', 120);
GO

INSERT INTO dbo.Enrolment (ParticipantID, EventID, CategoryID, EnrolmentDate, Status)
VALUES
(3, 1, 1, '2026-09-15T09:30:00', 'Confirmed'),
(4, 1, 1, '2026-09-16T11:00:00', 'Confirmed'),
(3, 2, 3, '2026-09-18T14:20:00', 'Confirmed'),
(4, 3, 5, '2026-09-20T10:15:00', 'Confirmed');
GO

INSERT INTO dbo.Result (EnrolmentID, FinishTime, FinishingPosition, RecordedDate)
VALUES
(1, '00:48:32', 12, '2026-10-10T12:00:00'),
(2, '00:52:15', 18, '2026-10-10T12:05:00');
GO

SELECT * FROM dbo.[User];
SELECT * FROM dbo.EventType;
SELECT * FROM dbo.Event;
SELECT * FROM dbo.Category;
SELECT * FROM dbo.Enrolment;
SELECT * FROM dbo.Result;
GO
