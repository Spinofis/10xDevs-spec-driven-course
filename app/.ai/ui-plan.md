# Architektura UI dla VibeTravels

## 1. Przegląd struktury UI

VibeTravels MVP jest webową aplikacją desktop-first do planowania wycieczek z pomocą AI. Interfejs powinien być zorganizowany wokół czterech głównych obszarów: listy wycieczek, tworzenia nowej wycieczki, workspace'u pojedynczej wycieczki oraz preferencji użytkownika. Auth jest opisany w PRD i API, ale zgodnie z decyzjami planistycznymi nie jest częścią aktywnego zakresu UI MVP; architektura pozostawia miejsce na późniejsze dodanie ekranów auth, guardów tras i interceptora HTTP.

Główna struktura aplikacji:

- Globalny layout z nawigacją: „Wycieczki”, „Nowa wycieczka”, „Preferencje”.
- Lista wycieczek jako domyślny widok aplikacji, z wyszukiwaniem, filtrowaniem, sortowaniem i paginacją kursorową.
- Formularz nowej wycieczki jako pierwszy krok procesu: użytkownik zapisuje dane wycieczki przez `POST /trips`, a następnie trafia do szczegółów wycieczki.
- Workspace wycieczki z podwidokami:
  - szczegóły i dane wejściowe wycieczki,
  - edytowalny plan wycieczki.
- Osobny ekran preferencji profilu użytkownika.

Kluczowe wymagania PRD uwzględnione w architekturze:

- Użytkownik może zapisać preferencje podróżnicze obejmujące tagi i pola strukturalne.
- Użytkownik może tworzyć, przeglądać, edytować i usuwać wycieczki.
- Lista wycieczek obsługuje filtrowanie, sortowanie i wyszukiwanie.
- Formularz wycieczki zbiera daty, długość pobytu, liczbę osób, budżet, tempo, miejsce, notatkę i tagi.
- Generowanie planu jest możliwe tylko po spełnieniu wymagań generacji.
- Generowanie i regeneracja planu nadpisują poprzedni plan.
- Plan jest widoczny, edytowalny i zapisywany ręcznie.
- Zapis preferencji i zapis planu są kluczowymi zdarzeniami produktu.

Aktualizacja względem PRD wynikająca z API v2 i notatek planowania:

- Plan nie jest renderowany jako HTML ani edytowany jako jeden blok tekstu. UI pracuje na strukturalnym JSON: `trip_plan` oraz `trip_plan_item`.
- Widok planu pokazuje edytowalne `summary` oraz listę dni, gdzie każdy dzień jest osobnym widgetem.
- Elementy planu są grupowane po `dayNumber`, sortowane rosnąco po dniach i po `order` w obrębie dnia.
- Dane wycieczki i plan są pobierane osobno: `GET /trips/{tripId}` oraz `GET /trips/{tripId}/plan`.
- Szczegóły wycieczki i plan są osobnymi podwidokami, ale współdzielą komponent kontekstu wycieczki.

Główne endpointy API i ich cele:

- `GET /tags` - pobranie słownika tagów, cache'owanego w pamięci aplikacji w trakcie sesji.
- `GET /me/profile` - pobranie profilu i preferencji użytkownika.
- `PUT /me/profile` - zapis profilu oraz pełnego zestawu tagów preferencji.
- `POST /trips` - utworzenie nowej wycieczki.
- `GET /trips` - lista wycieczek z `q`, `hasPlan`, `sort`, `limit`, `cursor`.
- `GET /trips/{tripId}` - szczegóły wycieczki wraz z tagami.
- `PATCH /trips/{tripId}` - częściowa aktualizacja wycieczki.
- `DELETE /trips/{tripId}` - miękkie usunięcie wycieczki.
- `POST /trips/{tripId}/generation-jobs` - uruchomienie generowania lub regenerowania planu.
- `GET /generation-jobs/{jobId}` - prosty polling statusu generowania, bez osobnego szczegółowego widoku joba.
- `GET /trips/{tripId}/plan` - pobranie aktualnego planu.
- `PUT /trips/{tripId}/plan` - zapis pełnego, aktualnego obiektu planu po ręcznej edycji.

Zasady UX, dostępności i bezpieczeństwa dla całej aplikacji:

- Każdy główny widok ma stany: ładowanie, pusty stan i błąd.
- Formularze używają wspólnego komunikatu błędu formularza zamiast błędów przy pojedynczych polach.
- Wszystkie pola formularzy mają etykiety, logiczną kolejność tabulacji i widoczny focus.
- Akcje destrukcyjne i nadpisujące wymagają potwierdzenia: usunięcie wycieczki oraz regeneracja planu.
- Filtry listy wycieczek są synchronizowane z query params, bez localStorage.
- Dane wprowadzane przez użytkownika są traktowane jako tekst, bez renderowania HTML z API.
- UI nie ujawnia szczegółów technicznych błędów ani tokenów; komunikaty są czytelne i produktowe.
- Architektura pozostawia miejsce na późniejsze podłączenie JWT, guardów tras i interceptora autoryzacyjnego.

## 2. Lista widoków

### Widok: Lista wycieczek

- Ścieżka widoku: `/trips`
- Główny cel: umożliwić użytkownikowi szybkie odnalezienie, filtrowanie, sortowanie i otwarcie zapisanych wycieczek.
- Kluczowe informacje do wyświetlenia:
  - tytuł wycieczki,
  - miejsce,
  - skrót notatki lub kontekstu,
  - zakres dat,
  - długość pobytu,
  - liczba osób,
  - poziom budżetu i tempo,
  - status posiadania planu,
  - data utworzenia i/lub wygenerowania planu.
- Kluczowe komponenty widoku:
  - pasek wyszukiwania `q`,
  - filtr `hasPlan`,
  - wybór sortowania `createdAt`, `generatedAt`, `title`,
  - selektor limitu wyników,
  - lista lub tabela wycieczek,
  - paginacja kursorowa „Dalej” i ewentualnie lokalny stos poprzednich kursorów dla nawigacji wstecz,
  - przycisk usunięcia wycieczki z potwierdzeniem,
  - komponent loading/empty/error.
- UX, dostępność i względy bezpieczeństwa:
  - kliknięcie w wiersz lub kartę otwiera `/trips/:tripId/details`,
  - jedyną szybką akcją na liście jest delete,
  - stan filtrów jest widoczny w URL i możliwy do odtworzenia po odświeżeniu,
  - przycisk delete ma etykietę dostępną dla czytników i nie jest jedynym elementem fokusu w wierszu,
  - potwierdzenie usunięcia zawiera nazwę wycieczki,
  - błędy `VALIDATION_ERROR` dla query params pokazują komunikat i pozwalają wyczyścić filtry.

### Widok: Nowa wycieczka

- Ścieżka widoku: `/trips/new`
- Główny cel: zebrać dane wejściowe i zapisać nową wycieczkę przed generowaniem planu.
- Kluczowe informacje do wyświetlenia:
  - tytuł,
  - miejsce jako tekst nieustrukturyzowany,
  - notatka,
  - zakres dat,
  - minimalna i maksymalna długość pobytu,
  - liczba osób,
  - budżet,
  - tempo,
  - tagi.
- Kluczowe komponenty widoku:
  - formularz wycieczki,
  - współdzielony wybór tagów,
  - sekcja preferencji wstępnie uzupełniona z profilu,
  - wspólny komunikat błędu formularza,
  - przycisk „Utwórz wycieczkę”.
- UX, dostępność i względy bezpieczeństwa:
  - preferencje użytkownika inicjalizują wyłącznie nową wycieczkę i nie nadpisują istniejących danych,
  - po sukcesie `POST /trips` użytkownik przechodzi do `/trips/:tripId/details`,
  - formularz waliduje wymagania tworzenia: tytuł, daty, długość pobytu, liczba osób,
  - UI może sygnalizować, czy dane spełniają ostrzejsze wymagania generacji: notatka, miejsce albo co najmniej dwa tagi,
  - podczas zapisu przycisk jest zablokowany,
  - błąd `TAG_NOT_FOUND` jest pokazany jako ogólny błąd formularza i sugeruje odświeżenie listy tagów.

### Widok: Szczegóły wycieczki

- Ścieżka widoku: `/trips/:tripId/details`
- Główny cel: pokazać i umożliwić edycję danych wejściowych wycieczki oraz uruchomienie generowania planu.
- Kluczowe informacje do wyświetlenia:
  - pełny kontekst wycieczki z `GET /trips/{tripId}`,
  - tagi przypisane do wycieczki,
  - informacja, czy plan został już wygenerowany,
  - data ostatniej generacji,
  - podstawowy stan generowania.
- Kluczowe komponenty widoku:
  - współdzielony komponent kontekstu wycieczki,
  - formularz edycji danych wycieczki,
  - współdzielony wybór tagów,
  - przycisk zapisu zmian danych wycieczki,
  - przycisk „Generuj plan” albo „Regeneruj plan”,
  - podsumowanie wymagań generacji pokazujące listę braków lub błędów,
  - dialog potwierdzenia regeneracji,
  - prosty baner lub inline status oczekiwania po uruchomieniu joba,
  - link/przycisk do podwidoku planu.
- UX, dostępność i względy bezpieczeństwa:
  - `PATCH /trips/{tripId}` aktualizuje lokalny stan bez ponownego pobierania szczegółów, także po zmianie tagów,
  - generowanie jest osobnym krokiem po zapisaniu wycieczki,
  - UI waliduje lokalnie te same podstawowe wymagania generacji co API: obecność i poprawność zakresu dat, długość pobytu w zakresie 2-21 dni, `stayLengthMaxDays >= stayLengthMinDays`, `peopleCount > 0` oraz minimum jedno źródło kontekstu: notatka, miejsce albo co najmniej dwa tagi,
  - jeśli lokalne wymagania generacji nie są spełnione, UI blokuje generowanie i pokazuje listę braków w podsumowaniu wymagań,
  - jeśli API zwróci `GENERATION_REQUIREMENTS_NOT_MET` albo `VALIDATION_ERROR`, komunikaty z API są pokazane w tym samym podsumowaniu jako lista błędów,
  - regeneracja wymaga potwierdzenia, bo nadpisuje poprzedni plan,
  - `JOB_ALREADY_ACTIVE` pokazuje informację, że generowanie już trwa,
  - `TRIP_NOT_FOUND` prowadzi do stanu braku dostępu lub usuniętej wycieczki, z linkiem powrotu do listy,
  - formularz nie pokazuje technicznych szczegółów walidacji przy polach, tylko jedno wspólne podsumowanie błędów, które może zawierać listę komunikatów.

### Widok: Plan wycieczki

- Ścieżka widoku: `/trips/:tripId/plan`
- Główny cel: pokazać aktualny plan wycieczki w czytelnej formie oraz umożliwić przejście do edycji planu bez zmiany adresu widoku.
- Kluczowe informacje do wyświetlenia:
  - kontekst wycieczki z `GET /trips/{tripId}`,
  - `summary` planu,
  - data wygenerowania planu,
  - informacja o niezapisanych zmianach tylko w trybie edycji,
  - lista dni posortowanych po `dayNumber`,
  - elementy dnia posortowane po `order`,
  - dla każdego elementu: tytuł, opis, lokalizacja, data/czas, kolejność, numer dnia i typ miejsca (jako taka lista widgetow dnia i jego elementow).
- Kluczowe komponenty widoku:
  - współdzielony komponent kontekstu wycieczki,
  - prezentacyjny widok planu jako domyślny tryb `read`,
  - przycisk „Edytuj plan” przełączający lokalny tryb widoku na `edit`,
  - czytelne `summary` nad listą dni,
  - lista kart dni w trybie podglądu,
  - prezentacyjne elementy planu pokazujące kolejność, typ miejsca, datę/czas, lokalizację, tytuł i opis,
  - formularz edycji planu widoczny tylko w trybie `edit`,
  - edytowalne pole `summary` w formularzu,
  - edytowalne elementy planu w formularzu,
  - podsumowanie błędów edycji planu pokazujące listę lokalnych błędów walidacji lub błędów z API,
  - wizualne oznaczenie typu miejsca: atrakcja, restauracja, hotel,
  - pasek akcji edycji z przyciskami „Zapisz” i „Anuluj” - pokazuja sie tylko w trybie edycji,
  - przycisk regeneracji z potwierdzeniem i tym samym podsumowaniem wymagań generacji, które działa w szczegółach wycieczki,
  - stany loading/empty/error.
- UX, dostępność i względy bezpieczeństwa:
  - widok planu domyślnie działa w trybie `read`, aby plan był łatwy do czytania i wizualnie spokojniejszy,
  - tryb edycji jest lokalnym stanem komponentu pod tym samym adresem `/trips/:tripId/plan`; nie wymaga osobnej trasy ani query params,
  - po kliknięciu „Edytuj plan” prezentacyjny widok planu zostaje ukryty, a w jego miejscu pojawia się formularz edycji,
  - zapis edycji wysyła pełny aktualny obiekt przez `PUT /trips/{tripId}/plan`,
  - formularz edycji planu waliduje lokalnie podstawowe wymagania zapisu zgodne z API: `items` nie może być puste, `items[].id` jest wymagane, `dayNumber >= 1`, `order >= 0`, `itemDate` jest wymagane, `title` nie może być puste, `createdAt` i `updatedAt` są wymagane, `createdAt <= updatedAt`, `placeType` musi być jednym z `attraction`, `restaurant`, `hotel`, a identyfikatory elementów nie mogą się powtarzać,
  - jeśli lokalna walidacja planu nie przechodzi, UI blokuje zapis i pokazuje listę błędów w podsumowaniu formularza edycji,
  - jeśli API zwróci `VALIDATION_ERROR` podczas `PUT /trips/{tripId}/plan`, komunikaty z API są pokazane w tym samym podsumowaniu błędów,
  - jeśli użytkownik uruchamia regenerację z widoku planu, UI waliduje wymagania generacji tak samo jak w widoku szczegółów i pokazuje listę braków albo błędów API,
  - przycisk „Zapisz” jest aktywny tylko, gdy formularz jest dirty, i zablokowany podczas requestu,
  - po sukcesie formularz jest oznaczany jako czysty albo plan jest odświeżany z API, a UI wraca do trybu `read`,
  - przycisk „Anuluj” wychodzi z trybu edycji i odrzuca lokalne zmiany formularza,
  - jeśli formularz jest dirty, anulowanie edycji powinno wymagać potwierdzenia utraty zmian,
  - po odświeżeniu strony użytkownik trafia do trybu `read`,
  - `PLAN_NOT_FOUND` jest traktowany jako pusty stan z akcją przejścia do generowania,
  - `JOB_ALREADY_ACTIVE` blokuje zapis lub regenerację zależnie od odpowiedzi API i pokazuje prosty komunikat,
  - pola tekstowe nie renderują HTML dostarczonego przez użytkownika ani API,
  - kolejność elementów jest czytelna dla klawiatury, a kontrolki edycji mają jednoznaczne etykiety,
  - fokus po wejściu w tryb edycji powinien trafić do pierwszego sensownego pola lub nagłówka formularza, a po anulowaniu lub zapisie wrócić w okolice przycisku „Edytuj plan”.


### Widok: Preferencje

- Ścieżka widoku: `/preferences`
- Główny cel: umożliwić użytkownikowi zapisanie domyślnych preferencji podróżniczych wykorzystywanych przy nowych wycieczkach.
- Kluczowe informacje do wyświetlenia:
  - domyślny budżet,
  - domyślna liczba osób,
  - domyślne tempo,
  - domyślne notatki,
  - tagi preferencji z kolejnością,
  - informacja o stanie zapisu.
- Kluczowe komponenty widoku:
  - formularz preferencji,
  - współdzielony wybór tagów,
  - przycisk zapisu preferencji,
  - wspólny komunikat błędu formularza,
  - stany loading/empty/error.
- UX, dostępność i względy bezpieczeństwa:
  - `GET /me/profile` zwraca także pusty profil, więc brak preferencji jest neutralnym stanem formularza,
  - `PUT /me/profile` zastępuje pełny zestaw preferencji i tagów,
  - po sukcesie UI utrzymuje wysłany stan lokalnie albo odświeża profil, jeśli potrzebuje reprezentacji kanonicznej,
  - preferencje są jasno opisane jako domyślne dla nowych wycieczek,
  - błędy `VALIDATION_ERROR` i `TAG_NOT_FOUND` są prezentowane jako wspólny komunikat.

### Widoki auth jako późniejsze rozszerzenie

- Ścieżki docelowe: `/auth/login`, `/auth/register`
- Główny cel: obsłużyć historyjki US-001, US-002 i US-003 po wejściu auth do zakresu UI.
- Kluczowe informacje do wyświetlenia:
  - email,
  - hasło,
  - komunikat błędu logowania lub rejestracji,
  - stan sesji użytkownika.
- Kluczowe komponenty widoku:
  - formularz rejestracji,
  - formularz logowania,
  - akcja wylogowania w globalnym layoucie po wdrożeniu auth,
  - auth guard dla tras prywatnych,
  - interceptor dodający `Authorization: Bearer <JWT>`.
- UX, dostępność i względy bezpieczeństwa:
  - te widoki nie są częścią aktywnego MVP UI,
  - architektura routingu nie powinna utrudniać ich dodania,
  - komunikaty auth nie powinny ujawniać nadmiarowych informacji o kontach,
  - token powinien być obsługiwany centralnie, nie przez pojedyncze widoki.

## 3. Mapa podróży użytkownika

Główny przepływ tworzenia i zapisania planu:

1. Użytkownik wchodzi do aplikacji na `/trips`.
2. Opcjonalnie przechodzi do `/preferences`, zapisuje domyślne preferencje i wraca do planowania.
3. Wybiera „Nowa wycieczka”.
4. Wypełnia formularz wycieczki: tytuł, miejsce lub notatkę albo tagi, daty, długość pobytu, liczbę osób, budżet i tempo.
5. Zapisuje wycieczkę przez `POST /trips`.
6. Po sukcesie trafia do `/trips/:tripId/details`.
7. W szczegółach może poprawić dane i zapisać je przez `PATCH /trips/{tripId}`.
8. Gdy dane spełniają wymagania generacji, uruchamia `POST /trips/{tripId}/generation-jobs`.
9. UI pokazuje prosty stan oczekiwania; nie powstaje osobny widok joba.
10. Po zakończeniu generowania użytkownik przechodzi do `/trips/:tripId/plan`.
11. Jeśli plan istnieje, użytkownik widzi `summary` i dni planu jako czytelne karty w trybie podglądu.
12. Użytkownik klika „Edytuj plan”, aby przejść do formularza edycji pod tym samym adresem.
13. Po zmianach przycisk „Zapisz” staje się aktywny i zapisuje pełny plan przez `PUT /trips/{tripId}/plan`.
14. Użytkownik wraca do listy wycieczek, gdzie wycieczka jest widoczna z informacją o posiadaniu planu.

Przepływ ponownej generacji:

1. Użytkownik otwiera szczegóły lub plan istniejącej wycieczki.
2. Wybiera „Regeneruj plan”.
3. UI pokazuje potwierdzenie informujące, że nowa generacja nadpisze obecny plan.
4. Po potwierdzeniu aplikacja uruchamia `POST /trips/{tripId}/generation-jobs`.
5. W trakcie aktywnego joba UI pokazuje prosty stan oczekiwania i blokuje konfliktowe akcje.
6. Po zakończeniu plan jest pobierany ponownie z `GET /trips/{tripId}/plan`.

Przepływ zarządzania listą:

1. Użytkownik korzysta z wyszukiwania, filtrów i sortowania na `/trips`.
2. Każda zmiana filtrów aktualizuje query params.
3. Użytkownik otwiera szczegóły przez kliknięcie wycieczki.
4. Użytkownik może usunąć wycieczkę z listy tylko po potwierdzeniu.
5. Po usunięciu lista odświeża bieżący zestaw wyników.

Mapowanie historyjek użytkownika z PRD:

| Historyjka | Pokrycie w UI |
|---|---|
| US-001 Rejestracja konta | Poza aktywnym MVP UI; przewidziane przyszłe `/auth/register`. |
| US-002 Logowanie | Poza aktywnym MVP UI; przewidziane przyszłe `/auth/login`, guardy i interceptor. |
| US-003 Wylogowanie | Poza aktywnym MVP UI; przewidziana akcja w globalnym layoucie po wdrożeniu auth. |
| US-004 Zapis preferencji | Widok `/preferences`, `GET /me/profile`, `PUT /me/profile`, współdzielony wybór tagów. |
| US-005 Tworzenie notatki/wycieczki | Widok `/trips/new`, `POST /trips`, pole notatki i dane wycieczki. |
| US-006 Edycja danych przed generacją | Widok `/trips/:tripId/details`, `PATCH /trips/{tripId}`, generowanie jako osobny krok po zapisie. |
| US-007 Generowanie planu | Akcja generowania w szczegółach, walidacja wymagań, `POST /generation-jobs`, prosty stan oczekiwania. |
| US-008 Regeneracja planu | Akcja regeneracji z potwierdzeniem w szczegółach i planie. |
| US-009 Zapis planu | Widok planu, zapis ręcznych zmian pełnego planu przez `PUT /trips/{tripId}/plan`. |
| US-010 Ręczna edycja planu | Widok planu oparty o `summary` i `trip_plan_item`, z domyślnym podglądem oraz formularzem dostępnym po kliknięciu „Edytuj plan”. |
| US-011 Lista wycieczek | Widok `/trips` z filtrowaniem, sortowaniem, wyszukiwaniem i paginacją. |
| US-012 Usuwanie wycieczki | Akcja delete na liście, `DELETE /trips/{tripId}`, potwierdzenie przed usunięciem. |

Potencjalne punkty bólu i odpowiedzi UI:

- Użytkownik boi się utraty planu przy regeneracji: dialog potwierdzenia jasno informuje o nadpisaniu.
- Użytkownik traci orientację między danymi wycieczki a planem: workspace ma stały kontekst wycieczki i podnawigację „Szczegóły” / „Plan”.
- Użytkownik nie chce stracić filtrów listy: query params zachowują aktualny stan wyszukiwania.
- Użytkownik edytuje długi plan: dni są osobnymi kartami, a typy miejsc są oznaczone wizualnie.
- Użytkownik próbuje zapisać bez zmian: przycisk zapisu zmian planu jest nieaktywny, gdy formularz nie jest dirty.
- Generowanie trwa dłużej: UI pokazuje prosty, stabilny stan oczekiwania bez technicznych detali joba.

## 4. Układ i struktura nawigacji

Globalny layout powinien składać się z:

- nagłówka lub bocznej nawigacji aplikacji,
- głównego obszaru treści,
- miejsca na globalne komunikaty błędów lub statusy,
- przyszłego miejsca na stan użytkownika i wylogowanie po wdrożeniu auth.

Główna nawigacja:

- „Wycieczki” prowadzi do `/trips`.
- „Nowa wycieczka” prowadzi do `/trips/new`.
- „Preferencje” prowadzi do `/preferences`.

Nawigacja workspace'u wycieczki:

- `/trips/:tripId/details` - dane i kontekst wycieczki.
- `/trips/:tripId/plan` - plan wycieczki.

Zasady routingu:

- `/` przekierowuje do `/trips`.
- Szczegóły i plan są osobnymi podwidokami, a nie stanami jednego ekranu.
- Komponent kontekstu wycieczki jest wspólny dla szczegółów i planu.
- Pobieranie danych wycieczki i pobieranie planu pozostają rozdzielone.
- Query params listy obejmują `q`, `hasPlan`, `sort`, `limit`, `cursor`.
- Nie stosuje się localStorage do filtrów listy.
- Przyszłe trasy auth mogą objąć `/auth/login` i `/auth/register`, a trasy aplikacyjne mogą zostać zabezpieczone guardami bez zmiany głównego modelu nawigacji.

Proponowana struktura tras:

| Ścieżka | Znaczenie |
|---|---|
| `/` | Przekierowanie do `/trips`. |
| `/trips` | Lista wycieczek. |
| `/trips/new` | Utworzenie nowej wycieczki. |
| `/trips/:tripId/details` | Szczegóły i edycja danych wycieczki. |
| `/trips/:tripId/plan` | Edytowalny plan wycieczki. |
| `/preferences` | Preferencje użytkownika. |
| `/auth/login` | Przyszły ekran logowania. |
| `/auth/register` | Przyszły ekran rejestracji. |

## 5. Kluczowe komponenty

### AppShell

Globalna rama aplikacji zawierająca nawigację, obszar treści i miejsce na przyszły stan sesji użytkownika. Powinna obsługiwać responsywne zwężenie układu, ale priorytetem jest wygoda desktopowa.

### MainNavigation

Nawigacja do „Wycieczki”, „Nowa wycieczka” i „Preferencje”. Aktywny element powinien być oznaczony wizualnie i semantycznie.

### TripListFilters

Komponent filtrów listy wycieczek: wyszukiwanie, `hasPlan`, sortowanie i limit. Źródłem prawdy są query params.

### TripList

Lista lub tabela wycieczek z obsługą kliknięcia w wycieczkę, statusu planu i akcji usunięcia. Powinna dobrze działać dla pustej listy, błędu API i ładowania kolejnej strony.

### CursorPagination

Komponent paginacji oparty o `nextCursor`. Powinien jasno komunikować, czy istnieje kolejna strona wyników.

### TripForm

Wspólny formularz danych wycieczki używany przy tworzeniu i edycji. Obejmuje tytuł, miejsce, notatkę, daty, długość pobytu, liczbę osób, budżet, tempo i tagi. W trybie nowej wycieczki może być inicjalizowany preferencjami użytkownika.

### TripContextPanel

Wspólny komponent dla szczegółów i planu. Pokazuje najważniejszy kontekst: tytuł, miejsce, daty, długość pobytu, liczbę osób, budżet, tempo, tagi oraz status planu.

### TripWorkspaceNav

Podnawigacja w obrębie jednej wycieczki: „Szczegóły” i „Plan”. Ułatwia rozdzielenie edycji danych wejściowych od edycji planu.

### TagSelector

Współdzielony komponent wyboru tagów dla formularza wycieczki i preferencji. Korzysta z `GET /tags`, a lista tagów jest cache'owana w pamięci aplikacji podczas sesji.

### PreferencesForm

Formularz preferencji użytkownika z domyślnym budżetem, liczbą osób, tempem, notatkami i tagami preferencji. Zapisuje pełny stan przez `PUT /me/profile`.

### GenerationAction

Komponent akcji generowania i regenerowania planu. Waliduje lokalnie wymagania generacji, pokazuje listę braków lub błędów API w podsumowaniu, obsługuje potwierdzenie regeneracji oraz prosty stan oczekiwania po utworzeniu joba.

### PlanEditor

Główny komponent formularza edycji planu, widoczny w trybie `edit`. Obejmuje `summary`, listę dni i elementów planu. Korzysta z mechanizmu dirty formularza, waliduje lokalnie payload planu, pokazuje listę błędów w podsumowaniu i zapisuje pełny plan przez `PUT /trips/{tripId}/plan`.

### PlanDayCard

Widget pojedynczego dnia planu. Grupuje elementy po `dayNumber`, zachowuje kolejność po `order` i zapewnia czytelne etykiety pól.

### PlanItemEditor

Edytowalny element planu obejmujący tytuł, opis, lokalizację, datę/czas, kolejność, numer dnia oraz typ miejsca. Powinien wspierać obsługę klawiaturą, czytelny focus i przekazywać błędy walidacji do wspólnego podsumowania formularza planu.

### PlaceTypeBadge

Mały znacznik typu miejsca: atrakcja, restauracja, hotel. Ułatwia skanowanie planu, ale nie powinien być jedynym nośnikiem znaczenia; tekstowa etykieta pozostaje wymagana.

### DirtySaveBar

Pasek zapisu dla edytowalnych formularzy, szczególnie planu. Pokazuje stan niezapisanych zmian, blokuje zapis w trakcie requestu i dezaktywuje akcję, gdy formularz nie jest dirty albo ma błędy walidacji blokujące zapis.

### ConfirmDialog

Wspólny dialog potwierdzenia dla usuwania wycieczki i regeneracji planu. Musi obsługiwać fokus, Escape, Enter oraz powrót fokusu do elementu wywołującego.

### ApiErrorBanner

Wspólny komponent komunikatu błędu. Mapuje kody API na czytelne komunikaty, m.in. `VALIDATION_ERROR`, `GENERATION_REQUIREMENTS_NOT_MET`, `TAG_NOT_FOUND`, `TRIP_NOT_FOUND`, `PLAN_NOT_FOUND`, `JOB_ALREADY_ACTIVE`.

### LoadingState, EmptyState, ErrorState

Zestaw komponentów stanów widoku używany na liście wycieczek, w szczegółach, planie i preferencjach. Każdy stan powinien zawierać jasną informację i możliwą następną akcję.

### Mapowanie wymagań na elementy UI

| Wymaganie | Element UI |
|---|---|
| Zapis preferencji | `/preferences`, `PreferencesForm`, `TagSelector`. |
| Tworzenie wycieczki | `/trips/new`, `TripForm`. |
| Edycja danych wejściowych | `/trips/:tripId/details`, `TripForm`, `TripContextPanel`. |
| Generowanie planu | `GenerationAction` w widoku szczegółów. |
| Regeneracja planu | `GenerationAction` z `ConfirmDialog` w szczegółach i planie. |
| Lista, filtrowanie, sortowanie | `/trips`, `TripListFilters`, `TripList`, `CursorPagination`. |
| Usuwanie wycieczki | Akcja delete w `TripList` z `ConfirmDialog`. |
| Edycja planu | `/trips/:tripId/plan`, `PlanEditor`, `PlanDayCard`, `PlanItemEditor`. |
| Zapis planu | `DirtySaveBar`, `PUT /trips/{tripId}/plan`. |

//TO DO  - czy to na pewno powinien byc banner a nie jakis widget? jesli bledow bedzie kilka to ciezko w banerze
| Obsługa błędów | `ApiErrorBanner`, `ErrorState`. |
| Dostępność | Etykiety pól, focus states, obsługa klawiatury, semantyczna nawigacja. |
| Bezpieczeństwo | Brak renderowania HTML z danych użytkownika, potwierdzenia akcji destrukcyjnych, miejsce na auth guardy i interceptor. |
