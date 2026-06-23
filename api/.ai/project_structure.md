# Project structure (Clean Architecture-ish + CQRS + Minimal API)

This document defines the **solution layout**, **projects**, and **folder conventions** for the MVP.

It follows the spirit of **Clean Architecture + CQRS (MediatR)**, with **intentional, documented deviations**:
- **Minimal API** instead of MVC Controllers
- **API Contracts (request/response)** live in **Application** and are used directly by endpoints/handlers (to reduce duplication)

---

## High-level goals

- Keep **use-cases** (commands/queries) isolated and testable (MediatR).
- Keep **Domain** free of infrastructure concerns (EF Core, HTTP, OpenAI).
- Support async plan generation via **DB-backed jobs + worker** (no streaming).
- Keep the MVP structure simple, fast to iterate, and easy to understand in a repo.

---

## Solution layout

```
/src
  VibeTravels.Api/                 # Minimal API host (HTTP boundary)
  VibeTravels.Application/         # Use-cases (CQRS), Contracts, abstractions
  VibeTravels.Domain/              # Domain model + business rules
  VibeTravels.Infrastructure/      # EF Core/Postgres, OpenAI integration, auth impl
  VibeTravels.Worker/              # Background processing (job polling/execution)

/tests
  VibeTravels.UnitTests/
  VibeTravels.IntegrationTests/

/docs
  architecture/
    project-structure.md           # this file
```

---

## Dependency rules

Allowed references (compile-time):

- `Api` → `Application`
- `Worker` → `Application`
- `Application` → `Domain`
- `Infrastructure` → `Application` and `Domain`
- `Api` → `Infrastructure` (composition root only, via DI)
- `Worker` → `Infrastructure` (composition root only, via DI)

Not allowed:
- `Domain` must not reference anything else.
- `Application` must not reference `Api` or `Infrastructure`.
- `Infrastructure` must not reference `Api` (it is reusable by Api/Worker).

---

## Project details and folder conventions

### 1) `VibeTravels.Domain` (Domain model + business rules)

**Purpose:** Pure domain logic.

```
VibeTravels.Domain/
  Entities/
    Users/
    Trips/
    Tags/
    Plans/
    Jobs/
  ValueObjects/
  DomainServices/      # domain-level operations that don't fit single entity
  Events/              # optional (domain events)
  Exceptions/
```

**Rules**
- No EF attributes, no HTTP, no OpenAI code.
- Entities encapsulate invariants (e.g., valid date range, job status transitions).

---

### 2) `VibeTravels.Application` (Use-cases + Contracts + Abstractions)

**Purpose:** Application layer containing **commands/queries**, validation, orchestration, and **API Contracts** used by Minimal API endpoints.

> **Deviation:** Contracts (request/response) are stored here and can be used across Api + Worker without duplicate DTOs.

```
VibeTravels.Application/
  Contracts/                       # request/response models used by API endpoints
    Common/
      ErrorEnvelope.cs
      Paging.cs
    Me/
    Tags/
    Trips/
    Plans/
    Jobs/

  Features/
    Me/
      Queries/
      Commands/
    Trips/
      Commands/
      Queries/
    Plans/
      Commands/
      Queries/
    Jobs/
      Commands/
      Queries/
    Tags/
      Queries/

  Abstractions/
    Persistence/
      IAppDbContext.cs             # unit-of-work abstraction (EF hidden behind interface)
    Integrations/
      IOpenAiClient.cs
      IClock.cs
      ICurrentUser.cs
      IIdGenerator.cs              # optional
    Security/
      ITokenService.cs             # optional (if needed for auth flows)

  Behaviors/                       # MediatR pipeline (validation, transaction, logging)
    ValidationBehavior.cs
    TransactionBehavior.cs         # optional but recommended for write commands
    LoggingBehavior.cs

  Validation/                      # FluentValidation validators
  Common/
    Errors/
    Time/
    Result.cs                      # optional: Result/OneOf-style pattern
  DependencyInjection.cs
```

**Rules**
- **Handlers implement use-cases**, not HTTP.
- Contracts can be shared by Api and Worker (e.g., job status responses, plan shape).
- If a Contract starts to become “transport-only” (e.g., includes HTTP-specific fields),
  prefer keeping it still here but strictly **API shaped**, and avoid leaking it to Domain.

---

### 3) `VibeTravels.Infrastructure` (EF Core/Postgres + OpenAI + security implementations)

**Purpose:** All external integrations and persistence implementations.

```
VibeTravels.Infrastructure/
  Persistence/
    AppDbContext.cs
    Configurations/                # EF Core Fluent configs per entity
    Migrations/
    Interceptors/                  # optional (auditing, soft-delete)
  Integrations/
    OpenAI/
      OpenAiClient.cs
      Prompting/
        Templates/
        Builders/
  Security/
    Jwt/
    PasswordHashing/
  Services/
    Clock.cs                       # implements IClock
    CurrentUser.cs                 # implements ICurrentUser (for API host)
  DependencyInjection.cs
```

**Rules**
- EF Core stays here.
- OpenAI client stays here (Application depends on `IOpenAiClient` only).
- This project should be reusable by both `Api` and `Worker`.

---

### 4) `VibeTravels.Api` (Minimal API host)

**Purpose:** HTTP boundary + routing + composition root.

> No controllers. Endpoints are grouped by feature.

```
VibeTravels.Api/
  Endpoints/
    MeEndpoints.cs
    TripsEndpoints.cs
    PlansEndpoints.cs
    JobsEndpoints.cs
    TagsEndpoints.cs
  Middleware/
  Auth/
  Extensions/
    EndpointRouteBuilderExtensions.cs   # MapFeatureEndpoints()
  Program.cs
```

**Rules**
- Endpoints should be **thin**: bind request → `IMediator.Send()` → return response.
- No business rules in endpoints (leave to Application handlers).
- Request/response types come from `VibeTravels.Application.Contracts`.

**Endpoint structure (suggestion)**
- Each `*Endpoints.cs` exposes `MapXEndpoints(IEndpointRouteBuilder app)`
- `Program.cs` composes:
  - `builder.Services.AddApplication();`
  - `builder.Services.AddInfrastructure(configuration);`
  - `app.MapFeatureEndpoints();`

---

### 5) `VibeTravels.Worker` (Background processing host)

**Purpose:** Poll DB for pending jobs, call OpenAI, write results back.

```
VibeTravels.Worker/
  HostedServices/
    JobPollingHostedService.cs
  Processing/
    GenerationJobProcessor.cs
  Program.cs
```

**Rules**
- Worker talks to Application via MediatR or dedicated Application services.
- Worker uses Infrastructure for persistence + OpenAI implementation through DI.
- Use DB job flags/statuses as the “queue” for MVP.

---

## Where do we keep specific things?

### Request/Response models (Contracts)
- **`VibeTravels.Application/Contracts/**`**
  - grouped by feature: `Trips`, `Plans`, `Jobs`, `Me`, etc.
  - keep consistent naming: `CreateTripRequest`, `TripResponse`, etc.

### Domain logic
- **`VibeTravels.Domain/**`**
  - invariants and rules live in Entities / ValueObjects / DomainServices.

### External services (OpenAI, etc.)
- Interface in **Application**: `Abstractions/Integrations/IOpenAiClient.cs`
- Implementation in **Infrastructure**: `Integrations/OpenAI/OpenAiClient.cs`

### Command handlers
- **`VibeTravels.Application/Features/<Feature>/(Commands|Queries)/**`**
  - `Command/Query` + `Handler` + `Validator` in same folder.

### DbContext
- **`VibeTravels.Infrastructure/Persistence/AppDbContext.cs`**
- Application sees only `IAppDbContext`.

### Application services (orchestrators)
- If a feature needs orchestration beyond a single handler:
  - put it in `VibeTravels.Application/Features/<Feature>/Services/`
  - keep it dependency-free (depends only on abstractions + Domain).

---

## Mapping strategy (important with shared Contracts)

Since endpoints and handlers share the same request/response types, mapping is reduced.

Still recommended:
- Domain entities are not returned directly.
- Responses should be Contracts shaped for API/clients.

Typical flow:

```
Minimal API endpoint
  -> MediatR Command/Query (using Contracts models)
     -> Domain + Persistence + Integrations
        -> return Contract Response
```

**Guideline:** If a response needs computed fields, do it in handler/query, not in endpoint.

---

## Clean Architecture alignment vs intentional deviations

### Aligned with Clean Architecture
- Domain is pure and isolated.
- Application defines abstractions; Infrastructure implements them.
- Api and Worker are thin hosts + DI composition roots.
- Use-cases are in Application (MediatR CQRS).

### Intentional deviations (and why)
1) **Contracts live in Application**
   - Pros: fewer types, faster iteration, shared between Api/Worker, less mapping boilerplate.
   - Cons: Application becomes coupled to API shape; versioning is harder later.
   - MVP rationale: optimize for speed and reduced duplication.

2) **Minimal API instead of Controllers**
   - Pros: smaller surface area, simpler routing, less ceremony.
   - Cons: requires discipline to keep endpoints thin.
   - MVP rationale: faster development and clearer endpoint grouping.

3) **EF Core access pattern**
   - Prefer `IAppDbContext` + direct EF queries in handlers (no mandatory repositories everywhere).
   - MVP rationale: avoids repository explosion; keeps code straightforward.

---

## Naming conventions

- Feature folders: `Me`, `Trips`, `Plans`, `Jobs`, `Tags`
- Command names: `CreateTripCommand`, `UpdateTripCommand`, `QueuePlanGenerationCommand`
- Query names: `GetTripQuery`, `ListTripsQuery`, `GetJobStatusQuery`
- Contract names:
  - Requests: `XRequest`
  - Responses: `XResponse`
  - Shared payloads: `XDto` only inside Contracts when reused across responses

---

## Testing layout

```
/tests
  VibeTravels.UnitTests/
    Domain/                  # entity + VO rules
    Application/             # handler tests with fakes/mocks
  VibeTravels.IntegrationTests/
    Api/                     # HTTP tests (TestServer/WebApplicationFactory)
    Persistence/             # DB integration with Postgres (testcontainers)
```

---

## When we outgrow this MVP structure (future-proofing)

If/when the API shape starts to diverge from internal needs:
- split `Application/Contracts` back into `Api/Contracts`
- introduce `Application/Dtos` (internal), and mapping at HTTP boundary
- consider API versioning strategy

Until then, this layout optimizes for MVP velocity while keeping most architectural benefits.
