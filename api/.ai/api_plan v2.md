# REST API Plan (VibeTravels MVP)

> **Note:** This API returns **JSON only**. The backend **does not render or store HTML** for plans.  
> Trip plans are stored as `trip_plan` + `trip_plan_item` and returned as structured JSON.

---

## 1. Resources

| Resource | DB Table(s) |
|---|---|
| Auth | `app_user` |
| Me Profile (includes preference tags) | `user_profile`, `user_preference_tag`, `tag` |
| Tags | `tag` |
| Trips | `trip` |
| Trip Tags | `trip_tag`, `tag` |
| AI Generation Jobs | `ai_generation_job` |
| Trip Input Snapshots | `trip_input_snapshot` |
| Trip Plan | `trip_plan` |
| Trip Plan Items | `trip_plan_item` |
| Audit Events (optional) | `audit_event` |

---

## 2. Global Conventions

### 2.1 Base URL
`/api/v1`

### 2.2 Auth
All endpoints (except `POST /auth/*` and optionally `GET /tags`) require:
`Authorization: Bearer <JWT>`

### 2.3 Standard headers
- `Content-Type: application/json`
- `Idempotency-Key: <uuid>` (recommended for job creation endpoints)

### 2.4 Pagination (list endpoints)
- Query params:
  - `limit` (default 20, max 100)
  - `cursor` (opaque string)
- Response:
```json
{
  "items": [],
  "nextCursor": "string|null"
}
```

### 2.5 Sorting
- Query param: `sort`
- Examples:
  - `sort=createdAt`
  - `sort=-createdAt` (descending)

### 2.6 Error envelope (all 4xx/5xx)
```json
{
  "error": {
    "code": "string",
    "message": "string",
    "details": { "any": "json" },
    "traceId": "string"
  }
}
```

### 2.7 Timestamps & time fields
- Timestamps are ISO-8601 strings (UTC recommended).
- Time-of-day in plan items uses `"HH:mm"` strings.
- Dates use `"YYYY-MM-DD"` strings.

---

## 3. Authentication & Authorization

### 3.1 POST `/auth/register`
Create an account.

**Request**
```json
{
  "email": "user@example.com",
  "password": "string"
}
```

**Response 201**
```json
{
}
```

**Errors**
- `400 VALIDATION_ERROR`
- `409 EMAIL_TAKEN`

---

### 3.2 POST `/auth/login`
Obtain JWT.

**Request**
```json
{
  "email": "user@example.com",
  "password": "string"
}
```

**Response 200**
```json
{
  "accessToken": "jwt",
  "expiresIn": 3600
}
```

**Errors**
- `401 INVALID_CREDENTIALS`
- `403 USER_INACTIVE`

---

### 3.3 POST `/auth/logout`
Stateless logout (client deletes token).

**Response 204**

---

## 4. Tags

> Tag uses `code` (not `slug`).

### 4.1 Tag DTO
```json
{
  "id": "uuid",
  "code": "museums",
  "displayName": "Museums",
  "createdAt": "timestamp"
}
```

### 4.2 GET `/tags`
List tags (currently public / anonymous access).

No query parameters are supported. Results are always returned sorted ascending by `code` (then by `id`). No pagination.

**Response 200**
```json
{
  "items": [
    { "id": "uuid", "code": "museums", "displayName": "Museums", "createdAt": "timestamp" }
  ]
}
```

**Errors**
- None (endpoint takes no input; no validation performed)

---

## 5. Me Profile (Profile + Preference Tags together)

> Preference tags are returned **inside** `/me/profile`. No separate `/me/preference-tags` endpoint.

### 5.1 GET `/me/profile`
Returns user profile plus preference tags.

Returns `200` with the same response shape even when the user has not saved a profile yet. In that case the response contains default/null profile values and an empty `preferenceTags` array.

**Response 200**
```json
{
  "userId": "uuid",
  "profile": {
    "defaultBudgetLevel": "low|medium|high|null",
    "defaultPeopleCount": 2,
    "defaultPace": "relaxed|normal|fast|null",
    "defaultNotes": "string|null",
    "isDefault": true,
    "createdAt": "timestamp",
    "updatedAt": "timestamp"
  },
  "preferenceTags": [
    {
      "tag": { "id": "uuid", "code": "mountains", "displayName": "Mountains", "createdAt": "timestamp" },
      "order": 1,
      "createdAt": "timestamp"
    }
  ]
}
```

---

### 5.2 PUT `/me/profile`
Upserts profile and replaces full preference tag set in one call.

**Request**
```json
{
  "profile": {
    "defaultBudgetLevel": "low|medium|high|null",
    "defaultPeopleCount": 2,
    "defaultPace": "relaxed|normal|fast|null",
    "defaultNotes": "string|null",
    "isDefault": true
  },
  "preferenceTags": [
    { "tagId": "uuid", "order": 1 }
  ]
}
```

**Response 204**
No response body.

After a successful save, the frontend should keep the submitted state locally or call `GET /me/profile` if it needs the canonical server representation.

**Errors**
- `400 VALIDATION_ERROR`
  - `profile` required
  - `preferenceTags` required; send `[]` to clear all preference tags
  - `defaultPeopleCount` > 0 when provided
  - `preferenceTags[].tagId` required
  - `preferenceTags[].order` >= 0
  - duplicate `preferenceTags[].tagId` values are rejected
- `404 TAG_NOT_FOUND`

**Side effects**
- Emit `audit_event` with `event_type = preferences_saved` (optional but recommended).

---

## 6. Trips

### 6.1 Trip DTO (summary)
```json
{
  "id": "uuid",
  "userId": "uuid",
  "title": "string",
  "placeText": "string|null",
  "noteText": "string|null",

  "dateFrom": "YYYY-MM-DD|null",
  "dateTo": "YYYY-MM-DD|null",
  "stayLengthMinDays": 2,
  "stayLengthMaxDays": 7,
  "peopleCount": 2,
  "budgetLevel": "low|medium|high|null",
  "pace": "relaxed|normal|fast|null",

  "generatedAt": "timestamp|null",
  "hasGeneratedPlan": false,

  "createdAt": "timestamp",
  "updatedAt": "timestamp"

}
```

Nullable fields in this DTO: `placeText`, `noteText`, `dateFrom`, `dateTo`, `stayLengthMinDays`, `stayLengthMaxDays`, `peopleCount`, `budgetLevel`, `pace`, `generatedAt`.

---

### 6.2 POST `/trips`
Create a trip with full parameters (no "empty trip" flow).

**Request**
```json
{
  "model": {
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
}
```

`tags[].order` is optional. If omitted or `null`, it is stored as `0`; if provided, it must be `>= 0`.

**Response 201**
```json
{
  "trip": { /* Trip DTO */ },
  "tags": [
    { "tag": { "id": "uuid", "code": "museums", "displayName": "Museums", "createdAt": "timestamp" }, "order": 1, "createdAt": "timestamp" }
  ]
}
```

**Errors**
- `400 VALIDATION_ERROR`
  - `model` required
  - `title` required
  - `dateFrom` required
  - `dateTo` required
  - `dateTo >= dateFrom`
  - `stayLengthMinDays` required and > 0
  - `stayLengthMaxDays` required and > 0
  - `stayLengthMaxDays >= stayLengthMinDays`
  - `peopleCount` required and > 0
  - at least one of `placeText`, `noteText`, or non-empty `tags` required
  - `tags[].tagId` required
  - `tags[].order` >= 0 when provided
- `404 TAG_NOT_FOUND` (if tags included)

---

### 6.3 GET `/trips`
List trips with filtering, pagination, sorting.

**Query params**
- `q` (optional search in title/placeText/noteText, max 200 characters)
- `hasPlan=true|false` (maps to `hasGeneratedPlan`)
- `includeDeleted=true|false` (default false)
- `limit` (default 20, valid range 1..100), `cursor`
- `sort` allowed:
  - `createdAt`, `generatedAt`, `title`
  - descending: prefix `-`

**Response 200**
```json
{
  "items": [
    { /* Trip DTO */ }
  ],
  "nextCursor": "string|null"
}
```

**Errors**
- `400 VALIDATION_ERROR` (invalid sort / cursor format)

---

### 6.4 GET `/trips/{tripId}`
Get full trip details including attached tags.

**Response 200**
```json
{
  "trip": { /* Trip DTO */ },
  "tags": [
    { "tag": { "id": "uuid", "code": "museums", "displayName": "Museums", "createdAt": "timestamp" }, "order": 1, "createdAt": "timestamp" }
  ]
}
```

**Errors**
- `400 VALIDATION_ERROR` (empty GUID)
- `404 TRIP_NOT_FOUND`

---

### 6.5 PATCH `/trips/{tripId}`
Partial update.

Only provided fields are changed. Omitted fields keep their current values.

**Request (partial)**
```json
{
  "title": "string",
  "placeText": "string",
  "noteText": "string|null",
  "dateFrom": "YYYY-MM-DD|null",
  "dateTo": "YYYY-MM-DD|null",
  "stayLengthMinDays": 2,
  "stayLengthMaxDays": 21,
  "peopleCount": 2,
  "budgetLevel": "low|medium|high|null",
  "pace": "relaxed|normal|fast|null",

  "tags": [
    { "tagId": "uuid", "order": 1 }
  ]
}
```

Patch semantics important for clients:
- request body must contain at least one field
- `noteText: null` clears the note
- `title: null` is invalid
- `placeText: null` is invalid
- `tags: []` removes all tags
- `tags` omitted leaves tags unchanged
- `tags: null` is invalid
- duplicate `tags[].tagId` values are rejected
- `tags[].order` is optional; if omitted or `null`, it is stored as `0`

**Response 200**
```json
{ "trip": { /* Trip DTO */ } }
```

The response does not include `tags`. If the client changes tags and needs the canonical tag list, call `GET /trips/{tripId}` after a successful patch.

**Errors**
- `400 VALIDATION_ERROR`
**Validation rules**
  - `dateTo >= dateFrom` after merging with existing values
  - `stayLengthMinDays` > 0 when provided
  - `stayLengthMaxDays` > 0 when provided
  - `stayLengthMaxDays >= stayLengthMinDays` after merging with existing values
  - `peopleCount` > 0 when provided
  - at least one of `placeText`, `noteText`, or tags must remain after patch
  - unknown tag in `tags`
- `404 TRIP_NOT_FOUND`

---

### 6.7 DELETE `/trips/{tripId}`
Soft delete.

**Response 204**

**Errors**
- `400 VALIDATION_ERROR` (empty GUID)
- `404 TRIP_NOT_FOUND`

---

## 7. AI Generation Jobs (async + race-safe)

### 7.1 POST `/trips/{tripId}/generation-jobs`
Queue (re)generation.

No request body is supported.

`Idempotency-Key` is not currently supported for this endpoint. Retrying the request can create a new job unless an active queued/processing job already blocks it with `409 JOB_ALREADY_ACTIVE`.

**Requirements (server validation)**
- date range present (`dateFrom`, `dateTo`) and `dateTo >= dateFrom`
- stay length present, both values in [2..21], and `stayLengthMaxDays >= stayLengthMinDays`
- people count present and > 0
- at least one of:
  - `noteText` not empty
  - `>= 2` trip tags
  - `placeText` not empty


**Response 202**
```json
{
  "job": {
    "id": "uuid",
    "tripId": "uuid",
    "status": "queued",
    "requestedAt": "timestamp",
    "attemptNo": 0
  }
}
```

**Errors**
- `400 VALIDATION_ERROR` (empty tripId)
- `404 TRIP_NOT_FOUND`
- `400 GENERATION_REQUIREMENTS_NOT_MET`
- `409 JOB_ALREADY_ACTIVE` (one queued/processing job per trip is allowed)

---

### 7.2 GET `/generation-jobs/{jobId}`
Poll job status.

**Response 200**
```json
{
  "id": "uuid",
  "tripId": "uuid",
  "status": "queued|processing|succeeded|failed|canceled",
  "requestedAt": "timestamp",
  "startedAt": "timestamp|null",
  "finishedAt": "timestamp|null",
  "attemptNo": 0,
  "errorCode": "string|null",
  "errorMessage": "string|null",

  "discarded": false,
  "discardReason": null
}
```

**Errors**
- `400 VALIDATION_ERROR` (empty jobId)
- `404 JOB_NOT_FOUND`

---

### 7.3 GET `/trips/{tripId}/generation-jobs`
List jobs for a trip.

**Query params**
- `limit` (default 20, valid range 1..100), `cursor`

Results are always sorted by `requestedAt` descending, then `id` descending.

**Response 200**
```json
{
  "items": [
    {
      "id": "uuid",
      "status": "queued|processing|succeeded|failed|canceled",
      "requestedAt": "timestamp",
      "finishedAt": "timestamp|null",
      "discarded": false,
      "discardReason": null
    }
  ],
  "nextCursor": null
}
```

**Errors**
- `400 VALIDATION_ERROR` (empty tripId or invalid limit)
- `404 TRIP_NOT_FOUND`

---

### 7.4 Worker concurrency rule (must-have)
When a job finishes generating a plan, **before writing `trip_plan`**:

- check if there exists a **newer job** for the same `tripId` by `requestedAt`,
  with status in: `queued|processing|succeeded`.

If a newer job exists:
- do **not** persist the plan result
- mark current job:
  - `status = succeeded`
  - `discarded = true`
  - `discardReason = "newer_job_exists"`

Only the newest job result is allowed to persist the plan.

---

## 8. Trip Plan (JSON: trip_plan + trip_plan_item)

### 8.1 Plan DTO
```json
{
  "tripId": "uuid",
  "version": 3,
  "status": "generated|saved",
  "generatedFromJobId": "uuid|null",
  "generatedAt": "timestamp|null",
  "savedAt": "timestamp|null",
  "summary": "string|null",
  "items": [
    {
      "id": "uuid",
      "dayNumber": 1,
      "order": 10,
      "title": "string",
      "description": "string|null",
      "locationText": "string|null",
      "itemDate": "timestamp",
      "createdAt": "timestamp",
      "updatedAt": "timestamp",
      "placeType": "attraction|restaurant|hotel"
    }
  ]
}
```

---

### 8.2 GET `/trips/{tripId}/plan`
Get current plan (if exists).

**Response 200**
```json
{
  "tripId": "uuid",
  "version": 3,
  "status": "generated|saved",
  "generatedFromJobId": "uuid|null",
  "generatedAt": "timestamp|null",
  "savedAt": "timestamp|null",
  "summary": "string|null",
  "items": [
    {
      "id": "uuid",
      "dayNumber": 1,
      "order": 10,
      "title": "string",
      "description": "string|null",
      "locationText": "string|null",
      "itemDate": "timestamp",
      "createdAt": "timestamp",
      "updatedAt": "timestamp",
      "placeType": "attraction|restaurant|hotel"
    }
  ]
}
```

**Errors**
- `404 PLAN_NOT_FOUND` (no plan generated yet)
- `404 TRIP_NOT_FOUND`

---

### 8.3 PUT `/trips/{tripId}/plan`
Replace entire plan (manual edit).

**Request**
```json
{
  "summary": "string|null",
  "items": [
    {
      "id": "uuid",
      "dayNumber": 1,
      "order": 10,
      "title": "string",
      "description": "string|null",
      "locationText": "string|null",
      "itemDate": "timestamp",
      "createdAt": "timestamp",
      "updatedAt": "timestamp",
      "placeType": "attraction|restaurant|hotel"
    }
  ]
}
```

**Response 200**
```json
{
  "tripId": "uuid",
  "version": 3,
  "status": "generated|saved",
  "generatedFromJobId": "uuid|null",
  "generatedAt": "timestamp|null",
  "savedAt": "timestamp|null",
  "summary": "string|null",
  "items": [
    {
      "id": "uuid",
      "dayNumber": 1,
      "order": 10,
      "title": "string",
      "description": "string|null",
      "locationText": "string|null",
      "itemDate": "timestamp",
      "createdAt": "timestamp",
      "updatedAt": "timestamp",
      "placeType": "attraction|restaurant|hotel"
    }
  ]
}
```

After a successful manual edit, the plan is persisted as the current plan. The API does not expose a separate finalization or "mark as saved" endpoint.

**Errors**
- `404 PLAN_NOT_FOUND`
- `404 TRIP_NOT_FOUND`
- `409 JOB_ALREADY_ACTIVE`
- `400 VALIDATION_ERROR`
  - `items` is required and cannot be empty
  - `items[].id` is required
  - `items[].dayNumber >= 1`
  - `items[].itemDate` is required
  - `items[].order >= 0`
  - `items[].title` is required and cannot be whitespace
  - `items[].createdAt` is required
  - `items[].updatedAt` is required
  - `items[].createdAt <= items[].updatedAt`
  - `items[].placeType` must be `attraction`, `restaurant`, or `hotel`
  - duplicate `items[].id` values are rejected

---

## 9. Trip Input Snapshots (optional read API)

### GET `/trips/{tripId}/input-snapshots`
**Query params**
- `limit`, `cursor`
- `sort=-generationNo` (default)

**Response 200**
```json
{
  "items": [
    {
      "id": "uuid",
      "kind": "before_generation|after_generation",
      "generationNo": 0,
      "payload": { "any": "json" },
      "createdAt": "timestamp"
    }
  ],
  "nextCursor": null
}
```

**Errors**
- `404 TRIP_NOT_FOUND`

---

## 10. Security & Operational Controls

- Rate limiting:
  - `/auth/login`, `/auth/register` (brute force)
  - `/trips/*/generation-jobs` (cost control)
- Input size limits:
  - noteText, summary, description fields
- Ownership enforcement at service layer + recommended Postgres RLS
- Idempotency for job creation (`Idempotency-Key`) to avoid duplicate job creation on retries

---
