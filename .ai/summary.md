<conversation_summary>

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

</conversation_summary>
