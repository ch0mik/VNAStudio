# Spec Delta

## Purpose

Odtwarza wykresy pomiarowe i interakcje znane z Analyser 306b, z kontrolą zgodności wyglądu, jednostek i odczytów względem udokumentowanych wzorców.

## ADDED Requirements

### Requirement: Sześć przebiegów i ich konfiguracja

System SHALL wyświetlać SWR, |Z|, RL, fazę, Rs i |Xs| z niezależnym wyborem widoczności i kolorów, zgodnie z wybranym profilem wizualnym. System SHALL odtwarzać odpowiednie ustawienia kolorów z dostarczonego INI.

#### Scenario: Wczytanie kolorów użytkownika

- **WHEN** aplikacja odczytuje kolory z dostarczonego `analyz.ini`
- **THEN** tło ma kolor #F0F0F0, SWR #FF0000, RL #0000FF, faza #D78502, Rs #19FF00, |Xs| #008000 i |Z| #E81AFC

### Requirement: Niezależne skale wykresu

System SHALL mapować każdy przebieg do jego potwierdzonej skali, zakresu i kierunku osi, przy wspólnej osi częstotliwości. Etykiety osi SHALL odpowiadać wartościom używanym w tabeli pomiarowej.

#### Scenario: Różne wielkości na jednym wykresie

- **WHEN** jednocześnie włączone są SWR, faza i impedancja
- **THEN** każda krzywa korzysta z właściwej skali, a wspólna pozycja X oznacza tę samą częstotliwość

### Requirement: Markery przypisane do próbek

System SHALL ustawiać M1 lewym przyciskiem i M2 prawym przyciskiem na wykresie. Odczyty wszystkich przebiegów dla danego markera SHALL pochodzić z tej samej wskazanej próbki.

#### Scenario: Ustawienie markerów

- **WHEN** użytkownik wskazuje dwa punkty lewym i prawym przyciskiem
- **THEN** aplikacja pokazuje M1 i M2 oraz odpowiadające im częstotliwości i wartości, bez przejęcia tych kliknięć przez menu kontekstowe lub przesuwanie wykresu

### Requirement: Minimum SWR i zoom do markerów

System SHALL umożliwiać wskazanie minimum SWR dla poprawnych próbek oraz powiększenie zakresu między M1 i M2 niezależnie od kolejności ich ustawienia. System SHALL pozwalać wrócić do poprzedniego zakresu.

#### Scenario: Odwrócona kolejność markerów

- **WHEN** częstotliwość M1 jest większa od częstotliwości M2 i użytkownik wybiera zoom
- **THEN** dolną granicą staje się mniejsza częstotliwość, górną większa, a poprzedni zakres jest dostępny do przywrócenia

#### Scenario: Brak poprawnych próbek SWR

- **WHEN** w zbiorze nie ma poprawnych próbek SWR
- **THEN** aplikacja nie wyznacza pozornego minimum i pokazuje brak dostępnego wyniku

### Requirement: Brak domyślnego wygładzania i łączenia błędów

System SHALL prezentować dane bez domyślnego wygładzania i przerywać krzywą w miejscach nieprawidłowych próbek.

#### Scenario: Luka pomiarowa

- **WHEN** pomiędzy dwiema poprawnymi próbkami występuje próbka nieprawidłowa
- **THEN** wykres pokazuje przerwę zamiast sugerowanego pomiaru przez lukę

### Requirement: Weryfikowalna zgodność wizualna

System SHALL umożliwiać porównanie wykresu ze wzorcem przy tych samych danych, wymiarach i zakresie. Ustalona tolerancja geometrii SHALL być oceniana oddzielnie od różnic antyaliasingu; trafianie markerów SHALL działać przy DPI 100%, 125% i 150%.

#### Scenario: Porównanie zrzutów

- **WHEN** wzorzec i nowa aplikacja prezentują ten sam zestaw danych i profil wizualny w tych samych wymiarach
- **THEN** raport porównuje pozycje osi, etykiet, siatki, krzywych i markerów według zapisanych tolerancji

### Requirement: Tabela i sterowanie markerami

System SHALL udostępnić dwa wiersze odczytów M1/M2 ze wspólną próbką dla wszystkich kolumn, osobną widoczność markerów oraz przesuwanie do sąsiedniej próbki. Stan braku wartości SHALL być przedstawiony jako „—”.

#### Scenario: Ukrycie markera

- **WHEN** użytkownik wyłącza widoczność M1
- **THEN** znika linia markera, a zapamiętana pozycja nie jest kasowana

### Requirement: Wspólne operacje przebiegów

System SHALL udostępnić All/None oraz edycję bazowych kolorów. Zmiany SHALL zachować dane pomiarowe i spójność legendy; aktywny motyw SHALL stosować własne reguły prezentacji bez nadpisywania bazowego profilu.

#### Scenario: Wyłączenie wszystkich przebiegów

- **WHEN** użytkownik wybiera None
- **THEN** krzywe są ukryte, a dane, ustawienia kolorów i pozycje markerów pozostają zachowane

### Requirement: Oddzielne stany zakresu

System SHALL odróżniać zakres pomiaru, zakres widoku i pamięć start/stop. System SHALL zachować istniejącą historię zoomu i udostępnić osobne zapamiętanie oraz przywrócenie zakresu.

#### Scenario: Przywrócenie zapamiętanego zakresu

- **WHEN** użytkownik zapamiętuje start/stop, zmienia powiększenie, a następnie przywraca zakres
- **THEN** akcja korzysta z pamięci zakresu i nie otwiera pliku pomiarowego ani nie zastępuje danych sesji

### Requirement: Potwierdzone ustawienia DISPLAY

System SHALL udostępniać ustawienia DISPLAY Full, 50 Ω i Xc/Xl dopiero po potwierdzeniu ich semantyki. Linia odniesienia 50 Ω SHALL być niezależna od impedancji odniesienia kalibracji, a reprezentacja C/L SHALL wymagać odpowiednich danych o reaktancji.

#### Scenario: Niedostępny znak reaktancji

- **WHEN** dane zawierają wyłącznie moduł X i nie pozwalają ustalić znaku
- **THEN** aplikacja nie przypisuje arbitralnie typu pojemnościowego lub indukcyjnego
