# API Endpoint Implementation Plan: POST /auth/register

## 1. Przegl¹d punktu koñcowego
Endpoint s³u¿y do rejestracji nowego konta u¿ytkownika. Przyjmuje `email` i `password`, tworzy wpis w `app_user` (opcjonalnie tak¿e domyœlny `user_profile`) i zwraca `201` z pustym obiektem JSON. Obs³uguje b³êdy walidacji (`400`) oraz konflikt unikalnoœci emaila (`409`).

## 2. Szczegó³y ¿¹dania
- Metoda HTTP: `POST`
- Struktura URL: `/auth/register`
- Parametry:
  - Wymagane: `email`, `password`
  - Opcjonalne: brak
- Request Body:
  ```json
  {
    "email": "user@example.com",
    "password": "string"
  }
  ```
- Wykorzystywane typy:
  - `RegisterUserCommandRequest` (Application/Features/Auth/Commands)
  - `RegisterUserCommand` (MediatR command)
  - `RegisterUserCommandResponse` (pusty rekord)

## 3. Szczegó³y odpowiedzi
- `201 Created` z pustym obiektem JSON:
  ```json
  {}
  ```
- Kody b³êdów (ProblemDetails + stabilne kody b³êdów):
  - `400 VALIDATION_ERROR` (np. niepoprawny email, puste has³o)
  - `409 EMAIL_TAKEN` (email ju¿ istnieje)
- Zwracaj nag³ówek `X-Correlation-Id` (przekazany lub wygenerowany).

## 4. Przep³yw danych
1. Controller `AuthController` odbiera ¿¹danie `POST /auth/register` i mapuje body do `RegisterUserCommandRequest`.
2. MediatR przekazuje `RegisterUserCommand` do handlera.
3. Walidator (FluentValidation) sprawdza `email` i `password`.
4. Handler deleguje logikê rejestracji do serwisu domenowego/infrastrukturalnego, np. `UserRegistrationService`.
5. Serwis:
   - normalizuje email (np. trim) i/lub polega na `citext` w DB,
   - hashuje has³o (argon2id zgodnie z `app_user.password_algo`),
   - tworzy `app_user` i (opcjonalnie) `user_profile` w jednej transakcji.
6. Repozytorium/DbContext zapisuje dane w PostgreSQL.
7. Handler zwraca `201` z pustym obiektem JSON.

## 5. Wzglêdy bezpieczeñstwa
- Has³o nigdy nie trafia do logów; stosuj redakcjê danych w logach.
- Hashowanie hase³: `argon2id` (zgodnie z DB planem), z bezpiecznymi parametrami (konfigurowalne).
- Email jest case-insensitive (`citext` + unikalny indeks).
- Endpoint publiczny (bez JWT), ale zabezpieczony przed nadu¿yciami:
  - ograniczanie liczby prób (rate limiting) lub throttling po IP/user-agent.
- Nie ujawniaj w treœci b³êdów danych wra¿liwych; dla `EMAIL_TAKEN` nie zwracaj emaila.

## 6. Obs³uga b³êdów
- `400 VALIDATION_ERROR`:
  - brak/niepoprawny format emaila,
  - puste has³o,
  - opcjonalnie zbyt krótkie has³o (jeœli polityka bezpieczeñstwa to wymaga).
- `409 EMAIL_TAKEN`:
  - wykryte istnienie emaila przez pre-check lub naruszenie unikalnego indeksu.
- `500`:
  - nieoczekiwane b³êdy infrastruktury/DB.
- Mapowanie b³êdów do ProblemDetails w globalnym middleware.
- Brak dedykowanej tabeli b³êdów w schemacie: u¿yj standardowego logowania aplikacyjnego + traceId.

## 7. Wydajnoœæ
- Minimalna liczba zapytañ (pre-check + insert, lub tylko insert z obs³ug¹ konfliktu).
- Indeks unikalny na `app_user.email` zapewnia szybkie wykrycie konfliktu.
- Operacje IO asynchroniczne end-to-end.
- Transakcja obejmuje utworzenie `app_user` i `user_profile` (jeœli wymagane).

## 8. Kroki implementacji
1. **Modele i mapowania DB**
   - PotwierdŸ mapowanie tabeli `app_user` (citext, unique) i ewentualnie `user_profile` w DbContext.
   - Dodaj/zweryfikuj migracje dla `citext` i `pgcrypto` (jeœli brak).
2. **Walidacja**
   - Utwórz `RegisterUserCommandRequestValidator` (FluentValidation):
     - `Email` wymagany, poprawny format.
     - `Password` wymagane; ewentualna polityka min. d³ugoœci.
3. **Serwis rejestracji**
   - Dodaj `IUserRegistrationService` / `UserRegistrationService`:
     - normalizacja emaila,
     - hashowanie has³a (argon2id),
     - zapis u¿ytkownika i opcjonalnego profilu w transakcji.
4. **Handler MediatR**
   - Implementuj `RegisterUserCommandHandler`:
     - wywo³anie serwisu,
     - obs³uga konfliktu emaila (pre-check lub catch unique violation -> `EMAIL_TAKEN`).
5. **Controller**
   - Dodaj `AuthController` z `POST /auth/register`.
   - Zwróæ `201` z `{}` oraz `X-Correlation-Id`.
6. **Error handling**
   - Dodaj/rozszerz mapowanie wyj¹tków do ProblemDetails z kodami `VALIDATION_ERROR` i `EMAIL_TAKEN`.
7. **Konfiguracja DI**
   - Zarejestruj serwis rejestracji i hasher.
8. **Testy**
   - Jednostkowe: walidator i handler (scenariusze 400/409/201).
   - Integracyjne: unikalnoœæ emaila w DB, poprawne hashowanie has³a.
