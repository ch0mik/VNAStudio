# Design

## Context

Typ przejściowy `DeviceProfile` nadal znajduje się w `Vna.Core`, a `Vna.Device.Vna65MHz` zawiera już źródło pomiaru, ustawienia protokołu i konwerter ADC. Pozostałe pola legacy są jeszcze używane przez UI i format sesji. Sprzętowe źródło pomiaru jest tworzone przez jawnie zarejestrowany plugin i katalog; adapter utrzymuje obecne API routera. Port pozostaje ścieżką systemową, a sesje zachowują migawkę profilu.

## Goals / Non-Goals

**Cele:** jawna granica aplikacja–urządzenie, możliwość dodawania innych modeli bez zmiany wykresów, izolacja protokołu i profili, zachowanie dotychczasowych sesji i ustawień, testowalność pluginu bez sprzętu.

**Poza zakresem pierwszej iteracji:** pobieranie pluginów z Internetu, ładowanie dowolnych DLL, sandboxing nieufnego kodu, marketplace, migracja wszystkich kalibracji do wspólnego formatu ani usunięcie dotychczasowych funkcji UI.

## Decisions

### 1. Granice projektów

- `Vna.Core` definiuje wyłącznie kontrakty stabilne dla UI i obliczeń wspólnych: metadane urządzenia, możliwości, opis profilu oraz interfejs źródła pomiarów.
- Nowy `Vna.Device.Vna65MHz` (Class Library) zawiera implementację urządzenia, algorytm konfiguracji DDS, serial framing, przeliczenia specyficzne dla detektora, opcje IOptions, parser/adapter profilu i zasoby profilu. Projekt może zależeć od Core i transportu, lecz Core nie zależy od pluginu.
- `Vna.App` zawiera UI, wspólny przepływ pomiaru, wybór źródła i rejestrację znanych pluginów. Nie odwołuje się do pól protokołu specyficznych dla VNA.
- `Vna.Devices.Transport` jest osobnym projektem z neutralną implementacją portu szeregowego (systemowa ścieżka, timeout, asynchroniczny odczyt i anulowanie); nie interpretuje komend konkretnego urządzenia. Plugin może referencjonować tę bibliotekę bez zależności od demo/playback i routera aplikacji.
- `Vna.Tests` testuje kontrakt, zachowanie każdego pluginu i testową implementację pluginu niezależnie od sprzętu.

### 2. Kontrakt pluginu

Core udostępnia `IDevicePlugin`, wersję kontraktu, `DevicePluginDescriptor`, modelowo neutralny `DeviceProfileDescriptor`, `DeviceProfilePresentationDescriptor`, `IMeasurementSource` i `DeviceConnectionRequest`. Profil kontraktowy zawiera zakres częstotliwości, domyślny sweep, impedancję odniesienia, capabilities/connectors, opcjonalne presety częstotliwości oraz nieprotokolarne wskazówki prezentacji (pasma, kolory RGB i domyślny VF). Nie ujawnia legacy flag, ustawień ADC, ramek, baud rate ani komend. Fabryka `IDevicePlugin.CreateSource` przyjmuje zadeklarowany descriptor profilu, a implementacja tworzy źródło bez zależności kontraktu Core od typu transportu. Operacje I/O są asynchroniczne, przyjmują `CancellationToken`, poprawnie zwalniają zasoby i nie blokują wątku UI.

Host i plugin muszą zgłaszać dodatnią wersję major i nieujemny minor. Akceptujemy zgodny major oraz minor pluginu nie większy od hosta; walidator tej reguły znajduje się w Core, a jego zastosowanie podczas rejestracji należy do etapu 3.1.

`DevicePluginCatalog` tworzy niezmienny snapshot jawnie zarejestrowanych implementacji, odrzuca niezgodny kontrakt, puste nazwy/ID, niepoprawne lub powtórzone ID profili i descriptorów (porównanie bez uwzględniania wielkości liter) oraz presety poza zakresem. Host tworzy katalog podczas startu, a niepoprawna rejestracja zatrzymuje start jasnym wyjątkiem zamiast pomijać plugin. VNA65 jest jawnie zarejestrowany; adapter do `IVnaDevice` pozostaje wyłącznie dla bieżącego routera i UI.

Help → Registered device profiles displays the validated plugin/profile descriptors, frequency ranges, reference impedances, capability availability and evidence levels. This catalog is informational and does not select or connect a source. Runtime plugin/profile switching remains a separate migration because the current router and legacy `DeviceProfile` adapter still bind one active hardware source.

Nie przekazywać do Core klas opcji VNA65, surowych bajtów ramek, rejestrów DDS ani pól legacy INI. Możliwości (`Sweep`, `Impedance`, `Calibration`, `GeneratorOutput`) deklaruje plugin; UI może ukrywać niedostępne funkcje na podstawie stabilnych możliwości, nie identyfikatora konkretnego modelu.

### 3. Profil urządzenia i konfiguracja

Profil to wersjonowany zbiór deklaratywnych parametrów danego wariantu urządzenia: granice i domyślne parametry skanu, impedancja odniesienia, etykieta, lista dostępnych funkcji oraz dane widoku. Profil nie zawiera wykonywalnego kodu. Plugin odpowiada za jego interpretację i walidację. Wbudowany profil będzie nazywał się „VNA 1–65 MHz”; `analyz.ini` może służyć jako źródło migracji podczas przeniesienia danych, ale nie pozostaje jego nazwą w UI.

Ogólne ustawienia użytkownika (port, język, motyw, preferencje wykresu) pozostają w ustawieniach aplikacji. Ustawienia protokołu, tolerancje i parametry sprzętowe należą do przestrzeni nazw/opcji pluginu. Ścieżka COM/tty przekazywana jest bez normalizacji pluginowi, zgodnie z systemem.

Przed migracją sesji część historycznych pól `DeviceProfile` utrzymuje zgodność INI/UI. Presety Generatora są już udostępniane jako neutralne `DeviceProfileDescriptor.FrequencyPresetsHz`, co nie deklaruje możliwości nadawania; `GeneratorOutput` pozostaje zablokowane do czasu potwierdzenia protokołu. Pasma, domyślne kolory i początkowy VF są mapowane do `DeviceProfilePresentationDescriptor`, dzięki czemu UI może je pobrać bez czytania pól legacy. ViewModel używa descriptoru do zakresów i domyślnej konfiguracji. Dla sesji zachowuje kolory z migawki historycznej; brak Zref w starej sesji pozostaje nieznany i nie jest uzupełniany z aktywnego profilu.

### 4. Odkrywanie i zaufanie

Pierwsze wydanie rejestruje pluginy statycznie przez `IServiceCollection` i jawne wywołania rejestracji z referencjonowanych projektów. Każdy plugin zgłasza major/minor wersji kontraktu. Host akceptuje zgodny major oraz minor pluginu nie większy od obsługiwanego minor; niezgodna wersja lub powtórzone ID powodują czytelny błąd konfiguracji i zatrzymują start hosta. Przy niekompatybilnej zmianie host zwiększa major i może jawnie wspierać dwa majory w okresie migracji. Plugin z nowszym, nieznanym majorem/minorem nie jest ładowany. Nie używamy automatycznego skanowania katalogów, `Assembly.LoadFrom` ani pluginów pobranych z sieci. To upraszcza podpisywanie/pakowanie i nie uruchamia nieufnego kodu.

### 5. DI, cykl życia i anulowanie

Rejestr pluginu dodaje jego fabryki i opcje (`IOptions<T>`/walidację). Każda aktywna instancja sprzętu jest scoped do połączenia, serializuje równoległy dostęp do portu, przyjmuje anulowanie przy skanie i zatrzymaniu, a przy zamknięciu/zmianie źródła jest asynchronicznie zwalniana. Przepływ UI zna tylko interfejs wspólny. Błędy pluginu są przekładane na wspólny model wyniku/błędu; nie wolno w razie błędu urządzenia po cichu przełączyć na demo.

### 6. Sesje, ustawienia i zgodność

Sesja zapisuje identyfikator pluginu, identyfikator profilu i migawki niezbędne do odczytu historycznego pomiaru. Format v6 dodaje opcjonalny neutralny `ProfileDescriptorSnapshot`; gdy dostępny jest stary `ProfileSnapshot`, migracja tworzy neutralny descriptor bez zmiany próbek. Nowe sesje zapisują neutralny descriptor, a starszy pełny profil pozostaje tylko do odczytu i jest zachowany przy ponownym zapisie starszej sesji. Ekran korzysta z historycznego descriptoru i pozwala otworzyć/odtworzyć sesję spoza aktywnego pluginu/profilu bez resolvera źródłowego pluginu. Odtwarzanie wymaga dokładnie zapisanego sweepu, nie otwiera COM i nie interpoluje. Skan sprzętowy pozostaje zablokowany, gdy zapisany plugin lub profil różni się od aktywnej konfiguracji; wyjątkiem jest jawna zgodność aliasu `legacy-vna65` z profilem `iw3hev-65mhz`. Legacy sesje bez `PluginId` zachowują wcześniejsze dopasowanie po profilu. Wersja v5 dodała `PluginId`; brakujące ID w sesjach v1–v4 o znanym profilu VNA65 i źródle `hardware` mapuje się na `legacy-vna65`; nieznane, demo i importowane źródła pozostają bez przypisanego pluginu. W formacie v5 brakujące ID pozostaje nieznane. Migawka nie jest nadpisywana aktualnym profilem. Ustawienia aplikacji przechowują wybór pluginu/profilu niezależnie od portu. W starszych JSON i sesjach zachować bieżące mapowanie dotychczasowego `iw3hev-65mhz`.

### 7. Pakowanie i dokumentacja

Każdy zatwierdzony plugin jest normalną zależnością projektu i trafia wraz z zasobami profilu do paczek RID w GitHub Actions. Test release sprawdza obecność assembly i profilu. Dokumentacja protokołu opisuje VNA 1–65 MHz jako implementację jednego pluginu; osobna strona opisuje publiczny kontrakt dla autorów kolejnych pluginów.

### 8. Dowody i dostępność funkcji

- Descriptor profilu rozdziela nazwę/model urządzenia, logiczny tryb pomiaru i fizyczne etykiety złączy. Nie zakłada mapowania nazw z pokrewnego oprogramowania (np. `ANT`/`VOB`) na etykiety BNC badanego urządzenia (`VNA`/`WOB`).
- Profil przechowuje descriptor dostępności/evidence dla trybów analizy, komend urządzenia i narzędzi aplikacji oraz osobne etykiety i role fizycznych złączy. Dane VNA/WOB są dołączane tylko do rozpoznanego profilu 1–65 MHz; obserwacja etykiety nie ustawia roli złącza. FILTER i Generator pozostają zablokowane. UI oparte na katalogu descriptorów pozostaje osobnym krokiem migracji.
- Każda funkcja sprzętowa ma status dowodowy w pluginie: potwierdzona dla tej wersji, eksperymentalna, nieobsługiwana albo zablokowana. UI może pokazać opis eksperymentalny, lecz polecenia sprzętowe aktywuje wyłącznie plugin, który ma potwierdzony protokół, fixture i bezpieczne warunki.
- Dla VNA 1–65 MHz zachowujemy nierozstrzygniętą konwencję punktu początkowego (`START` vs `START+STEP`) i 9600/115200 baud jako odrębne obserwacje transportowe. Nie zmieniamy etykiet próbek bez niezależnego pomiaru częstotliwości na dokładnym wariancie. Nieznane firmware ani port fizyczny nie mogą być wywnioskowane z VID/PID, obudowy lub podobnego źródła BASCOM.
- Sprzętowe FILTER i Generator pozostają wyłączone, dopóki komendy, role portów i ograniczenia RF dla dokładnego wariantu, wymagane fixture oraz niezależne pomiary nie przejdą review. Konfiguracja dziesięciu częstotliwości Generatora nie jest dowodem działania wyjścia.

### 9. Semantyka danych, kalibracja i sesje

- Wspólny model odróżnia próbki odbiciowe, skalarne próbki transmisyjne oraz zespolone parametry sieciowe. Sama amplituda FILTER, `|Z|` lub niepewny znak fazy nie tworzą zespolonego `S11` ani `S21`.
- Zespolone dane sieciowe są obecnie osobnym wersjonowanym `NetworkDataSet` z `PortCount`, próbkami 1-/2-portowymi i jawnym dodatnim Zref. Nie są konstruowane z `MeasurementPoint.TransmissionDb`; format plikowy Touchstone jest osobnym adapterem.
- Sesja zachowuje częstotliwości próbek dokładnie tak, jak zarejestrowano, identyfikatory pluginu/profilu, migawkę ustawień, tryb, impedancję odniesienia, pochodzenie surowych kanałów i zastosowane korekty. Konwencja siatki DDS może mieć wartość `Unknown`; odtwarzanie nie wymaga aktywnego pluginu i nie interpretuje ponownie historycznych próbek.
- Rekord korekty jest wersjonowany, niezmienny i ograniczony do pluginu/profilu, firmware evidence, trybu, zakresu i płaszczyzny odniesienia. Offset wyświetlania, korekta zegara DDS, kalibracja odbiciowa, skalarna normalizacja FILTER i fizyczny trim są osobnymi kategoriami. Plugin odrzuca niedopasowany rekord zamiast wybierać podobny.
- Sesja używa wersjonowanej migawki przetwarzania z osobnymi etapami offsetu widoku, korekty DDS, kalibracji odbicia, normalizacji FILTER i hardware trim. Każdy etap przechowuje status zastosowania, źródło, status dowodów, użyte parametry i opcjonalną niepewność; brak oszacowania zapisuje się jako brak wartości, nie jako zero. Migawka profilu konfiguracyjnego VNA 1–65 MHz jest oznaczona jako niezweryfikowana wzorcem.
- Brak migawki w starszym JSON lub imporcie CSV oznacza nieznane pochodzenie. Odtwarzanie pozostawia to pole puste i nie odtwarza ponownie obliczeń z aktualnym profilem. Nie kopiujemy wartości `RL_OFFSET_*` z konfiguracji VNA3p2/Analyser do profilu użytkownika.

### 10. Przyszłe narzędzia analityczne

- Snapshoty i porównywanie śladów są źródłowo neutralnym narzędziem; każdy ślad zachowuje jednostki, grid, tryb, profil i informację, czy jest pomiarem, importem, symulacją czy transformacją.
- Transformacja linii zapisuje osobno portowe `Zref` i charakterystyczne `Zc`, długość, VF, model strat, kierunek i pochodzenie. Wynik jest nową pochodną, nigdy nadpisanym sweepem.
- Projekt wejść, kalibracji/pochodzenia, ograniczeń idealnego port-extension, danych fixture, niezmiennego wyniku i bramek walidacji zapisano w [TRANSMISSION-LINE-TRANSFORMS.md](../../../docs/TRANSMISSION-LINE-TRANSFORMS.md) i [wersji polskiej](../../../docs/TRANSMISSION-LINE-TRANSFORMS.pl.md). Nie implementować UI/algorytmu de-embeddingu do czasu przejścia tamtejszych wzorców; skalarne FILTER i niejednoznaczne S11 pozostają odrzucone.
- Import/eksport Touchstone wymaga jednoznacznego portu, jednostek, impedancji odniesienia i zespolonych próbek. Nie generujemy S21/S2P z odbicia lub skalarnego FILTER ani S1P z samego modułu bez uzasadnionej fazy/znaku.
- Pierwszy adapter `Vna.IO` obsługuje tylko Touchstone v1.0/1.1 S1P/S2P, wspólne rzeczywiste Zref i RI/MA/DB. Odrzuca strukturę 2.0/2.1, nierówne referencje per port i inne typy parametrów; UI importu/eksportu pozostaje osobnym zadaniem.
- Automatyczna analiza kwarcu jest nową funkcją: etapuje się od markerów do Q dopiero po walidacji fazy, rozdzielczości i fixture. Ręczny kalkulator Q pozostaje osobny; dokumentacja legacy określa zakładkę XTAL jako niezaprogramowaną.
- RLC, trapy, transformatory i eksperyment IF są szablonami badawczymi do czasu weryfikacji exact-variant fixture i modelu. Eksperyment IF nie jest opisywany jako analizator widma.

### Podstawa badawcza

Szczegółowy indeks dowodów, nieprywatne streszczenia i surowe materiały reverse-engineering pozostają w osobnym katalogu badawczym, poza repozytorium VNA Studio. Publiczne źródła workflow: [VNA3p2](https://sp2swj.sp-qrp.pl/VNA3p2/VNA3p2.htm), [pomiary wzorcowe VNA3p](https://sp2swj.sp-qrp.pl/IW3HEV/VNA3p_montaz/VNA3p_Pomiary_wzorcowe.htm), [Zplots](https://www.ac6la.com/zplots1.html) i [uwagi o znaku reaktancji](https://www.ac6la.com/zpapndx3.html). Są to źródła pokrewnych workflow, a nie dowód zgodności hardware VNA 1–65 MHz.

### Smith chart: usability and evidence gates

The Zplots review is recorded in the sibling research directory `../../../../VNA-ReverseEngineering/analysis/zplots-smith-review-2026-11-06.md`, outside this repository. Selected vna/J 3.4.8 Smith/serial classes were also inspected statically with system `javap`; the separate [research report](../../../../VNA-ReverseEngineering/analysis/vnaj-3.4.8-javap-static-review-2026-11-07.md) records its configurable admittance/impedance grids and distinct standard-driver protocol, without treating them as VNA65 hardware facts. The optional constant-SWR circle and nearest-sampled-point hover readout are implemented. Named `.vnasnapshot.json` files preserve the complete validated session plus a separate display-only return-loss offset. Smith overlays retain each sweep's own samples and `Zref`, draw behind the active trace, and do not interpolate or infer an unverified reactance sign. A present neutral descriptor is authoritative when `Zref` is unknown; older profile metadata is used only when no neutral descriptor exists. The frequency plot can overlay one selected metric at a time, using snapshot-specific colors and native sample grids. The current overlay list is session-local and can be reopened from files. Transmission-line add/subtract remains gated on exact-profile complex-data and model validation; `Zref` and line `Zc` stay distinct and results remain derived from immutable source sweeps.

## Risks / Trade-offs

Stabilność kontraktu ogranicza tempo zmian w Core; wersjonowanie API i test kontraktowy redukują ryzyko. Statyczna rejestracja wymaga zmiany/publikacji aplikacji przy dodaniu pluginu, ale jest prostsza i bezpieczniejsza od ładowania obcego kodu. Migracja profilu legacy wymaga ostrożnego zachowania sesji, kolorów i ustawień użytkownika.

## Migration Plan

Najpierw zinwentaryzować zależności i zamrozić testami dotychczasowe zachowanie, następnie wprowadzić neutralne kontrakty i atrapę pluginu, wydzielić transportową logikę VNA 1–65 MHz, migrować rejestrację i ustawienia, potem format sesji/pakowanie/dokumentację. Każdy etap kończy się testami i review niezależnego subagenta. Obecny działający sterownik należy zachować, aż test integracyjny udowodni identyczne komendy, bajty, konwersje, anulowanie i zwalnianie COM.

## Open Questions

- Czy `IVnaDevice` ma reprezentować urządzenie sprzętowe, czy dostarczać `IMeasurementSource`/fabrykę skanu? Ustalić na etapie kontraktu po sprawdzeniu aktualnych interfejsów źródła, bez przenoszenia logiki protokołu do Core.
- Czy profile będą JSON-em czy typowanymi rekordami dostarczanymi przez plugin? Preferowany jest model typowany w runtime i deklaratywny, wersjonowany JSON jako zasób profilu, jeśli nie komplikuje walidacji.
- Które możliwości występują niezależnie od `MeasurementMode` i jak mapować dotychczasowy CSV/session bez fałszywej identyfikacji sprzętu?
