# RaceDay Event Management System

## Project Description

RaceDay is a web-based event management system designed for South African road running, walking and cycling events. The system allows organisers to create and manage events, categories, enrolments and participant results. Participants can register, browse available events, enrol in an event category and view their results and performance history.

This repository contains the planning and database components for Part 1 of the PROG6212 Programming 2B Portfolio of Evidence.

## User Roles

### Organiser

An Organiser can:
- Create, update and delete events.
- Create and manage event categories.
- View participant enrolments for managed events.
- Capture and update participant results.
- View information relating to managed events.

### Participant

A Participant can:
- Register and log in to the system.
- Browse available events.
- Enrol in an event and select a category.
- View their own event enrolments.
- View their own results and performance history.

## Part 1 Deliverables

The Part 1 planning and database documentation is stored in the `docs` folder.

- **Entity Relationship Diagram:** `docs/RaceDay_ERD.pdf`
- **RESTful API Endpoint Plan:** `docs/RaceDay_API_Endpoint_Plan.md`
- **SQL Server Database Script:** `docs/RaceDay_Database.sql`

## Database Design

The RaceDay database contains six main entities:

1. User
2. EventType
3. Event
4. Category
5. Enrolment
6. Result

The `User` entity supports both Organiser and Participant roles. An Organiser can create multiple events, while Participants can enrol in events. The `Enrolment` entity resolves the relationship between Participants and Events and records the selected event category. An enrolment may later produce a Result.

## Database Setup

The database was developed and tested using Microsoft SQL Server and SQL Server Management Studio (SSMS).

To create the database:

1. Open SQL Server Management Studio.
2. Connect to a SQL Server instance.
3. Open `docs/RaceDay_Database.sql`.
4. Execute the complete SQL script.
5. The script creates the `RaceDayDB` database and its tables, constraints and sample data.
6. Verification queries at the end of the script display the inserted records.

The SQL script has been tested successfully using SQL Server Express.

## RESTful API Planning

The API endpoint plan defines the planned RESTful services for:

- Authentication
- User Profile
- Events
- Event Types
- Categories
- Event Enrolments
- Results

Each endpoint specifies the HTTP method, route, description, required role, request body and expected HTTP responses.

## Continuous Integration

GitHub Actions is used to validate the Part 1 repository structure.

The workflow checks that the following required files exist and are not empty:

- `docs/RaceDay_ERD.pdf`
- `docs/RaceDay_API_Endpoint_Plan.md`
- `docs/RaceDay_Database.sql`
- Root `README.md`

The workflow configuration is stored at:

`.github/workflows/part1-ci.yml`

### Successful CI Build

![Successful GitHub Actions Build](docs/ci-success.png)

## Part 1 Video Walkthrough

An unlisted YouTube video demonstrating the Part 1 planning and database implementation will be available here:

**Video:** [YouTube Walkthrough - LINK TO BE ADDED]

## Repository Structure

```text
RaceDay-Event-Management-System/
├── .github/
│   └── workflows/
│       └── part1-ci.yml
├── docs/
│   ├── README.md
│   ├── RaceDay_ERD.pdf
│   ├── RaceDay_API_Endpoint_Plan.md
│   ├── RaceDay_Database.sql
│   └── ci-success.png
└── README.md
```

## Technologies Used

- Microsoft SQL Server
- SQL Server Management Studio (SSMS)
- Git
- GitHub
- GitHub Actions
- RESTful API planning

## Module Information

**Module:** PROG6212 - Programming 2B  
**Assessment:** Portfolio of Evidence - Part 1  
**System:** RaceDay Event Management System
