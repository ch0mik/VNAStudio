# Tasks

## 1. Inwentaryzacja i kontrakt

- [x] 1.1 Sporządzić mapę zależności Core/App/Devices/IO i pól `DeviceProfile`, które są używane w każdym projekcie (`docs/DEVICE-PROFILE-DEPENDENCIES.md`).
- [x] 1.2 Zdefiniować w Core wersję kontraktu, neutralny descriptor profilu bez pól legacy/protokołu, descriptor pluginu, `IMeasurementSource` oraz fabrykę niezależną od transportu (`IDevicePlugin.CreateSource`).
- [x] 1.3 Dodać zgodnościowy test kontraktu z testowym pluginem bez sprzętu (`reviews/2026-11-07-stage-1-3-plugin-contract-test.md`).
- [x] 1.4 Udokumentować politykę zgodności major/minor i dodać test akceptacji/odrzucenia wersji (`reviews/2026-11-07-stage-1-4-version-policy-test.md`).
- [x] 1.4a Udokumentować zasady zgodności API pluginów, deskryptory, rejestrację i poziomy dowodów EN/PL (`docs/DEVICE-PLUGINS.md`, `.pl.md`; `reviews/2026-11-07-stage-1-4a-plugin-docs.md`).
- [x] 1.5 Dodać test architektury bezpośrednich operacyjnych odczytów (referencji IL) pól protokołu VNA 1–65 MHz w App/Core i brak tych pól w neutralnym API pluginu; `DeviceProfile` pozostaje wyłącznie zgodnościowym snapshotem starych sesji (`reviews/2026-11-07-stage-1-5-plugin-boundary-test.md`).
- [x] 1.6 Wykonać review subagenta i poprawić kontrakt przed migracją sterownika (`reviews/2026-11-06-stage-1-2-plugin-contract.md`).

## 2. Projekt pluginu VNA 1–65 MHz

- [x] 2.1 Utworzyć `Vna.Device.Vna65MHz` i przenieść do niego profil „VNA 1–65 MHz” wraz z walidacją i opcjami `IOptions` (`reviews/2026-11-06-stage-2-1a-vna65-profile-project.md`, `reviews/2026-11-06-stage-2-1b-profile-seed.md`).
- [x] 2.1a Utworzyć projekt `Vna.Device.Vna65MHz` i przenieść do niego typowane opcje INI, walidator i fabrykę profilu; rejestracja nadal używa `IOptions<T>`.
- [x] 2.1b Przenieść źródło deklaracji profilu do pluginu i usunąć legacy statyczny `DeviceProfile.Vna65MHz` z Core po migracji jego konsumentów (`reviews/2026-11-06-stage-2-1b-profile-seed.md`).
- [x] 2.2 Przenieść protokół, ramki, komendy i urządzeniowe przeliczenie ADC→RF za interfejs pluginu bez zmiany zachowania transportu COM.
- [x] 2.2a Wydzielić neutralny port szeregowy (`Vna.Devices.Transport`) z `Vna.Devices`: zachować ścieżkę systemową bez zmian, port catalog, async I/O, timeout i CancellationToken; nie przenosić komend/framingu do projektu transportu. (`reviews/2026-11-06-stage-2-2a-serial-transport-project.md`)
- [x] 2.2b Przenieść komendy DDS, ramki, parsowanie kanałów i obsługę UART do `Vna.Device.Vna65MHz`; zachować dotychczasowe bajty, timing, cancellation i użycie neutralnego `DeviceConnectionRequest.PortName` (`reviews/2026-11-06-stage-2-2b-vna65-source.md`).
- [x] 2.2c Przenieść przeliczenie ADC→RF z `Vna.Core.RfSampleCalculator` do pluginu lub jawnego neutralnego pipeline'u przetwarzania; zachować wyniki historyczne przez wersjonowany profil przetwarzania i migawkę sesji (`reviews/2026-11-06-stage-2-2c-vna65-conversion.md`).
- [x] 2.3 Przenieść do pluginu pamięci generatora i parametry specyficzne dla urządzenia; UI i pomiarowe API używają neutralnych descriptorów, a typ legacy pozostaje wyłącznie na granicy zgodności starszych sesji (`reviews/2026-11-06-stage-2-3a-plugin-protocol-settings.md`, `reviews/2026-11-06-stage-2-3b-frequency-presets.md`, `reviews/2026-11-07-stage-2-3c5-processing-provenance.md`).
- [x] 2.3a Wprowadzić typowane ustawienia protokołu VNA65 w projekcie pluginu; źródło pomiaru i konwerter nie mogą przyjmować `DeviceProfile` z Core (`reviews/2026-11-06-stage-2-3a-plugin-protocol-settings.md`).
- [x] 2.3b Przenieść pamięci częstotliwości Generatora do danych pluginu i odseparować je od ogólnego profilu sesji (`reviews/2026-11-06-stage-2-3b-frequency-presets.md`).
- [x] 2.3c Zastąpić pozostałe operacyjne pola `DeviceProfile` neutralnymi descriptor/presentation DTO oraz migracją sesji; zachować legacy snapshot wyłącznie do odczytu/zapisu zgodnościowego bez reinterpretacji próbek (`reviews/2026-11-06-stage-2-3c1-presentation-descriptor.md`, `reviews/2026-11-06-stage-2-3c2-viewmodel-presentation.md`, `reviews/2026-11-06-stage-2-3c3-session-v6.md`, `reviews/2026-11-06-stage-2-3c4-session-playback.md`, `reviews/2026-11-07-stage-2-3c5-processing-provenance.md`, `reviews/2026-11-07-stage-2-3c6-neutral-csv-import.md`).
- [x] 2.3c.1 Dodać neutralny `DeviceProfilePresentationDescriptor`; katalog pluginów waliduje i kopiuje pasma, kolory RGB oraz domyślny VF, a plugin VNA65 mapuje do niego dane zgodnościowe bez ujawniania pól protokołu (`reviews/2026-11-06-stage-2-3c1-presentation-descriptor.md`).
- [x] 2.3c.2 Przenieść bieżący zakres, etykietę, pasma, domyślny VF i paletę UI na neutralny descriptor; zachować kolory oraz Zref historycznej sesji, a brak migawki Zref pozostawić jako nieznany (`reviews/2026-11-06-stage-2-3c2-viewmodel-presentation.md`).
- [x] 2.3c.3 Dodać format sesji v6 z opcjonalną neutralną migawką profilu; migrować starszy `ProfileSnapshot` bez zmiany próbek/częstotliwości i nadal czytać format legacy (`reviews/2026-11-06-stage-2-3c3-session-v6.md`).
- [x] 2.3c.4 Konsumować neutralny snapshot w widoku historycznej sesji i zachować go przy ponownym zapisie; umożliwić playback danych spoza aktywnego profilu bez resolvera źródłowego pluginu (`reviews/2026-11-06-stage-2-3c4-session-playback.md`).
- [x] 2.3c.5 Budować tymczasowy profil UI z neutralnego descriptoru i otrzymywać pochodzenie przetwarzania od źródła, bez zapisywania placeholderów DeviceProfile (`reviews/2026-11-07-stage-2-3c5-processing-provenance.md`).
- [x] 2.3c.6 Importować CSV względem aktywnego neutralnego descriptoru i zapisywać go jako migawkę sesji, zachowując nieznane Zref; walidować zakres skanu demonstratora descriptor-em, jeśli jest dostępny (`reviews/2026-11-07-stage-2-3c6-neutral-csv-import.md`).
- [x] 2.4 Uruchomić testy znanych ramek, częstotliwości i pomiaru oraz review subagenta (`reviews/2026-11-07-stage-2-4-vna65-offline-frame-tests.md`).

## 3. Rejestracja i wybór

- [x] 3.1 Rejestrować zatwierdzone pluginy jawnie przez DI; walidować zgodność wersji API oraz unikalność identyfikatorów.
- [x] 3.1a Dodać niezmienny katalog pluginów walidujący zgodność wersji kontraktu oraz niepuste, unikalne (bez względu na wielkość liter) ID pluginów i profili. Podłączyć katalog do composition root; rejestracja konkretnego VNA65 nastąpi po przeniesieniu implementacji w 2.2 (`reviews/2026-11-06-stage-3-1a-plugin-catalog.md`).
- [x] 3.1b Jawnie zarejestrować `Vna65MHzPlugin` w DI/katalogu i tworzyć jego źródło przez descriptor; pozostawić adapter tylko dla obecnego routera/UI (`reviews/2026-11-06-stage-2-2b-vna65-source.md`).
- [x] 3.2 Udostępnić listę modeli i profili w UI według descriptorów, a dostępność skanów sprzętowych sterować deklarowanymi możliwościami (`reviews/2026-11-06-stage-3-2a-profile-catalog-ui.md`, `reviews/2026-11-07-stage-3-2b-capability-scan-gate.md`).
- [x] 3.2a Dodać w Pomocy informacyjny, EN/PL katalog pluginów i profili według `DevicePluginCatalog.Descriptors`, z zakresem i możliwościami/statusami z descriptorów. Dialog nie przełącza źródła; wybór aktywnego profilu pozostaje osobnym zadaniem (`reviews/2026-11-06-stage-3-2a-profile-catalog-ui.md`).
- [x] 3.2b Sterować dostępnością skanu sprzętowego według możliwości wybranego descriptora; blokować skany zablokowane/nieznane przed połączeniem wykonywanym przez Start i wyjaśniać stan po polsku/angielsku (`reviews/2026-11-07-stage-3-2b-capability-scan-gate.md`).
- [x] 3.3 Zachować konfigurację ścieżki COM/tty jako wartość systemową przekazywaną bez zmian; używać wyłącznie opcji pluginu dla ustawień jego protokołu (`reviews/2026-11-06-stage-3-3-port-boundary.md`).
- [x] 3.4 Potwierdzić błędy, zmianę źródła, Stop, anulowanie i async disposal; wykonać review subagenta (`reviews/2026-11-07-stage-3-4-source-lifecycle.md`).

## 4. Sesje, ustawienia i starsze dane

- [x] 4.1 Zapisać ID pluginu/profilu w nowych sesjach i zachować migawkę historyczną niezbędną do interpretacji wykresu (`reviews/2026-11-06-stage-4-1a-session-plugin-id.md`).
- [x] 4.1a Dodać opcjonalny `PluginId` w formacie sesji v5; nowe skany zapisują tożsamość źródła, a sesje v1–v4 ze znanym profilem VNA65 i `hardware` migrują do `legacy-vna65`. Nieznane ID w nowych sesjach pozostaje puste. Odtworzenie zachowuje ID z pliku i nie wymaga aktywnego pluginu. (`reviews/2026-11-06-stage-4-1a-session-plugin-id.md`)
- [x] 4.2 Migrować profil `iw3hev-65mhz` oraz starsze sesje, zachowując zapisane próbki, sweep, Z0, fazę, kolory i zakres, gdy te metadane są obecne; brak historycznych danych pozostaje nieznany i nie jest uzupełniany z aktywnego profilu (`reviews/2026-11-06-stage-4-2-legacy-session-migration-audit.md`).
- [x] 4.3 Rozdzielić wybór pluginu/profilu od ogólnych preferencji aplikacji i dodać walidację ustawień (`reviews/2026-11-07-stage-4-3-device-selection.md`).
- [x] 4.3a Zapisać wybór jako osobną sekcję `DeviceSelection` i zachować kompatybilność pustej sekcji przez jednoznaczny wybór jednego pluginu/profilu.
- [x] 4.3b Rozwiązywać wybrany plugin/profil z katalogu, tworzyć źródło z descriptora i odrzucać nieznane lub częściowe wybory bez fallbacku.
- [x] 4.3c Walidować ustawienia przy odczycie/zapisie, utrzymać port i źródło poza identyfikatorem profilu oraz opisać konfigurację EN/PL.
- [x] 4.3d Wykonać review subagenta; zapisać ograniczenia i poprawki.
- [x] 4.4 Odtwarzać sesję z zapisanych sweepu i migawki bez wymaganego pluginu; dodać test nieznanego/uninstaliowanego plugin ID (`reviews/2026-11-07-stage-4-4b-playback-unknown-plugin.md`).
- [x] 4.4a Otwierać i odtwarzać sesję z neutralnym/legacy snapshotem spoza aktywnego pluginu/profilu; zachować dokładny sweep i nie otwierać źródłowego pluginu (`reviews/2026-11-06-stage-2-3c4-session-playback.md`).
- [x] 4.4b Dodać test migracji/playback dla nieznanego lub niezainstalowanego plugin ID (`reviews/2026-11-07-stage-4-4b-playback-unknown-plugin.md`).
- [x] 4.5 Potwierdzić testy migracji i playback bez pluginu; wykonać review subagenta (`reviews/2026-11-07-stage-4-5-session-playback-verification.md`).

## 5. Release i dokumentacja

- [x] 5.1 Uwzględnić assembly i zasób profilu pluginu we wszystkich dostępnych RID; walidować każdy wynik publish przed pakowaniem w workflow release (`scripts/ReleaseTools.psm1`, `scripts/Publish-Release.ps1`).
- [x] 5.2 Zaktualizować README EN/PL oraz dokumentację protokołu, opisując architekturę i kontrakt dodawania modeli (`reviews/2026-11-06-stage-5-2-plugin-documentation.md`).
- [x] 5.3 Uruchomić pełne testy i strict OpenSpec validation; wykonać finalne review subagenta (`reviews/2026-11-07-stage-5-3-full-verification.md`).

## 6. Dowody, dane RF i funkcje przyszłe

- [x] 6.1 Dodać do profilu jawny status dowodów i dostępności dla trybów, komend i fizycznych złączy. Profile FILTER/Generator pozostają zablokowane; etykiety VNA/WOB są zapisane bez przypisywania niepotwierdzonych ról elektrycznych. Katalog UI sterowany descriptorami pozostaje w 3.2.
- [x] 6.2 Zapisywać `FrequencyGridConvention` w sesji, domyślnie `Unknown`, i nie zmieniać zachowanych częstotliwości punktów. Rzeczywiste `START` vs `START+STEP` pozostaje bramką sprzętową przed zmianą mapowania.
- [x] 6.3a Dodać typ sesji odróżniający odbicie pochodne, skalarną amplitudę transmisji i nieznane dane; walidować kształt próbek i zgodność z trybem.
- [x] 6.3b Dodać wersjonowany model zespolonych S-parametrów 1-/2-portowych z jawnym `Zref`; nie konstruować S-parametrów z danych skalarnego FILTER.
- [x] 6.3c Dodać do `Vna.IO` import/eksport Touchstone v1.0/1.1 S1P/S2P z jawnym wspólnym `Zref`, konwersją RI/MA/DB, domyślnymi wartościami nagłówka z normy i udokumentowanym odrzucaniem nieobsługiwanych plików 2.x (`reviews/2026-11-06-stage-6-3c-touchstone-io.md`).
- [x] 6.4 Dodać wersjonowaną migawkę przetwarzania/kalibracji z pochodzeniem, stanem dowodów i miejscem na niepewność; rozdzielić display offset, korektę DDS, kalibrację odbicia, normalizację FILTER i hardware trim. Starsze sesje zachowują brak migawki jako `null` i odtwarzają dotychczasowe próbki bez zmian.
- [x] 6.5 Zaplanować transformację linii z oddzielnym `Zref`/`Zc` i adapterem Touchstone; udokumentować wymagania zespolonych danych, kalibracji DUT/fixture, pochodzenie wyniku i bramki walidacji (`docs/TRANSMISSION-LINE-TRANSFORMS.md`, `.pl.md`; `reviews/2026-11-06-stage-6-5-transmission-line-design.md`).
- [x] 6.5a Dodać format nazwanej, niezmiennej migawki, zachowujący pełną sesję, pochodzenie i natywne częstotliwości próbek (`reviews/2026-11-06-stage-6-10a-snapshot-file.md`).
- [x] 6.6 Utrzymać ręczny Q odrębny; automatyczne kwarce, RLC/trapy/transformatory i IF pozostają zablokowane do walidacji fixture/modelu i wymagają statusu eksperymentalnego (`docs/RESONANCE-Q.md`, `.pl.md`; `reviews/2026-11-06-stage-6-6-resonance-and-future-analysis-gates.md`).
- [x] 6.7 Przejrzeć wymagania i zatwierdzone źródła reverse-engineering przed implementacją; nie traktować pokrewnych VNA3p2/PA7N materiałów jako identyfikacji VNA 1–65 MHz (`reviews/2026-11-06-research-additions.md`).
- [ ] 6.8 Zmierzyć niezależnym wzorcem częstotliwości próbki 0/1/ostatniej na zidentyfikowanym firmware i dopiero wtedy ustawić konwencję dla profilu sprzętowego.
- [x] 6.9a Dodać do Smith chart koło SWR w trybie ukryte/ustalone/związane z markerem; wartość ustalona musi być skończona i `SWR ≥ 1`, a tryb markerowy można wyliczać tylko z prawidłowego zespolonego Γ przy `|Γ| < 1` i skończonym wyniku; nie zmieniać przebiegu źródłowego (`reviews/2026-11-06-stage-6-9a-smith-swr-circle.md`).
- [x] 6.9b Dodać odczyt hover najbliższej rzeczywistej próbki Smith: częstotliwość, `R ± jX` tylko przy zweryfikowanym znaku, `Zref` i confidence; dla niejednoznacznego znaku pokazać oba warianty bez interpolacji (`reviews/2026-11-06-stage-6-9b-smith-hover.md`).
- [x] 6.10 Dodać porównywanie przebiegów z zachowaniem pochodzenia i natywnych siatek częstotliwości; nie interpolować po cichu (`reviews/2026-11-06-stage-6-10c-frequency-snapshot-overlays.md`).
- [x] 6.10a Dodać nazwany format pliku migawki, który kopiuje próbki i metadane, zachowuje zapisane sesje i korzysta z walidacji/migracji sesji (`reviews/2026-11-06-stage-6-10a-snapshot-file.md`).
- [x] 6.10b Dodać w aplikacji zapis/otwieranie wielu migawek i nakładki na wykresie Smitha z `Zref` właściwym dla każdej sesji oraz bez interpolacji (`reviews/2026-11-06-stage-6-10b-smith-snapshot-overlays.md`).
- [x] 6.10c Dodać na wykresie częstotliwościowym nakładkę wybranej wielkości dla każdej widocznej migawki, zachowując próbki i właściwe offsety wyświetlania (`reviews/2026-11-06-stage-6-10c-frequency-snapshot-overlays.md`).
- [ ] 6.11 Rozważyć transformację linii dopiero po walidacji exact-profile danych zespolonych i modelu; przechowywać osobno `Zref`/`Zc`, parametry linii i pochodzenie wyniku pochodnego.
