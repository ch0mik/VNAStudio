# Spec Delta

## Purpose

Planuje narzędzia obecne na ekranach i w instrukcjach oryginału: różnicę częstotliwości, Q, pomiary kabli, FILTER, generator oraz analizę rezonansów.

## ADDED Requirements

### Requirement: Różnica częstotliwości markerów

System SHALL pokazywać `abs(f2−f1)` ze wspólnego zestawu próbek w jednoznacznej jednostce. Różnica markerów SHALL być odróżniana od pasma wyznaczonego automatycznie według kryterium pomiarowego.

#### Scenario: Odwrócone markery

- **WHEN** M1 ma częstotliwość 7148000 Hz, a M2 3842000 Hz
- **THEN** różnica wynosi 3306000 Hz i nie zależy od kolejności markerów

### Requirement: Q z określoną metodą

System SHALL podawać metodę Q, częstotliwość odniesienia i sposób wyznaczenia granic pasma. System SHALL pokazywać brak wyniku dla zerowej szerokości, brakujących próbek lub niepotwierdzonej metody. System SHALL NOT przedstawiać liczby z niezweryfikowanej hipotezy algorytmu legacy jako potwierdzonego Q rezonatora.

#### Scenario: Brak częstotliwości rezonansu

- **WHEN** użytkownik ustawił dwa markery, ale metoda wymaga f0 i nie ma poprawnej wartości f0
- **THEN** aplikacja pokazuje różnicę częstotliwości i przyczynę niedostępności Q

### Requirement: Osobny model transmisji

System SHALL odróżniać amplitudę transmisji od return loss i odbicia. Narzędzie FILTER SHALL używać właściwej kalibracji Through/LOOP i pokazywać wyłącznie wielkości dostępne z potwierdzonej zdolności urządzenia.

#### Scenario: Transmisja skalarna

- **WHEN** sterownik udostępnia wyłącznie amplitudę transmisji w dB
- **THEN** FILTER nie pokazuje pozornie wyliczonego SWR ani impedancji zespolonej i nie deklaruje pomiaru fazy S21

### Requirement: Długość i straty kabla

System SHALL obliczać długość i straty wyłącznie metodą przypisaną do znanego układu pomiarowego. Długość SHALL uwzględniać VF, a straty SHALL jawnie określać jedno- lub dwukierunkowość odczytu.

#### Scenario: Nieprawidłowy współczynnik kabla

- **WHEN** VF jest niedodatnie, większe od 1 albo niefinitywne
- **THEN** aplikacja odrzuca obliczenie i zachowuje poprzednie dane wejściowe

### Requirement: Generator z potwierdzonym kontraktem

System SHALL udostępniać ON/OFF, częstotliwość i dziesięć pamięci dla sterownika potwierdzającego obsługę generatora. Zakres generatora i zachowanie rozłączenia SHALL wynikać z kontraktu. System SHALL NOT uruchamiać wyjścia wyłącznie na podstawie otwarcia zakładki.

#### Scenario: Wybór pamięci przy wyłączonym wyjściu

- **WHEN** użytkownik wybiera jedną z dziesięciu częstotliwości przy stanie OFF
- **THEN** aplikacja waliduje i ustawia wybór bez niejawnego uruchomienia wyjścia

### Requirement: Analiza kwarcu i rezonansów

System SHALL umożliwić analizę dostępnych pomiarów rezonansu według jawnej metody. Automatyczne wyznaczanie parametrów zastępczych kwarcu SHALL być oddzielną zdolnością, wymagającą zweryfikowanych algorytmów i wzorców.

#### Scenario: Niedostępny automat XTAL

- **WHEN** brak potwierdzonego algorytmu automatycznej analizy kwarcu
- **THEN** aplikacja może udostępniać pomiar FILTER i narzędzia ręczne, a automat jest oznaczony jako niedostępny
