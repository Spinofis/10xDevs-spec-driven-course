Jesteś wykwalifikowanym programistą C#/.Net, którego zadaniem jest stworzenie biblioteki typów request/response (Data Transfer Object) i Command Model dla aplikacji. Twoim zadaniem jest przeanalizowanie definicji modelu bazy danych i planu API, a następnie utworzenie odpowiednich typów request/response, które dokładnie reprezentują struktury danych wymagane przez API, zachowując jednocześnie połączenie z podstawowymi modelami bazy danych.

Najpierw dokładnie przejrzyj następujące dane wejściowe:

1. Modele bazy danych:
<database_models>
20260116170705_vibe_trvelers.sql
20260120101530_rename_tags_slug_to_code
</database_models>

2. Plan API (zawierający zdefiniowane request/response):
<api_plan>
api_plan v2.md
</api_plan>

3. Plan struktury projektu
<project_structure>
project_structure.md
</project_structure>

Twoim zadaniem jest utworzenie definicji typów C#/.Net dla request/response i Command Modeli określonych w planie API, upewniając się, że pochodzą one z modeli bazy danych. Wykonaj następujące kroki:

1. Przeanalizuj modele bazy danych i plan API.
2. Utwórz typy request/response  na podstawie planu API, wykorzystując definicje encji bazy danych.
3. Zapewnienie zgodności między request/response i Command Modeli a wymaganiami API.
4. Stosowanie odpowiednich funkcji języka C# w celu tworzenia, zawężania lub rozszerzania typów zgodnie z potrzebami.
5. Wykonaj końcowe sprawdzenie, aby upewnić się, że wszystkie request/response są uwzględnione i prawidłowo połączone z definicjami encji.
6. Rozważ różne typy obiektów class, interface , record w zależności od tego co gdzie najlepiej pasuje
7. Rozważ użycie primary constructors tam gdzie to pasuje
8. Użyj odpowiednich access modifiers

Przed utworzeniem ostatecznego wyniku, pracuj wewnątrz tagów <request/response_analysis> w swoim bloku myślenia, aby pokazać swój proces myślowy i upewnić się, że wszystkie wymagania są spełnione. W swojej analizie:
- Wymień wszystkie request/response  zdefiniowane w planie API, numerując każdy z nich.
- Dla każdego request/response i Comand Modelu:
 - Zidentyfikuj odpowiednie encje bazy danych i wszelkie niezbędne transformacje typów.
  - Utwórz krótki szkic struktury request/response i Command Modelu.
- Wyjaśnij, w jaki sposób zapewnisz, że każde request/response i Command Model jest bezpośrednio lub pośrednio połączone z definicjami typów encji.

Po przeprowadzeniu analizy, podaj ostateczne definicje typów request/response i Command Modeli

Pamiętaj:
- Upewnij się, że wszystkie request/response  zdefiniowane w planie API są uwzględnione.
- Każdy request/response i Command Model powinien bezpośrednio odnosić się do jednej lub więcej encji bazy danych.
- W razie potrzeby używaj funkcji TypeScript, takich jak Pick, Omit, Partial itp.
- Dodaj komentarze, aby wyjaśnić złożone lub nieoczywiste manipulacje typami.

Końcowy wynik powinien składać się wyłącznie z definicji typów request/response, które zapiszesz z uwzględnieniem struktury projektu {{project-structure.md}}