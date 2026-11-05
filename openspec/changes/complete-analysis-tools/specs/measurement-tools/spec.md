# Spec Delta

## ADDED Requirements

### Requirement: Semantyka i stan trybów analizy

System SHALL wyświetlać, dla każdego trybu Cable Loss, Cable Length, Xtal, Filter i Generator, jego metodę, wymagania wejściowe oraz stan dostępności. System SHALL nie deklarować funkcji aktywnej wyłącznie dlatego, że przycisk trybu jest widoczny.

#### Scenario: Brak zweryfikowanego protokołu wyjściowego

- **WHEN** profil nie potwierdza komend generatora
- **THEN** sterowanie wyjściem pozostaje niedostępne, a częstotliwości pamięci są jedynie wartościami edytowymi.

### Requirement: Odrębny tor danych dla filtrów

System SHALL rozróżniać S11 od odpowiedzi transmisyjnej filtra. System SHALL pokazywać amplitudę, fazę i impedancję wyłącznie wtedy, gdy dana wielkość wynika z kontraktu urządzenia i właściwej kalibracji.

#### Scenario: Tylko zweryfikowana amplituda

- **WHEN** urządzenie udostępnia skalibrowaną amplitudę toru FILTER, ale brak potwierdzonego S21
- **THEN** widok pokazuje wyłącznie skalarną odpowiedź częstotliwościową i nie wylicza SWR ani impedancji z tej amplitudy.

#### Scenario: Protokół sprzętowy FILTER niepotwierdzony

- **WHEN** profil urządzenia nie potwierdza komendy skanu transmisyjnego
- **THEN** sterownik sprzętowy odrzuca skan przed zapisem do COM, a demo i playback zachowują osobne próbki `TransmissionDb`.

### Requirement: Jawne zakończenie kabla dla Cable Loss

System SHALL pokazywać oszacowanie jednowrotnych strat wyłącznie jako `−ReturnLossDb / 2` z etykietą oszacowania i jawnym ograniczeniem dotyczącym fixture. Potwierdzenie fixture nie może być przenoszone między trybami ani sesjami.

#### Scenario: Brak potwierdzonego open/short

- **WHEN** operator nie potwierdzi kabla na ANT z otwartym lub zwartym dalszym końcem
- **THEN** wykres Cable Loss pozostaje ukryty.

#### Scenario: Terminator 50 Ω na końcu kabla

- **WHEN** kabel jest zakończony 50 Ω
- **THEN** układ nie spełnia warunku metody jednowrotnych strat i operator nie powinien potwierdzać fixture.

### Requirement: Metody ręczne i oznaczone

System SHALL odróżniać ręczny kalkulator opisany w instrukcji od automatycznej funkcji legacy, która nie była zaprogramowana. Wynik długości kabla SHALL wymagać sąsiednich rezonansów tego samego rodzaju oraz poprawnego VF.

#### Scenario: Markery nie tworzą prawidłowej pary rezonansów

- **WHEN** markery nie obejmują dwóch sąsiednich szczytów lub dwóch sąsiednich dołków
- **THEN** długość kabla pozostaje niedostępna i UI wskazuje wymagany sposób ustawienia markerów.
