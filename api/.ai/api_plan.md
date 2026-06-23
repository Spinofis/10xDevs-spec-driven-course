# REST API Plan – VibeTravels (MVP)

This document defines the complete REST API design for the **VibeTravels MVP**, based on the current database schema, PRD decisions, and technology stack. It is authoritative and self‑contained.

---

## 1. Overview

The API enables users to:
- manage trips (notes, dates, parameters, tags),
- define travel preferences,
- asynchronously generate travel plans using AI,
- view, edit, and save generated plans.

Key architectural principles:
- RESTful, stateless HTTP API
- JSON-only payloads (no HTML rendering in backend)
- Asynchronous AI generation with polling
- Strong ownership and authorization boundaries
- Race-condition safe generation workflow

---

## 2. Resources and Data Model Mapping

| Resource | Database Table(s) |
|--------|-------------------|
| User | `app_user` |
| User Profile | `user_profile` |
| Tag | `tag` |
| User Preference Tag | `user_preference_tag` |
| Trip | `trip` |
| Trip Tag | `trip_tag` |
| AI Generation Job | `ai_generation_job` |
| Trip Plan | `trip_plan` |
| Trip Plan Item | `trip_plan_item` |
| Trip Input Snapshot | `trip_input_snapshot` |
| Audit Event (optional) | `audit_event` |

---

## 3. Conventions

- Base URL: `/api/v1`
- Authentication: `Authorization: Bearer <JWT>`
- Content-Type: `application/json`

### Pagination
- `limit` (default 20, max 100)
- `cursor` (opaque)

### Sorting
- `sort=field` or `sort=-field`

### Error Envelope
```json
{
  "error": {
    "code": "string",
    "message": "string",
    "details": {},
    "traceId": "string"
  }
}
```

---

## 4. Authentication

### POST `/auth/register`

```json
{ "email": "user@example.com", "password": "string" }
```

### POST `/auth/login`

```json
{ "email": "user@example.com", "password": "string" }
```

### POST `/auth/logout`

Stateless logout (client deletes token).

---

## 5. Tags

### Tag Model
```json
{
  "id": "uuid",
  "code": "mountains",
  "displayName": "Mountains",
  "createdAt": "timestamp"
}
```

### GET `/tags`

Public list of tags.

---

## 6. User Profile

### GET `/me/profile`

### PUT `/me/profile`

```json
{
  "defaultBudgetLevel": "low|medium|high|null",
  "defaultPeopleCount": 2,
  "defaultPace": "relaxed|normal|fast|null",
  "defaultNotes": "string|null",
  "isDefault": true
}
```

Saving profile emits `audit_event = preferences_saved`.

---

## 7. User Preference Tags

### GET `/me/preference-tags`

### PUT `/me/preference-tags`

```json
{
  "items": [
    { "tagId": "uuid", "order": 1 }
  ]
}
```

---

## 8. Trips

### POST `/trips`

Creates a fully initialized trip (single request, no empty trips).

```json
{
  "title": "Trip to Rome",
  "placeText": "Rome, Italy",
  "noteText": "We love food and history",

  "dateFrom": "2026-05-01",
  "dateTo": "2026-05-07",
  "stayLengthMinDays": 5,
  "stayLengthMaxDays": 7,
  "peopleCount": 2,
  "budgetLevel": "medium",
  "pace": "normal",

  "tags": [
    { "tagId": "uuid", "order": 1 }
  ]
}
```

### GET `/trips`

Supports filtering, pagination, and sorting:
- `hasPlan=true|false`
- `sort=-createdAt|-generatedAt`

### GET `/trips/{tripId}`

### PATCH `/trips/{tripId}`

Partial update of trip parameters.

### DELETE `/trips/{tripId}`

Soft delete.

---

## 9. AI Generation Jobs

Generation is asynchronous and race-condition safe.

### POST `/trips/{tripId}/generation-jobs`

Creates a generation job if requirements are met.

Rules:
- date range required
- stay length required (2–21 days)
- people count required
- at least one of:
  - noteText
  - ≥ 2 tags
  - placeText

```json
{
  "useProfileDefaults": true
}
```

### GET `/generation-jobs/{jobId}`

```json
{
  "id": "uuid",
  "status": "queued|processing|succeeded|failed",
  "discarded": false,
  "discardReason": null
}
```

### Concurrency Rule

When a job finishes:
- if a newer job exists for the same trip → result is **discarded**
- only the newest job may persist a plan

Discarded jobs:
- status = `succeeded`
- `discarded = true`
- `discardReason = newer_job_exists`

---

## 10. Trip Plan (JSON Only)

### Data Model

A plan is structured JSON composed of:
- `trip_plan`
- `trip_plan_item[]`

No HTML is stored or returned by the backend.

### GET `/trips/{tripId}/plan`

```json
{
  "tripId": "uuid",
  "version": 3,
  "status": "generated|saved",
  "generatedFromJobId": "uuid",
  "generatedAt": "timestamp",
  "savedAt": "timestamp|null",
  "summary": "string|null",
  "items": [
    {
      "id": "uuid",
      "dayNumber": 1,
      "order": 10,
      "title": "string",
      "description": "string",
      "locationText": "string|null",
      "startTime": "HH:mm|null",
      "endTime": "HH:mm|null",
      "durationMinutes": 90,
      "costLevel": "low|medium|high|null",
      "tags": ["culture", "walking"]
    }
  ]
}
```

### PUT `/trips/{tripId}/plan`

Replaces entire plan (manual edit).

### POST `/trips/{tripId}/plan/save`

Marks plan as saved.

Conditions:
- no trip inputs changed since last generation

Uses optimistic concurrency:

```
If-Match: "planVersion"
```

Saving emits `audit_event = plan_saved`.

---

## 11. Trip Input Snapshots (Optional Read API)

### GET `/trips/{tripId}/input-snapshots`

Used for debugging and traceability.

---

## 12. Authorization & Security

- JWT authentication
- Owner-based authorization on all user data
- Recommended Postgres RLS enforcement
- Rate limiting:
  - auth endpoints
  - generation endpoints

---

## 13. Summary

This API design ensures:
- clean separation of concerns (no HTML in backend)
- deterministic AI generation behavior
- minimal frontend/backend coupling
- extensibility beyond MVP

This document is the canonical REST API reference for VibeTravels MVP.
