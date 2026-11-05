# Proposal

## Why

Użytkownik potrzebuje aplikacji C# dla swojego VNA do 65 MHz, z wykresami i obsługą pomiarów znanymi z Analyser 306b. Docelowo program ma odtworzyć uzgodnione funkcje vna/J 3.4.8, a pierwszy etap ma potwierdzić działanie na posiadanym urządzeniu.

## What Changes

- Powstanie aplikacja C# z Avalonia UI i własną kontrolką wykresu, z oddzielnymi modułami obliczeń RF, sterowników, plików i interfejsu. Pierwszym środowiskiem wdrożenia będzie Windows.
- Pierwszy profil urządzenia obejmie konfigurację 1–65 MHz, zegar DDS 191993220 Hz i komunikację przez wirtualny COM/FT232; protokół wymaga potwierdzenia.
- Wykresy odtworzą kolory, niezależne skale, markery M1/M2, minimum SWR i zoom do markerów, z odniesieniem do zasobów Analyser 306b.
- Tryb demonstracyjny i odtwarzanie zapisanych próbek pozwolą rozwijać oraz sprawdzać program bez urządzenia.
- Interfejs obsługuje język polski i angielski przez pliki `.resx`, przełączanie w czasie pracy i zapis języka w ustawieniach użytkownika.
- Kalibracja, formaty plików i kolejne funkcje vna/J będą wdrażane według macierzy zgodności, z rozróżnieniem funkcji potwierdzonych i niezweryfikowanych.
- Wyniki dekompilacji pozostają materiałem analitycznym. Sam pseudokod nie będzie traktowany jako kompletna implementacja ani dowód zgodności.
- Funkcje z dostarczonych ekranów są rozpisane w `roadmap-screenshots.md`: tabela i sterowanie markerami, ustawienia DISPLAY, pamięć zakresów, BW/Q, Hold i źródła danych, kalibracja, FILTER, kable, generator oraz pliki legacy. Niejasne zachowania wymagają potwierdzenia przed implementacją.

## Capabilities

### New Capabilities

- `device-acquisition`: konfiguracja urządzenia, pomiary, przerwanie i obsługa awarii połączenia.
- `rf-calibration`: przeliczanie surowych próbek, kalibracja i kontrola zgodności wyników RF.
- `legacy-plots`: wykresy, niezależne skale, kolory i interakcje zgodne z oryginalną aplikacją.
- `measurement-files`: zapis, odczyt i odtwarzanie sesji oraz jawna zgodność starszych formatów.
- `localization`: zasoby PL/EN, zmiana języka, formatowanie liczb i zachowanie ustawienia języka.
- `release-packaging`: kompilacja paczek RID po publikacji release, wersjonowanie, sprawdzenia i załączniki z sumami SHA-256.
- `app-themes`: warianty Light/Dark/NightVision interfejsu i wykresu, zachowanie ustawienia oraz podglądy README.
- `measurement-tools`: różnica częstotliwości markerów, Q z jawną metodą, transmisja, analiza kabli i rezonansów oraz generator.

### Modified Capabilities

Brak istniejących specyfikacji OpenSpec; sprawdzono `openspec list --specs`.

## Impact

Moduły: `Vna.Localization`, `Vna.Core`, `Vna.Devices`, `Vna.IO`, `Vna.App`, `Vna.Tests`. Materiały wejściowe to `PLAN.md`, `PLOTS.md`, instrukcje, INI i wyniki analizy lokalnych binariów. Materiały źródłowe i wyniki dekompilacji są przechowywane obok katalogu projektu, poza repozytorium. Implementacja obejmuje demonstrator, sterownik COM oparty na odzyskanym kontrakcie, odtwarzanie sesji i interfejs; fizyczna zgodność sprzętowa pozostaje niezweryfikowana. Zmiana nie publikuje kodu pochodnego.
