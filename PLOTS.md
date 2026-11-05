# Odtworzenie wykresów Analyser 306b w C#

> Wymagania i zadania są prowadzone w OpenSpec: [legacy-plots](openspec/changes/port-vna-to-csharp/specs/legacy-plots/spec.md), [projekt](openspec/changes/port-vna-to-csharp/design.md) i [zadania](openspec/changes/port-vna-to-csharp/tasks.md). Ten dokument pozostaje analizą rozwiązań i źródeł.

Analiza dwóch zrzutów rozszerzona o instrukcje i zasoby EXE: [macierz brakujących funkcji i etapy realizacji](openspec/changes/port-vna-to-csharp/roadmap-screenshots.md).

## Materiał odniesienia

Z instrukcji `VNA - pomiary.pdf` wyodrębniono zrzuty bez uruchamiania aplikacji:

- `../reference/hardware/screenshots/original-page2-R19.png`: pomiar anteny, wieloskalowy wykres i markery.
- `../reference/hardware/screenshots/original-page3-R26.png`: kolejny ekran pomiarowy.

Zrzuty w instrukcji mają tytuł starszej wersji programu (V1.3.2), więc są wzorcem rodziny interfejsu, nie potwierdzeniem wyglądu każdej kontrolki wersji 306b. Zasoby EXE i dostarczony INI będą dodatkowym źródłem zgodności.

W zasobach 306b występują kontrolki osi, markerów, kolorów, minimum SWR i zoomu: m.in. `lblswr*`, `LineRL*`, `LinePH*`, `Line50ohm`, `cmdZoomSweep`, `fraMarkers`, `chkMinSWR`. Nie ma podstaw do uznania `chart1` za nazwę zewnętrznej biblioteki wykresów — to nazwa formularza. W importach i odczytanych nazwach kontrolek nie znaleziono jednoznacznie dedykowanego komponentu wykresowego.

## Porównanie rozwiązań

| Rozwiązanie | Przydatność | Nakład i ograniczenia |
|---|---|---|
| Własna kontrolka Avalonia: Control.Render(DrawingContext) — wybrana przez użytkownika | Kontrola nad skalami, etykietami, siatką i markerami; możliwość dalszych wydań na różne systemy | Trzeba napisać transformacje, interakcje i eksport oraz sprawdzić DPI i poszczególne platformy |
| Własna kontrolka WPF: DrawingVisual / DrawingContext + StreamGeometry | Największa kontrola nad zgodnością wyglądu: ręczne skale, etykiety, siatka, kolejność warstw i markery | Trzeba napisać transformacje osi, zoom, interakcje i eksport |
| ScottPlot 5 / ScottPlot.WPF | Szybciej uzyskać działające wykresy, wiele osi i interakcje; MIT | Potrzebne własne ticki, style, reguły skalowania i nadpisanie domyślnych akcji myszy; nie gwarantuje wyglądu 1:1 |
| OxyPlot / OxyPlot.Wpf | Alternatywa dla wykresów technicznych z osiami i sterowaniem wejściem; MIT | Podobnie wymaga dopasowania osi i interakcji; domyślny styl różni się od oryginału |

Decyzja użytkownika: Avalonia UI z własną kontrolką wykresu. Pierwszy prototyp `VnaPlotControl` rysuje siatkę, zakres częstotliwości, sześć przebiegów w kolorach INI oraz markery M1/M2 i minimum SWR. Używa wstępnych zakresów SWR 1–10, RL −30–0 dB, fazy 0–180° i impedancji 0–300 Ω na podstawie przykładu z instrukcji. Te zakresy i zwroty wymagają potwierdzenia na odczytach 306b, a kontrolka nie ma jeszcze zweryfikowanej geometrii ani responsywności. WPF, ScottPlot i OxyPlot pozostają porównanymi alternatywami.

## Zakres odtworzenia

1. Wspólna oś częstotliwości z wejściem w Hz i etykietami MHz; niezależne mapowanie Y dla SWR, RL, fazy i impedancji. Odtworzyć zwroty osi i zakresy dla każdego trybu. Nie zakładać skali logarytmicznej SWR bez sprawdzenia kodu i wzorcowych odczytów.
2. Kolory i widoczność sześciu przebiegów: SWR, |Z|, RL, faza, Rs i |Xs|. Odczytywać rzeczywiste kolory z INI; zrzut pokazuje inną konfigurację niż dostarczony plik.
3. Marker M1 ustawiany lewym przyciskiem, M2 prawym; wartości z tej samej próbki dla wszystkich przebiegów; dodatkowo znacznik minimum SWR.
4. Zoom do przedziału M1–M2 i zachowanie cofania zoomu zgodne z oryginałem. Automatyczne interakcje biblioteki nie mogą przejąć przycisków używanych przez markery.
5. Osobne warstwy: tło/siatka, przebiegi, markery. Dla 649 punktów na przebieg nie ma na tym etapie powodu zakładać konieczności specjalnego silnika GPU; wydajność zweryfikować na docelowym sprzęcie.
6. Przerywać linię na błędnych/brakujących próbkach, nie wygładzać danych pomiarowych bez wyraźnej opcji. Wartości wyświetlane na osiach i w tabeli muszą korzystać z tych samych jednostek i transformacji.
7. Skalowanie DPI i trafianie markerem w próbkę sprawdzić dla 100%, 125% i 150%. Porównania obrazów wykonywać dla tego samego rozmiaru pola wykresu, danych, czcionek i zakresów; osobno badać błędy geometrii oraz antyaliasing.

Ewentualny wykres Smitha z vna/J wymaga osobnego modułu i weryfikacji. Nie wynika z wyodrębnionego ekranu Analyser 306b.

## Źródła techniczne

- Avalonia, własne kontrolki: https://docs.avaloniaui.net/docs/custom-controls/custom-drawn-controls
- Avalonia, platformy: https://docs.avaloniaui.net/docs/supported-platforms
- WPF DrawingVisual: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/using-drawingvisual-objects
- WPF geometrie i wydajność: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-2d-graphics-and-imaging
- ScottPlot, platformy i MIT: https://github.com/ScottPlot/ScottPlot
- ScottPlot, wiele osi: https://scottplot.net/cookbook/5/MultiAxis/
- ScottPlot, markery/crosshair: https://scottplot.net/cookbook/5/Crosshair/
- ScottPlot, DPI WPF: https://scottplot.net/faq/dpi-scaling/
- OxyPlot, osie: https://oxyplot.readthedocs.io/en/latest/models/axes/
- OxyPlot, licencja: https://github.com/oxyplot/oxyplot/blob/develop/LICENSE
