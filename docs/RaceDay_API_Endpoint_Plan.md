# RaceDay API Endpoint Plan

**Module:** PROG6212 Programming 2B  
**Part:** PoE Part 1 - System Planning and Database  
**System:** RaceDay Event Management System

This endpoint plan defines the RESTful API structure to be implemented in Part 2. It covers Authentication, User Profile, Events, Categories, Event Enrolments, and Results.

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | `/api/auth/register` | Registers a new RaceDay user account as an Organiser or Participant. | None | `{ firstName, lastName, email, password, role, phoneNumber }` | `201 Created` - user account created; `400 Bad Request` - invalid data; `409 Conflict` - email already registered |
| POST | `/api/auth/login` | Authenticates a registered user and returns authentication details for future protected requests. | None | `{ email, password }` | `200 OK` - login successful and token/user details returned; `400 Bad Request` - missing fields; `401 Unauthorized` - invalid credentials |
| GET | `/api/profile` | Returns the profile details of the currently authenticated user. | Any | None | `200 OK` - user profile returned; `401 Unauthorized` - user is not authenticated |
| PUT | `/api/profile` | Updates the currently authenticated user's profile details. | Any | `{ firstName, lastName, phoneNumber }` | `200 OK` - profile updated; `400 Bad Request` - invalid data; `401 Unauthorized` - user is not authenticated |
| GET | `/api/events` | Returns all RaceDay events available in the system. | None | None | `200 OK` - list of events returned |
| GET | `/api/events/{id}` | Returns the details of a specific RaceDay event using the supplied event ID. | None | None | `200 OK` - event returned; `404 Not Found` - event does not exist |
| POST | `/api/events` | Creates a new RaceDay event for the authenticated Organiser. | Organiser | `{ eventName, description, eventDate, location, distance, eventTypeID }` | `201 Created` - event created; `400 Bad Request` - invalid event data; `401 Unauthorized` - not authenticated; `403 Forbidden` - user is not an Organiser |
| PUT | `/api/events/{id}` | Updates an existing RaceDay event owned by the authenticated Organiser. | Organiser | `{ eventName, description, eventDate, location, distance, eventTypeID }` | `200 OK` - event updated; `400 Bad Request` - invalid data; `403 Forbidden` - not allowed to manage event; `404 Not Found` - event does not exist |
| DELETE | `/api/events/{id}` | Deletes an existing RaceDay event owned by the authenticated Organiser. | Organiser | None | `204 No Content` - event deleted; `403 Forbidden` - not allowed to manage event; `404 Not Found` - event does not exist |
| GET | `/api/eventtypes` | Returns the available RaceDay event types such as running, walking and cycling. | None | None | `200 OK` - event types returned |
| GET | `/api/events/{eventId}/categories` | Returns all categories configured for a specific RaceDay event. | None | None | `200 OK` - categories returned; `404 Not Found` - event does not exist |
| POST | `/api/events/{eventId}/categories` | Creates a new category for an event managed by the authenticated Organiser. | Organiser | `{ categoryName, description, maximumParticipants }` | `201 Created` - category created; `400 Bad Request` - invalid data; `403 Forbidden` - not allowed to manage event; `404 Not Found` - event does not exist |
| PUT | `/api/categories/{id}` | Updates an existing event category. | Organiser | `{ categoryName, description, maximumParticipants }` | `200 OK` - category updated; `400 Bad Request` - invalid data; `403 Forbidden` - not allowed to manage category; `404 Not Found` - category does not exist |
| DELETE | `/api/categories/{id}` | Deletes an existing event category when it can be safely removed. | Organiser | None | `204 No Content` - category deleted; `403 Forbidden` - not allowed to manage category; `404 Not Found` - category does not exist; `409 Conflict` - category is already in use by enrolments |
| POST | `/api/enrolments` | Enrols the authenticated Participant in an Event and records the selected Category. | Participant | `{ eventID, categoryID }` | `201 Created` - enrolment created; `400 Bad Request` - invalid event/category data; `403 Forbidden` - user is not a Participant; `404 Not Found` - event or category does not exist; `409 Conflict` - participant already enrolled |
| GET | `/api/enrolments/me` | Returns all event enrolments belonging to the authenticated Participant. | Participant | None | `200 OK` - participant enrolments returned; `401 Unauthorized` - not authenticated; `403 Forbidden` - user is not a Participant |
| GET | `/api/events/{eventId}/enrolments` | Returns the enrolments for an event managed by the authenticated Organiser. | Organiser | None | `200 OK` - event enrolments returned; `403 Forbidden` - not allowed to view enrolments; `404 Not Found` - event does not exist |
| DELETE | `/api/enrolments/{id}` | Cancels an enrolment belonging to the authenticated Participant. | Participant | None | `204 No Content` - enrolment cancelled; `403 Forbidden` - enrolment does not belong to participant; `404 Not Found` - enrolment does not exist |
| POST | `/api/enrolments/{enrolmentId}/result` | Captures the finish time and finishing position for a Participant's enrolment. | Organiser | `{ finishTime, finishingPosition }` | `201 Created` - result recorded; `400 Bad Request` - invalid result data; `403 Forbidden` - not allowed to manage this event; `404 Not Found` - enrolment does not exist; `409 Conflict` - result already exists |
| PUT | `/api/results/{id}` | Updates an existing participant result captured for an event. | Organiser | `{ finishTime, finishingPosition }` | `200 OK` - result updated; `400 Bad Request` - invalid data; `403 Forbidden` - not allowed to manage result; `404 Not Found` - result does not exist |
| GET | `/api/results/me` | Returns all race results belonging to the authenticated Participant for performance history. | Participant | None | `200 OK` - participant results returned; `401 Unauthorized` - not authenticated; `403 Forbidden` - user is not a Participant |
| GET | `/api/events/{eventId}/results` | Returns all recorded results for a specific event managed by the authenticated Organiser. | Organiser | None | `200 OK` - event results returned; `403 Forbidden` - not allowed to view event results; `404 Not Found` - event does not exist |

## Role Summary

- **None** - Public endpoint; no login is required.
- **Any** - Any authenticated RaceDay user.
- **Organiser** - Only authenticated users with the Organiser role.
- **Participant** - Only authenticated users with the Participant role.

## Planning Notes

- Organisers can create, update and delete events and manage event categories.
- Participants can browse events, enrol in an event, select a category and view their own enrolments.
- Organisers can view enrolments for events they manage and capture participant results.
- Participants can view their own results and performance history.
- The endpoint plan is designed to align with the RaceDay ERD and Part 1 SQL database design.
