<conversation_summary>

<decisions>

„Wycieczka” i „notatka” są jedną encją logiczną (trips).

Relacja użytkownik–wycieczka to 1:N; każda wycieczka należy do dokładnie jednego użytkownika.

Tagi są globalnym słownikiem zrealizowanym przez tabelę tags oraz tabele łączące (np. trip_tags, user_preference_tags).

Wagi preferencji są typu INT, z domyślną wartością 5.

Preferencje użytkownika są modelowane jako jawne kolumny, bez użycia JSONB.

Zakres dat i długości pobytu jest przechowywany jako pola wejściowe (from/to, min/max), a wynik generacji jako pola wybrane.

Historia inputów wycieczki nie jest przechowywana; zmiana inputu wymaga ponownej generacji.

Istnieje tabela generation_jobs, reprezentująca jedno logiczne generowanie wycieczki.

Dla jednej wycieczki może istnieć tylko jeden aktywny job naraz.

Job posiada snapshot inputu zapisany jako JSONB oraz input_hash.

Statusy jobów są realizowane jako enum PostgreSQL.

Job może być jawnie anulowany (status canceled), a worker sprawdza flagę przed zapisem wyników.

Pozycje planu są powiązane bezpośrednio z wycieczką i są nadpisywane przy każdej nowej generacji.

Struktura planu zawiera osobną tabelę „nagłówka” planu (1:1 z wycieczką) oraz tabelę pozycji planu (1:N).

Nadpisywanie planu odbywa się atomowo w jednej transakcji.

Pozycje planu mają osobne kolumny date (DATE) i time (TIME, nullable).

Kolejność pozycji planu jest kontrolowana przez sort_order.

Typ miejsca jest ograniczony enumem do: attraction, restaurant, hotel.

Nazwa miejsca i opis są tekstowe; opis może być długi (bez sztywnego limitu).

Usuwanie danych jest typu hard delete z użyciem ON DELETE CASCADE.

RLS jest pomijane na etapie MVP.

Kluczowe indeksy są dodane zgodnie z rekomendacjami (listowanie wycieczek, jobów i planów).

</decisions>
<matched_recommendations>

Użycie JSONB + input_hash do snapshotu inputu joba zamiast wersjonowania całych wycieczek.

Wymuszenie „jednego aktywnego joba” poprzez unikalny constraint warunkowy na generation_jobs.

Jawne statusy jobów jako enum PostgreSQL, z obsługą anulowania.

Modelowanie generowania jako osobnej encji (generation_jobs) powiązanej z wycieczką.

Atomowe nadpisywanie planu w jednej transakcji.

Rozdzielenie „nagłówka planu” i pozycji planu dla lepszej spójności i czytelności modelu.

Użycie DATE + TIME zamiast TIMESTAMP, bez obsługi stref czasowych na MVP.

Kontrola kolejności pozycji planu przez sort_order.

Enum dla typu miejsca, ograniczony do trzech wartości.

Zastosowanie ON DELETE CASCADE dla zachowania integralności przy hard delete.
</matched_recommendations>

<database_planning_summary>
a. Główne wymagania dotyczące schematu bazy danych

Schemat bazy danych PostgreSQL dla MVP ma wspierać zarządzanie użytkownikami, ich wycieczkami, preferencjami, procesem generowania planów podróży oraz przechowywaniem wynikowych planów. Kluczowe są: prostota modelu, deterministyczne generowanie, atomowość operacji oraz możliwość bezpiecznego nadpisywania danych.

b. Kluczowe encje i ich relacje

users – użytkownicy systemu.

trips – wycieczki (1 użytkownik → N wycieczek).

user_preferences – preferencje użytkownika (1:1 z users).

tags – słownik tagów.

trip_tags / user_preference_tags – relacje N:M z tagami.

generation_jobs – procesy generowania (1 trip → N jobs, ale max 1 aktywny).

trip_plans – aktualny plan wycieczki (1:1 z trips).

plan_items – pozycje planu (1 trip → N pozycji).

c. Ważne kwestie dotyczące bezpieczeństwa i skalowalności

Brak RLS na MVP upraszcza model i zapytania.

Hard delete z kaskadami zapobiega osieroconym rekordom.

Indeksy są dobrane pod najczęstsze zapytania (listy wycieczek, jobów, planów).

Snapshot inputu w jobie zapewnia spójność i możliwość debugowania bez przechowywania historii wycieczek.

d. Wszelkie nierozwiązane kwestie lub obszary wymagające dalszego wyjaśnienia

Model jest kompletny na potrzeby MVP; dalsze decyzje będą dotyczyć głównie:

ewentualnego wersjonowania planów w przyszłości,

rozszerzenia typów miejsc,

dodania RLS i soft delete w kolejnych iteracjach produktu.

</database_planning_summary>

<unresolved_issues>

Brak istotnych nierozwiązanych kwestii na etapie MVP.
</unresolved_issues>

</conversation_summary>