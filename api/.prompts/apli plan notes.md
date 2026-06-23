1. Plan nie przechowujmy jako html , jest do tego tabla trip plan i plan item, zwracajmy plan jako json z danymi planu oraz itemu, popraw tez prd w tym zakresie
2.zamien nazwe slug na code
3. dlaczego post /trips ma tylko notes, nie powinnismy przekazac wiecej parametrow? wiem ze updatujemy inne parametry w patch, ale po co 
przekazywac dwa oddzielne requesty, niech post trips odbiera tez inne parametry oprocz notes tak jak ptach, jak tags i dates np
4. przy generacji joba dodaj ze jesli job skonczy sie generowac to sprawdza czy nie ma nowszego juz dla tej samej wycieczki i jesli jest to nie zapisuje planu dla niego

runda 2
1. czemu mamy oddzielny endpoint na pobieranie tagow
/me/preference-tags
niech to bedzie pobrane razem w /me/profile
2. Metody sa jakies wybrakowane brakuje w niektorych opisow, request, response
Wydaje mi sie ze poprzedni plan to mial, podobal mi sie poprzedni plan wez go w calosci tylko doloz te zmiany o ktore cie prosilem czyli
<zmiany-v2>
1. Plan nie przechowujmy jako html , jest do tego tabla trip plan i plan item, zwracajmy plan jako json z danymi planu oraz itemu, popraw tez prd w tym zakresie
2.zamien nazwe slug na code
3. dlaczego post /trips ma tylko notes, nie powinnismy przekazac wiecej parametrow? wiem ze updatujemy inne parametry w patch, ale po co 
przekazywac dwa oddzielne requesty, niech post trips odbiera tez inne parametry oprocz notes tak jak ptach, jak tags i dates np
4. przy generacji joba dodaj ze jesli job skonczy sie generowac to sprawdza czy nie ma nowszego juz dla tej samej wycieczki i jesli jest to nie zapisuje planu dla niego
</zmiany-v2>
Oto przyklady brakow o ktorych mowilem wyzej
GET /trips

Supports filtering, pagination, and sorting:

hasPlan=true|false

sort=-createdAt|-generatedAt

GET /trips/{tripId}
PATCH /trips/{tripId}

Partial update of trip parameters.

DELETE /trips/{tripId}

WAZNE! nie renderuj mi planu w czaci, stworz mi plik to pobrania api_plan.md