# Macierz funkcji i modeli — Analyser 306b oraz vna/J 3.4.8

Stan źródeł: 2026-11-07. Macierz rozdziela implementację VNA Studio od inwentaryzacji zachowania starszych programów. Obecność klasy, przycisku lub napisu nie jest dowodem zgodności sprzętowej.

## Priorytety

| Priorytet | Znaczenie |
| --- | --- |
| P0 | Bezpieczny, powtarzalny pomiar i poprawne pochodzenie danych dla konkretnego, zidentyfikowanego profilu. |
| P1 | Interfejs, wykresy, odczyty, sesje i formaty potrzebne do codziennej pracy offline. |
| P2 | Funkcje wymagające osobnego algorytmu, kalibracji, obciążenia lub właściwego toru ANT–WOB. |
| P3 | Nowe profile urządzeń i zachowanie pokrewnego oprogramowania, bez założenia zgodności z VNA 1–65 MHz. |

## Modele i granice zgodności

| Model / rodzina | Źródło i stan analizy | Stan VNA Studio | Priorytet i kryterium dodania |
| --- | --- | --- | --- |
| **VNA 1–65 MHz użytkownika** | Obudowa i porty rozpoznane ze zdjęć; profil i tryb odbicia opisane w `docs/VNA-COMMUNICATION.md`; skan COM9 i terminator 50 Ω odnotowane w raporcie pomiarowym. PCB i rewizja firmware nieznane. | Osobny plugin `Vna.Device.Vna65MHz`; odbicie działa w oprogramowaniu i jest sprawdzone na COM9. Nie deklaruje potwierdzonej kalibracji absolutnej, znaku X ani funkcji WOB/Generator. | **P0.** Zachować komendy/ramki z profilu. Każda nowa zdolność sprzętowa wymaga dowodu dla tego modelu i właściwego fixture. |
| **Analyser 306b 3.00.0006** | Główny eksport PE32/VB6 zawiera pseudokod dla 74/78 kandydatów; brakujący pseudokod uzupełniono wybranymi analizami Ghidra, w tym handlera trybów. Ograniczenia i nierozpoznane procedury są w [mapie dekompilacji](../../../../VNA-ReverseEngineering/inventory/decompilation-function-map-2026-11-06.md) oraz [review handlera](reviews/2026-11-04-targeted-mode-handler.md). Brak odzyskanego projektu VB6. | Główne źródło historycznych funkcji interfejsu i profilu `analyz.ini`; tożsamość programu nie jest nazwą profilu urządzenia. | **P0–P2.** Każda pozycja macierzy funkcji niżej wymaga osobnego dowodu; nie wyprowadzać semantyki wyłącznie z etykiet. |
| **miniVNA230 2.03** | Oddzielny PE32/VB6; pseudokod dla 135/142 kandydatów. Dostarczony jako osobny program/pakiet, nie dowodzi, że urządzenie użytkownika jest miniVNA. | Brak sterownika miniVNA230. Nie mieszać ustawień/konwersji jego programu z profilem VNA 1–65 MHz. | **P3.** Osobny profil dopiero po identyfikacji zgodnego sprzętu, porównaniu protokołu, plików i praw do publikacji. |
| **vna/J `serial/std`** | Statycznie przejrzana klasa z JAR 3.4.8 przez `javap`: cztery bajty/próbkę, dwa 16-bitowe kanały big-endian, tryby 0/1 i tekstowe słowa częstotliwości. Szczegóły i ograniczenia w [raporcie javap](../../../../VNA-ReverseEngineering/analysis/vnaj-3.4.8-javap-static-review-2026-11-07.md). | Zachowanie służy jako porównawczy materiał; nie używamy go jako protokołu VNA65. | **P3.** Pełna analiza klasy bazowej, kalibracji, surowych wektorów, praw/licencji oraz identycznego testu na sprzęcie tego profilu. |
| **Pozostałe rodziny vna/J** | W spisie archiwum 3.4.8 znaleziono `serial/std2`, `pro`, `pro2`, `proext`, `tiny`, `max6`, `max6_500`, `metro`, `vnarduino` oraz odmiany `serial/std/lf` i `serial/pro/lf`, a także przykładowy driver `sample`. To inwentarz nazw pakietów, nie dowód aktualnego wsparcia ani szczegółów protokołu. | Nie ma odpowiadających im pluginów VNA Studio. | **P3.** Rozpatrywać osobno. Dla każdej rodziny: wersjonowany raport statyczny, rozstrzygnięte prawa, własny profil/plugin, zapisane wektory i niezależna walidacja. |

## Macierz funkcji Analyser 306b i stanu portu

Szczegółowe kryteria dla elementów interfejsu `C01–F03` pozostają w [`roadmap-screenshots.md`](roadmap-screenshots.md). Poniższa tabela dopisuje aktualny zakres, dowód i kryterium zakończenia; jej status z 2026-11-07 jest aktualniejszy od wstępnych statusów w macierzy screenshotów. Nie uznaje pozycji za zgodną tylko dlatego, że ma widok w aplikacji.

| Grupa / źródło | Stan VNA Studio (2026-11-07) | Otwarte kryterium zgodności | Priorytet |
| --- | --- | --- | --- |
| Skan Antenna / C01–C03, M01, K01 | Port i profil konfigurowalne; start/stop, punkty, postęp, Hold/Stop, odbicie oraz odtwarzanie sesji. Podstawowa komunikacja i próbki z obciążenia 50 Ω sprawdzone na COM9; kontrola nie dowodzi kalibracji antenowej. | OPEN/SHORT, obciążenie reakcyjne, kalibracja dla dokładnego egzemplarza oraz pomiar porównawczy z oryginałem. | P0 |
| Marker i odczyty / U01–U04, A01–A02 | Tabela M1/M2, widoczność markerów, próbka sąsiednia, minimum SWR, BW, Q z markerów i ręczny kalkulator rezonansu. | Oryginalny algorytm Q z dwóch screenshotów pozostaje hipotezą; XTAL automation legacy nie była zaprogramowana według instrukcji. | P1; Q fizyczny P2 |
| Krzywe, skale i zakres / U05–U10, R01–R03, K02–K03 | Sześć przebiegów i własne skale, kolory, RL×2, RL offset, linia 50 Ω, Xc/Xl, zoom/pan, historia i zapisany zakres. | Dokładne ticki zależne od zakresu oraz część zachowania `Bands F/F` nie są potwierdzone; screenshoty wyznaczają tylko widoczne punkty odniesienia. | P1 |
| Cable Length / M03 | Kalkulator oparty o parę porównywalnych rezonansów i VF; obsługuje zakres do 65 MHz. | Brak fizycznego kabla o znanej długości/VF; instrukcja ogranicza użyteczność do 60 MHz. | P2 |
| Cable Loss / M04 | Osobny, ostrożnie nazwany estymator z odbicia; wymaga jawnego potwierdzenia kabla open/short i bramki pochodzenia. Nie jest portem potwierdzonego algorytmu Analyser. | Brak odzyskanego wzoru/kalibracji legacy i brak kabla o znanym tłumieniu do testu. | P2 |
| FILTER / M02 | Dane skalarnej transmisji istnieją dla demonstratora/sesji; host blokuje wysłanie niezweryfikowanej komendy hardware. | Wymaga potwierdzonego toru ANT–WOB, fixture, komendy i kalibracji Through/LOOP dla dokładnego modelu. | P0 dla dowodu; funkcja P2 |
| Generator / M05 | Pamięci i edycja częstotliwości są dostępne, lecz wyjście sprzętowe pozostaje zablokowane. | Pełna sekwencja ON/OFF/strojenia oraz bezpieczny pomiar częstotliwości wyjściowej nie są ustalone. | P2 |
| Files / F01–F03 | Sesje JSON i CSV VNA Studio; import rozpoznanych wariantów CSV jest neutralny wobec urządzeń; Touchstone jest osobnym formatem zespolonych danych sieciowych. Menu File/About działa. | Brak rzeczywistego pliku legacy `.vec`; nie deklarować zgodności legacy CSV poza zbadanymi kolumnami/przykładami. | P1–P2 |
| Smith chart | Aplikacja ma osobny wykres Smitha i analityczne wektory testowe; vna/J statycznie wyznacza `Γ=(Z−Zref)/(Z+Zref)` i siatki Z/Y/SWR. | Zgodność wizualna z vna/J i wiarygodność rzeczywistych danych pozostają otwarte. Nie przenosić jego siatek/drivera jako faktów o VNA65. | P1/P2 |

## Funkcje vna/J widoczne w materiałach 3.4.8

Poniższe pozycje pochodzą z changelogu `readme.3.4.8.html` oraz statycznej zawartości JAR. Opisy zmian w historii nie zastępują analizy handlera. Zależności/licencje mają osobne otwarte zadanie 1.8; nie kopiować ani nie dystrybuować JAR-a lub kodu zdekompilowanego.

| Funkcja z dokumentacji / zasobów | Co wiadomo | Priorytet i kryterium przeniesienia |
| --- | --- | --- |
| Smith chart oraz jego konfigurowalne siatki | Kod trzech klas był ograniczenie sprawdzony `javap`; zachowanie mapowania `Γ` i klas okręgów opisano w raporcie. | **P1/P2.** Kontrolne obciążenia i geometria; osobno porównać siatkę i obsługę UI. |
| Odbicie, transmisja i kalibracje | Changelog wspomina rozszerzenia kalibracji dla konkretnych miniVNA/VNArduino rodzin oraz dane transmisji. Zakres zależy od sterownika. | **P0/P3.** Każdy tryb wymaga profilu, znanych standardów i niezależnego wektora; nie przenosić do VNA65 z JAR-owego opisu. |
| Speedup, moving average, smoothing | Changelog opisuje osobne speedup/averaging, smoothing oraz użytkownikowi konfigurowalny plik Gaussian coefficients. Dokładny pipeline nie został tu odtworzony. | **P2.** Ustalić kolejność, parametry, wpływ na marker/kalibrację i provenance; wykazać wyniki na zapisanych przebiegach. |
| Cable data / cable length | Changelog wymienia kable Hyperflex/Highflex oraz wybór współczynnika kabla. Nie stanowi to potwierdzenia formatu lub dokładności kalkulatora VNA Studio. | **P2.** Osobne testy znanego przewodu i VF; utrzymać identyfikator/pochodzenie tabeli kabla. |
| Generator i pozostawienie pracy po skanie | Changelog opisuje wyłączanie generatora po skanie oraz opcję utrzymania go aktywnym dla wymienionych problemów startowych. | **P2.** Poznać pełny protokół/model i stan bezpieczny po Stop/rozłączeniu; pomiar wyjścia na właściwym obciążeniu. |
| Detekcja urządzenia, firmware update | W changelogu są zmiany wykrywania wybranych modeli i okna aktualizacji firmware. | **P3.** Brak implementacji i dowodu zgodności dla VNA65. Wymaga jawnie obsługiwanej rodziny oraz zweryfikowanego, bezpiecznego przepływu aktualizacji; nie uruchamiać ani nie programować obcego firmware w ramach inwentaryzacji. |
| Eksport / import kalibracji i danych | Changelog wspomina eksport kalibracji, XLS oraz zgodność plików w obrębie konkretnych wersji. Format `.vec` Analyser 306b nie jest tym samym co vna/J. | **P2/P3.** Wzorcowe pliki i pełna specyfikacja formatu przed deklaracją interoperacyjności. |

## Materiały źródłowe i ograniczenia

- Analyser 306b/miniVNA230: [`decompiled/SPEC.md`](../../../../VNA-ReverseEngineering/decompiled/SPEC.md), [`README dekompilacji`](../../../../VNA-ReverseEngineering/decompiled/README.md), [mapa funkcji](../../../../VNA-ReverseEngineering/inventory/decompilation-function-map-2026-11-06.md).
- Profile i protokół VNA65: [`analyz.ini`](../../../../VNA-ReverseEngineering/reference/software/analyz.ini), [`VNA-COMMUNICATION.md`](../../../docs/VNA-COMMUNICATION.md), [raport skanu 50 Ω](../../../docs/measurements/2026-11-04-com9-50ohm.md).
- vna/J 3.4.8: [`readme.3.4.8.html`](../../../../VNA-ReverseEngineering/reference/vnaj-3.4.8/readme.3.4.8.html), [statyczny raport `javap`](../../../../VNA-ReverseEngineering/analysis/vnaj-3.4.8-javap-static-review-2026-11-07.md), [inwentarz wpisów JAR](../../../../VNA-ReverseEngineering/reference/vnaj-3.4.8/jar-entries.txt).

Pliki zostały obejrzane statycznie. JAR/dekompilatory ani stare programy nie zostały uruchomione na potrzeby tej macierzy. Zadania zależne od WOB, właściwych standardów, rzeczywistych próbek `.vec`, praw/licencji lub pełnej dekompilacji pozostają otwarte.
