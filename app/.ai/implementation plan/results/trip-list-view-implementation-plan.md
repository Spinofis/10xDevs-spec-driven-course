# Plan implementacji widoku Lista wycieczek

## 1. Przegląd
Widok Lista wycieczek pozwala zalogowanemu użytkownikowi przeglądać zapisane wycieczki, szybko zawężać wyniki po tekście i statusie planu, sortować listę, zmieniać rozmiar strony, przechodzić między stronami paginacji kursorowej oraz usuwać wycieczki po potwierdzeniu. Widok powinien zastąpić obecny placeholder w komponencie `TripListPageComponent` i korzystać z backendowego endpointu `GET /trips`.

Najważniejsze cele widoku:
- pokazać najważniejsze informacje o każdej wycieczce w formie listy lub tabeli,
- utrzymywać stan filtrów i sortowania w URL,
- otwierać szczegóły wycieczki po kliknięciu w wiersz lub kartę,
- obsługiwać usuwanie przez `DELETE /trips/{tripId}` tak, aby usunięta wycieczka znikała z listy,
- czytelnie prezentować stany loading, empty, error i błędy walidacji query params.

## 2. Routing widoku
Widok jest dostępny pod ścieżką `/trips`.

Routing jest już zdefiniowany w `app/vibe-travelers-ui/src/app/app.routes.ts`:
- `path: 'trips'`,
- `component: TripListPageComponent`,
- `title: 'Wycieczki | VibeTravels'`.

Docelowo komponent `TripListPageComponent` powinien odczytywać query params z URL:
- `q`,
- `hasPlan`,
- `sort`,
- `limit`,
- `cursor`.

Przykładowe adresy:
- `/trips`,
- `/trips?q=Lizbona&hasPlan=true&sort=-createdAt&limit=20`,
- `/trips?sort=title&limit=50&cursor=...`.

Nie należy dodawać osobnej ścieżki dla listy. Nawigacja do szczegółów odbywa się przez `/trips/:tripId/details`.

## 3. Struktura komponentów
Proponowana hierarchia:


```text
TripListPageComponent
├── TripListToolbarComponent
│   ├── TripSearchInputComponent
│   ├── TripHasPlanFilterComponent
│   ├── TripSortSelectComponent
│   └── TripLimitSelectComponent
├── TripListStateBannerComponent
├── TripListComponent
│   ├── TripListRowComponent
│   └── DeleteTripButtonComponent
├── TripListPaginationComponent
└── ConfirmDeleteTripDialogComponent
```

W MVP można wdrożyć część komponentów jako mniejsze sekcje w szablonie `TripListPageComponent`, ale warto wydzielić przynajmniej:
- `TripsApiService` dla komunikacji HTTP,
- `TripListToolbarComponent` dla filtrów,
- `TripListComponent` i `TripListRowComponent` dla wyników,
- `ConfirmDeleteTripDialogComponent` dla potwierdzenia usunięcia.

## 4. Szczegóły komponentów

### TripListPageComponent
- Opis komponentu: kontener widoku `/trips`. Odpowiada za synchronizację query params z formularzem filtrów, pobieranie danych, obsługę paginacji kursorowej, nawigację do szczegółów i koordynację usuwania.
- Główne elementy: nagłówek strony, link `Nowa wycieczka`, toolbar filtrów, banner błędu, lista wyników, paginacja, dialog potwierdzenia usunięcia.
- Obsługiwane interakcje: zmiana filtrów, zmiana sortowania, zmiana limitu, kliknięcie w wiersz, przejście dalej, powrót do poprzedniej strony, wyczyszczenie filtrów, otwarcie i zatwierdzenie dialogu delete.
- Obsługiwana walidacja: normalizacja `q` do maksymalnie 200 znaków, dopuszczalne wartości `hasPlan`, `sort`, `limit`, brak ręcznego generowania kursora poza wartością otrzymaną z API.
- Typy: `TripDto`, `ListTripsRequestParams`, `ListTripsResponse`, `TripListFiltersVm`, `TripListPageState`, `ApiErrorVm`.
- Propsy: brak, jest komponentem routowanym.

### TripListToolbarComponent
- Opis komponentu: formularz sterujący wyszukiwaniem, filtrem statusu planu, sortowaniem i limitem wyników.
- Główne elementy: `form`, `input type="search"`, `select` dla `hasPlan`, `select` dla `sort`, `select` dla `limit`, przycisk czyszczenia filtrów widoczny po aktywowaniu któregokolwiek filtra.
- Obsługiwane interakcje: wpisywanie tekstu, zatwierdzenie wyszukiwania, zmiana selectów, reset filtrów.
- Obsługiwana walidacja: `q.length <= 200`; `hasPlan` tylko `'' | 'true' | 'false'`; `sort` tylko `createdAt | -createdAt | generatedAt | -generatedAt | title | -title`; `limit` tylko wartości z zakresu `1..100`, rekomendowane opcje UI: `10`, `20`, `50`, `100`.
- Typy: `TripListFiltersVm`, `TripSortOption`, `TripLimitOption`.
- Propsy: `filters`, `isLoading`, `validationMessage`; zdarzenia `filtersChange`, `clearFilters`.

### TripListComponent
- Opis komponentu: prezentuje kolekcję wycieczek jako tabelę na desktopie lub listę kart na węższych ekranach. Powinien być zoptymalizowany pod szybkie skanowanie danych.
- Główne elementy: tabela lub lista `article`, nagłówki kolumn, `TripListRowComponent`, stan empty.
- Obsługiwane interakcje: kliknięcie w wiersz lub kartę otwiera `/trips/:tripId/details`; kliknięcie delete nie uruchamia nawigacji.
- Obsługiwana walidacja: nie renderuje niepełnych identyfikatorów; dla pól opcjonalnych pokazuje neutralny placeholder, np. `Brak miejsca`, `Brak dat`.
- Typy: `TripListItemVm`.
- Propsy: `items`, `isLoading`, `error`, `onOpenTrip`, `onRequestDelete`.

### TripListRowComponent
- Opis komponentu: pojedynczy element listy z podsumowaniem wycieczki.
- Główne elementy: tytuł, miejsce, skrót notatki z tooltipem, zakres dat, długość pobytu, liczba osób, budżet, tempo, status planu, daty utworzenia i wygenerowania, przycisk delete.
- Obsługiwane interakcje: kliknięcie w obszar wiersza, obsługa `Enter` i `Space` dla otwarcia szczegółów, fokus na linku lub przycisku, otwarcie tooltipa notatki przez hover i focus.
- Obsługiwana walidacja: `id` musi istnieć; teksty długie są skracane wizualnie, ale pełna treść jest dostępna w tooltipie lub `title`; przycisk delete ma `aria-label` zawierający nazwę wycieczki.
- Typy: `TripListItemVm`.
- Propsy: `item`, `isDeleting`, `open`, `requestDelete`.

### DeleteTripButtonComponent
- Opis komponentu: szybka akcja usunięcia wycieczki na liście.
- Główne elementy: `button type="button"` z widoczną etykietą lub ikoną i `aria-label`.
- Obsługiwane interakcje: kliknięcie zatrzymuje propagację zdarzenia z wiersza i otwiera dialog potwierdzenia.
- Obsługiwana walidacja: przycisk jest zablokowany podczas usuwania danego elementu; etykieta dostępności zawiera tytuł, np. `Usuń wycieczkę Lizbona`.
- Typy: `TripListItemVm`.
- Propsy: `tripTitle`, `disabled`, `requestDelete`.

### ConfirmDeleteTripDialogComponent
- Opis komponentu: modal potwierdzenia usunięcia, wymagany przed wykonaniem `DELETE`.
- Główne elementy: natywny `dialog` lub dostępny modal, nagłówek, komunikat z nazwą wycieczki, przycisk anulowania, przycisk potwierdzenia.
- Obsługiwane interakcje: potwierdzenie delete, anulowanie, zamknięcie przez `Escape`, powrót fokusu do przycisku delete po zamknięciu.
- Obsługiwana walidacja: nie pozwala zatwierdzić, gdy `tripId` jest pusty; komunikat zawsze zawiera `title`.
- Typy: `PendingDeleteTripVm`.
- Propsy: `trip`, `isDeleting`, `error`; zdarzenia `confirm`, `cancel`.

### TripListPaginationComponent
- Opis komponentu: obsługuje paginację kursorową z przyciskiem `Dalej` oraz lokalnym stosem poprzednich kursorów dla przycisku `Wstecz`.
- Główne elementy: przyciski `Wstecz` i `Dalej`, informacja o bieżącej stronie, stan disabled.
- Obsługiwane interakcje: przejście do następnej strony przy użyciu `nextCursor`, powrót do poprzedniego kursora, reset stosu po zmianie filtrów/sortowania/limitu.
- Obsługiwana walidacja: `Dalej` aktywny tylko, gdy API zwróci `nextCursor`; `Wstecz` aktywny tylko, gdy stos poprzednich kursorów nie jest pusty; cursor w URL musi odpowiadać bieżącemu sortowaniu.
- Typy: `CursorPageState`.
- Propsy: `nextCursor`, `canGoBack`, `isLoading`, `pageIndex`; zdarzenia `next`, `previous`.

### TripListStateBannerComponent
- Opis komponentu: wspólny komponent komunikatów loading, empty i error.
- Główne elementy: komunikat tekstowy, opcjonalny przycisk `Wyczyść filtry`, opcjonalny przycisk `Spróbuj ponownie`.
- Obsługiwane interakcje: ponowienie pobrania danych, czyszczenie filtrów po błędzie walidacji.
- Obsługiwana walidacja: dla `VALIDATION_ERROR` wynikającego z query params pokazuje jasny komunikat i akcję czyszczenia filtrów.
- Typy: `ApiErrorVm`.
- Propsy: `state`, `message`, `canClearFilters`; zdarzenia `retry`, `clearFilters`.

## 5. Typy
Typy najlepiej umieścić w katalogu domenowym, np. `src/app/trips/trips.models.ts`.

```ts
export type BudgetLevel = 'low' | 'medium' | 'high';
export type Pace = 'relaxed' | 'normal' | 'fast';
export type TripSortField = 'createdAt' | 'generatedAt' | 'title';
export type TripSort = TripSortField | `-${TripSortField}`;
export type HasPlanFilter = '' | 'true' | 'false';
```

### TripDto
DTO zgodne z odpowiedzią API:
- `id: string` - UUID wycieczki,
- `userId: string` - UUID właściciela,
- `title: string`,
- `placeText: string | null`,
- `noteText: string | null`,
- `dateFrom: string | null` - format `YYYY-MM-DD`,
- `dateTo: string | null` - format `YYYY-MM-DD`,
- `stayLengthMinDays: number | null`,
- `stayLengthMaxDays: number | null`,
- `peopleCount: number | null`,
- `budgetLevel: BudgetLevel | null`,
- `pace: Pace | null`,
- `generatedAt: string | null` - timestamp ISO,
- `hasGeneratedPlan: boolean`,
- `createdAt: string` - timestamp ISO,
- `updatedAt: string` - timestamp ISO.

### ListTripsRequestParams
Model query params dla `GET /trips`:
- `q?: string`,
- `hasPlan?: boolean`,
- `includeDeleted?: boolean` - w widoku domyślnie nie wysyłać,
- `limit?: number`,
- `cursor?: string`,
- `sort?: TripSort`.

### ListTripsResponse
Model odpowiedzi:
- `items: TripDto[]`,
- `nextCursor: string | null`.

### TripListFiltersVm
Stan formularza i URL:
- `q: string`,
- `hasPlan: HasPlanFilter`,
- `sort: TripSort`,
- `limit: number`.

Domyślne wartości:
- `q: ''`,
- `hasPlan: ''`,
- `sort: '-createdAt'`,
- `limit: 20`.

### TripListItemVm
Model przygotowany do renderowania:
- `id: string`,
- `title: string`,
- `placeLabel: string`,
- `notePreview: string`,
- `noteFullText: string | null`,
- `dateRangeLabel: string`,
- `stayLengthLabel: string`,
- `peopleCountLabel: string`,
- `budgetLabel: string`,
- `paceLabel: string`,
- `planStatusLabel: string`,
- `planStatusTone: 'success' | 'neutral'`,
- `createdAtLabel: string`,
- `generatedAtLabel: string`,
- `detailsUrl: string`.

### CursorPageState
Stan paginacji:
- `currentCursor: string | null`,
- `nextCursor: string | null`,
- `previousCursors: Array<string | null>`,
- `pageIndex: number`.

Pierwsza strona ma `currentCursor: null`. Przy przejściu dalej obecny cursor trafia na `previousCursors`, a `currentCursor` przyjmuje `nextCursor`.

### TripListPageState
Stan ekranu:
- `items: TripListItemVm[]`,
- `isLoading: boolean`,
- `error: ApiErrorVm | null`,
- `filters: TripListFiltersVm`,
- `pagination: CursorPageState`,
- `deletingTripId: string | null`,
- `pendingDelete: PendingDeleteTripVm | null`.

### PendingDeleteTripVm
Model dialogu delete:
- `id: string`,
- `title: string`.

### ApiErrorVm
Ujednolicony model błędu UI:
- `code: 'VALIDATION_ERROR' | 'NOT_FOUND' | 'UNAUTHORIZED' | 'UNKNOWN'`,
- `message: string`,
- `field?: string`,
- `correlationId?: string`,
- `canClearFilters: boolean`.

## 6. Zarządzanie stanem
Rekomendowane jest użycie customowego hooka w stylu Angular service/facade, np. `TripListStore` lub `useTripList` jako injectable service dostarczony na poziomie strony. W Angularze 22 można oprzeć stan o signals:
- `signal<TripListFiltersVm>()` dla filtrów,
- `signal<CursorPageState>()` dla paginacji,
- `signal<boolean>()` dla loading,
- `signal<ApiErrorVm | null>()` dla błędów,
- `signal<string | null>()` dla `deletingTripId`,
- `computed<TripListItemVm[]>()` dla danych po mapowaniu z DTO.

Źródłem prawdy dla filtrów powinien być URL. Komponent przy inicjalizacji:
1. odczytuje `ActivatedRoute.queryParamMap`,
2. parsuje i normalizuje wartości,
3. aktualizuje stan formularza,
4. pobiera listę przez API.

Zmiana `q`, `hasPlan`, `sort` lub `limit`:
- aktualizuje query params przez `Router.navigate`,
- usuwa `cursor`,
- czyści `previousCursors`,
- wraca na pierwszą stronę,
- uruchamia nowe pobranie danych.

Dla pola `q` warto zastosować debounce, np. `300 ms`, oraz `distinctUntilChanged`, aby nie wysyłać zapytania po każdym znaku bez potrzeby. Jeśli zespół chce prostszy MVP, można pobierać dane dopiero po submit formularza wyszukiwania.

## 7. Integracja API
Należy dodać `provideHttpClient()` w `app.config.ts`, jeśli nie jest jeszcze skonfigurowany.

### GET /trips
Wywołanie:

```http
GET /trips?q={q}&hasPlan={true|false}&limit={1..100}&cursor={cursor}&sort={sort}
```

Parametry:
- `q` - opcjonalne wyszukiwanie po `title`, `placeText`, `noteText`, maksymalnie 200 znaków,
- `hasPlan` - opcjonalne `true` lub `false`, mapowane na `hasGeneratedPlan`,
- `includeDeleted` - domyślnie `false`; w tym widoku nie wysyłać,
- `limit` - domyślnie 20, API akceptuje `1..100`,
- `cursor` - wartość zwrócona przez API, zależna od sortowania,
- `sort` - `createdAt`, `generatedAt`, `title`, z opcjonalnym prefiksem `-` dla sortowania malejącego.

Odpowiedź `200`:

```ts
interface ListTripsResponse {
  items: TripDto[];
  nextCursor: string | null;
}
```

Błędy:
- `400 VALIDATION_ERROR` - niepoprawny `sort`, `cursor`, `limit` lub za długi `q`,
- `401` - brak autoryzacji, docelowo przekierowanie do logowania,
- pozostałe - komunikat ogólny z możliwością ponowienia.

### DELETE /trips/{tripId}
Wywołanie:

```http
DELETE /trips/{tripId}
```

Odpowiedź sukcesu:
- `204 No Content`.

Obsługa po sukcesie:
- zamknąć dialog,
- usunąć element z widocznej listy optymistycznie albo ponownie pobrać bieżącą stronę,
- jeśli bieżąca strona stanie się pusta i istnieje możliwość cofnięcia, wrócić do poprzedniej strony albo odświeżyć pierwszą stronę.

Błędy:
- `404` - wycieczka nie istnieje lub została już usunięta; pokazać komunikat i odświeżyć listę,
- `400` - nieprawidłowy identyfikator,
- `401` - brak autoryzacji,
- pozostałe - pozostawić dialog otwarty i umożliwić ponowienie.

## 8. Interakcje użytkownika
- Użytkownik wpisuje tekst w wyszukiwarce: po debounce albo submit aktualizuje się `q` w URL i lista pobiera dane od pierwszej strony.
- Użytkownik wybiera status planu: `hasPlan=true`, `hasPlan=false` albo brak parametru dla wszystkich.
- Użytkownik zmienia sortowanie: URL otrzymuje nowe `sort`, cursor jest czyszczony, stos poprzednich cursorów jest resetowany.
- Użytkownik zmienia limit: URL otrzymuje nowe `limit`, cursor jest czyszczony, lista wraca na pierwszą stronę.
- Użytkownik klika w wiersz lub kartę: aplikacja przechodzi do `/trips/:tripId/details`.
- Użytkownik przechodzi dalej: komponent ustawia `cursor=nextCursor` i zapisuje poprzedni cursor w lokalnym stosie.
- Użytkownik przechodzi wstecz: komponent przywraca ostatni cursor ze stosu i aktualizuje URL.
- Użytkownik klika delete: otwiera się dialog z nazwą wycieczki.
- Użytkownik potwierdza delete: wywoływane jest `DELETE /trips/{tripId}`, element znika z listy.
- Użytkownik trafia na błąd walidacji query params: widzi komunikat oraz akcję `Wyczyść filtry`, która usuwa `q`, `hasPlan`, `sort`, `limit`, `cursor` i ładuje domyślną listę.

## 9. Warunki i walidacja
- `q` ma maksymalnie 200 znaków. UI powinno ograniczać długość przez `maxlength="200"` i dodatkowo walidować wartość przed wysłaniem.
- Pusty lub składający się z białych znaków `q` nie powinien być wysyłany do API.
- `hasPlan` może być tylko `true`, `false` albo puste. Niepoprawną wartość z URL należy potraktować jako błąd walidacji UI albo wyczyścić filtr.
- `limit` musi mieścić się w zakresie `1..100`. Select powinien udostępniać tylko poprawne wartości.
- `sort` musi być jedną z wartości: `createdAt`, `-createdAt`, `generatedAt`, `-generatedAt`, `title`, `-title`.
- `cursor` jest nieprzezroczysty dla frontendu. Nie wolno go parsować ani modyfikować; należy przekazywać wyłącznie wartość zwróconą przez API.
- Cursor zależy od sortowania. Po zmianie `sort`, `q`, `hasPlan` lub `limit` trzeba usunąć `cursor`.
- `includeDeleted` nie jest częścią UI listy w MVP, ponieważ widok ma pokazywać aktywne wycieczki.
- Usunięcie wymaga potwierdzenia zawierającego tytuł wycieczki.
- Przycisk delete musi mieć dostępną etykietę i nie może być jedynym elementem fokusu w wierszu.
- Długi `noteText` powinien być skrócony w widoku listy, a pełna treść dostępna przez tooltip lub inny dostępny mechanizm.

## 10. Obsługa błędów
- Loading pierwszego pobrania: pokazać skeleton albo komunikat ładowania w obszarze wyników.
- Loading po zmianie filtrów: pozostawić toolbar aktywny, zablokować paginację i delete.
- Empty bez filtrów: pokazać komunikat, że nie ma jeszcze zapisanych wycieczek, oraz link `Nowa wycieczka`.
- Empty z filtrami: pokazać komunikat, że nic nie pasuje do filtrów, oraz przycisk `Wyczyść filtry`.
- `VALIDATION_ERROR` dla query params: pokazać komunikat, nie wykonywać kolejnych żądań z tymi parametrami, udostępnić czyszczenie filtrów.
- Błąd sieci lub `5xx`: pokazać komunikat ogólny i przycisk `Spróbuj ponownie`.
- `401`: docelowo przekierować do logowania; na etapie PoC można pokazać komunikat o braku dostępu.
- `404` po delete: potraktować jako stan rozbieżności, odświeżyć listę i pokazać krótki komunikat.
- Błąd delete: pozostawić element na liście, odblokować przycisk i umożliwić ponowienie.
- Niepoprawne daty lub wartości null w DTO: formatery ViewModel powinny zwracać neutralne etykiety zamiast rzucać wyjątek.

## 11. Kroki implementacji
1. Dodać konfigurację HTTP w `app.config.ts` przez `provideHttpClient()`, jeśli nie została wcześniej dodana.
2. Utworzyć modele w `src/app/trips/trips.models.ts`: `TripDto`, `ListTripsRequestParams`, `ListTripsResponse`, `TripListFiltersVm`, `TripListItemVm`, `CursorPageState`, `PendingDeleteTripVm`, `ApiErrorVm`.
3. Utworzyć `TripsApiService` z metodami `listTrips(params: ListTripsRequestParams)` i `deleteTrip(tripId: string)`.
4. Dodać mapper DTO do ViewModel, np. `mapTripDtoToListItemVm`, z formatterami dat, długości pobytu, budżetu, tempa i statusu planu.
5. Rozbudować `TripListPageComponent` o odczyt query params, stan loading/error, pobieranie danych, obsługę paginacji i delete.
6. Zaimplementować synchronizację filtrów z URL: każda zmiana filtrów usuwa `cursor` i resetuje stos poprzednich cursorów.
7. Zaimplementować `TripListToolbarComponent` albo równoważną sekcję formularza w stronie z walidacją `q`, `hasPlan`, `sort`, `limit`.
8. Zaimplementować `TripListComponent` i `TripListRowComponent` z dostępnością klikalnych wierszy oraz oddzielną akcją delete.
9. Zaimplementować tooltip lub dostępny mechanizm pokazania pełnego `noteText` przy skróconej notatce.
10. Zaimplementować `ConfirmDeleteTripDialogComponent` z nazwą wycieczki w treści potwierdzenia i blokadą przycisków podczas żądania.
11. Zaimplementować `TripListPaginationComponent` obsługujący `nextCursor`, lokalny stos poprzednich cursorów i stan disabled podczas ładowania.
12. Zaimplementować komponent lub sekcję stanów loading, empty i error, w tym specjalną obsługę `VALIDATION_ERROR` z przyciskiem `Wyczyść filtry`.
13. Dopasować style do istniejących klas globalnych (`page`, `page-header`, `panel`, `button`, `badge`, `trip-row`) oraz stacku Angular + TypeScript + Tailwind/Sass, bez rozbijania obecnego języka wizualnego aplikacji.
14. Dodać testy jednostkowe mapperów i parsera query params: limity, sortowanie, `hasPlan`, `q`, wartości null w DTO.
15. Dodać testy komponentu lub integracyjne dla scenariuszy: pierwsze pobranie, filtracja, reset filtrów, paginacja dalej/wstecz, delete success, delete error, `VALIDATION_ERROR`.
16. Zweryfikować ręcznie `/trips`: odświeżenie strony odtwarza filtry z URL, kliknięcie w element otwiera szczegóły, delete nie wywołuje nawigacji, a stany loading/empty/error są czytelne.
