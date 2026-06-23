# PostgreSQL schema plan — VibeTravels (MVP)

Źródła:
- PRD: `@prd.md`
- Tech stack: `@tech stack.md`
- Notatki sesji: `@planowanie db.md` + decyzje z rozmowy (m.in. jawna flaga generacji, `timestamp` bez stref, `order`, generacja nadpisuje, brak streamów)

## 1. Lista tabel

> Konwencje:
> - wszystkie PK: `uuid` (domyślnie `gen_random_uuid()`; wymaga rozszerzenia `pgcrypto`)
> - daty/czasy: `timestamp without time zone` (zgodnie z ustaleniem)
> - `created_at`, `updated_at` aktualizowane w aplikacji (lub triggerem, jeśli zechcesz)
> - dane formularza/generacji w `jsonb` dla elastyczności MVP

### 1.1 `app_user`
Konto użytkownika (rejestracja/logowanie).

| kolumna | typ | ograniczenia / uwagi |
|---|---|---|
| id | uuid | PK, default `gen_random_uuid()` |
| email | citext | NOT NULL, UNIQUE |
| password_hash | text | NOT NULL |
| password_algo | text | NOT NULL, default `'argon2id'` (lub zgodnie z implementacją) |
| is_active | boolean | NOT NULL, default true |
| created_at | timestamp | NOT NULL, default `now()` |
| updated_at | timestamp | NOT NULL, default `now()` |
| last_login_at | timestamp | NULL |

**Uwagi:** użyj `citext` dla case-insensitive email.

---

### 1.2 `user_profile`
Preferencje użytkownika (strukturalne) + flaga „domyślne”.

| kolumna | typ | ograniczenia / uwagi |
|---|---|---|
| user_id | uuid | PK, FK → `app_user(id)` ON DELETE CASCADE |
| default_budget_level | text | NULL (np. `low/medium/high` albo zakresy) |
| default_people_count | integer | NULL, CHECK (`default_people_count` > 0) |
| default_pace | text | NULL (np. `relaxed/normal/fast`) |
| default_notes | text | NULL (opcjonalne) |
| is_default | boolean | NOT NULL, default true |
| created_at | timestamp | NOT NULL, default `now()` |
| updated_at | timestamp | NOT NULL, default `now()` |

---

### 1.3 `tag`
Słownik tagów (wspólny dla preferencji i wycieczek).

| kolumna | typ | ograniczenia / uwagi |
|---|---|---|
| id | uuid | PK, default `gen_random_uuid()` |
| code | text | NOT NULL, UNIQUE (np. `mountains`, `museums`) |
| display_name | text | NOT NULL |
| created_at | timestamp | NOT NULL, default `now()` |

---

### 1.4 `user_preference_tag`
Tagi preferencji użytkownika z kolejnością (`order`).

| kolumna | typ | ograniczenia / uwagi |
|---|---|---|
| user_id | uuid | NOT NULL, FK → `app_user(id)` ON DELETE CASCADE |
| tag_id | uuid | NOT NULL, FK → `tag(id)` ON DELETE RESTRICT |
| order | integer | NOT NULL, default 0 |
| created_at | timestamp | NOT NULL, default `now()` |

PK: `(user_id, tag_id)`

---

### 1.5 `trip`
„Notatka/wycieczka” — encja nadrzędna dla formularza, inputów i planu.

| kolumna | typ | ograniczenia / uwagi |
|---|---|---|
| id | uuid | PK, default `gen_random_uuid()` |
| user_id | uuid | NOT NULL, FK → `app_user(id)` ON DELETE CASCADE |
| title | text | NOT NULL |
| place_text | text | NOT NULL (nieustrukturyzowane „miejsce”) |
| note_text | text | NULL (szybkie notatki użytkownika) |
| date_from | date | NULL |
| date_to | date | NULL, CHECK (`date_to` IS NULL OR `date_from` IS NULL OR `date_to` >= `date_from`) |
| stay_length_min_days | integer | NULL, CHECK (`stay_length_min_days` IS NULL OR `stay_length_min_days` > 0) |
| stay_length_max_days | integer | NULL, CHECK (`stay_length_max_days` IS NULL OR `stay_length_max_days` > 0) |
| people_count | integer | NULL, CHECK (`people_count` IS NULL OR `people_count` > 0) |
| budget_level | text | NULL |
| pace | text | NULL |
| generated_at | timestamp | NULL (ostatnia udana generacja) |
| has_generated_plan | boolean | NOT NULL, default false |
| save_requested | boolean | NOT NULL, default false *(jawna flaga — worker sprawdza)* |
| deleted_at | timestamp | NULL (soft delete; lista ma ukrywać usunięte) |
| created_at | timestamp | NOT NULL, default `now()` |
| updated_at | timestamp | NOT NULL, default `now()` |

**Uwagi:** sortowanie/filtrowanie listy: po `created_at`, `generated_at`, `has_generated_plan`, `deleted_at`.

---

### 1.6 `trip_tag`
Tagi przypięte do wycieczki (filtrowanie i kontekst dla AI).

| kolumna | typ | ograniczenia / uwagi |
|---|---|---|
| trip_id | uuid | NOT NULL, FK → `trip(id)` ON DELETE CASCADE |
| tag_id | uuid | NOT NULL, FK → `tag(id)` ON DELETE RESTRICT |
| order | integer | NOT NULL, default 0 |
| created_at | timestamp | NOT NULL, default `now()` |

PK: `(trip_id, tag_id)`

---

### 1.7 `trip_input_snapshot`
Zapis inputu: przed pierwszą generacją i po każdej kolejnej generacji (PRD).

| kolumna | typ | ograniczenia / uwagi |
|---|---|---|
| id | uuid | PK, default `gen_random_uuid()` |
| trip_id | uuid | NOT NULL, FK → `trip(id)` ON DELETE CASCADE |
| user_id | uuid | NOT NULL, FK → `app_user(id)` ON DELETE CASCADE |
| kind | text | NOT NULL, CHECK (`kind` IN ('before_generation','after_generation')) |
| generation_no | integer | NOT NULL, CHECK (`generation_no` >= 0) |
| payload | jsonb | NOT NULL (pełny „form state” + ewentualnie preferencje użyte do generacji) |
| created_at | timestamp | NOT NULL, default `now()` |

**Uwagi:** `generation_no` rośnie z każdą próbą generacji (nawet jeśli finalnie fail).

---

### 1.8 `ai_generation_job`
Statusy asynchronicznego procesu generacji AI (polling; brak streamów).

| kolumna | typ | ograniczenia / uwagi |
|---|---|---|
| id | uuid | PK, default `gen_random_uuid()` |
| trip_id | uuid | NOT NULL, FK → `trip(id)` ON DELETE CASCADE |
| user_id | uuid | NOT NULL, FK → `app_user(id)` ON DELETE CASCADE |
| status | text | NOT NULL, CHECK (`status` IN ('queued','processing','succeeded','failed','canceled')) |
| requested_at | timestamp | NOT NULL, default `now()` |
| started_at | timestamp | NULL |
| finished_at | timestamp | NULL |
| attempt_no | integer | NOT NULL, default 0, CHECK (`attempt_no` >= 0) |
| error_code | text | NULL |
| error_message | text | NULL |
| request_payload | jsonb | NOT NULL (co wysłaliśmy do AI) |
| response_payload | jsonb | NULL (surowa odpowiedź, jeśli przechowujesz) |

**Ograniczenia dodatkowe:**
- opcjonalnie: unikalność „jedna aktywna generacja na trip” poprzez partial unique index (patrz indeksy).

---

### 1.9 `trip_plan`
Aktualny plan wycieczki (nadpisywany przy każdej generacji; brak wersjonowania).

| kolumna | typ | ograniczenia / uwagi |
|---|---|---|
| trip_id | uuid | PK, FK → `trip(id)` ON DELETE CASCADE |
| user_id | uuid | NOT NULL, FK → `app_user(id)` ON DELETE CASCADE |
| current_text | text | NOT NULL (tekst planu o stałej strukturze) |
| rendered_html | text | NULL (cache HTML; można też trzymać po stronie app) |
| last_generated_job_id | uuid | NULL, FK → `ai_generation_job(id)` ON DELETE SET NULL |
| updated_at | timestamp | NOT NULL, default `now()` |
| saved_at | timestamp | NULL *(ustawiane tylko po kliknięciu „Zapisz plan”)* |
| is_saved | boolean | NOT NULL, default false |

**Uwagi:**
- „Zapisz plan” ustawia `is_saved=true`, `saved_at=now()`.  
- Edycja planu modyfikuje `current_text` (brak rozróżnienia AI vs ręczne).

---

### 1.10 `audit_event` (opcjonalne, pod metryki sukcesu)
Minimalny log zdarzeń produktu.

| kolumna | typ | ograniczenia / uwagi |
|---|---|---|
| id | uuid | PK, default `gen_random_uuid()` |
| user_id | uuid | NOT NULL, FK → `app_user(id)` ON DELETE CASCADE |
| event_type | text | NOT NULL, CHECK (`event_type` IN ('preferences_saved','plan_saved')) |
| entity_type | text | NOT NULL |
| entity_id | uuid | NOT NULL |
| created_at | timestamp | NOT NULL, default `now()` |
| payload | jsonb | NULL |

---

## 2. Relacje między tabelami

- `app_user (1) — (1) user_profile` (1:1, klucz = `user_id`)
- `app_user (1) — (N) trip` (1:N)
- `trip (1) — (N) trip_input_snapshot` (1:N)
- `trip (1) — (N) ai_generation_job` (1:N)
- `trip (1) — (1) trip_plan` (1:1; plan może powstać po pierwszej generacji)
- `tag (N) — (N) app_user` przez `user_preference_tag` (N:M)
- `tag (N) — (N) trip` przez `trip_tag` (N:M)
- `app_user (1) — (N) audit_event` (1:N)

## 3. Indeksy

> Poza PK/UNIQUE.

### `app_user`
- `UNIQUE (email)` (wymagane do logowania)

### `user_preference_tag`
- `INDEX user_preference_tag_user_order_idx (user_id, "order", tag_id)`
- `INDEX user_preference_tag_tag_idx (tag_id)`

### `trip`
- `INDEX trip_user_created_idx (user_id, created_at DESC)`
- `INDEX trip_user_generated_idx (user_id, generated_at DESC)`
- `INDEX trip_user_hasplan_idx (user_id, has_generated_plan, created_at DESC)`
- `INDEX trip_save_requested_idx (save_requested) WHERE save_requested = true` *(worker polling)*  
- `INDEX trip_not_deleted_idx (user_id) WHERE deleted_at IS NULL`

### `trip_tag`
- `INDEX trip_tag_trip_order_idx (trip_id, "order", tag_id)`
- `INDEX trip_tag_tag_idx (tag_id)`

### `trip_input_snapshot`
- `INDEX trip_input_snapshot_trip_gen_idx (trip_id, generation_no DESC)`
- `INDEX trip_input_snapshot_user_idx (user_id, created_at DESC)`

### `ai_generation_job`
- `INDEX ai_job_trip_requested_idx (trip_id, requested_at DESC)`
- `INDEX ai_job_status_requested_idx (status, requested_at DESC)`
- `UNIQUE INDEX ai_job_one_active_per_trip_idx (trip_id) WHERE status IN ('queued','processing')` *(opcjonalnie, ale zalecane)*

### `trip_plan`
- `INDEX trip_plan_user_saved_idx (user_id, is_saved, saved_at DESC)`

### `audit_event`
- `INDEX audit_event_user_type_idx (user_id, event_type, created_at DESC)`
- `INDEX audit_event_entity_idx (entity_type, entity_id)`

## 4. Zasady PostgreSQL (RLS)

Zakładamy, że aplikacja ustawia w transakcji:
- `SET LOCAL app.user_id = '<uuid>'` (uuid użytkownika po JWT)
- opcjonalnie `SET LOCAL app.is_admin = 'true/false'`

### 4.1 Włączenie RLS
Włącz RLS na wszystkich tabelach z danymi użytkowników:
- `user_profile`
- `user_preference_tag`
- `trip`
- `trip_tag`
- `trip_input_snapshot`
- `ai_generation_job`
- `trip_plan`
- `audit_event`

`tag` może być publiczny (bez RLS), jeśli jest globalnym słownikiem.

### 4.2 Polityki (przykładowe)

**Wspólna reguła ownera** (dla tabel z kolumną `user_id`):
- SELECT/INSERT/UPDATE/DELETE dozwolone, gdy `user_id = current_setting('app.user_id')::uuid`

**`user_profile`** (PK = `user_id`):
- SELECT/UPDATE/DELETE: `user_id = current_setting('app.user_id')::uuid`
- INSERT: `user_id = current_setting('app.user_id')::uuid`

**Tabele z `trip_id` bez `user_id`** (np. `trip_tag`):
- w polityce użyj `EXISTS (SELECT 1 FROM trip t WHERE t.id = trip_tag.trip_id AND t.user_id = current_setting('app.user_id')::uuid)`

**Worker**:
- jeśli worker działa jako rola DB `app_worker`, możesz dodać polityki `TO app_worker` pozwalające na:
  - SELECT na `trip` z `save_requested=true`
  - UPDATE statusów w `ai_generation_job` i `trip_plan`
- alternatywnie worker może działać „jako użytkownik” (ustawiając `app.user_id`) — zależnie od architektury.

## 5. Dodatkowe uwagi

- **Nadpisywanie planu**: brak wersjonowania realizujemy przez 1:1 `trip_plan` i aktualizację `current_text`. Historia inputów jest zachowana w `trip_input_snapshot` (zgodnie z PRD).
- **Spójność transakcyjna**: przy generacji (lub „Zapisz plan”) zapisuj w jednej transakcji:
  - snapshot inputu,
  - zmianę statusu joba,
  - aktualizację `trip_plan` i flag na `trip`.
- **Soft delete**: `trip.deleted_at` ułatwia MVP (bez kaskadowego usuwania historii). Jeśli wolisz hard delete, usuń tę kolumnę i polegaj na `ON DELETE CASCADE`.
- **Rozszerzenia**: zalecane `pgcrypto` (UUID) i `citext` (email).

## 0. Status aktualizacji (GET /trips/{tripId}/plan)

Ten dokument zawiera starsze fragmenty i niektore opisy historyczne.
Aktualny model planu dla endpointow API (w tym `GET /trips/{tripId}/plan`) to:

- tabela `trip_plans` jako naglowek planu (1:1 z `trip` po `trip_id`);
- tabela `plan_items` jako lista pozycji planu (1:N po `trip_id`);
- odczyt planu musi byc wykonywany z `trip_plans + plan_items`, nie z tekstowego `trip_plan.current_text`.

W praktyce:

- status planu jest wyliczany z danych naglowka (`generated` gdy jest `generation_job_id`, inaczej `saved`);
- pozycje planu sa czytane i sortowane stabilnie (`item_date`, potem `sort_order`, potem `id`);
- endpoint rozroznia bledy `TRIP_NOT_FOUND` oraz `PLAN_NOT_FOUND`.

Uwaga: sekcje opisujace tabele `trip_plan` (liczba pojedyncza) i pole `current_text`
traktowac jako nieaktualne dla implementacji endpointow planu.
