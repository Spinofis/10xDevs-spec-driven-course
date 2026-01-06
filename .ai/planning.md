Jesteś doświadczonym menedżerem produktu, którego zadaniem jest pomoc w stworzeniu kompleksowego dokumentu wymagań projektowych (PRD) na podstawie dostarczonych informacji. Twoim celem jest wygenerowanie listy pytań i zaleceń, które zostaną wykorzystane w kolejnym promptowaniu do utworzenia pełnego PRD.

Prosimy o uważne zapoznanie się z poniższymi informacjami:

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

Przeanalizuj dostarczone informacje, koncentrując się na aspektach istotnych dla tworzenia PRD. Rozważ następujące kwestie:
<prd_analysis>
1. Zidentyfikuj główny problem, który produkt ma rozwiązać.
2. Określ kluczowe funkcjonalności MVP.
3. Rozważ potencjalne historie użytkownika i ścieżki korzystania z produktu.
4. Pomyśl o kryteriach sukcesu i sposobach ich mierzenia.
5. Oceń ograniczenia projektowe i ich wpływ na rozwój produktu.
</prd_analysis>

Na podstawie analizy wygeneruj listę 10 pytań i zaleceń w formie łączonej (pytanie + zalecenie). Powinny one dotyczyć wszelkich niejasności, potencjalnych problemów lub obszarów, w których potrzeba więcej informacji, aby stworzyć skuteczny PRD. Rozważ pytania dotyczące:

1. Szczegółów problemu użytkownika
2. Priorytetyzacji funkcjonalności
3. Oczekiwanego doświadczenia użytkownika
4. Mierzalnych wskaźników sukcesu
5. Potencjalnych ryzyk i wyzwań
6. Harmonogramu i zasobów

<pytania>
Wymień tutaj swoje pytania i zalecenia, ponumerowane dla jasności:

Przykładowo:
1. Czy już od startu projektu planujesz wprowadzenie płatnych subskrypcji?

Rekomendacja: Pierwszy etap projektu może skupić się na funkcjonalnościach darmowych, aby przyciągnąć użytkowników, a płatne funkcje można wprowadzić w późniejszym etapie.
</pytania>

Kontynuuj ten proces, generując nowe pytania i rekomendacje w oparciu o odpowiedzi użytkownika, dopóki użytkownik wyraźnie nie poprosi o podsumowanie.

Pamiętaj, aby skupić się na jasności, trafności i dokładności wyników. Nie dołączaj żadnych dodatkowych komentarzy ani wyjaśnień poza określonym formatem wyjściowym.

Pracę analityczną należy przeprowadzić w bloku myślenia. Końcowe dane wyjściowe powinny składać się wyłącznie z pytań i zaleceń i nie powinny powielać ani powtarzać żadnej pracy wykonanej w sekcji prd_analysis.




1. Myślę że możemy dodać listę tagów które można by zaznaczać, typu np city break, architektura, natura, atrakcje dla dzieci. Zaproponuj jakąś listę tagów. Następnie na podstawie tagów można by uzupełniać dodatkowo prompt przed wysłaniem do ai
2. Notatki tworzymy ręcznie, pole z zakresem dat i preferowana dlugoscia podrozy to spoko pomysl.
Takie preferencje tez moga byc zapisane jako defaultowe uzytkownika i pozowlic mu uzupelnic je wszystkie od razu za pomoca jakiegos guzika , uzyj domyslnych preferencji.
Czy pole na miejsce jest potrzebne? pewnie czesc uzytkownikow bedzie chciala wpisac jakies ogolne informacje, a my powinnismy zaproponowac mu miejsce, a czesc byc moze wskaze jakies miasto i zebysmy my zaplnowali wycieczke w tym miejsce. Wydaje mi sie ze bez podawania pola na miejsce ale mozesz to przeanalizowac sam.
3. Minimalne dane to tak: na pewno zakres dat i czas wycieczki, miejsce startowe tez, liczba osob tez, budzet tez, tempo zwiedzanie tez moze byc i te pola wydaje mi sie ze powinny byc strukturalne, reszta to notatki i wyzej wspomniane tagi, jako labele do zaznaczania.
4. Zróbmy dzień po dniu listę a w danym dniu podsekcje z informacjami co robimy danego dnia, gdzie spimy itd, dodatkowo propozycje restauracji w danym dniu w zaleznosci od tego gdzie jestesmy
5. Robimy prostą edycje tekstową + opcja rewygeneruj
6. Peferencje to tagi z pkt 1 moich odpowiedzi oraz pola strukturalne z pkt 3. Wydaje mi sie ze nie powinny byc obowiazkowe na etapie uzupelenienia profilu, tylko przy planowaniu kazdej wycieczki opcja uzyj zapisanych preferencji  z profilu, to pozwoli nam na pewna elastycznosc
7. Jesli chodzi o liste preferencji ktore maja najwiekszy wplyw to zrobmy tak ze pozwolimy uzytkownikowi uszeregowac zaznaczone tagi w kolejnosci od najwazniejszego do najmniej i zaznaczymy to pozniej w skonwertowanym prompcie
8. Nie przywiazujmy na tym etapie uwagi do regulacji prawnych i usuwania kony, to ma byc w miare bezpieczna autentykacja i prosta w implemenatacji, moze byc prosta wersja OAuth albo basic
9. Na tym etapie pomijamy ten punkt, jesli uzytkownikowi cos nie pasuje moze wygenerowac plan ponownie
10. Platforma web , mobile nie, bede robil to sam z pomoca ai, budzet ai - nie skupiajmy sie na tym dostosuje go pozniej 



1. Zrobmy tak jak w rekomendacji 
2. Na pewno zakres dat w ktorym mamy szukac , dlugosc pobytu (jakis przedzial), budzet i liczba osob
3. Uzupelnia puste pola
4. Komunikat/label, uszereguj preferencje wg ważności - coś w tym stylu
5. Dozwolona zmiana pól/ notatek i opcja wygeneruj ponowanie
6. Wg rekomendacji
7. Niech to bedzie lista, ktora mozna posortowac i wyfiltrofac. Sortujmy i filtrujmy po dacie, po wygenerowanych i niewygenerowanych.
8. Jesli uzytkownik zapisze wygenerowany plan to mierzmy to jako faktycznie wygenerowany
9. kluczowe jest zapisanie planu po generacji i uzupelnienie preferencji
10. Brak pustch dni, jesli jakis nocleg powtarza sie danego dnia co raczej jest prowdopodobne niech bedzie wpisany jeszcze raz w drugi dzien, aktywnosci moga sie powtarzac miedzy dniami ale niezbyt czesto, nie wiem jak to mierzyc. Na pewno kazdy dzien musi zawierac aktywnosc nocleg restauracje.
11. wg rekomendacji
12. Nie wychodzimy nic poza to co opisalem, zadnego sharowania panow notatek, zadnych importow, 
brak mapy, input i output od usera w sposob jakie opisalem wyzej


1. zrobmy jadnak wszystko na jednym formularzu, wymagane do zakres dat, dlugosc pobytu, liczba osob 
2. wydaje mi sie ze od 2 dni do 1. zrobmy jadnak wszystko na jednym formularzu, wymagane do zakres dat, dlugosc pobytu, liczba osob 
2. wydaje mi sie ze od 2 dni do 21 dni, i ma wybrac jedna dlugosc pobytu z wskazanego zakresu
3. budzet calosciowy
4. Pole tekstowe
5. uzytkownik ma swiadomie zapisac plan, przed opuszczenie strony powinien byc komunikat przypominajacy mu ze tego nie zrobil
6. Nie, tylko jeden plan, kolejny adpisuje poprzedni
7. data generacji okn
8. Nie generujemy automatycznie planu ponownie, wyswietlamy komunikat ze pola przez uzytkownika sa zmienione a plan jest wygenerownay dla innych pol
9. wg rekomendacji
10. wg rekomendacji
11. wg rekomenracji
12. 
21 dni, i ma wybrac jedna dlugosc pobytu z wskazanego zakresu
3. budzet calosciowy
4. Pole tekstowe
5. uzytkownik ma swiadomie zapisac plan, przed opuszczenie strony powinien byc komunikat przypominajacy mu ze tego nie zrobil
6. Nie, tylko jeden plan, kolejny nadpisuje poprzedni
7. data generacji ok
8. Nie generujemy automatycznie planu ponownie, wyswietlamy komunikat ze pola przez uzytkownika sa zmienione a plan jest wygenerownay dla innych pol
9. wg rekomendacji
10. wg rekomendacji
11. wg rekomenracji
12. 
