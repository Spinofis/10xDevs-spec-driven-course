# Stack technologiczny – VibeTravels (PoC / MVP)

Ten dokument opisuje **wybrany stack technologiczny** dla projektu VibeTravels.
Jego celem jest zapewnienie **jednoznacznego kontekstu technicznego** dla:

* członków zespołu,
* osób dołączających do projektu,
* narzędzi AI wspierających rozwój (generowanie kodu, architektury, testów).

Dokument skupia się na **odpowiedzialnościach komponentów**, a nie na marketingowym opisie technologii.

---

## Frontend

**Angular + TypeScript + Tailwind**

Frontend odpowiada za:

* interfejs użytkownika aplikacji,
* formularze notatek podróży i preferencji,
* inicjowanie procesu generowania planu,
* odbiór statusu generowania (polling lub streaming),
* prezentację wygenerowanych planów podróży.

Frontend **nie komunikuje się bezpośrednio z API AI**.
Wszystkie operacje związane z AI realizowane są przez backend.

---

## Backend

**ASP.NET Core Web API (.NET 8)**

Backend odpowiada za:

* autoryzację i uwierzytelnianie użytkowników,
* zarządzanie notatkami i planami podróży (CRUD),
* komunikację z zewnętrznym API AI,
* obsługę asynchronicznych procesów generowania planów,
* egzekwowanie zasad bezpieczeństwa i limitów.

Backend jest **stateless** i udostępnia wyłącznie API HTTP.

---

## Komunikacja z AI

* Integracja z **OpenAI API** realizowana wyłącznie po stronie backendu.
* Backend:

  * przyjmuje dane wejściowe od użytkownika,
  * wykonuje wywołanie AI,
  * zapisuje wynik generacji w bazie danych.
* Klucze API i logika promptów nigdy nie są dostępne w frontendzie.

---

## Asynchroniczność generowania

* Generowanie planów realizowane jest **asynchronicznie**.
* Backend:

  * zapisuje job generacji w bazie danych,
  * wykonuje generację w tle,
  * aktualizuje status i wynik.
* Frontend:

  * odpyta status generacji (polling),
  * opcjonalnie odbiera wynik w formie strumienia (SSE / HTTP streaming).

Brak kolejek i zewnętrznych brokerów zdarzeń na etapie PoC/MVP.

---

## Baza danych

**PostgreSQL**

Baza danych przechowuje:

* użytkowników i ich preferencje,
* notatki podróży,
* wygenerowane plany podróży,
* statusy procesów generacji AI.

---

## Infrastruktura

* **VPS z publicznym IPv4**
* **Docker + Docker Compose**

Aplikacja uruchamiana jest jako zestaw kontenerów:

* frontend,
* backend API,
* baza danych,
* reverse proxy.

Całość uruchamiana i zarządzana za pomocą `docker-compose`.

---

## CI / CD

**GitHub Actions**

Pipeline CI/CD:

1. Buduje obrazy Docker.
2. Publikuje obrazy do **GitHub Container Registry (GHCR)**.
3. Łączy się przez SSH z VPS.
4. Uruchamia deploy poprzez:

   * `docker compose pull`
   * `docker compose up -d`

Na serwerze produkcyjnym nie są wykonywane buildy obrazów.

---

## Założenia projektowe

* Stack dobrany pod **PoC / MVP**.
* Minimalna złożoność infrastruktury.
* Brak kolejek, event busów i orkiestracji na start.
* Architektura przygotowana pod późniejszą rozbudowę (skalowanie, workerzy, kolejki).
