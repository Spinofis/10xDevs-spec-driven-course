# API Endpoint Implementation Plan: PATCH /trips/{tripId}

## 1. Przeglad punktu koncowego
Endpoint sluzy do czesciowej aktualizacji istniejacej wycieczki (`trip`). Jest to endpoint typu command/write i powinien zwracac `200 OK` z aktualnym `Trip DTO` po zapisaniu zmian.

Najwazniejsza cecha tego endpointu to poprawna semantyka PATCH:
- pole nieobecne w JSON = wartosc pozostaje bez zmian,
- pole obecne z `null` = wyczyszczenie wartosci, ale tylko dla pol, ktore kontrakt dopuszcza jako nullable,
- pole obecne z nowa wartoscia = nadpisanie.

To wymaga innego modelu wejscia niz obecny `UpdateTripCommandModel`, bo sam `string?` / `DateOnly?` nie pozwala rozroznic "brak pola" od "pole ustawione na null".

## 2. Szczegoly zadania
- Metoda HTTP: `PATCH`
- Struktura URL: `/trips/{tripId}`
- Naglowki:
  - Opcjonalne: `X-Correlation-Id`
  - Docelowo wymagane: `Authorization: Bearer <token>`
- Parametry:
  - Wymagane:
    - `tripId` (`uuid`) w route
  - Opcjonalne w body:
    - `title` (`string`) - jesli pole wystepuje, nie moze byc puste; `null` powinno byc odrzucone
    - `placeText` (`string`) - jesli pole wystepuje, moze byc pustym konceptem biznesowo tylko wtedy, gdy po zmergowaniu nadal spelniony jest warunek "placeText lub noteText lub tags"; `null` powinno byc odrzucone, chyba ze zespol swiadomie dopusci czyszczenie tego pola przez `null`
    - `noteText` (`string|null`) - moze zostac wyczyszczone przez `null`
    - `dateFrom` (`YYYY-MM-DD|null`) - moze zostac wyczyszczone przez `null`
    - `dateTo` (`YYYY-MM-DD|null`) - moze zostac wyczyszczone przez `null`
    - `stayLengthMinDays` (`int`) - jesli podane, musi byc `> 0`
    - `stayLengthMaxDays` (`int`) - jesli podane, musi byc `> 0`
    - `peopleCount` (`int`) - jesli podane, musi byc `> 0`
    - `budgetLevel` (`low|medium|high|null`) - `null` czysci wartosc
    - `pace` (`relaxed|normal|fast|null`) - `null` czysci wartosc
    - `tags` (`array|null`) - rekomendowana semantyka:
      - brak pola `tags` = bez zmian,
      - `tags: []` = wyczyszczenie wszystkich tagow,
      - `tags: [...]` = zastapienie calej kolekcji tagow nowa lista,
      - `tags: null` = traktowac jak blad walidacji albo jawnie zabronic w kontrakcie, aby uniknac niejednoznacznosci

### Wymagane typy i modele
- API / Application:
  - nowy `PatchTripCommand`
  - nowy `PatchTripCommandRequest`
  - nowy `PatchTripCommandResponse`
  - nowy `PatchTripCommandValidator`
  - nowy `PatchTripCommandRequestValidator`
  - nowy `PatchTripCommandModel`
- Modele pomocnicze:
  - `TripTagCommandModel` mozna wykorzystac ponownie
  - `TripQueryModel` mozna wykorzystac ponownie w odpowiedzi
- Konieczny model obecnosci pola:
  - rekomendacja: wprowadzic wrapper typu `OptionalField<T>` / `PatchField<T>` z informacja `IsSet`
  - alternatywa: osobny custom JSON converter dla patch modeli
- Domain / Application:
  - dodac metode domenowa `Trip.ApplyPatch(...)` albo dedykowany serwis `ITripUpdateService`
  - dodac blad `TRIP_NOT_FOUND` do `ResultErrors`

### Wyodrebnienie logiki do service
Handler nie powinien sam recznie mergowac 10+ pol i synchronizowac relacji `trip_tags`. Najlepszy podzial:
- `PatchTripCommandHandler` odpowiada za pobranie danych, ownership i zapis
- `ITripUpdateService` / `TripUpdateService` odpowiada za:
  - zbudowanie "stanu po zmianie" na bazie aktualnej encji i patcha,
  - walidacje reguly biznesowej po mergu,
  - synchronizacje tagow,
  - mapowanie patcha na zmiany encji

Jesli zespol chce trzymac logike blizej domeny, serwis moze tylko przygotowac dane wejsciowe, a sama encja `Trip` powinna miec metode `ApplyPatch(...)`.

## 3. Szczegoly odpowiedzi
- `200 OK`
  ```json
  {
    "trip": {
      "id": "uuid",
      "userId": "uuid",
      "title": "string",
      "placeText": "string|null",
      "noteText": "string|null",
      "dateFrom": "YYYY-MM-DD|null",
      "dateTo": "YYYY-MM-DD|null",
      "stayLengthMinDays": 2,
      "stayLengthMaxDays": 21,
      "peopleCount": 2,
      "budgetLevel": "low|medium|high|null",
      "pace": "relaxed|normal|fast|null",
      "generatedAt": "timestamp|null",
      "hasGeneratedPlan": false,
      "createdAt": "timestamp",
      "updatedAt": "timestamp"
    }
  }
  ```
- Naglowki odpowiedzi:
  - `X-Correlation-Id`
- Kody statusu:
  - `200` dla poprawnej aktualizacji
  - `400 VALIDATION_ERROR` dla blednego payloadu albo niespojnego stanu po mergu
  - `401 UNAUTHORIZED` docelowo po wlaczeniu auth
  - `404 TRIP_NOT_FOUND` gdy `tripId` nie istnieje albo nie nalezy do usera
  - `500 INTERNAL_ERROR` dla bledow nieoczekiwanych

Uwaga o kontrakcie odpowiedzi:
- spec dla PATCH zwraca tylko `{ "trip": ... }`
- mimo ze request moze aktualizowac `tags`, odpowiedz nie musi ich zwracac
- istniejacy `UpdateTripCommandResponse(TripQueryModel Trip)` pasuje do tego kontraktu i moze zostac wykorzystany po poprawieniu samego commandu

## 4. Przeplyw danych
1. Klient wywoluje `PATCH /trips/{tripId}` z body zawierajacym tylko pola do zmiany.
2. Minimal API w `TripsEndpoints`:
   - odczytuje/generuje `X-Correlation-Id`,
   - binduje `tripId` z route,
   - binduje body do `PatchTripCommandRequest`,
   - pobiera `userId` z kontekstu auth; tymczasowo, jesli repo utrzymuje obecny wzorzec developerski, moze uzyc `DevelopmentUserId`.
3. Endpoint wysyla `PatchTripCommand` przez `IMediator`.
4. `ValidationBehavior` uruchamia FluentValidation dla sprawdzen strukturalnych:
   - poprawny `tripId`,
   - dozwolone enumy,
   - dodatnie liczby,
   - poprawne elementy `tags`.
5. Handler pobiera `Trip` po `tripId` i `userId` oraz dolacza `TripTags`.
6. Jesli encja nie istnieje, handler zwraca `Result.Fail(TRIP_NOT_FOUND)`.
7. Handler lub `ITripUpdateService` buduje stan wynikowy:
   - bierze aktualne wartosci z encji,
   - nadpisuje tylko pola oznaczone jako obecne,
   - dla pol nullable respektuje jawne `null`,
   - dla `tags` wykonuje replace calej kolekcji tylko wtedy, gdy `tags` zostalo przekazane.
8. Po mergu uruchamiana jest walidacja biznesowa calego finalnego stanu:
   - `dateTo >= dateFrom` jesli obie daty sa obecne,
   - `stayLengthMaxDays >= stayLengthMinDays` jesli obie wartosci sa obecne,
   - `peopleCount > 0` jesli ustawione,
   - co najmniej jedno z `placeText`, `noteText`, `tags` pozostaje uzupelnione po zmianie.
9. Jesli `tags` zostaly przekazane:
   - jednym zapytaniem pobierz wszystkie wskazane `tagId`,
   - brakujace `tagId` mapuj do `400 VALIDATION_ERROR`, bo spec PATCH nie przewiduje `TAG_NOT_FOUND`,
   - zsynchronizuj rekordy `trip_tags` bez N+1.
10. Zapisz zmiany przez EF Core i zwroc `UpdateTripCommandResponse`.

## 5. Wzgledy bezpieczenstwa
- Docelowo endpoint powinien byc chroniony JWT, zgodnie z `.cursor/rules/shared.mdc`.
- Ownership musi byc sprawdzany w handlerze: zapytanie po `Trip` powinno filtrowac po `Id` i `UserId`.
- `tripId` przyjmowany jest tylko z route; nigdy z body.
- Nie wolno dynamicznie mapowac pol bez allow-listy dla enumow i tagow.
- Nie logowac pelnej tresci `noteText`; logowac `tripId`, `userId`, `traceId`, `correlationId`.
- Wszystkie operacje DB musza byc async i przyjmowac `CancellationToken`.
- PATCH jest szczegolnie podatny na przypadkowe "mass assignment", dlatego model patch powinien jawnie definiowac tylko dozwolone pola, bez przyjmowania arbitralnego JSON.
-WAZNE!!! na razie pomin chronienir endpoint bedzie na dalszym etapie

## 6. Obsluga bledow
- `400 VALIDATION_ERROR`
  - body nie daje sie zdeserializowac
  - `tripId` jest pusty lub niepoprawny
  - `title` przeslane jako pusty string
  - `stayLengthMinDays <= 0`, `stayLengthMaxDays <= 0`, `peopleCount <= 0`
  - `dateTo < dateFrom` po zmergowaniu danych
  - `stayLengthMaxDays < stayLengthMinDays` po zmergowaniu danych
  - po aktualizacji nie zostaje zadne z: `placeText`, `noteText`, `tags`
  - `tags` zawiera duplikaty `tagId`
  - `tags` zawiera nieistniejace `tagId` i chcemy pozostac zgodni ze spec PATCH
  - klient probuje ustawic `null` dla pola, ktore kontraktowo nullable nie jest
- `404 TRIP_NOT_FOUND`
  - rekord nie istnieje
  - rekord nalezy do innego usera
- `401 UNAUTHORIZED`
  - brak lub niepoprawny token, po wlaczeniu auth
- `500 INTERNAL_ERROR`
  - bledy EF Core, constrainty, wyjatki nieoczekiwane

### Rejestrowanie bledow
Aktualny plan DB nie zawiera dedykowanej tabeli bledow. `audit_event` jest opcjonalny i nie jest dobrym miejscem na techniczne exception logi. Dlatego:
- nie dodawac osobnej tabeli bledow tylko na potrzeby tego endpointu,
- wykorzystywac `ILogger` + `traceId` + `X-Correlation-Id`,
- pozostawic `ExceptionHandlingMiddleware` jako warstwe dla nieoczekiwanych `500`.

## 7. Wydajnosc
- Pobieraj `Trip` razem z `TripTags` w jednym zapytaniu, aby uniknac dodatkowych round-tripow.
- Weryfikacje tagow wykonuj jednym `WHERE id IN (...)`.
- Synchronizacje tagow wykonuj jako diff lub "replace set" w pamieci, a nie przez wiele pojedynczych zapisow.
- Nie przebudowuj encji, ktore nie ulegly zmianie; ograniczy to niepotrzebne update'y i churn w `updated_at`.
- Jesli handler bedzie ladowal tagi z `Include`, zadbaj o brak zbednych nawigacji poza potrzebnym zakresem.
- Dla testow i dalszego rozwoju warto utrzymac spojne indeksy z `.ai/db_plan.md` dla `trip` i `trip_tag`, ale sam PATCH nie wymaga nowych indeksow poza juz planowanymi.

## 8. Kroki implementacji
1. Wprowadz osobny kontrakt dla PATCH
   - Nie binduj bezposrednio `UpdateTripCommandModel` z requestu HTTP.
   - Dodaj `PatchTripCommandModel` z wrapperem obecnosci pola (`OptionalField<T>` / `PatchField<T>`).
   - Zachowaj `UpdateTripCommandModel` tylko jako model wewnetrzny po mergu, jesli to upraszcza implementacje.
2. Popraw warstwe Application
   - Zamien obecny szkic `UpdateTripCommand` na poprawny MediatR request:
     `IRequest<Result<UpdateTripCommandResponse>>`.
   - Dodaj `UserId` do commandu, analogicznie do `CreateTripCommand`.
   - Dodaj walidatory dla route + body + elementow `tags`.
3. Dodaj blad domenowy i mapowanie HTTP
   - Rozszerz `ResultErrors` o `TRIP_NOT_FOUND` ze statusem `404`.
   - Dla brakujacych `tagId` uzyj `VALIDATION_ERROR`, aby pozostac zgodnym ze spec PATCH.
4. Dodaj logike mergowania i walidacji finalnego stanu
   - Dodaj `Trip.ApplyPatch(...)` albo `ITripUpdateService`.
   - Waliduj nie tylko pola pojedynczo, ale tez finalna spojnosc po zastosowaniu patcha.
   - Ustal jedna jawna semantyke dla `tags` i opisz ja w kodzie/testach.
5. Rozszerz encje domenowe tylko tam, gdzie to potrzebne
   - Obecna encja `Trip` ma fabryke `Create`, ale nie ma metody update.
   - Dodaj metode aktualizacji ustawiajaca `UpdatedAt` i broniaca niepoprawnego stanu.
   - Jesli potrzeba, dodaj helper do synchronizacji `TripTags`.
6. Zaimplementuj handler
   - Pobierz encje po `tripId + userId`.
   - Zweryfikuj istnienie przekazanych tagow.
   - Zastosuj patch, zapisz zmiany, zmapuj do `TripQueryModel`.
7. Dodaj endpoint Minimal API
   - W `TripsEndpoints` dodaj `MapPatch("/{tripId:guid}", PatchTrip)`.
   - Zwroc `200` na sukces i `ProblemDetails` dla failure przez `ToHttpResult`.
   - Zachowaj obsluge `X-Correlation-Id`.
8. Dodaj testy
   - Integracyjne:
     - poprawny PATCH jednego pola zwraca `200`
     - brak `tripId` / zly payload zwraca `400`
     - `404 TRIP_NOT_FOUND` dla obcego lub nieistniejacego tripa
     - `noteText: null` czysci note
     - brak pola `noteText` nie zmienia note
     - `tags: []` usuwa wszystkie tagi
     - nieistniejacy `tagId` zwraca `400 VALIDATION_ERROR`
     - `dateTo < dateFrom` po mergu zwraca `400`
   - Jednostkowe:
     - wrapper obecnosci pola poprawnie rozroznia brak pola od `null`
     - merge finalnego stanu
     - walidacja regul biznesowych po mergu
