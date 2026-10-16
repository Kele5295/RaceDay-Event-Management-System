\# Part 2 Testing



\## Automated Tests



The xUnit suite contains 20 test cases covering authentication,

password hashing, sessions, event validation, role restrictions,

resource ownership, and enrolment viewing and cancellation.



Run from the repository root:



dotnet test Part2/RaceDay.API/RaceDay.API.slnx



The suite passed locally and in GitHub Actions.



Tests use isolated EF Core InMemory databases. They do not verify

SQL Server constraints, generated defaults or transaction behaviour.

Enrolment records used in automated cancellation tests are seeded

directly rather than created through the enrolment endpoint.



\## Manual Swagger Checks



The following checks were completed against the local SQL Server

database on 16 October 2026.



| Check | Observed result |

| --- | --- |

| Register organiser | 201 Created |

| Log in as organiser | 200 OK |

| View profile using login session | 200 OK |

| Create event after decimal validation fix | 201 Created |

| Create event category | 201 Created |

| Register participant | 201 Created |

| Log in as participant | 200 OK |

| List event categories | 200 OK |

| Enrol participant in event category | 201 Created |

| View participant's own enrolments | 200 OK, Confirmed status |

| Submit duplicate enrolment | 409 Conflict |

| Create event as participant | 403 Forbidden |

| Open Swagger after documentation update | Loaded successfully |



\## Issues Found



Distance validation initially returned 500 because decimal range

limits were parsed using regional settings. The Range attribute

was updated to use invariant culture, and event creation then

returned 201 Created.



The initial category request used maxParticipants instead of

maximumParticipants. The returned capacity was null. The README

now documents the correct request property. The existing test

category's capacity was not subsequently corrected during these checks.



\## Coverage Limits



Manual checks did not exercise every endpoint. Result capture,

result updates and concurrent category capacity handling were not

verified during this session.

