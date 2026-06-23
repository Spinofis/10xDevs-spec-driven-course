Jesteś ekspertem PostgreSQL, który uwielbia tworzyć bezpieczne schematy baz danych.

Utwórz migrację bazy danych w pliku sql

Utwórz migracje dla następującego db-plan:
<db-plan>
{{db-plan}} <- przekaż referencję do db-plan.md
</db-plan>

## Tworzenie pliku migracji

Biorąc pod uwagę kontekst wiadomości użytkownika, utwórz plik YYYYMMDDHHmmss_vibe_trvelers.sql

Plik MUSI przestrzegać następującej konwencji nazewnictwa:

Plik MUSI być nazwany w formacie `YYYYMMDDHHmmss_vibe_trvelers.sql` z odpowiednim rozróżnianiem wielkości liter dla miesięcy, minut i sekund w czasie UTC:

1. `YYYY` - Cztery cyfry dla roku (np. `2024`).
2. `MM` - Dwie cyfry dla miesiąca (01 do 12).
3. `DD` - Dwie cyfry dla dnia miesiąca (01 do 31).
4. `HH` - Dwie cyfry dla godziny w formacie 24-godzinnym (00 do 23).
5. `mm` - Dwie cyfry dla minuty (00 do 59).
6. `ss` - Dwie cyfry dla sekundy (00 do 59).
7. Dodaj odpowiedni opis dla migracji.

Na przykład:

```
20240906123045_create_profiles.sql
```

## Wytyczne SQL

- Dodaj do pliku komendy tworzące wszystkie potrzebne tabele
- Zadbaj, żeby każda tabela miała odpowiedni PK tak jak w specyfikacji
- Dodaj wszystkie potrzebne relacje
- Dodaj indeksy
- Utwórz skrypt tak żeby można go było wywołać w całości, to znaczy jeśli jest przykładowo tabela A, które ma relacje do B - stwórz najpierw B.
Jeśli utowrzenie jakiegoś indeksu wymaga najpierw tabeli dodaj najpierw tworzenie tabeli później indeksu
- Dodaj komentarze opisujące co się dzieje
- Sformatuj odpowiednio SQL
- Napisz sql tak, żeby go można było wywołać wiele razy bez błędów. To znaczy , dodaj sprawdzenia czy dana rzecz istnieje, jeśli nie to dopiero ją utwórz