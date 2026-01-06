Jesteś doświadczonym menedżerem produktu, którego zadaniem jest stworzenie kompleksowego dokumentu wymagań produktu (PRD) w oparciu o poniższe opisy:

<project_description>
# Aplikacja - VibeTravels (MVP)

### Główny problem
Planowanie angażujących i interesujących wycieczek jest trudne. Dzięki wykorzystaniu potencjału, kreatywności i wiedzy AI, w VibeTravels możesz zamieniać uproszczone notatki o miejscach i celach podróży na konkretne plany.

### Najmniejszy zestaw funkcjonalności
- Zapisywanie, odczytywanie, przeglądanie i usuwanie notatek o przyszłych wycieczkach
- Prosty system kont użytkowników do powiązania użytkownika z notatkami
- Strona profilu użytkownika służąca do zapisywania preferencji turystycznych
- Integracja z AI umożliwiająca konwersję notatek w szczegółowe plany, biorące pod uwagę preferencje, czas, liczbę osób oraz potencjalne miejsca i atrakcje

### Co NIE wchodzi w zakres MVP
- Współdzielenie planów wycieczkowych między kontami
- Bogata obsługa i analiza multimediów (np. zdjęć miejsc do odwiedzenia)
- Zaawansowane planowanie czasu i logistyki

### Kryteria sukcesu
- 90% użytkowników posiada wypełnione preferencje turystyczne w swoim profilu
- 75% użytkowników generuje 3 lub więcej planów wycieczek na rok
</project_description>

<project_details>
<decisions>
1. MVP dotyczy aplikacji webowej (bez mobile), umożliwiającej planowanie wycieczek z użyciem AI na podstawie notatek, tagów i pól strukturalnych.
2. Cały proces tworzenia wycieczki odbywa się na jednym formularzu.
3. Pola wymagane do generacji planu to: zakres dat, zakres długości pobytu (2–21 dni), liczba osób.
 Do tego są wymagane notatki albo dwa tagi lub pole miejsce
4. Długość pobytu wybierana jest przez AI jako jedna konkretna wartość z zakresu ustawionego sliderem przez użytkownika.
5. Zakres dat określa „okno czasowe”, w którym AI wybiera konkretne dni podróży.
6. Budżet jest podawany jako kwota całkowita w jednej domyślnej walucie.
7. Pole „miejsce” jest polem tekstowym, opcjonalnym i niewalidowanym strukturalnie.
8. Preferencje użytkownika składają się z:
   - tagów (z możliwością sortowania wg ważności),
   - pól strukturalnych (daty, budżet, liczba osób, tempo itp.).
9. Preferencje nie są obowiązkowe w profilu; użytkownik może zapisać je jako domyślne i używać przy planowaniu wycieczek.
10. Przed pierwszą generacją użytkownik może zapisać uzupełnione dane przyciskiem „Zapisz preferencje”.
11. Po wygenerowaniu planu jedyną akcją zapisu jest „Zapisz plan”.
12. Input formularza jest zapisywany:
    - przed pierwszą generacją (dowolne uzupełnione pola),
    - po kolejnej generacji planu (aby uniknąć niespójności).
13. Jeśli użytkownik zmieni input po generacji, musi ponownie wygenerować plan, aby móc zapisać zmiany.
14. Istnieją tylko dwa stany obiektu: plan niewygenerowany i plan wygenerowany.
15. Nie istnieje pojęcie „planu nieaktualnego” ani wersjonowanie.
16. Każda nowa generacja nadpisuje poprzedni plan.
17. Plan jest zapisywany jako tekst z ustaloną strukturą logiczną (dni → sekcje).
18. W widoku aplikacji plan renderowany jest jako wystylizowany HTML.
19. W trybie edycji plan wyświetlany jest jako czysty tekst i edytowany w całości.
20. System nie rozróżnia, czy plan został zmodyfikowany ręcznie czy pochodzi w całości z AI.
21. Struktura każdego dnia planu jest sztywna: data → Aktywności → Nocleg → Restauracje.
22. Opis dnia ma formę list punktowanych; każdy punkt max. 5 zdań.
23. Każdy dzień musi zawierać wszystkie trzy sekcje; powtórzenia noclegów i restauracji są dozwolone.
24. Brak walidacji jakości outputu po stronie aplikacji – kompletność jest wymuszana wyłącznie promptem AI.
25. Lista wycieczek/notatek jest widokiem listy z filtrowaniem i sortowaniem (po dacie, wygenerowane/niewygenerowane).
26. Plan liczy się do metryk tylko wtedy, gdy użytkownik świadomie kliknie „Zapisz plan”.
27. Brak współdzielenia, map, importów, multimediów, sugestii UX i historii zmian – świadome ograniczenia MVP.
</decisions>

<matched_recommendations>
1. Uproszczony, jednoformularzowy flow zamiast wizardów – przyjęty.
2. Jasne minimum danych wymaganych do generacji – zdefiniowane i egzekwowane przy generowaniu.
3. Iteracyjny loop: zmień dane → wygeneruj ponownie → zapisz plan – przyjęty.
4. Traktowanie planu jako snapshotu bez wersjonowania – przyjęte w uproszczonej formie (1 plan).
5. Sztywna, przewidywalna struktura outputu AI – w pełni przyjęta.
6. Świadome ograniczenie zakresu MVP (out of scope) – jasno zdefiniowane i zaakceptowane.
7. Mierzenie sukcesu poprzez konkretne akcje użytkownika (zapis planu, uzupełnienie preferencji) – przyjęte.
</matched_recommendations>

<prd_planning_summary>
a. Główne wymagania funkcjonalne:
- Tworzenie wycieczki na jednym formularzu z polami strukturalnymi, tagami i notatkami.
- Generowanie planu wycieczki przez AI na podstawie danych użytkownika.
- Zapisywanie planu jako jedynej, aktualnej wersji.
- Ręczna edycja całego planu w formie tekstowej.
- Zarządzanie listą notatek i planów (tworzenie, przeglądanie, usuwanie).
- System kont użytkowników z prostą autentykacją.

b. Kluczowe historie użytkownika i ścieżki:
- Użytkownik tworzy notatkę → uzupełnia dane → generuje plan → zapisuje plan.
- Użytkownik eksperymentuje z danymi → generuje plan wielokrotnie → zapisuje finalny wariant.
- Użytkownik zapisuje domyślne preferencje i wykorzystuje je przy kolejnych wycieczkach.
- Użytkownik edytuje wygenerowany plan ręcznie i zapisuje zmiany.

c. Kryteria sukcesu i pomiar:
- 90% użytkowników ma zapisane preferencje (mierzone zapisem preferencji).
- 75% użytkowników zapisuje ≥3 plany rocznie (liczone po akcji „Zapisz plan”).
- Kluczowe eventy: zapis preferencji, zapis planu.

d. Obszary wymagające dalszego wyjaśnienia:
- Brak – wszystkie kluczowe decyzje produktowe dla MVP zostały podjęte.
</prd_planning_summary>

<unresolved_issues>
Brak nierozwiązanych kwestii blokujących przygotowanie kompletnego PRD dla MVP.
</unresolved_issues>

</project_details>

Wykonaj następujące kroki, aby stworzyć kompleksowy i dobrze zorganizowany dokument:

1. Podziel PRD na następujące sekcje:
   a. Przegląd projektu
   b. Problem użytkownika
   c. Wymagania funkcjonalne
   d. Granice projektu
   e. Historie użytkownika
   f. Metryki sukcesu

2. W każdej sekcji należy podać szczegółowe i istotne informacje w oparciu o opis projektu i odpowiedzi na pytania wyjaśniające. Upewnij się, że:
   - Używasz jasnego i zwięzłego języka
   - W razie potrzeby podajesz konkretne szczegóły i dane
   - Zachowujesz spójność w całym dokumencie
   - Odnosisz się do wszystkich punktów wymienionych w każdej sekcji

3. Podczas tworzenia historyjek użytkownika i kryteriów akceptacji
   - Wymień WSZYSTKIE niezbędne historyjki użytkownika, w tym scenariusze podstawowe, alternatywne i skrajne.
   - Przypisz unikalny identyfikator wymagań (np. US-001) do każdej historyjki użytkownika w celu bezpośredniej identyfikowalności.
   - Uwzględnij co najmniej jedną historię użytkownika specjalnie dla bezpiecznego dostępu lub uwierzytelniania, jeśli aplikacja wymaga identyfikacji użytkownika lub ograniczeń dostępu.
   - Upewnij się, że żadna potencjalna interakcja użytkownika nie została pominięta.
   - Upewnij się, że każda historia użytkownika jest testowalna.

Użyj następującej struktury dla każdej historii użytkownika:
- ID
- Tytuł
- Opis
- Kryteria akceptacji

4. Po ukończeniu PRD przejrzyj go pod kątem tej listy kontrolnej:
   - Czy każdą historię użytkownika można przetestować?
   - Czy kryteria akceptacji są jasne i konkretne?
   - Czy mamy wystarczająco dużo historyjek użytkownika, aby zbudować w pełni funkcjonalną aplikację?
   - Czy uwzględniliśmy wymagania dotyczące uwierzytelniania i autoryzacji (jeśli dotyczy)?

5. Formatowanie PRD:
   - Zachowaj spójne formatowanie i numerację.
   - Nie używaj pogrubionego formatowania w markdown ( ** ).
   - Wymień WSZYSTKIE historyjki użytkownika.
   - Sformatuj PRD w poprawnym markdown.

Przygotuj PRD z następującą strukturą:

```markdown
# Dokument wymagań produktu (PRD) - {{app-name}}
## 1. Przegląd produktu
## 2. Problem użytkownika
## 3. Wymagania funkcjonalne
## 4. Granice produktu
## 5. Historyjki użytkowników
## 6. Metryki sukcesu
```

Pamiętaj, aby wypełnić każdą sekcję szczegółowymi, istotnymi informacjami w oparciu o opis projektu i nasze pytania wyjaśniające. Upewnij się, że PRD jest wyczerpujący, jasny i zawiera wszystkie istotne informacje potrzebne do dalszej pracy nad produktem.

Ostateczny wynik powinien składać się wyłącznie z PRD zgodnego ze wskazanym formatem w markdown, który zapiszesz w pliku .ai/prd.md