# Wektory referencyjne i tolerancje — audyt offline

Stan: 2026-11-07. Ten audyt porządkuje testy i próbki już utrzymywane w repozytorium. Rozróżnia wektory analityczne, screenshotowe i rzeczywiste próbki COM9. Nie zamienia powtarzalności testu matematycznego w twierdzenie o kalibracji RF urządzenia.

## Wektory i ich pochodzenie

| Obszar | Wersjonowane źródło | Wektor / oracle | Przyjęta tolerancja testowa | Co ten dowód potwierdza |
| --- | --- | --- | --- | --- |
| DDS i sweep | `tests/Vna.Tests/MiniVnaSerialDeviceTests.cs`; `src/Vna.Device.Vna65MHz/Vna65MHzMeasurementSource.cs` | Wektor dotyczy sweepu 1–4 MHz, N=3, z profilem `DDS_CLOCK=191993220`: komendy ASCII `0\r`, `22370411\r`, `3\r`, `22370411\r`; zgłoszone próbki to 1, 2 i 3 MHz, a Stop nie jest próbką. Word zmienia się dla innego zakresu lub N. | Komendy i wordy: dokładna równość bajtów/liczb całkowitych. Częstotliwość próbki liczona deterministycznie do Hz; brak niezależnego pomiaru zegara DDS. | Regresja implementacji i konwencji dzielnika N, nie dokładność fizycznej referencji zegara. |
| ADC odbicia dla profilu użytkownika | `tests/Vna.Tests/LegacyAdcConversionTests.cs`; `MiniVnaSerialDeviceTests.cs`; `docs/measurements/2026-11-04-com9-50ohm.md` | Jeden zachowany wektor z rzeczywistej ramki COM9/50 Ω: `AF 00 C1 03` → phase ADC 175, magnitude ADC 961. Drugi punkt w teście serial-source jest wyłącznie wypełnieniem kontraktu sweepu. | Signed S11 i faza porównywane do 8 miejsc; SWR/R/X w jawnych przedziałach zaokrąglonych. Nie przypisano dokładności przyrządu, terminatora ani połączeń. | Odczyt bajtów i zgodność wzoru z odzyskaną ścieżką RF dla dostarczonego profilu. Fizyczne znaki X oraz `PH_b != 0` pozostają niepotwierdzone. |
| Powtarzalność 50 Ω | `docs/measurements/2026-11-04-com9-50ohm.md` | Trzy skany 1–65 MHz/649 punktów; zakresy SWR, rezystancji, signed S11 i wybrane próbki około 1, 14 i 64.9 MHz. | To obserwacje, nie limity akceptacyjne. Tolerancja/świadectwo terminatora, adaptery i kalibracja nieznane. | Podstawowy transport i bliskie 50 Ω odczyty na pojedynczym egzemplarzu. Nie kalibrację absolutną ani dokładność w paśmie. |
| Przeliczenie ADC i affine calibration | `tests/Vna.Tests/LegacyAdcConversionTests.cs` | Oba `VNA_type`, kolejność affine → odniesienie/polarność → offset; jawne przykłady `−18.5 dB`, `5.66 dB`, `35°`; wejście niefinitywne daje `Invalid`. | Osiem miejsc po przecinku dla double; asercje wartości null/statusu są dokładne. | Kolejność kodu i zachowanie numeryczne wobec wzoru źródłowego, nie kalibrację legacy na OPEN/SHORT/LOAD. |
| Smith chart | `tests/Vna.Tests/SmithChartMathTests.cs` | Ideal 50 Ω match, short, open; 25/50/100 Ω przy 50 Ω oraz 37.5/150 Ω przy 75 Ω; znane `Γ=0.2±j0.4`; zespolone round-trip. | `Γ` do 12 miejsc, impedancja odwrotna do 10; near-open test: względny błąd ≤ `1e-12`; siatka wewnątrz koła z marginesem `1e-12`. | Implementację standardowego mapowania matematycznego oraz geometrię koła. Nie znak fazy fizycznego urządzenia, konwencję dla danych niepewnych ani zgodność wizualną z vna/J. |
| Siatki i skale wykresu częstotliwości | `tests/Vna.Tests/PlotGeometryTests.cs`; screenshoty S1/S2; `roadmap-screenshots.md` | Ticki SWR, signed RL/RL×2, fazy i Ω ze screenshotów; transformacje końców, środka i etykiet. | Numeryczne mapowanie wartości–pozycja do 8 miejsc; tekst etykiet i wartości ticków dokładnie. Nie ma ustalonej tolerancji pikselowej dla antyaliasingu ani zakresowych ticków nieobecnych na zrzutach. | Zgodność wewnętrzną wspólnej transformacji oraz widocznych wartości screenshotowych, nie pełne odtworzenie handlera legacy. |
| Szerokość/Q z markerów | `tests/Vna.Tests/PlotInteractionTests.cs`; `roadmap-screenshots.md` | Dwa screenshotowe zestawy: 3.842/7.148 MHz → BW 3.306 MHz, Q pokazane 2; 18.582/21.532 MHz → BW 2.950 MHz, Q pokazane 7. | BW: dokładne odejmowanie całkowitych Hz; Q: wynik zaokrąglony zgodny z podanymi obrazami. Źródło wzoru Q pozostaje hipotezą. | Reprodukcję wartości z obrazów, nie potwierdzony algorytm oryginału ani fizyczne Q anteny/filtra. |
| Rozmiar/DPI i markery | `tests/Vna.Tests/PlotInteractionTests.cs`; `MainWindowResizeTests.cs` | Headless layout 1024×768, 1360×840, 1920×1080; DPI 100/125/150; markery na próbce w widocznym zakresie. | Wymiary/layout sprawdzane zakresami i stanami widoczności; współrzędne wykresu w DIP; brak oracle dla zachowania natywnego window managera w tych testach. | Responsywny Avalonia layout/rendering i interakcje w headless. Nie zachowanie OS window managera na każdym systemie. |

## Tolerancje i status walidacji

- Dokładna równość bajtów i całkowitych wordów jest właściwa dla wysyłanych komend oraz kontrolowanego transportu w pamięci.
- Tolerancje 8–12 miejsc dotyczą oracle liczbowych z wzorów double/zespolonych; są tolerancjami testu implementacji, a nie specyfikacją producenta.
- Raport COM9 zawiera rozrzut obserwowanych wyników, nie zatwierdzony limit błędu. Nie wyliczono metrologicznej niepewności bez wzorców z certyfikatem.
- Screenshoty zapewniają oracle tekstów, kolejności osi i kilku pozycji markerów. Nie ma zatwierdzonych obrazów surowych, rozmiaru pola i tolerancji różnicy pikselowej dla pełnego Analyser 306b/vna/J.
- Rzeczywisty wektor COM9 zawiera jedną kompletną próbkę. Pełne binarne przechwycenia są poza repozytorium; test nie używa fikcyjnych punktów jako rzeczywistego pomiaru.

## Luki wymagane do zamknięcia zadania 1.7

1. Wygenerować/wydobyć pełny, pochodzący z konkretnego egzemplarza wektor ADC oraz zgodny sweep z oryginalnej aplikacji; wskazać port/profil/fixture, hash pliku i ograniczenia kalibracji.
2. Pozyskać OPEN/SHORT i niezależny LOAD oraz referencję częstotliwości z określoną niepewnością; dopiero z tego określić fizyczne tolerancje RL, fazy, Z, SWR i siatki DDS.
3. Dla wykresów zachować obraz i ustawienia referencyjne 306b/vna/J, określić jawne tolerancje geometrii i testy porównawcze przy tej samej skali/rozmiarze/DPI.
4. Otrzymać rzeczywisty legacy `.vec` i warianty CSV, by utworzyć plikowe oracle bez zgadywania schematu.

Pozycja 1.7 pozostaje otwarta, aż te niezależne źródła będą dostępne. W tym etapie urządzenia ani WOB nie podłączano.
