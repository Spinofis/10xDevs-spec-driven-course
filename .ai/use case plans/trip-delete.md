# API Endpoint Implementation Plan: DELETE /trips/{tripId}

## 1. Przeglad punktu koncowego
Endpoint sluzy do miekkiego usuniecia wycieczki uzytkownika przez ustawienie znacznika usuniecia zamiast fizycznego kasowania rekordu. Operacja dotyczy tylko zasobu nalezacego do aktualnie zalogowanego uzytkownika i po sukcesie zwraca `204 No Content`.

Soft delete musi byc zgodny ze specyfikacja:
- rekord `trip` pozostaje w bazie,
- pole `deleted_at` zostaje ustawione na aktualny czas UTC,
- kolejne odczyty i operacje biznesowe powinny traktowac taki rekord jako usuniety,
- jezeli trip nie istnieje, nie nalezy do uzytkownika albo jest juz usuniety, endpoint zwraca `404 TRIP_NOT_FOUND`.

Istotna uwaga implementacyjna: aktualny kod repo ma placeholder `DeleteTripCommand`, ale encja `Trip` i konfiguracja EF nie posiadaja jeszcze `DeletedAt`. Plan wdrozenia musi wiec objac rowniez uzupelnienie modelu domenowego i mapowania bazy.

## 2. Szczegoly zadania
- Metoda HTTP: `DELETE`
- Struktura URL: `/trips/{tripId}`
- Naglowki:
  - wymagane: `Authorization: Bearer <token>` w docelowej implementacji,
  - opcjonalne: `X-Correlation-Id`
- Parametry:
  - wymagane:
    - `tripId` (`Guid`) w sciezce,
  - opcjonalne: brak
- Request Body: brak

Wykorzystywane typy i kontrakty:
- `DeleteTripCommandRequest(Guid TripId)` jako model wejscia dla warstwy Application.
- `DeleteTripCommand(Guid UserId, DeleteTripCommandRequest Request) : IRequest<Result>`; rekomendowane jest dopasowanie sygnatury do aktualnych commandow, takich jak `CreateTripCommand`.
- `DeleteTripCommandResponse` nie jest wymagane dla `204 No Content`; mozna pozostawic pusty typ tylko dla spojnosci lub usunac go po refaktorze.
- `Trip` musi zostac rozszerzony o `DateTimeOffset? DeletedAt`.
- Nalezy dodac domenowa metode, np. `Trip.SoftDelete(DateTimeOffset now)`, aby nie ustawac stanu encji bezposrednio z handlera.

Walidacja wejscia:
- `tripId` musi byc poprawnym `Guid` i nie moze byc `Guid.Empty`,
- brak request body upraszcza walidacje do parametru sciezki,
- ownership nie jest walidacja skladniowa, ale obowiazkowym warunkiem biznesowym w handlerze.

Ekstrakcja logiki do service:
- dla samego ustawienia `DeletedAt` osobny serwis nie jest konieczny; lepiej trzymac logike zmiany stanu w encji `Trip`,
- jezeli delete ma pozniej obejmowac dodatkowe akcje koordynacyjne, np. anulowanie aktywnych jobow AI lub czyszczenie powiazanych danych widokowych, wtedy warto wydzielic `ITripDeletionService` w warstwie Application.
- na etapie MVP rekomendowany kompromis:
  - handler odpowiada za pobranie rekordu, ownership i zapis,
  - encja `Trip` odpowiada za sama zmiane stanu soft delete.

## 3. Szczegoly odpowiedzi
- `204 No Content`
  - brak body,
  - odpowiedz powinna nadal zawierac `X-Correlation-Id`, jesli zostal przekazany albo wygenerowany.
- `401 Unauthorized`
  - brak lub niepoprawny JWT.
- `404 TRIP_NOT_FOUND`
  - trip o podanym `tripId` nie istnieje,
  - trip nie nalezy do aktualnego uzytkownika,
  - trip jest juz soft-deleted i ma byc traktowany jak nieistniejacy.
- `400 VALIDATION_ERROR`
  - niepoprawny format `tripId` albo `Guid.Empty`.
- `500 INTERNAL_ERROR`
  - nieoczekiwany blad infrastruktury lub kodu.

Mapowanie bledow powinno pozostac zgodne z obecnym wzorcem `Result` + `ProblemDetails`. W praktyce trzeba dodac nowy blad domenowy, np. `ResultErrors.TripNotFound(...)`, ktory zwroci:
- `Code = "TRIP_NOT_FOUND"`
- `Status = HttpStatusCode.NotFound`
- komunikat technicznie neutralny, np. `"Trip was not found."`

## 4. Przeplyw danych
1. Klient wysyla `DELETE /trips/{tripId}` z tokenem JWT i opcjonalnym `X-Correlation-Id`.
2. Minimal API w `TripsEndpoints`:
   - odczytuje lub generuje `X-Correlation-Id`,
   - pobiera `userId` z kontekstu autoryzacji; do czasu wdrozenia auth mozna tymczasowo utrzymac `DevelopmentUserId`, ale docelowy plan powinien opierac sie na JWT,
   - buduje `DeleteTripCommandRequest`,
   - wysyla `DeleteTripCommand` przez `IMediator`.
3. `DeleteTripCommandHandler`:
   - waliduje istnienie uzytkownika tylko wtedy, gdy jest to nadal wymagany pattern w projekcie,
   - pobiera `Trip` po `tripId`, `userId` i `DeletedAt == null`,
   - jesli rekordu brak, zwraca `Result.Fail(ResultErrors.TripNotFound(...))`,
   - wywoluje `trip.SoftDelete(DateTimeOffset.UtcNow)`,
   - zapisuje zmiany przez `IAppDbContext.SaveChangesAsync(cancellationToken)`.
4. Endpoint mapuje sukces na `Results.NoContent()`.
5. Bledy oczekiwane sa mapowane przez `ResultHttpMapper`, a bledy nieoczekiwane przez `ExceptionHandlingMiddleware`.

Wplyw na pozostale flow:
- `GET /trips` powinien domyslnie ukrywac rekordy z `DeletedAt != null`,
- `GET /trips/{tripId}`, `PUT /trips/{tripId}`, generowanie planu i endpointy planu/jobow powiazane z tripem powinny takze traktowac usuniety rekord jako `TRIP_NOT_FOUND`,
- plan wdrozenia delete powinien zawierac przeglad istniejacych query i commandow pod katem filtra `DeletedAt == null`.

## 5. Wzgledy bezpieczenstwa
- Endpoint musi byc chroniony JWT Bearer; `AllowAnonymous()` nie jest zgodne z docelowym kontraktem dla operacji delete.
- `userId` nie moze pochodzic z request body ani query; jedynym zrodlem ma byc claim z tokenu.
- Handler musi egzekwowac ownership po stronie aplikacji nawet wtedy, gdy w przyszlosci pojawi sie RLS.
- Odpowiedz `404 TRIP_NOT_FOUND` dla zasobu nienalezacego do uzytkownika ogranicza ryzyko IDOR i nie ujawnia istnienia cudzego rekordu.
- Nie nalezy logowac tokenow ani pelnych danych tripa; wystarczy `tripId`, `userId`, `traceId`, `correlationId`.
- Operacja musi byc asynchroniczna i respektowac `CancellationToken`.
- WAZNE!!! na razie pomin zabezpieczanie endpointu bedzie to zrobione w dalszych etapach

## 6. Obsluga bledow
- `400 VALIDATION_ERROR`
  - `tripId` jest pustym GUID,
  - binder lub walidator odrzuci niepoprawna wartosc.
- `401 Unauthorized`
  - brak tokenu,
  - token niewazny lub wygasly.
- `404 TRIP_NOT_FOUND`
  - rekord nie istnieje,
  - rekord nalezy do innego uzytkownika,
  - rekord zostal juz usuniety.
- `500 INTERNAL_ERROR`
  - blad bazy danych,
  - nieobsluzony wyjatek runtime.

Rejestrowanie bledow:
- aktualny schemat nie zawiera dedykowanej tabeli bledow,
- opcjonalna tabela `audit_event` nie jest zaplanowana do logowania bledow technicznych,
- rekomendacja: pozostac przy `ILogger` + `traceId` + `correlationId` w `ProblemDetails`,
- nie dodawac osobnej tabeli bledow tylko dla tego endpointu.

## 7. Wydajnosc
- Operacja dotyczy pojedynczego rekordu identyfikowanego po `tripId`, wiec koszt jest niski.
- Najwazniejsze jest unikniecie niepotrzebnego ladowania relacji; do soft delete wystarczy jedna encja `Trip`.
- Jesli `deleted_at` bedzie wykorzystywany szerzej w odczytach, warto utrzymac indeks wspierajacy aktywne rekordy:
  - zgodnie z `.ai/db_plan.md`: partial index dla nieusunietych rekordow,
  - praktycznie mozna rozwazyc tez indeks po `(user_id, id)` lub polegac na PK + filtrze po `user_id`, zalezne od finalnego schematu.
- Wszystkie operacje DB musza pozostac async.

## 8. Kroki implementacji
1. Rozszerzyc model domenowy `Trip`
   - dodac w encji `Trip` pole `DeletedAt`,
   - dodac metode `SoftDelete(DateTimeOffset now)` ustawiajaca `DeletedAt` i aktualizujaca `UpdatedAt`,
   - zabezpieczyc metode przed ponownym usunieciem, jesli taka inwarianta ma byc egzekwowana w domenie.
2. Uzgodnic mapowanie EF i schemat DB
   - dodac mapowanie `DeletedAt` w `TripConfiguration`,
   - przygotowac migracje dla kolumny `deleted_at`,
   - zweryfikowac rozbieznosci miedzy `.ai/db_plan.md` a obecnym kodem (`trip` vs `trips`, `note_text` vs `notes`, itp.) i wybrac jeden spojny kierunek.
3. Uporzadkowac kontrakty Application
   - zmienic `DeleteTripCommand` tak, aby implementowal `IRequest<Result>` i przyjmowal `UserId`,
   - pozostawic `DeleteTripCommandRequest(Guid TripId)`,
   - zdecydowac, czy pusty `DeleteTripCommandResponse` zostaje dla spojnosci, czy jest usuwany jako nieuzywany przy `204`.
4. Dodac walidacje i bledy domenowe
   - utworzyc `DeleteTripCommandRequestValidator`,
   - dodac `ResultErrors.TripNotFound(...)`,
   - upewnic sie, ze `ResultHttpMapper` zwroci `ProblemDetails` z kodem `TRIP_NOT_FOUND`.
5. Zaimplementowac handler MediatR
   - utworzyc `DeleteTripCommandHandler`,
   - pobierac rekord po `tripId`, `userId` i `DeletedAt == null`,
   - wywolywac metode domenowa `SoftDelete`,
   - zapisac zmiany i zwrocic `Result.Ok()`.
6. Dodac endpoint minimal API
   - rozszerzyc `TripsEndpoints` o `MapDelete("/{tripId:guid}", DeleteTrip)`,
   - dodac `.Produces(StatusCodes.Status204NoContent)`,
   - dodac `.ProducesProblem(StatusCodes.Status401Unauthorized)`,
   - dodac `.ProducesProblem(StatusCodes.Status404NotFound)`,
   - docelowo ustawic `RequireAuthorization()`.
7. Dopiac kontekst uzytkownika
   - jesli auth nie jest jeszcze gotowe, zachowac tymczasowe `DevelopmentUserId` tylko jako etap przejsciowy,
   - docelowo pobierac `userId` z claims i usunac zaleznosc od stalego GUID z endpointu.
8. Zweryfikowac skutki soft delete w pozostalych endpointach
   - `ListTripsQueryHandler` powinien domyslnie filtrowac po `DeletedAt == null` i honorowac `includeDeleted`,
   - pobranie pojedynczego tripa, aktualizacja, generowanie planu i plan save/update powinny ignorowac rekordy usuniete.
9. Dodac testy
   - jednostkowe dla walidatora `DeleteTripCommandRequestValidator`,
   - jednostkowe dla `Trip.SoftDelete`,
   - testy handlera:
     - sukces i ustawienie `DeletedAt`,
     - `TRIP_NOT_FOUND` dla obcego lub nieistniejacego rekordu,
     - `TRIP_NOT_FOUND` dla rekordu juz usunietego,
   - testy integracyjne endpointu:
     - `204` dla poprawnego delete,
     - `401` bez auth,
     - `404` dla obcego lub brakujacego `tripId`,
     - potwierdzenie obecnosci `X-Correlation-Id`,
     - potwierdzenie, ze usuniety rekord nie wraca w `GET /trips` bez `includeDeleted=true`.
