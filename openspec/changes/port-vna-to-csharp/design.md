# Design

## Context

Uzasadnienie i zakres opisuje `proposal.md`. Plan przeniesiono z `PLAN.md` i `PLOTS.md`, zachowując te pliki jako materiały odniesienia.

Stan na 2026-11-03:

- vna/J 3.4.8 pobrano i zinwentaryzowano: 656 klas. Dekompilacja Java nie została wykonana.
- Analyser 306b i miniVNA230 są aplikacjami VB6 native. Ghidra uzyskała pseudokod 74/78 i 135/142 wykrytych kandydatów procedur; pozostałe mają asembler. Te proporcje nie oznaczają procentu odzyskanej funkcjonalności.
- INI wskazuje 1–65 MHz, DDS 191993220 Hz, 649 próbek i 50 Ω. To zapis konfiguracji, nie odczyt urządzenia ani potwierdzenie rewizji hardware/firmware.
- Instrukcja wskazuje Analyser 306b jako właściwy program. miniVNA230 służy w pakiecie instalacji zależności.
- Zrzuty instrukcji przedstawiają starszą wersję interfejsu; aktualny wygląd należy uzgadniać z zasobami 306b i INI.

## Goals / Non-Goals

**Goals:**

- Oddzielić transport, obliczenia i grafikę tak, aby te same dane referencyjne sprawdzały każdy etap.
- Najpierw dostarczyć obsługę wariantu użytkownika, potem rozszerzać macierz zgodności z vna/J.
- Zapisać jawne kryteria zgodności wyników i wyglądu wykresów.

**Non-Goals:**

- Pierwszy etap nie obejmuje wszystkich modeli miniVNA ani wszystkich narzędzi vna/J; pozostają w dalszych zadaniach tej mapy rozwoju.
- Nie deklarujemy pełnego odzyskania źródeł VB6, formalnego procesu clean-room ani zgodności ze sprzętem na podstawie symulatora.

## Decisions

### Platforma i granice modułów

Decyzja użytkownika: Avalonia UI z własną kontrolką wykresu. Projekt przypina target .NET 10 i pakiety Avalonia 12.1.4. Kompilacja wszystkich projektów przeszła z SDK 10.0.402; 59 testów, w tym interakcje Avalonia Headless/Skia, lokalizacja i motywy, przeszło 2026-11-03. Potwierdzono uruchomienie wcześniejszej wersji okna na pulpicie Windows. Zakres i ograniczenia sprawdzeń zapisują `reviews/2026-11-03-plot.md`, `reviews/2026-11-03-xaml-startup.md`, `reviews/2026-11-03-localization.md` oraz `reviews/2026-11-03-themes.md`. Pierwsze wdrożenie i testy sprzętowe dotyczą Windows; architektura zachowuje możliwość wydania na Linux/macOS, ale nie deklaruje zgodności tych platform bez osobnych sprawdzeń.

- `Vna.Localization`: wspólne zasoby RESX, kultura interfejsu oraz komunikaty z zachowanymi kluczami i argumentami; bez zależności od Avalonia i portów.
- `Vna.Core`: jednostki, liczby zespolone, modele próbek, kalibracja i obliczenia bez UI/portów.
- `Vna.Devices`: transport, profile i sterowniki urządzeń, anulowanie i timeouty.
- `Vna.IO`: sesje, profile, pomiary, kalibracje i import/eksport.
- `Vna.App`: Avalonia/MVVM i własna kontrolka wykresu.
- `Vna.Tests`: wektory referencyjne oraz symulator transportu, bez zastępowania testów sprzętowych.

### Protokół jako etap badawczy przed sterownikiem

Najpierw powstanie udokumentowany kontrakt urządzenia: baudrate, format ramek, kolejność bajtów, jednostki, komendy i odpowiedzi każdego trybu. Dane PA7N (115200/8N1, parametry zakończone CR, cztery bajty na próbkę) są hipotezą do sprawdzenia. Nie przyjmować domyślnego DDS 400 MHz ani zgodności protokołu fabrycznego miniVNA.

Nieznane szczegóły protokołu są wynikiem zaplanowanego etapu badawczego. Implementację odpowiedniego sterownika rozpocząć po dostarczeniu tego kontraktu, nie po upływie czasu ani na podstawie założeń.

Nazwa portu jest ustawieniem użytkownika przechowywanym w pliku konfiguracyjnym w `LocalApplicationData`. Pokazać nazwy zgłaszane przez system jako opcje, ale pozostawić ręczne wpisanie możliwe. Traktować wybraną nazwę jako nieprzezroczysty identyfikator platformy: przekazywać `COM5`, `/dev/ttyUSB0`, `/dev/ttyACM0` lub inną podaną ścieżkę bez konwersji na numer portu. `RS_address=5` z legacy INI opisuje tylko dawną konfigurację Windows i nie stanowi wartości domyślnej na innych systemach.

Surowy transport `System.IO.Ports` przyjmuje baudrate, bity danych, parzystość, bity stopu i handshake jako jawne ustawienia urządzenia. Nie umieszczać zgadywanych parametrów domyślnych w warstwie wspólnej. Protokół Analyser 306b nie jest jeszcze podłączony do tego transportu.

### Lokalizacja PL/EN

Zgodnie z decyzją użytkownika teksty aplikacji są zapisane w `.resx`: angielski jest zestawem neutralnym, polski jest zasobem satelitarnym `pl`. Selektor języka zmienia powiązania XAML, legendę i własne komunikaty podczas pracy. `AppSettings.Language` zachowuje wybór przy zapisie ustawień wraz z portem. Starsza konfiguracja wybiera polski.

Kultura usługi lokalizacji służy do wyświetlania i odczytu liczb, bez zmiany kultury całego procesu. Przełączenie zachowuje wartości pól i próbek. Sesje używają identyfikatorów źródeł niezależnych od tłumaczenia. Zmiany opcji są przekazywane na wątek UI; subskrypcje usługi są zwalniane razem z modelem widoku.

### Paczki release

Workflow `release.published` waliduje tag `vX.Y.Z` i publikuje self-contained dla Windows/Linux/macOS, x64/ARM64. Windows otrzymuje ZIP, platformy Unix tar.gz z prawem wykonywania. Skrypty sprawdzają wersję assembly, RID zależności, runtime, profil i zasoby. Dopiero po testach oraz powodzeniu całej macierzy osobny job z `contents: write` przesyła sześć paczek i manifest SHA-256 do istniejącego release. Szczegóły i ograniczenia sprawdzeń zapisują `docs/RELEASING.md` oraz `reviews/2026-11-03-release.md`.

### Motywy Light, Dark i Night Vision

Okno wybiera ThemeVariant z ustawień modelu widoku i korzysta z semantycznych zasobów DynamicResource. NightVision dziedziczy szablony Dark, a konkretne zasoby kontrolek Fluent definiują czerwone kolory stanów interakcji. `AppSettings.Theme` zachowuje wybór razem z portem i językiem. Motyw jest preferencją wyświetlania; nie nadpisuje kolorów profilu zapisywanych w sesji.

Wykres i legenda współdzielą paletę. Dark rozjaśnia ciemne kolory na ciemnym tle, a Night Vision używa czerwonych odcieni i różnych wzorów linii. Każdy ciąg poprawnych próbek jest osobną figurą StreamGeometry, dzięki czemu wzór nie resetuje się między próbkami, a luki są zachowane. Zrzuty README są generowane przez rzeczywisty renderer Skia w testach headless na syntetycznym skanie 649 punktów.

### Rysowanie wykresów

Wybrano własną kontrolkę Avalonia dziedziczącą po `Control`, z rysowaniem przez `Render(DrawingContext)`. Daje kontrolę nad nietypowymi skalami, układem etykiet i warstwami. Transformacje wartości na współrzędne i wybór próbek mają pozostawać niezależne od Avalonia, aby można je było sprawdzać bez okna. Rozdzielić tło/siatkę, przebiegi oraz markery; unieważniać rysowanie po zmianie danych, profilu lub rozmiaru. Buforowanie dodać tam, gdzie pomiary prototypu wykażą potrzebę.

Okno ma obsługiwać minimalizację, maksymalizację i zmianę rozmiaru. Utrzymywać minimalny rozmiar 1024×768 DIP, używać kompaktowych czcionek i przewijać panel ustawień przy ograniczonej wysokości. Obszar wykresu rozciąga się w siatce gwiazdkowej, a kontrolka wykresu wylicza siatkę, osie i pozycje markerów z bieżącego rozmiaru zamiast zapamiętywać geometrię początkową.

`PlotAxisScale` jest wspólnym źródłem wartości etykiet i pozycji krzywych. `PlotSeriesGeometry` buduje segmenty, przerywając je dla niedostępnych i nieskończonych wartości oraz błędnej jakości próbki. `PlotCoordinateTransform` obsługuje mapowanie X/Y i odwrócenie pozycji wskaźnika na częstotliwość. Markery wybierają najbliższą próbkę w bieżącym widoku; przy przeciąganiu kontrolka przechwytuje wskaźnik. Stos historii zoomu umożliwia powrót przez poprzednie zakresy. Profile skal pozostają wstępne do czasu porównania z odczytami oryginału.

Rozważono WPF z własnym rysowaniem oraz ScottPlot i OxyPlot. Zgodnie z wyborem użytkownika realizujemy Avalonię z własną kontrolką; biblioteka wykresowa nie jest zależnością planowanej implementacji. Obsługę wskaźnika, przechwytywania podczas przeciągania i przeliczeń DPI oprzeć na mechanizmach Avalonia, zachowując przypisania M1/M2. Interakcje nie mogą zmieniać danych RF.

Nie zakładać logarytmicznej skali SWR ani tego samego kierunku każdej osi. Profile wizualne mają przechowywać potwierdzone zakresy, jednostki, kierunki i kolory. Wybór Avalonia nie zmienia wymagań `legacy-plots` ani kryteriów zgodności z oryginałem.

### Referencje i zgodność

Dla każdej funkcji macierz zgodności określa źródło, zakres, status i sposób sprawdzenia. Najpierw porównać surowe próbki i częstotliwości punktów, potem kalibrację i wartości RF, a na końcu grafikę. Tolerancje numeryczne oraz wizualne ustalić i zapisać przed oceną zgodności; nie wyznaczać ich z wyniku nowej implementacji.

Format sesji własnej ma zawierać wersję, profil urządzenia, jednostki, próbki oraz odniesienie do kalibracji. Import starszych plików deklarować dopiero po analizie formatu i testach. Demonstracje mają być wyraźnie oznaczone jako dane demonstracyjne.

## Risks / Trade-offs

- [Niepełna dekompilacja obejmuje duże procedury] → analizować asembler i porównywać zachowanie; prowadzić listę nierozstrzygniętych funkcji.
- [Nieznany firmware i protokół] → etap badawczy oraz zapisy komunikacji przed sterowaniem sprzętem.
- [Zrzuty starszej wersji] → osobno sprawdzać geometrię zrzutów, zasoby 306b i konfigurację kolorów.
- [Różnice DPI/fontów] → porównania przy tych samych wymiarach i skalowaniu 100/125/150%, z rozdzieleniem geometrii od antyaliasingu.
- [Dodatkowy koszt własnego wykresu] → najpierw prototyp na zapisanych próbkach i ocena kryteriów z `legacy-plots`.
- [Deklaracja CC BY-NC-ND w JAR i nieustalone warunki PA7N] → zinwentaryzować warunki przed rozpowszechnianiem portu; nie traktować tego dokumentu jako oceny prawnej.

## Migration Plan

Uszczegółowienie kolejnych etapów na podstawie dwóch ekranów i instrukcji zapisano w `roadmap-screenshots.md` (E0–E5). Priorytet P0 ma kontrakt urządzenia, poprawność przeliczeń i kalibracja. Niezależny pakiet UI/obliczeń E1 można wykonywać na sesjach i demonstratorze podczas badań protokołu. Dodatkowe narzędzia zależą od zweryfikowanych zdolności odbicia/transmisji/generatora; nazwa zakładki nie oznacza nowego polecenia sprzętowego.

Plan rozdziela zakres pomiaru, powiększenie i pamięć start/stop oraz różnicę markerów od fizycznego pasma/Q. Nowe formaty zachowują tryb i kontekst analizy, a odczyt v1 nie nadaje starym wynikom niepotwierdzonej kalibracji. Dotychczasowe PL/EN, motywy, branding VNA Studio/SQ7MRU i workflow RID pozostają częścią kryteriów odbioru.

1. Zachować oryginały, sumy i raporty, a plan prowadzić w tej zmianie OpenSpec.
2. Zbudować kontrakt protokołu, macierz zgodności i referencje.
3. Dostarczyć obliczenia i tryb demonstracyjny; potem wykresy i sterownik sprzętu.
4. Dołączać formaty oraz kolejne urządzenia/funkcje według macierzy.
5. Przed wydaniem sprawdzić pełny przepływ i opublikować zakres potwierdzonej zgodności. Poprzednie programy i ich dane pozostają dostępne jako punkt odniesienia.

Archiwizacja zmiany OpenSpec następuje po implementacji i spełnieniu zadań, nie po samym zapisaniu planu.
