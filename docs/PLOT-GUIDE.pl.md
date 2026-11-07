# Obsługa wykresów

VNA Studio udostępnia widoki Frequency, Smith i Both dla pomiarów Antenna. Wybrany widok jest zapisywany w `settings.json`. Przyciski wykresu zmieniają widok; nie zmieniają parametrów wykonanego skanu.

## Wykres częstotliwościowy

- Kliknij lewym przyciskiem wewnątrz wykresu, aby ustawić M1; prawym — M2. Przeciągnij linię markera, aby go przesunąć. Gdy wykres ma fokus, strzałki Left/Right przesuwają aktywny marker o jedną zmierzoną próbkę.
- Użyj `+` / `−` lub przycisków, aby powiększyć zakres częstotliwości. **Zoom M1–M2** działa także, gdy M2 ma niższą częstotliwość niż M1. **Poprzedni zakres** cofa historię zoomu.
- **Pełny zakres** przywraca cały wczytany skan. **Zakres profilu** ustawia granice wybranego profilu. **Zapisz zakres** i **Przywróć zakres** przechowują tymczasowy widok; nie zapisują ani nie otwierają plików pomiaru.
- Przytrzymaj środkowy przycisk myszy i przeciągnij albo użyj Shift+lewy przycisk, aby przesuwać wykres poziomo. Widok jest ograniczony zakresem profilu.
- Checkboxy przebiegów lub Wszystkie/Żadne wybierają wyświetlane wielkości. Zmiana koloru dotyczy preferencji widoku, nie zapisanych próbek.
- Brakujące, niefinitywne lub niepoprawne próbki przerywają przebieg. Wykres nie interpoluje ani nie wygładza luk pomiarowych.

Oś częstotliwości jest liniowa. Każda wielkość ma własną skalę pionową; obecne wartości ticków pochodzą z dostarczonych zrzutów ekranu. Nie ustalono jeszcze reguł legacy dla każdego zakresu częstotliwości Analyser 306b.

## Wykres Smitha

- Kliknięcie lewym przyciskiem ustawia M1, prawym — M2, na najbliższej widocznej częstotliwości pomiarowej. Przeciąganie przesuwa marker.
- Użyj przycisków zoomu Smitha lub kółka myszy, aby powiększyć widok wokół wskaźnika. Środkowy przycisk myszy przesuwa wykres.
- Najedź kursorem na punkt, aby zobaczyć częstotliwość, impedancję, impedancję odniesienia i pewność znaku fazy.
- Gdy źródło nie ustala fizycznego znaku fazy/reaktancji, wykres pokazuje oba lustrzane rozwiązania. Nie zgaduje, czy obciążenie ma charakter indukcyjny, czy pojemnościowy.
- Jeśli zbiór nie zawiera użytecznej fazy ani impedancji zespolonej, wykres wyjaśnia, że nie ma punktu do pokazania. Nieznana impedancja odniesienia nie jest zastępowana przez 50 Ω.

## Zmiana rozmiaru i ograniczenia danych

Okno można zmieniać i maksymalizować; minimalny rozmiar to 1024×768 DIP. Panel ustawień przewija się pionowo. Wykres i tabela markerów dopasowują się do rozmiaru okna. Testy zmiany rozmiaru i DPI używają renderera headless Avalonia; natywny menedżer okien może zachowywać się różnie na poszczególnych platformach.

### Pomiar renderowania 649 punktów

Lokalny pomiar Release użył 649 syntetycznych próbek `DemoVnaDevice`, wszystkich sześciu widocznych przebiegów, okna 1440×900 DIP i 20 rozgrzanych renderowań headless. Mediana czasu unieważnienia, layoutu, wymuszonego renderu i przechwycenia obrazu wyniosła **13,681 ms**, a zaobserwowany zakres **11,803–26,133 ms**. To lokalny test wydajności renderera headless, nie gwarancja dla innych komputerów ani natywnego backendu. PNG i plik czasu są zapisywane w ignorowanym katalogu kompilacji `tests/Vna.Tests/bin/Release/net10.0/layout-review/`.

## Dokładność i ograniczenia porównań

Geometria wykresu, etykiety ticków, markery, luki danych i responsywny układ mają testy regresji. Matematyka wykresu Smitha jest testowana na idealnych obciążeniach i analitycznych liczbach zespolonych. Dokładna zgodność pikselowa z oryginałem, profile ticków dla wszystkich zakresów oraz fizyczna dokładność fazy/kalibracji pozostają niezweryfikowane. Pomiary terminatorem 50 Ω na COM9 sprawdzały podstawową akwizycję i spójność, a nie kalibrację metrologiczną. Zobacz [wektory referencyjne i granice tolerancji](../openspec/changes/port-vna-to-csharp/reference-vectors-and-tolerances.md) oraz [szczegóły komunikacji VNA](VNA-COMMUNICATION.pl.md).
