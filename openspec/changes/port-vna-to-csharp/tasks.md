# Tasks

## 1. Materiały i specyfikacja referencyjna

- [x] 1.1 Zachować pakiet vna/J, spis klas i hash; dowód: [`vna/J 3.4.8`](../../../../VNA-ReverseEngineering/reference/vnaj-3.4.8/), [`PLAN.md`](../../../PLAN.md) oraz manifest badawczy.
- [x] 1.2 Zinwentaryzować i częściowo zdekompilować lokalne aplikacje VB6; dowód: [`decompiled/README.md`](../../../../VNA-ReverseEngineering/decompiled/README.md), `summary.json` i raporty brakujących procedur.
- [x] 1.3 Odczytać profil 65 MHz i zebrać wzorce wykresów; dowód: [`decompiled/SPEC.md`](../../../../VNA-ReverseEngineering/decompiled/SPEC.md), [`PLOTS.md`](../../../PLOTS.md) i [`reference/hardware/screenshots`](../../../../VNA-ReverseEngineering/reference/hardware/screenshots/).
- [ ] 1.4 Zdekompilować klasy aplikacji vna/J narzędziem o zapisanej wersji; sprawdzić trudne metody przez `javap` i dostarczyć raport błędów oraz polecenia powtórzenia. Wybrane klasy sprawdzono `javap`-em w podzadaniu 1.4a; pełna dekompilacja pozostaje otwarta.
- [x] 1.4a Statycznie przejrzeć wybrane klasy Smith i standardowego sterownika szeregowego przez systemowy `javap 11.0.25`; zapisać komendy, wynikowe fakty i granice wnioskowania bez uruchamiania JAR-a. Review: [`2026-11-07-stage-1-4a-vnaj-javap.md`](reviews/2026-11-07-stage-1-4a-vnaj-javap.md); raport: `../../../../VNA-ReverseEngineering/analysis/vnaj-3.4.8-javap-static-review-2026-11-07.md`.
- [x] 1.5 Utworzyć macierz funkcji Analyser 306b/vna/J z priorytetami, źródłami i kryteriami zgodności; sprawdzić uwzględnienie wszystkich funkcji i modeli z dokumentacji oraz zasobów (`feature-model-matrix.md`, `reviews/2026-11-07-stage-1-5-feature-model-matrix.md`).
- [x] 1.6 Ustalić kontrakt protokołu wariantu użytkownika: port, komendy trybów, DDS, kodowanie ADC i timeouty; zweryfikować zapisem transmisji lub udokumentowaną analizą kodu oraz oznaczyć brak potwierdzenia sprzętowego, jeśli go nie ma (`reviews/2026-11-07-stage-1-6-protocol-contract.md`).
- [ ] 1.7 Zebrać wektory referencyjne i ustalić tolerancje obliczeń oraz geometrii wykresów; dowód: wersjonowane próbki, oczekiwane wyniki, pochodzenie i kryteria oceny.
- [x] 1.7a Zinwentaryzować istniejące wektory COM9, analityczne i screenshotowe wraz z tolerancjami testowymi oraz jawnie rozdzielić je od brakujących oracle fizycznych i pikselowych (`reference-vectors-and-tolerances.md`, `reviews/2026-11-07-stage-1-7a-vector-audit.md`).
- [ ] 1.8 Zinwentaryzować licencje źródeł i zależności oraz warunki publikacji; dowód: dokument z warunkami i zakresem możliwej dystrybucji. Podzadanie 1.8a zapisuje tylko dostarczone notice i brak ustalenia dokładnego zakresu dla JAR-a.
- [x] 1.8a Zanotować treść dołączonego `lizenz.html`, sprawdzić indeks wpisów JAR oraz wyznaczyć granicę pakowania: nie dołączać JAR-a ani dekompilacji do release VNA Studio, dopóki licencja programu i zależności nie są ustalone (`reviews/2026-11-07-stage-1-8a-vnaj-license-inventory.md`; raport w `../../../../VNA-ReverseEngineering/analysis/vnaj-3.4.8-javap-static-review-2026-11-07.md`).

## 2. Fundament C# i obliczenia

- [x] 2.1a Utworzyć rozwiązanie .NET z projektami Core, Devices, IO i Avalonia App/MVVM; przypiąć Avalonia 12.1.4 i target .NET 10 w `global.json`.
- [x] 2.1b Uzupełnić wymagany podział modułów według design, skompilować całość i zapisać wykryte wersje SDK/zależności; dowód: `reviews/2026-11-03-plot.md`, projekt `Vna.Tests` i udana kompilacja z SDK 10.0.402.
- [x] 2.1c Skonfigurować host DI i `IOptionsMonitor<AppSettings>` dla ustawień aplikacji; zwalniać subskrypcje i usługi asynchronicznie.
- [x] 2.1d Usunąć zależność okna od generowanych nazw XAML i ostrzeżenie AVLN3001; sprawdzić oba konstruktory testami oraz uruchomić okno na pulpicie Windows. Dowód: `reviews/2026-11-03-xaml-startup.md`.
- [x] 2.2a Załadować profil 65 MHz z bundlowanego INI przez typed options; uwzględnić zakres, zegar DDS, próbki, rozdzielczości ADC i zachować współczynniki kalibracji.
- [x] 2.2c Wyświetlać nazwę profilu `VNA 1-65 MHz`, zgodnie z oznaczeniem urządzenia na zdjęciach użytkownika, zachowując identyfikator profilu dla wcześniejszych sesji; aktualizować podglądy.
- [x] 2.2b Zweryfikować przeliczenia Hz/MHz, walidację zakresu i zgodność wszystkich wartości profilu z INI (`reviews/2026-11-07-stage-2-2b-profile-match.md`).
- [x] 2.3a Dodać niezależny od UI kalkulator z podpisanego return loss i fazy na SWR oraz impedancję zespoloną; użyć go w demonstratorze.
- [ ] 2.3b Potwierdzić konwencje z kodem/odczytami 306b, dodać kalibrację z INI i sprawdzić wektory 50 Ω, OPEN, SHORT, granice zakresów oraz osobliwości.
- [x] 2.4a Udokumentować standardowe wzory kalkulatora C#, znaki R/X i jawne założenie konwencji fazy.
- [ ] 2.4b Ustalić konwencje i tolerancje dla Analyser 306b oraz sprawdzić dokumentację na zatwierdzonych wektorach.

## 3. Sesje i odtwarzanie

- [x] 3.1a Dodać wersjonowany zapis/odczyt JSON sesji i podstawową walidację wersji, profilu oraz liczby punktów.
- [x] 3.1b Sprawdzić round-trip próbek i odrzucanie uszkodzonych/nieobsługiwanych plików; rozszerzyć kontekst sesji o zweryfikowane dane kalibracji. Dowód: `reviews/2026-11-04-session-verification.md`.
- [x] 3.1c Walidować porządek częstotliwości i stany próbek przy odczycie oraz zapisie sesji.
- [x] 3.1d Zapisywać snapshot profilu i stałych kalibracyjnych razem z sesją JSON.
- [x] 3.2a Dodać demonstrator danych syntetycznych i jawnie oznaczyć źródło w interfejsie.
- [x] 3.2b Dodać otwieranie i odtwarzanie zapisanej sesji JSON w UI, z kontrolą zgodności profilu.
- [x] 3.2c Sprawdzić odtwarzanie bez COM oraz przypadki uszkodzonego pliku i niezgodnego profilu. Dowód: `reviews/2026-11-04-session-verification.md`.
- [x] 3.3 Udokumentować format sesji i przykładowy plik; sprawdzić jego odczyt opisaną procedurą. Dowód: `docs/SESSION-FORMAT.md`, `reviews/2026-11-04-session-verification.md`.

## 4. Wykresy zgodne z oryginałem

- [ ] 4.1 Ustalić transformacje, zwroty osi, zakresy i układ na podstawie kodu oraz wzorcowych odczytów; dowód: profil wizualny i kontrolne pary wartość–położenie, bez domyślnego założenia logarytmicznego SWR.
- [x] 4.2a Dodać wstępną własną kontrolkę Avalonia `Render(DrawingContext)` z siatką, sześcioma przebiegami i markerami.
- [ ] 4.2b Wydzielić/weryfikować transformacje osi, niezależne skale i geometrię na zatwierdzonych wzorcach; sprawdzić kompilację i kolory w runtime.
- [x] 4.2c Włączyć zmianę rozmiaru okna z minimum 1024×768 DIP; użyć elastycznego obszaru wykresu, przewijanego panelu ustawień i geometrii liczonej z aktualnych `Bounds`.
- [x] 4.2d Wydzielić wspólne definicje liniowych skal dla krzywych i etykiet oraz geometrię segmentów bez zależności od Avalonia; sprawdzić pozycje krańców i etykiet testami.
- [x] 4.3a Dodać markery M1/M2 z odczytem tej samej próbki, znacznik minimum SWR, zoom między markerami i powrót do pełnego zakresu.
- [x] 4.3b Sprawdzić scenariusze `legacy-plots`, w tym odwróconą kolejność markerów oraz brak poprawnych próbek (`reviews/2026-11-07-stage-4-3b-legacy-plot-edge-cases.md`).
- [x] 4.3c Dodać przeciąganie M1/M2 z przechwytywaniem wskaźnika, wybór próbek w widocznym zakresie i historię zoomu; sprawdzić odwróconą kolejność markerów, powrót przez kolejne zakresy i brak pozornego minimum SWR.
- [x] 4.3d Dodać przyciski i klawisze +/− do dwukrotnego zoomu częstotliwości, wspólną historię, granice 1 Hz/profilu oraz zachowanie widoku przy kolejnych skanach tych samych ustawień. Dowód: `reviews/2026-11-05-plot-zoom.md`, testy ViewModel i rzeczywistych interakcji headless w 1024x768.
- [ ] 4.4 Obsłużyć luki w danych oraz DPI 100/125/150%; sprawdzić przerwy krzywych, trafianie w próbki, porównania obrazów i responsywność na 649 punktach na przebieg. Testy DPI, responsywność oraz porównanie strukturalne są wykonane; pozostaje pixel parity na dopasowanych próbkach/original runtime.
- [x] 4.4a Sprawdzić przerwy dla `null`, NaN, nieskończoności i błędnej jakości; uruchomić testy interakcji headless w konfiguracjach 1024×768/100%, 1440×900/125% oraz 1920×1080/150%.
- [x] 4.4b Przejrzeć UI w minimalnym, przywróconym i zmaksymalizowanym oknie; sprawdzić, że osie, markery, etykiety i panel ustawień pozostają dostępne (`reviews/2026-11-07-stage-4-4b-window-layout.md`).
- [x] 4.4c Porównać dostarczony oryginalny zrzut z rendererem Avalonia i zapisać zgodne elementy, różnice układu oraz ograniczenia porównania danych (`docs/PLOT-COMPARISON.md`, `docs/PLOT-COMPARISON.pl.md`, `reviews/2026-11-07-stage-4-4c-structural-plot-comparison.md`).
- [x] 4.5 Zapisać wynik porównania kontrolki Avalonia z kryteriami, pomiary responsywności oraz instrukcję obsługi wykresów; sprawdzić zgodność opisu z działającym prototypem (`docs/PLOT-GUIDE.md`, `docs/PLOT-GUIDE.pl.md`, `PLOTS.md`, `reviews/2026-11-07-stage-4-5-plot-guide-performance.md`).

## 5. Połączenie z urządzeniem

- [x] 5.1a Dodać ustawienie nazwy/ścieżki portu, zapis w konfiguracji użytkownika, wykrywanie nazw portów systemowych oraz przekazywanie wybranej wartości bez zmian do interfejsu urządzenia.
- [x] 5.1b Dodać surowy transport szeregowy z parametrami linii wymaganymi jawnie, odczytem fragmentowanym, timeoutem i anulowaniem.
- [x] 5.1c Dodać offline'owy symulator transportu testujący fragmentację, timeout, nadmiarowe bajty, anulowanie i uciętą ramkę; granice potwierdzonego kontraktu oraz brak weryfikacji dodatkowych bajtów firmware opisać (`docs/DEVICE-PROTOCOL.md`, `reviews/2026-11-07-stage-5-1c-stream-simulator.md`).
- [x] 5.2 Zaimplementować sterownik z konfigurowalnym zegarem DDS; porównać komendy i dekodowane ADC z zapisami i wektorami (`docs/DEVICE-PROTOCOL.md`, `reviews/2026-11-07-stage-5-2-5-3-source-flow.md`).
- [x] 5.3 Podłączyć skany pojedyncze i ciągłe do UI; wykorzystać konfigurowalną ścieżkę portu, walidować parametry, zatrzymać skan i wykazać ponowne połączenie bez restartu (`reviews/2026-11-04-hardware-com9.md`, `reviews/2026-11-07-stage-5-2-5-3-source-flow.md`).
- [ ] 5.4 Wykonać porównawcze pomiary wzorców i tego samego obiektu w obu aplikacjach; dostarczyć raport wyników i zakres potwierdzonej zgodności sprzętowej.
- [ ] 5.5 Udokumentować podłączenie, profil urządzenia i procedurę kalibracji; sprawdzić kroki na docelowym egzemplarzu.

## 6. Rozszerzanie zgodności z vna/J

- [ ] 6.1 Na podstawie macierzy wdrożyć pozostałe tryby urządzenia użytkownika, w tym FILTER/transmisję, generator i pomiar kabli; każdy scenariusz potwierdzić właściwymi referencjami i uaktualnić raport.
- [ ] 6.2 Wdrożyć zweryfikowane importy/eksporty pomiarów i kalibracji starszych aplikacji; sprawdzić przykładowymi plikami oraz opisać nieobsługiwane warianty.
- [ ] 6.3 Odtworzyć wykres Smitha i pozostałe narzędzia objęte macierzą vna/J; sprawdzić każdy element zapisanym kryterium i dokumentacją użycia.
- [ ] 6.4 Dodawać kolejne sterowniki objęte macierzą jako oddzielne profile; sprawdzić ich protokoły i wyniki niezależnie od wariantu 65 MHz.
- [ ] 6.5 Rozliczyć wszystkie pozycje docelowej macierzy; zweryfikować, że żaden element nie jest przedstawiany jako zgodny bez dowodu, a pozostałe różnice są jawne.

## 7. Integracja i wydanie

- [ ] 7.1 Sprawdzić pełny przepływ: połączenie → kalibracja → skan → markery → zapis → odtworzenie; dostarczyć raport integracyjny z docelowego środowiska.
- [ ] 7.2 Przygotować powtarzalny pakiet Windows oraz opis zgodności i ograniczeń; sprawdzić instalację/uruchomienie bez zależności VB6 oraz warunki publikacji z 1.8.

## 8. Lokalizacja PL/EN

- [x] 8.1 Dodać wspólny projekt zasobów `.resx` z angielskim zestawem neutralnym i polskim zestawem satelitarnym; sprawdzić kompletność kluczy i argumentów.
- [x] 8.2 Przenieść teksty UI, statusy i własne komunikaty walidacji do zasobów; dodać zmianę języka bez restartu, również dla legendy i istniejących komunikatów.
- [x] 8.3 Dodać pole Language do AppSettings, wybór Polski/English i wspólny zapis portu oraz języka; zachować zgodność starszych ustawień.
- [x] 8.4 Stosować kulturę wybranego języka przy formatowaniu i odczycie pól; zachować dane sesji i ścieżki portów; sprawdzić UI, IOptionsMonitor, zapis i formatowanie. Dowód: `reviews/2026-11-03-localization.md` (44/44 testy).
- [x] 8.5 Ustawić domyślne English/Dark i automatycznie zapisywać obsługiwane preferencje przy zwykłym zamknięciu; serializować zapis ręczny/końcowy, zachować jawne wybory PL/light i odtwarzać JSON przy starcie. Dowód: `reviews/2026-11-05-preferences.md`, testy zamknięcia/odtworzenia i błędu zapisu.

## 9. Automatyczne paczki release

- [x] 9.1 Dodać workflow release.published z walidacją taga vX.Y.Z i wersją assembly wynikającą z taga.
- [x] 9.2 Dodać publikację self-contained dla jedenastu RID, pakowanie ZIP/tar.gz, metadane i walidację wymaganych plików.
- [x] 9.3 Dołączać komplet jedenastu paczek i SHA256SUMS dopiero po powodzeniu całej macierzy; ograniczyć uprawnienia zapisu do joba uploadu.
- [x] 9.4 Sprawdzić skrypty lokalnie i rzeczywistą paczkę win-x64; zapisać instrukcję oraz przegląd w `reviews/2026-11-03-release.md`.
- [x] 9.5 Rozszerzyć macierz o Windows x86, Linux ARM i trzy warianty musl; sprawdzić publikacje nowych RID, zależności natywne i 140 testów w procesie x86. Dowód i ograniczenia: `reviews/2026-11-05-release-rids.md`.
- [ ] 9.6 Uruchomić workflow w docelowym GitHub, sprawdzić jedenaście wyników i pobieranie załączników; potwierdzić uruchomienie na platformach docelowych.
- [x] 9.6a Lokalnie sprawdzić walidatory macierzy RID, wersjonowania i manifestów bez publikowania do GitHub (`tests/release/Release.Tests.ps1`, `reviews/2026-11-07-stage-9-6a-release-validation.md`).

## 10. Motywy aplikacji

- [x] 10.1 Dodać semantyczne zasoby i warianty Light, Dark oraz czerwony Night Vision; objąć formularze, stany interakcji i własny wykres.
- [x] 10.2 Zastosować wspólną paletę wykresu i legendy, wzory linii Night Vision oraz zachowanie oryginalnego snapshotu przy przełączaniu.
- [x] 10.3 Zachować Theme w AppSettings z językiem i portem, stosować IOptionsMonitor i przetłumaczyć nazwy motywów.
- [x] 10.4 Sprawdzić przełączanie, sesje, wygląd interakcji i piksele Night Vision; wyrenderować i dodać podglądy do README. Dowód: `reviews/2026-11-03-themes.md` (59/59 testów).
- [x] 10.5 Ustawić nazwę VNA Studio i SQ7MRU po prawej stronie nagłówka, zaktualizować metadane i podglądy. Dowód: `reviews/2026-11-03-branding.md`.

## 11. Funkcje pozostałe po analizie zrzutów

Kolejność i kryteria: `roadmap-screenshots.md`. Identyfikatory C/U/R/A/K/M/F odnoszą się do 31 pozycji macierzy. Zrealizowanie planu dokumentacyjnego nie oznacza implementacji poniższych funkcji.

- [x] 11.1 Porównać S1/S2, instrukcje i zasoby EXE z aktualnym kodem; zapisać macierz braków, priorytety, zależności i niejasności. Nie zaliczać tego jako pełnej macierzy vna/J z 1.5.
- [x] 11.2a E0: Zindeksować lokalne EXE/INI, manual, FTDI package, zrzuty i recoverability; potwierdzić ślady Xc/Xl i udokumentować granice pewności/nieobecność protokołu. Dowód: `reviews/2026-11-04-e0-source-review.md`.
- [x] 11.2c E0: Odzyskać dedykowany handler zmiany trybu z istniejącej bazy Ghidra; potwierdzić nazwy trybów i komunikat wyłączający stan CAL przy zmianie, bez dopisywania nieodzyskanej matematyki. Dowód: `reviews/2026-11-04-targeted-mode-handler.md`.
- [x] 11.2d E0: Odzyskać handler kalibracji/odświeżania wykresu; udokumentować komunikat LOOP, zakończenie `0dB` i widoczne skale RL bez zgadywania korekcji. Dowód: `reviews/2026-11-04-targeted-calibration-handler.md`.
- [ ] 11.2b E0: Domknąć źródłową analizę XTAL/Q, zakresowych profili ticków i Bands `F/F`; podzadania rozdzielają ustalony ręczny wzór od nierozpoznanego zachowania UI (U07, A02, U12; [`analyser306b-source-gaps-2026-11-07.md`](../../../../VNA-ReverseEngineering/analysis/analyser306b-source-gaps-2026-11-07.md)).
- [x] 11.2b.1 Potwierdzić, że instrukcje dokumentują ręczne `Q = Fres / (Fh − Fl)` i mówią, że automatyczny XTAL nie był jeszcze zaprogramowany; odróżnić pomocnik VNA Studio od portu automatyki (`reviews/2026-11-07-stage-11-2b-source-audit.md`).
- [ ] 11.2b.2 Odtworzyć profile podziałek dla wszystkich zakresów z handlera wykresu lub wzorcowych odczytów; etykiety i screenshoty same nie wystarczają.
- [ ] 11.2b.3 Wyjaśnić rolę Bands `F/F` i powiązanie checkboxa z ograniczeniami `BANDS_LIMIT` na podstawie formularza/eventu lub obserwacji oryginału.
- [x] 11.2b.4 Statycznie potwierdzić dwa zestawy etykiet RL w zachowanym disassembly `VB6_0043094f`, bez przypisywania gałęzi do `RL×2`, `F/F` ani pasma częstotliwości (`reviews/2026-11-07-stage-11-2b-4-rl-label-branches.md`).
- [ ] 11.3 E0: Zamknąć kontrakt protokołu i mapę zdolności odbicie/transmisja/generator oraz zapisy referencyjne, powiązane z 1.6.
- [x] 11.3a E2: Dodać źródłowo oparty transportowy sterownik miniVNA/306b: COM 115200 8N1, komendy tekstowe DDS, ustawiany z INI rozmiar ramki 4/6 bajtów, dekodowanie dwóch potwierdzonych kanałów, timeout i zerwanie po niepełnym skanie. Dowód: `docs/DEVICE-PROTOCOL.md`, `reviews/2026-11-04-serial-driver.md`; bez deklaracji testu na sprzęcie.
- [x] 11.3b E2: Walidować częstotliwości skanu względem siatki DDS faktycznie zwracanej przez wybrane źródło, tak aby poprawny ostatni punkt poniżej Stop nie odrzucał pełnego skanu. Test integracyjny VM + sterownik + atrapowy COM. Dowód: `reviews/2026-11-04-dds-frequency-grid.md`.
- [x] 11.3c E2: Zamykać port przy anulowaniu lub przerwaniu enumeracji także po poprawnym odebraniu ramki, nie tylko podczas ReadExactly. Test anulowania po pierwszym punkcie i sprzątania enumeratora. Dowód: `reviews/2026-11-04-hardware-sweep-lifecycle.md`.
- [x] 11.3d E2: Sprawdzić fizyczny skan na COM9 i poprawić rozdzielenie VNA_type/SVNA: VNA_type=1, SVNA=0 daje 4 bajty na punkt. Potwierdzono 16/64 oraz 649/2596, Stop w odczycie i restart przez MainWindowViewModel. Dowód: `reviews/2026-11-04-hardware-com9.md`.
- [ ] 11.3e E2: Uzupełnić weryfikację RF o obciążenia open/short i porównanie pomiaru oryginalnej aplikacji, niezerowy PH_b, znak Xc/Xl i dokładność kalibracji. Podstawowe skalowanie profilu użytkownika oraz 50 ohm potwierdzono w 11.3f.
- [x] 11.3f E2: Poprawić skalowanie ADC: affine przed poziomem odniesienia, polaryzacja VNA_type, baza 30 dB i korekcja -9.84 dB dla type1, offset RL po polaryzacji. Potwierdzić terminator 50 ohm w trzech skanach 649 punktów oraz niezależnie porównać z algebrą oryginału. Dowód: `reviews/2026-11-04-50ohm-adc.md`, `docs/measurements/2026-11-04-com9-50ohm.md`.
- [x] 11.4a E1: Dodać tabelę M1/M2 z częstotliwością, SWR, |Z|, RL, fazą, R i |X|; brakujące wartości pokazują kreskę. Dowód: `reviews/2026-11-04-marker-table.md` (U01).
- [x] 11.4b E1: Dodać osobne przełączniki widoczności M1/M2; ukrywanie nie kasuje pozycji ani tabeli. Dowód: `reviews/2026-11-04-marker-controls.md` (U02).
- [x] 11.4c E1: Dodać strzałki i lewo/prawo po fokusie wykresu do przesuwania markera o jedną próbkę; zatrzymać ruch na granicach danych i zoomu. Dowód: `reviews/2026-11-04-marker-controls.md` (U03).
- [x] 11.4d E1: Dodać przełącznik znacznika minimum SWR i akcję M1 → minimum; brak prawidłowego wyniku blokuje akcję, a powrót z pełnego widoku przywraca poprzedni zoom. Dowód: `reviews/2026-11-04-minimum-swr.md` (U04).
- [x] 11.5a E1: Dodać polecenia Wszystkie/Żadne do widoczności przebiegów, bez zmiany danych i markerów (U05; dowód `reviews/2026-11-04-marker-controls.md`).
- [x] 11.5b E1: Dodać paletę wyboru kolorów, reset do profilu i zapis w `settings.json`; utrzymać rozjaśnianie Dark oraz czerwoną paletę Night Vision. Dowód: `reviews/2026-11-04-trace-colors.md` (U06).
- [x] 11.6a E1: Dodać niezależne, przełączane odniesienie 50 Ω na skali impedancji; nie zmieniać Z0 ani kalibracji. Dowód: `reviews/2026-11-04-reference-line.md` (U08).
- [x] 11.6b E1: Dodać opcjonalne przeliczenie signed reactance markerów na pF/µH ze standardowych wzorów; wynik pokazuje się tylko dla próbki Valid i f > 0. Dowód: `reviews/2026-11-04-xc-xl.md` (U10).
- [x] 11.6c E1: Odtworzyć ze zrzutów ticki SWR 1/2.5/5/7.5/10 oraz sprawdzić ich położenie względem krzywej. Dowód: `reviews/2026-11-04-swr-ticks.md`.
- [x] 11.6e E1: Dodać przełącznik RL×2 oparty na odzyskanych zakresach osi 0–30/0–60 dB; zmienia wyłącznie widok, zapisuje się w ustawieniach. Dowód: `reviews/2026-11-04-rl-x2-range.md`.
- [x] 11.6f E1: Dodać DISPLAY Full do wyboru pełnej tabeli markerów lub skrótu FREQ/RL; zachować pełny widok jako domyślny. Dowód: `reviews/2026-11-04-display-full.md`.
- [x] 11.6g E1: Dodać regulowaną korektę RL dB dla wyświetlania; przeliczać SWR/impedancję markerów i wykresu bez modyfikacji próbek sesji, zapisać wartość jako preferencję. Dowód: `reviews/2026-11-04-rl-offset-display.md`.
- [x] 11.6d E1: Dodać oddzielne podziałki fazy 0/45/90/135/180°, impedancji 0/75/150/225/300 Ω oraz dodatnią prezentację wartości ujemnego return loss; pozycje etykiet muszą używać tej samej transformacji co przebieg. SWR pozostaje tymczasowy do potwierdzenia mapowania ze zrzutów. Dowód: `reviews/2026-11-04-axis-ticks.md`.
- [x] 11.7a E1: Dodać niezależny widok zakresu profilu, pamięć zakresu widoku oraz zachować odrębną historię zoomu (R01–R02). Dowód: `reviews/2026-11-04-range-bandwidth.md`.
- [x] 11.7b E1: Dodać presety pasm amatorskich HF/VHF dla Polski, filtrowane według zakresu profilu; preset ustawia wyłącznie granice skanu i nie steruje nadajnikiem (R03). Dowód: `reviews/2026-11-04-band-presets.md`, źródło UKE w `docs/BAND-PRESETS.md`.
- [x] 11.7c E1: Odczytać opcjonalne pary `F_M160s/e`–`F_M70cs/e` z oryginalnej sekcji `[BANDS_LIMIT]`; niepełną lub odwróconą parę odrzucić, a zakres poza profilem ukryć. Gdy sekcja pusta, zachować presety UKE. Dowód: `reviews/2026-11-04-legacy-band-limits.md`.
- [x] 11.7d E1: Rozdzielić Full/Zoom zakresu skanu od Full/Zoom widoku wykresu. Pełny skan ustawia granice profilu; zakres markerów ustawia granice skanu rosnąco, bez automatycznego uruchamiania pomiaru. Potwierdzone etykietami `Full range scan` i `Zoom to marker` z EXE. Dowód: `reviews/2026-11-04-sweep-range-controls.md`.
- [x] 11.8 E1: Dodać szerokość pasma jako bezwzględną różnicę częstotliwości markerów, niezależną od kolejności; wyświetlić MHz i sprawdzić obliczenie testem. Dowód: `reviews/2026-11-04-bandwidth.md` (A01).
- [x] 11.9a E1: Dodać zlokalizowane menu Plik/Pomoc, operacje otwarcia/zapisu sesji i CSV oraz okno About z VNA Studio, SQ7MRU, wersją assembly i informacją o statusie obsługi sprzętu. Dowód: `reviews/2026-11-04-file-about.md` (F03).
- [x] 11.9b E1: Pokazać nazwę bieżącej sesji oraz stan pomiaru niezapisanego po zakończeniu skanu; otwarcie/zapis aktualizują nazwę i czyszczą stan niezapisany. Dowód: `reviews/2026-11-04-file-about.md` (F01).
- [x] 11.10 E2: Dodać źródła Demonstrator/Urządzenie/Odtwarzanie i sterownik profilu, Połącz/Rozłącz oraz brak automatycznego fallbacku do demo; potwierdzić testami i raportem (`reviews/2026-11-04-device-selection.md`, `../device-plugin-architecture/reviews/2026-11-07-stage-3-4-source-lifecycle.md`, `reviews/2026-11-07-stage-5-2-5-3-source-flow.md`).
- [x] 11.10a E2: Zaimplementować osobny `IVnaDevice` dla protokołu COM i testować go przez wstrzykiwany transport, bez zmiany źródła UI z demonstratora. Dowód: `reviews/2026-11-04-serial-driver.md`.
- [x] 11.10b E2: Jawnie wybierać Demonstrator albo Urządzenie, zapisywać wybór w ustawieniach, dodawać Połącz/Rozłącz i blokować zmianę źródła/portu podczas pracy; błędy sprzętu nie przełączają na demonstrator. Ręczne połączenie pozostaje aktywne po pełnym skanie. Dowód: `reviews/2026-11-04-device-selection.md`.
- [x] 11.10c E2: Dodać Odtwarzanie jako odrębne źródło: połączenie wymaga wczytanej zgodnej sesji, ponowny sweep wymaga dokładnego zapisanego zakresu i liczby punktów, a wartości są zwracane bez interpolacji. Zapisać źródło w settings i umożliwić PL/EN. Dowód: `reviews/2026-11-04-session-playback-source.md`.
- [x] 11.10d E2: Sprawdzić awarię skanu źródła sprzętowego: router nie uruchamia demonstratora, nie zmienia wybranego trybu ani nazwy źródła po błędzie. Dowód: `reviews/2026-11-04-device-selection.md`.
- [x] 11.11a E1: Pokazać licznik próbek skanu, pasek postępu podczas akwizycji i czas ostatniego pełnego skanu; próbki bieżącego skanu zatwierdzać do wykresu dopiero po kompletnym, uporządkowanym wyniku. Nie nadpisywać danych ani czasu wynikiem przerwanym lub błędnym. Dowód: `reviews/2026-11-04-scan-progress.md` (C03).
- [x] 11.11c E1: Dodać single/continuous nad `IVnaDevice`, przełącznik Hold zatrzymujący pętlę po bieżącym pełnym skanie oraz Stop/anulowanie zachowujące ostatni kompletny wynik; pauza między sweepami respektuje `CancellationToken`. Nie deklarować sprzętowego timeoutu ani reconnect. Dowód: `reviews/2026-11-04-continuous-hold.md`.
- [x] 11.11d E1: Dopasować Hold do odzyskanej instrukcji użytkowej: zaznaczenie wstrzymuje pętlę po pełnym sweepie, odznaczenie samo rozpoczyna/wznawia pomiary ciągłe; Start pozostaje skanem jednorazowym, Stop ustawia Hold i anuluje bieżący skan. PL/EN, test UI command i README. Dowód: `reviews/2026-11-04-legacy-hold-toggle.md`.
- [ ] 11.11b E2: Domknąć timeout, odrzucanie niepełnej ramki i reconnect dla cyklu komunikacji urządzenia (C02); implementacja i symulacja są gotowe, lecz test timeoutu na dokładnym firmware pozostaje otwarty.
- [x] 11.11b.1 Zaimplementować i testować offline zamknięcie/odrzucenie sweepu po timeout lub uciętej ramce oraz wymagać nowego połączenia przed kolejnym skanem (`tests/Vna.Tests/MiniVnaSerialDeviceTests.cs`, `reviews/2026-11-07-stage-11-11b-1-timeout-recovery.md`).
- [ ] 11.11b.2 Potwierdzić zachowanie timeoutu i ponownego skanu na dokładnym firmware/profilu VNA; wymaga testu fizycznego, nie wykonanego w tym etapie.
- [x] 11.12a E2: Dodać czystą korektę afiniczną `y=ax+b` do już przeskalowanego, podpisanego RL/fazy; dekodowanie ADC i RL_OFFSET pozostawić protokołowi po potwierdzeniu. Dowód: `reviews/2026-11-04-adc-calibration.md`.
- [ ] 11.12 E2: Zastosować zweryfikowaną kalibrację odbicia i sprawdzić wzorce 50 Ω/OPEN/SHORT oraz znane obciążenie (K01, M01, zadanie 2.3b).
- [ ] 11.13 E3: Dodać FILTER z osobnymi wielkościami transmisji, kalibracją Through/LOOP, offsetem i potwierdzonym zakresem dB/RL×2 (M02, K01–K02).
- [x] 11.14a E1: Dodać jawnie nazwany szacunek z markerów używający zaokrąglonego midpoint/BW, odtworzyć dwa widoczne wyniki 2 i 7 oraz opisać ograniczenie (A02). Dowód: `reviews/2026-11-04-marker-q.md`.
- [x] 11.14b E4: Dodać odrębny, ręczny kalkulator Q rezonatora z f0 oraz granicami pasma; podać metodę ±45° fazy i wzór z instrukcji, oddzielając go od szacunku markerów. Dowód: reviews/2026-11-04-resonance-q.md.
- [x] 11.15a E4: Dodać obliczanie długości kabla z odstępu częstotliwości sąsiednich rezonansów tego samego typu i konfigurowalnego VF; pokazać jawnie, że wzór jest wyprowadzony z drogi w obie strony. Dowód: reviews/2026-11-04-cable-length.md, docs/CABLE-LENGTH.md.
- [ ] 11.15 E4: Dodać pomiar strat kabla, wybór układu pomiarowego oraz fizyczną weryfikację długości na wzorcowym kablu OPEN/SHORT (M03–M04).
- [x] 11.15b E4: Przenieść do wskazówki Cable Length ograniczenie z instrukcji (rezonanse powyżej 60 MHz), wymóg OPEN/SHORT i eksperymentalnego VF oraz przykładowe VF 0,63/0,50; opisać wąski zalecany zakres przy profilu 65 MHz, bez wymuszania cutoffu. Dowód: `reviews/2026-11-04-cable-length-manual-limits.md`.
- [x] 11.16a E4: Wczytać dziesięć pamięci generatora z MEMORY w analyz.ini do profilu i snapshotu sesji; pokazać wybór/pole częstotliwości walidowane zakresem profilu, bez pozorowania wyjścia RF. Dowód: reviews/2026-11-04-generator-memories.md, docs/GENERATOR.md.
- [ ] 11.16 E4: Dodać sterowanie generatora ON/OFF oraz pomiar częstotliwości wyjściowej po odzyskaniu/zweryfikowaniu kontraktu komend.
- [ ] 11.17 E4: Dodać pomocnik rezonansów kwarców/trapów nad potwierdzonymi pomiarami FILTER/odbicia (M06).
- [ ] 11.18 P3: Osobno określić i wdrożyć automat parametrów kwarcu, jeśli będą dostępne zatwierdzone algorytmy i wektory; nie przedstawiać go jako potwierdzonej funkcji zakładki legacy.
- [x] 11.19a E5: Dodać własny eksport CSV z jednostkami, invariant numeric format, pustymi kolumnami dla brakujących wartości i obsługą anulowania. Dowód: `reviews/2026-11-04-csv-export.md`.
- [x] 11.19b E5: Podnieść format sesji do v2, zachować odczyt/migrację v1 do Antenna i zapisywać wybrany tryb analizy; zablokować zapis/CSV dla trybów bez właściwego źródła danych. Dowód: `reviews/2026-11-04-session-v2.md`.
- [ ] 11.19c E5: Zbadać binarny format .vec oraz zweryfikować warianty legacy CSV na rzeczywistych plikach przed deklaracją zgodności.
- [x] 11.19c.1 Zmapować wyłącznie odzyskane skojarzenia procedur dialogów zapisu/odczytu `.vec`, rozdzielić Analyser 306b od miniVNA230 i zanotować brak przykładowych plików/gramatyki (`../../../../VNA-ReverseEngineering/inventory/decompilation-function-map-2026-11-06.md`, `../../../../VNA-ReverseEngineering/inventory/software-artifact-index-2026-11-06.md`, `reviews/2026-11-07-stage-11-19c-1-vec-static-audit.md`).
- [x] 11.19d E5: Dodać import CSV VNA Studio oraz rozpoznanego nagłówka Analyser 306b (Frequency/Return Loss, opcjonalnie Phase) bez dopowiadania brakujących wielkości. Sprawdzić import, eksport i równomierny sweep testami. Dowód: reviews/2026-11-04-csv-import.md.
- [x] 11.19e E5: Zachować rzeczywistą, nieinkluzywną siatkę DDS w sesji i odtwarzaniu; walidować rosnące próbki wewnątrz zapisanego zakresu zamiast wymuszać równomierny sweep z punktem Stop. Test round-trip sprzętowej siatki 1/2/3 MHz dla zakresu 1–4 MHz. Dowód: `reviews/2026-11-04-dds-session-roundtrip.md`.
- [ ] 11.20 E5: Przejść kryteria liczbowej i wizualnej zgodności z roadmapy, PL/EN, trzy motywy, minimum/przywrócenie/maksymalizacja i DPI; zapisać osobne wyniki sprzętowe oraz przegląd etapu.
- [x] 11.20a E5: Odświeżyć sześć README PNG po zmianach źródeł i zakresu skanu przez test renderujący Avalonia/Skia, skontrolować obraz 1440×900 i potwierdzić opis demonstracyjnych danych. Pełne porównanie geometrii z oryginałem pozostaje w 11.20.
- [x] 11.21 E1: Dodać pasek wyboru sześciu trybów i kontekstowe grupy kontrolek; ukryć sterowanie specyficzne dla Antenna w pozostałych trybach i pokazać objaśnienie braku potwierdzonego źródła danych, bez prezentowania demonstracyjnego wykresu jako wyniku innego trybu. Dowód: `reviews/2026-11-04-analysis-modes.md` (U11).
- [ ] 11.22 E0: Wyjaśnić oznaczenie Bands `F/F` na podstawie formularza/eventów lub oryginału (U12); bieżący przegląd potwierdził nazwy kontrolek i klucze w binarium, ale dostarczony INI nie ma wartości `BANDS_LIMIT`, a semantyka `F/F` pozostaje nieznana ([`analyser306b-source-gaps-2026-11-07.md`](../../../../VNA-ReverseEngineering/analysis/analyser306b-source-gaps-2026-11-07.md)). Presety HF/VHF są w 11.7b, a Hold nad cyklem akwizycji w 11.11c; nie rozszerzać zakresu profilu.
- [x] 11.22.1 Zinwentaryzować z zachowanych odwołań statycznych surowe klucze konfiguracji pasm (`F_M...`); nie przypisywać im jednostek ani semantyki `F/F` bez dalszych dowodów. Review: `reviews/2026-11-07-stage-11-22-1-band-key-inventory.md`.

## 12. Katalog projektu i czytelność ustawień

- [x] 12.1 Przygotować samodzielny katalog projektu z kodem, testami, workflow i dokumentacją EN/PL; nie dołączać oryginalnych instalatorów i plików wykonywalnych.
- [x] 12.2 Poszerzyć panel ustawień skanu z uwzględnieniem przewijania i sprawdzić minimalny rozmiar okna.
- [x] 12.3 Sprawdzić testy aplikacji, skrypty wydania i poprawność OpenSpec w nowym katalogu; zapisać wynik przeglądu.
- [x] 12.4 Usunąć publiczne README/OpenSpec odnośniki do plików poza projektem i prywatną ścieżkę Windows, zachowując informację o zewnętrznym pochodzeniu źródeł; sprawdzić, że `reference` i `decompiled` pozostają rodzeństwem `VnaStudio`.
- [x] 12.5 Wykonać końcowy przegląd implementacji przez subagenta; poprawić błąd ponownego Connect zamykającego aktywny transport, dodać test regresji i sprawdzić pełną suitę.
- [x] 12.6 Po przeglądzie podglądu poszerzyć panel ustawień do 350 DIP, aby Hold i zakres markerów były widoczne bez obcięcia przez pasek przewijania; spakować Start/Stop i odświeżyć sześć podglądów.

## Workflow follow-up

- Aktualizować zadania i specyfikacje przy zmianie potwierdzonych wymagań.
- Archiwizować zmianę OpenSpec po realizacji zakresu i weryfikacji; sam status kompletu dokumentów nie oznacza gotowego portu.













