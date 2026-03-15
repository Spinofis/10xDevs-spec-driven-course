# API Endpoint Implementation Plan: GET /generation-jobs/{jobId}

## 1. Przeglad punktu koncowego
Endpoint sluzy do odpytywania o aktualny stan asynchronicznego zadania generacji AI po jego zakolejkowaniu. Jest to endpoint typu query/read i zgodnie z kontraktem ma zwracac aktualny stan joba zapisany w bazie, bez wykonywania jakiejkolwiek logiki generacji w pipeline HTTP.

Najwazniejsze cechy wdrozenia:
- endpoint jest niezalezny od zasobu `trip` w URL, ale nadal musi egzekwowac ownership po relacji `job -> trip/user`;
- odpowiedz ma odzwierciedlac persisted state z tabeli jobow, a nie stan wyliczany w locie;
- aktualne repo zawiera juz szkic kontraktow `GetGenerationJobByIdQuery`, `GetGenerationJobByIdQueryRequest`, `GetGenerationJobByIdQueryResponse` oraz `GenerationJobQueryModel`;
- obecny kod ma rozjazd nazewniczy miedzy specyfikacja a domena: domena pracuje na statusach `pending/running`, a API ma zwracac `queued/processing`;
- model query juz przewiduje pola `discarded` i `discardReason`, ale encja `AiGenerationJob` i konfiguracja EF ich jeszcze nie posiadaja, wiec implementacja endpointu wymaga domkniecia modelu danych.

## 2. Szczegoly zadania
- Metoda HTTP: `GET`
- Struktura URL: `/generation-jobs/{jobId}`
- Naglowki:
  - docelowo wymagane: `Authorization: Bearer <token>`
  - opcjonalne: `X-Correlation-Id`
- Parametry:
  - wymagane: `jobId` (`Guid`) w route
  - opcjonalne: brak
- Request Body:
  - brak

### Wymagane reguly walidacji
- `jobId` nie moze byc pustym `Guid`
- rekord joba musi istniec
- rekord joba musi nalezec do aktualnego usera; dla cudzego joba endpoint powinien zwrocic `404 JOB_NOT_FOUND`, nie `403`
- jesli w modelu persistence pozostaja statusy domenowe `pending/running`, odpowiedz API musi je mapowac odpowiednio na `queued/processing`
- pola czasowe i bledy musza byc odczytywane bezposrednio z rekordu joba
- jesli backend wspiera logike discardowania wyniku starszego joba, odpowiedz musi expose'owac `discarded` i `discardReason`; jesli nie, plan wdrozenia musi objac dodanie tych pol do persistence

### Wymagane typy i modele
- istniejace do wykorzystania lub dopracowania:
  - `GetGenerationJobByIdQuery`
  - `GetGenerationJobByIdQueryRequest`
  - `GetGenerationJobByIdQueryResponse`
  - `GenerationJobQueryModel`
- rekomendowane nowe lub brakujace elementy:
  - `GetGenerationJobByIdQueryValidator`
  - `GetGenerationJobByIdQueryHandler`
- typy domenowe / persistence zaangazowane w odczyt:
  - `AiGenerationJob`
  - `AiGenerationJobStatus`
  - `IAppDbContext`
- typy pomocnicze do mapowania kontraktu:
  - `GenerationJobStatus` z warstwy Application
  - `ResultErrors.JobNotFound(...)` lub analogiczny stabilny blad `404`

### Wyodrebnienie logiki do service
Ten endpoint nie wymaga osobnego serwisu aplikacyjnego, jesli jego odpowiedzialnosc pozostanie prosta:
- handler pobiera rekord joba z DB z filtrem ownership
- handler mapuje encje domenowa na `GenerationJobQueryModel`
- handler zwraca `Result<GetGenerationJobByIdQueryResponse>`

Osobny serwis ma sens tylko wtedy, gdy zespol chce wspoldzielic logike mapowania statusow i projection miedzy:
- `GET /generation-jobs/{jobId}`
- `GET /trips/{tripId}/generation-jobs`
- ewentualnie workerem aktualizujacym rekord joba

W takim przypadku warto wydzielic niewielki komponent, np. `IGenerationJobReadModelMapper`, zamiast budowac ciezki service domenowy.

## 3. Szczegoly odpowiedzi
- `200 OK`
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
- Naglowki odpowiedzi:
  - `X-Correlation-Id`
- Kody statusu:
  - `200` po poprawnym odczycie joba
  - `400 VALIDATION_ERROR` dla bledow technicznych requestu, np. pusty `jobId`
  - `401 UNAUTHORIZED` docelowo po wlaczeniu auth
  - `404 JOB_NOT_FOUND` gdy job nie istnieje lub nie nalezy do usera
  - `500 INTERNAL_ERROR` dla bledow nieoczekiwanych

Rekomendacja kontraktowa:
- endpoint powinien zwracac bezposrednio model joba zgodny ze specyfikacja 7.2, a nie obudowe `{ "job": ... }`, bo spec dla tego endpointu pokazuje plaski obiekt;
- `GetGenerationJobByIdQueryResponse` warto uproscic do ksztaltu bezposrednio zgodnego z wire contract, albo endpoint powinien jawnie rozpakowywac `response.Job`.

## 4. Przeplyw danych
1. Klient wywoluje `GET /generation-jobs/{jobId}` z JWT i opcjonalnym `X-Correlation-Id`.
2. Minimal API:
   - odczytuje lub generuje `X-Correlation-Id`,
   - binduje `jobId` z route,
   - pobiera `userId` z auth; tymczasowy `DevelopmentUserId` moze pozostac tylko jako etap przejsciowy.
3. Endpoint wysyla `GetGenerationJobByIdQuery` przez `IMediator`.
4. `GetGenerationJobByIdQueryValidator` wykonuje walidacje skladniowa:
   - `JobId != Guid.Empty`.
5. `GetGenerationJobByIdQueryHandler` pobiera rekord joba z bazy:
   - filtrowanie po `jobId`,
   - filtrowanie po ownership przez `job.UserId == currentUserId` albo przez dolaczenie `Trip` i sprawdzenie `Trip.UserId`.
6. Jesli rekord nie istnieje, handler zwraca `Result.Fail(ResultErrors.JobNotFound(...))`.
7. Handler mapuje rekord `AiGenerationJob` na `GenerationJobQueryModel`:
   - `pending -> queued`
   - `running -> processing`
   - pozostale statusy bez zmian semantycznych
8. Handler mapuje pola techniczne:
   - `RequestedAt`
   - `StartedAt`
   - `FinishedAt`
   - `AttemptNo`
   - `ErrorCode`
   - `ErrorMessage`
   - `Discarded`
   - `DiscardReason`
9. Endpoint zwraca `200 OK` z plaskim obiektem JSON zgodnym ze spec.

### Zapytanie do bazy
Najprostsza i wystarczajaca implementacja MVP:
- jedno zapytanie `AsNoTracking()`
- projection bez materializacji zbednych encji
- filtr ownership juz w zapytaniu, np. po `job.UserId`

Jesli encja `AiGenerationJob` nie ma jeszcze `UserId`, plan wdrozenia musi to rozstrzygnac. Aktualny `db_plan` przewiduje `user_id` w `ai_generation_job`, ale obecna encja i konfiguracja EF go nie zawieraja. Najlepsza opcja:
- dodac `UserId` do encji i konfiguracji
- ustawic go juz na etapie `POST /trips/{tripId}/generation-jobs`
- dzieki temu odczyt joba jest prosty i nie wymaga joinu do `Trip`

## 5. Wzgledy bezpieczenstwa
- Endpoint powinien byc chroniony JWT Bearer; publiczny odczyt statusu joba jest niedopuszczalny.
- Ownership musi byc egzekwowany po `jobId + userId`; nie wolno zwracac informacji, czy cudzy `jobId` istnieje.
- Odpowiedz nie powinna ujawniac `request_payload`, `response_payload`, `input_snapshot` ani innych danych wewnetrznych joba.
- `errorMessage` moze zawierac dane techniczne; worker i handler powinny pilnowac, by nie przechowywac w nim sekretow, promptow i tokenow.
- Logi endpointu nie powinny dumpowac pelnej encji joba ani payloadow AI.
- Wszystkie operacje I/O musza byc async i przyjmowac `CancellationToken`.
- `X-Correlation-Id` powinien byc zachowany w odpowiedzi, aby laczyc odczyt statusu z requestem tworzenia joba i logami workera.

## 6. Obsluga bledow
- `400 VALIDATION_ERROR`
  - `jobId` jest pustym `Guid`
  - binder nie potrafi sparsowac route param
- `404 JOB_NOT_FOUND`
  - rekord nie istnieje
  - rekord nalezy do innego usera
- `401 UNAUTHORIZED`
  - brak lub niepoprawny token po wlaczeniu auth
- `500 INTERNAL_ERROR`
  - nieoczekiwany blad EF Core
  - niespojnosc danych w kolumnie `status`
  - inny nieobslugiwany blad runtime

### Rejestrowanie bledow
Ten endpoint jest read-only, wiec nie wymaga zapisu do osobnej tabeli bledow. Rekomendacja:
- dla bledow requestu korzystac z `Result` + `ProblemDetails`
- dla bledow samego procesu generacji korzystac z pol `error_code` i `error_message` w tabeli jobow
- dla bledow nieoczekiwanych polegac na `ExceptionHandlingMiddleware` i standardowym logowaniu aplikacji

### Wymagane rozszerzenia `ResultErrors`
Nalezy potwierdzic, ze warstwa domenowa/aplikacyjna ma stabilny blad:
- `JOB_NOT_FOUND` ze statusem `404`

Jesli takiego helpera jeszcze nie ma, trzeba go dodac obok:
- `TRIP_NOT_FOUND`
- `JOB_ALREADY_ACTIVE`
- `GENERATION_REQUIREMENTS_NOT_MET`

## 7. Wydajnosc
- Uzywac `AsNoTracking()` dla odczytu statusu joba.
- Wykonac pojedynczy select z projection do read modelu zamiast pobierania encji i mapowania po stronie aplikacji, jesli zespol chce ograniczyc overhead.
- Utrzymac indeks po `id` jako PK; to wystarczy dla lookupu po `jobId`.
- Jesli ownership jest sprawdzany po `userId`, warto miec indeks wspierajacy odczyty listujace joby, ale dla pojedynczego `jobId` lookup po PK pozostaje dominujacy.
- Endpoint nie powinien wykonywac joinow ani ladowac `Trip`, jesli `userId` jest przechowywany bezposrednio na `ai_generation_job`.
- Odczyt statusu ma pozostac tani i czesty, bo z zalozenia sluzy do pollingu.

## 8. Kroki implementacji
1. Uporzadkowac kontrakt persistence dla joba
   - porownac spec 7.2, `GenerationJobQueryModel`, encje `AiGenerationJob` i konfiguracje EF
   - dodac brakujace pola `AttemptNo`, `ErrorCode`, `Discarded`, `DiscardReason`, `UserId`, jesli nie sa jeszcze mapowane
   - utrzymac kompatybilnosc ze statusem domenowym i mapowaniem do statusu API
2. Dodac lub dopracowac blad domenowy
   - upewnic sie, ze istnieje `JOB_NOT_FOUND` mapowany na `404`
   - zadbac, by ownership dla cudzego joba tez konczylo sie `JOB_NOT_FOUND`
3. Dodac walidacje query
   - utworzyc `GetGenerationJobByIdQueryValidator`
   - zwalidowac `JobId != Guid.Empty`
4. Zaimplementowac handler MediatR
   - dodac `GetGenerationJobByIdQueryHandler`
   - wykonac `AsNoTracking()` query po `jobId` i `userId`
   - zwrocic `Result<GetGenerationJobByIdQueryResponse>` albo bezposredni model, zgodnie z przyjeta konwencja w projekcie
   - zmapowac domenowe statusy i pola odpowiedzi
5. Dodac endpoint Minimal API
   - utworzyc nowa grupe endpointow, np. `GenerationJobsEndpoints`, albo dopiac route globalnie poza `/trips`
   - dodac `GET /generation-jobs/{jobId:guid}`
   - zachowac obsluge `X-Correlation-Id`
   - dodac `.Produces<...>(200)`, `.ProducesProblem(400)`, `.ProducesProblem(401)`, `.ProducesProblem(404)`
6. Uporzadkowac shape odpowiedzi HTTP
   - upewnic sie, ze wire contract odpowiada plaskiemu JSON ze specyfikacji
   - jesli `GetGenerationJobByIdQueryResponse` opakowuje `Job`, endpoint powinien rozpakowac ten model przed serializacja
7. Rozszerzyc testy jednostkowe
   - validator: pusty `Guid`
   - handler: sukces dla joba nalezacego do usera
   - handler: `JOB_NOT_FOUND` dla nieistniejacego joba
   - handler: `JOB_NOT_FOUND` dla joba innego usera
   - handler: poprawne mapowanie `pending/running` na `queued/processing`
8. Dodac testy integracyjne
   - `GET /generation-jobs/{jobId}` zwraca `200`
   - odpowiedz zawiera `X-Correlation-Id`
   - odpowiedz ma plaski shape zgodny ze spec
   - cudzy job zwraca `404`
   - nieistniejacy job zwraca `404`
   - statusy, pola bledow i pola discard sa poprawnie serializowane
