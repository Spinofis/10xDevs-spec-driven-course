Jako starszy programista frontendu Twoim zadaniem jest stworzenie szczegółowego planu wdrożenia nowego widoku w aplikacji internetowej. Plan ten powinien być kompleksowy i wystarczająco jasny dla innego programisty frontendowego, aby mógł poprawnie i wydajnie wdrożyć widok.

Najpierw przejrzyj następujące informacje:

1. Product Requirements Document (PRD):
<prd>
api\.ai\prd.md
</prd>

2. Opis widoku:
<view_description>
  ### Widok: Lista wycieczek

- Ścieżka widoku: `/trips`
- Główny cel: umożliwić użytkownikowi szybkie odnalezienie, filtrowanie, sortowanie i otwarcie zapisanych wycieczek.
- Kluczowe informacje do wyświetlenia:
  - tytuł wycieczki,
  - miejsce,
  - skrót notatki lub kontekstu (to chyba jako jakis "dymek" tooltip, bo tekst moze byc dlugi),
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
</view_description>

3. User Stories:
<user_stories>
US-011
Tytuł: Lista wycieczek
Opis: Jako użytkownik chcę przeglądać listę wycieczek.
Kryteria akceptacji:
- lista umożliwia sortowanie i filtrowanie.

US-012
Tytuł: Usuwanie wycieczki
Opis: Jako użytkownik chcę usunąć wycieczkę.
Kryteria akceptacji:
- wycieczka znika z listy i nie jest dostępna.
</user_stories>

4. Endpoint Description:
<endpoint_description>
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
</endpoint_description>

5. Endpoint Implementation:
<endpoint_implementation>
api\VibeTravelers.API\VibeTravels.Application\Features\Trips\Handlers\ListTripsQueryHandler.cs
</endpoint_implementation>


7. Tech Stack:
<tech_stack>
api\.ai\tech_stack.md
</tech_stack>

Przed utworzeniem ostatecznego planu wdrożenia przeprowadź analizę i planowanie wewnątrz tagów <implementation_breakdown> w swoim bloku myślenia. Ta sekcja może być dość długa, ponieważ ważne jest, aby być dokładnym.

W swoim podziale implementacji wykonaj następujące kroki:
1. Dla każdej sekcji wejściowej (PRD, User Stories, Endpoint Description, Endpoint Implementation, Type Definitions, Tech Stack):
  - Podsumuj kluczowe punkty
 - Wymień wszelkie wymagania lub ograniczenia
 - Zwróć uwagę na wszelkie potencjalne wyzwania lub ważne kwestie
2. Wyodrębnienie i wypisanie kluczowych wymagań z PRD
3. Wypisanie wszystkich potrzebnych głównych komponentów, wraz z krótkim opisem ich opisu, potrzebnych typów, obsługiwanych zdarzeń i warunków walidacji
4. Stworzenie wysokopoziomowego diagramu drzewa komponentów
5. Zidentyfikuj wymagane DTO i niestandardowe typy ViewModel dla każdego komponentu widoku. Szczegółowo wyjaśnij te nowe typy, dzieląc ich pola i powiązane typy.
6. Zidentyfikuj potencjalne zmienne stanu i niestandardowe hooki, wyjaśniając ich cel i sposób ich użycia
7. Wymień wymagane wywołania API i odpowiadające im akcje frontendowe
8. Zmapuj każdej historii użytkownika do konkretnych szczegółów implementacji, komponentów lub funkcji
9. Wymień interakcje użytkownika i ich oczekiwane wyniki
10. Wymień warunki wymagane przez API i jak je weryfikować na poziomie komponentów
11. Zidentyfikuj potencjalne scenariusze błędów i zasugeruj, jak sobie z nimi poradzić
12. Wymień potencjalne wyzwania związane z wdrożeniem tego widoku i zasugeruj możliwe rozwiązania

Po przeprowadzeniu analizy dostarcz plan wdrożenia w formacie Markdown z następującymi sekcjami:

1. Przegląd: Krótkie podsumowanie widoku i jego celu.
2. Routing widoku: Określenie ścieżki, na której widok powinien być dostępny.
3. Struktura komponentów: Zarys głównych komponentów i ich hierarchii.
4. Szczegóły komponentu: Dla każdego komponentu należy opisać:
 - Opis komponentu, jego przeznaczenie i z czego się składa
 - Główne elementy HTML i komponenty dzieci, które budują komponent
 - Obsługiwane zdarzenia
 - Warunki walidacji (szczegółowe warunki, zgodnie z API)
 - Typy (DTO i ViewModel) wymagane przez komponent
 - Propsy, które komponent przyjmuje od rodzica (interfejs komponentu)
5. Typy: Szczegółowy opis typów wymaganych do implementacji widoku, w tym dokładny podział wszelkich nowych typów lub modeli widoku według pól i typów.
6. Zarządzanie stanem: Szczegółowy opis sposobu zarządzania stanem w widoku, określenie, czy wymagany jest customowy hook.
7. Integracja API: Wyjaśnienie sposobu integracji z dostarczonym punktem końcowym. Precyzyjnie wskazuje typy żądania i odpowiedzi.
8. Interakcje użytkownika: Szczegółowy opis interakcji użytkownika i sposobu ich obsługi.
9. Warunki i walidacja: Opisz jakie warunki są weryfikowane przez interfejs, których komponentów dotyczą i jak wpływają one na stan interfejsu
10. Obsługa błędów: Opis sposobu obsługi potencjalnych błędów lub przypadków brzegowych.
11. Kroki implementacji: Przewodnik krok po kroku dotyczący implementacji widoku.

Upewnij się, że Twój plan jest zgodny z PRD, historyjkami użytkownika i uwzględnia dostarczony stack technologiczny.

Ostateczne wyniki powinny być w języku polskim i zapisane w pliku o nazwie app/.ai/implementation-plan/results/{view-name}-view-implementation-plan.md. Nie uwzględniaj żadnej analizy i planowania w końcowym wyniku.

Oto przykład tego, jak powinien wyglądać plik wyjściowy (treść jest do zastąpienia):

```markdown
# Plan implementacji widoku [Nazwa widoku]

## 1. Przegląd
[Krótki opis widoku i jego celu]

## 2. Routing widoku
[Ścieżka, na której widok powinien być dostępny]

## 3. Struktura komponentów
[Zarys głównych komponentów i ich hierarchii]

## 4. Szczegóły komponentów
### [Nazwa komponentu 1]
- Opis komponentu [opis]
- Główne elementy: [opis]
- Obsługiwane interakcje: [lista]
- Obsługiwana walidacja: [lista, szczegółowa]
- Typy: [lista]
- Propsy: [lista]

### [Nazwa komponentu 2]
[...]

## 5. Typy
[Szczegółowy opis wymaganych typów]

## 6. Zarządzanie stanem
[Opis zarządzania stanem w widoku]

## 7. Integracja API
[Wyjaśnienie integracji z dostarczonym endpointem, wskazanie typów żądania i odpowiedzi]

## 8. Interakcje użytkownika
[Szczegółowy opis interakcji użytkownika]

## 9. Warunki i walidacja
[Szczegółowy opis warunków i ich walidacji]

## 10. Obsługa błędów
[Opis obsługi potencjalnych błędów]

## 11. Kroki implementacji
1. [Krok 1]
2. [Krok 2]
3. [...]
```

Rozpocznij analizę i planowanie już teraz. Twój ostateczny wynik powinien składać się wyłącznie z planu wdrożenia w języku polskim w formacie markdown, który zapiszesz w pliku app/.ai/implementation-plan/results/{view-name}-view-implementation-plan.md i nie powinien powielać ani powtarzać żadnej pracy wykonanej w podziale implementacji.