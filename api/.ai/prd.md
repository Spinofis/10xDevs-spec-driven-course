# Dokument wymagań produktu (PRD) - VibeTravels (MVP)

## 1. Przegląd produktu
VibeTravels to aplikacja webowa umożliwiająca planowanie wycieczek z wykorzystaniem AI. Produkt pozwala użytkownikom zamieniać uproszczone notatki, tagi oraz dane strukturalne w spójne, szczegółowe plany podróży. MVP koncentruje się na prostym, jednoformularzowym procesie generowania planu oraz zarządzaniu notatkami i preferencjami użytkownika.

Aplikacja nie posiada wersji mobilnej, nie oferuje współdzielenia planów ani zaawansowanych funkcji logistycznych. Celem MVP jest walidacja wartości automatycznego planowania podróży przy minimalnym zakresie funkcjonalnym.

## 2. Problem użytkownika
Planowanie angażujących i sensownych wycieczek jest czasochłonne i wymaga wiedzy, doświadczenia oraz przeszukiwania wielu źródeł. Użytkownicy często posiadają jedynie luźne pomysły, notatki lub cele podróży, które trudno przekształcić w konkretny, dzienny plan.

VibeTravels rozwiązuje ten problem, umożliwiając:
- szybkie zebranie myśli w formie notatek i tagów,
- zapis preferencji podróżniczych,
- automatyczne wygenerowanie kompletnego planu wycieczki przez AI.

## 3. Wymagania funkcjonalne
1. System kont użytkowników:
   - rejestracja, logowanie i wylogowanie,
   - powiązanie danych (notatki, plany, preferencje) z kontem.

2. Profil użytkownika:
   - możliwość zapisu preferencji turystycznych,
   - preferencje obejmują tagi (z wagą/ważnością) oraz pola strukturalne (budżet, liczba osób, tempo),
   - preferencje nie są obowiązkowe,
   - możliwość zapisania preferencji jako domyślnych.

3. Notatki i wycieczki:
   - tworzenie, przeglądanie, usuwanie notatek/wycieczek,
   - widok listy z filtrowaniem i sortowaniem (data, wygenerowane/niewygenerowane).

4. Formularz planowania wycieczki:
   - jeden formularz obejmujący wszystkie dane,
   - pola wymagane do generacji:
     - zakres dat,
     - zakres długości pobytu (2–21 dni),
     - liczba osób,
     - notatki lub co najmniej dwa tagi lub pole „miejsce”,
   - budżet jako jedna kwota w domyślnej walucie,
   - pole „miejsce” jako tekst nieustrukturyzowany.

5. Generowanie planu przez AI:
   - AI wybiera konkretne daty w podanym zakresie,
   - AI wybiera jedną długość pobytu z zakresu,
   - każda generacja nadpisuje poprzedni plan,
   - brak wersjonowania planów.

6. Plan wycieczki:
   - plan zapisywany jako tekst o stałej strukturze:
     - dni → data → aktywności → nocleg → restauracje,
   - każdy dzień zawiera wszystkie sekcje,
   - renderowanie planu jako HTML w widoku,
   - edycja planu jako czystego tekstu,
   - brak rozróżnienia planu AI i edycji ręcznej.

7. Zapisywanie danych:
   - zapis inputu przed pierwszą generacją,
   - zapis inputu po każdej kolejnej generacji,
   - zapis planu tylko po kliknięciu „Zapisz plan”.

## 4. Granice produktu
Poza zakresem MVP:
- współdzielenie planów między użytkownikami,
- wersjonowanie i historia zmian,
- mapy, multimedia, importy,
- walidacja jakości planu po stronie aplikacji,
- sugestie UX i rekomendacje,
- aplikacja mobilna,
- zaawansowane planowanie logistyki i czasu.

## 5. Historyjki użytkowników

US-001
Tytuł: Rejestracja konta
Opis: Jako nowy użytkownik chcę utworzyć konto, aby zapisywać swoje notatki i plany.
Kryteria akceptacji:
- użytkownik może utworzyć konto przy użyciu formularza,
- konto umożliwia zalogowanie się do aplikacji.

US-002
Tytuł: Logowanie
Opis: Jako użytkownik chcę się zalogować, aby uzyskać dostęp do swoich danych.
Kryteria akceptacji:
- poprawne dane logowania umożliwiają dostęp,
- błędne dane wyświetlają komunikat o błędzie.

US-003
Tytuł: Wylogowanie
Opis: Jako użytkownik chcę się wylogować, aby zabezpieczyć swoje konto.
Kryteria akceptacji:
- po wylogowaniu użytkownik traci dostęp do danych.

US-004
Tytuł: Zapis preferencji
Opis: Jako użytkownik chcę zapisać preferencje podróżnicze, aby AI uwzględniało je w planach.
Kryteria akceptacji:
- użytkownik może zapisać tagi i pola strukturalne,
- zapisane preferencje są dostępne przy planowaniu.

US-005
Tytuł: Tworzenie notatki
Opis: Jako użytkownik chcę utworzyć notatkę wycieczki, aby rozpocząć planowanie.
Kryteria akceptacji:
- notatka jest zapisana i widoczna na liście.

US-006
Tytuł: Edycja danych przed generacją
Opis: Jako użytkownik chcę edytować dane wejściowe przed generacją planu.
Kryteria akceptacji:
- zmiany są zapisywane automatycznie przed generacją.

US-007
Tytuł: Generowanie planu
Opis: Jako użytkownik chcę wygenerować plan wycieczki przy użyciu AI.
Kryteria akceptacji:
- generacja jest możliwa tylko przy spełnieniu wymagań,
- plan ma wymaganą strukturę.

US-008
Tytuł: Regeneracja planu
Opis: Jako użytkownik chcę ponownie wygenerować plan po zmianie danych.
Kryteria akceptacji:
- nowa generacja nadpisuje poprzedni plan.

US-009
Tytuł: Zapis planu
Opis: Jako użytkownik chcę zapisać wygenerowany plan.
Kryteria akceptacji:
- plan jest liczony do metryk tylko po zapisie,
- zapis jest możliwy tylko bez zmian w inputach.

US-010
Tytuł: Ręczna edycja planu
Opis: Jako użytkownik chcę edytować plan ręcznie.
Kryteria akceptacji:
- plan edytowany jest w całości jako tekst,
- zapisuje się jako aktualna wersja.

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

## 6. Metryki sukcesu
- 90% użytkowników zapisuje preferencje w profilu,
- 75% użytkowników zapisuje co najmniej 3 plany rocznie,
- kluczowe zdarzenia:
  - zapis preferencji,
  - zapis planu.
