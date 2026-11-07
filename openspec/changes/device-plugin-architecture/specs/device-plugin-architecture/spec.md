# Capability: Architektura pluginów urządzeń

## ADDED Requirements

### Requirement: Izolacja zachowania urządzeń
The application SHALL depend only on versioned, model-neutral device contracts for enumeration, capabilities, profiles, measurement acquisition, and lifecycle; device-specific protocol details SHALL remain inside the device plugin.

#### Scenario: Dodanie kolejnego modelu
- **WHEN** nowy zatwierdzony plugin zostanie zarejestrowany w DI
- **THEN** aplikacja udostępnia jego modele i funkcje na podstawie descriptorów bez odwołań UI do pól protokołu konkretnego urządzenia

#### Scenario: Podgląd zarejestrowanych profili
- **WHEN** użytkownik otworzy w Pomocy katalog profili urządzeń
- **THEN** aplikacja wyświetla pluginy, profile, zakresy i deklarowane możliwości z descriptorów, wraz ze statusem i poziomem dowodów; ten podgląd nie przełącza ani nie łączy źródła

### Requirement: Rozdzielenie profilu i pluginu
The application SHALL treat a device profile as versioned declarative model data and a device plugin as the implementation of communication and model-specific behavior.

#### Scenario: Wyświetlenie modelu
- **WHEN** użytkownik wybiera profil urządzenia VNA 1–65 MHz
- **THEN** UI pokazuje nazwę sprzętu, a nie nazwę pliku źródłowego lub flagę protokołu

### Requirement: Trwały, jawny wybór urządzenia
The host SHALL keep the selected plugin/profile identity separate from general application preferences, validate both identifiers against the registered catalog, and SHALL NOT silently replace an invalid selection with Demo or another profile.

#### Scenario: Jednoznaczny wybór domyślny
- **WHEN** both configured IDs are empty and exactly one plugin with one profile is registered
- **THEN** the host selects that profile and persists its stable plugin and profile IDs with user settings

#### Scenario: Niejednoznaczny lub nieprawidłowy wybór
- **WHEN** multiple choices exist, only one ID is configured, or either configured ID is unknown
- **THEN** startup reports the invalid or missing selection and does not silently connect to a different source

#### Scenario: Skan sprzętowy zablokowany możliwościami profilu
- **WHEN** the selected profile marks a requested hardware scan as blocked, unknown, or omits its capability
- **THEN** the scan command is disabled and no device command is sent, with a localized explanation

### Requirement: Bezpieczna rejestracja
The application SHALL register approved plugins explicitly at composition time, verify unique identifiers and supported contract versions, and SHALL NOT load arbitrary plugin assemblies from writable directories or the network.

#### Scenario: Niezgodna wersja API
- **WHEN** plugin deklaruje nieobsługiwaną wersję kontraktu albo zduplikowany identyfikator
- **THEN** rejestracja odrzuca ten plugin, a host przerywa start z jasnym błędem diagnostycznym zawierającym jego ID i przyczynę

### Requirement: Neutralny kontrakt pluginu
The host SHALL communicate with device plugins through a versioned model-neutral profile descriptor, plugin descriptor, source interface, and transport-independent source factory. The shared profile descriptor SHALL NOT expose a device protocol, serial framing, or legacy configuration field.

#### Scenario: Budowa źródła pomiarowego
- **WHEN** host wybiera zadeklarowany profil pluginu
- **THEN** wywołuje fabrykę pluginu z profilem, a później przekazuje neutralne żądanie połączenia do utworzonego źródła; szczegóły transportu oraz protokołu pozostają wewnątrz pluginu

#### Scenario: Niezgodny kontrakt
- **WHEN** major wersji pluginu różni się od major hosta albo minor pluginu jest nowszy niż minor hosta
- **THEN** wersja nie przechodzi kontroli zgodności

### Requirement: Izolowany cykl życia urządzenia
Device plugin operations SHALL be asynchronous, accept cancellation for scans and connections, and release the active transport when the source is stopped, changed, or disposed.

#### Scenario: Zatrzymanie skanu
- **WHEN** użytkownik zatrzymuje pomiar
- **THEN** anulowanie dociera do aktywnego pluginu i urządzenie jest gotowe do bezpiecznego ponownego skanu

#### Scenario: Odtwarzanie bez danych wejściowych
- **WHEN** playback jest wybrane, ale nie ma otwartej sesji ani kompletnego skanu w pamięci
- **THEN** polecenia połączenia i skanu pozostają wyłączone do czasu udostępnienia danych

### Requirement: Trwałe pochodzenie pomiaru
New measurement sessions SHALL preserve plugin ID, profile ID, and the profile snapshot needed to interpret the measurement; playback SHALL remain available when the originating plugin is not installed.

#### Scenario: Odtwarzanie historycznej sesji
- **WHEN** otwierana jest sesja z zapisaną neutralną migawką profilu, nawet gdy jej źródłowy plugin nie jest rozwiązywany albo aktywny profil jest inny
- **THEN** aplikacja wyświetla i odtwarza zapisane próbki oraz descriptor bez podstawiania profilu aktywnego urządzenia; odtwarzanie używa sweepu z pliku i nie interpoluje

#### Scenario: Migracja sesji sprzętowej bez plugin ID
- **WHEN** czytnik formatu v6 otwiera sesję v1–v4 o profilu `iw3hev-65mhz`, źródle `hardware` i bez `PluginId`
- **THEN** importer przypisuje wyłącznie historyczną tożsamość `legacy-vna65`, zachowując próbki, zakres i migawkę profilu

#### Scenario: Brak tożsamości w nowej sesji
- **WHEN** sesja v5 pochodzi ze źródła, któremu nie można przypisać zweryfikowanego pluginu
- **THEN** `PluginId` pozostaje null i importer nie wywnioskuje go z samej etykiety `hardware`

### Requirement: Dowodowo ograniczone możliwości sprzętu
A device plugin SHALL expose hardware controls only when the command behavior and required physical path are validated for that profile; unknown device, firmware, connector mapping, or output limits SHALL NOT be treated as verified capability.

#### Scenario: Nieznana komenda FILTER lub Generator
- **WHEN** profile nie ma potwierdzonej komendy, bezpiecznego fixture i niezależnego sprawdzenia trybu FILTER/Generator
- **THEN** aplikacja nie wysyła polecenia do urządzenia i pokazuje status niedostępności/niezweryfikowania

### Requirement: Rozdzielone znaczenie kanałów pomiarowych
The application SHALL distinguish reflection data, scalar transmission magnitude, and complex network data; it SHALL NOT infer a complex parameter or impedance from scalar magnitude alone.

#### Scenario: Odczyt skalarnego FILTER
- **WHEN** plugin dostarcza tylko amplitudę toru transmisyjnego
- **THEN** aplikacja nie przedstawia jej jako impedancji, zespolonego S21 ani pliku S2P

### Requirement: Jawny model zespolonych parametrów sieci
The application SHALL represent complex network parameters separately from scalar or reflection-derived samples and SHALL require a positive finite reference impedance for every network dataset.

#### Scenario: Zapis danych Touchstone
- **WHEN** dane zespolone S1P/S2P są importowane do modelu sieci
- **THEN** model zapisuje liczbę portów, zespolone wartości parametrów, częstotliwości w Hz i jawne wspólne Zref, walidując kształt próbki dla 1 lub 2 portów

#### Scenario: Brak zespolonej transmisji
- **WHEN** wejściem jest wyłącznie skalarna amplituda transmisji
- **THEN** import nie tworzy S21 ani innych zespolonych parametrów

### Requirement: Niezmienna siatka częstotliwości sesji
A saved session SHALL retain its recorded sample frequencies and frequency-grid convention/status; the application SHALL NOT silently relabel historical samples using a newer profile or inferred device behavior.

#### Scenario: Konwencja siatki nieznana
- **WHEN** częstotliwość pierwszej próbki lub firmware nie są zweryfikowane
- **THEN** aplikacja zachowuje częstotliwości zapisane w sesji i oznacza konwencję jako nieznaną, nie wywnioskując jej z profilu

### Requirement: Jawne pochodzenie korekt sesji
New sessions SHALL record a versioned snapshot that distinguishes display-only offset, DDS frequency correction, reflection calibration, FILTER normalization, and hardware trim, including source and evidence state. Unknown uncertainty SHALL remain unknown, and a profile configuration SHALL NOT be presented as reference-validated calibration.

#### Scenario: Zapis pomiaru z niezweryfikowanymi współczynnikami profilu
- **WHEN** sesja sprzętowa zapisuje próbki odbiciowe przeliczone z ustawień profilu bez niezależnego pomiaru wzorcowego
- **THEN** migawka zapisuje zastosowane współczynniki jako skonfigurowane, ale niezweryfikowane, nie przypisuje nieznanej niepewności i zachowuje osobne etapy offsetu wyświetlania, DDS, FILTER i trimu

#### Scenario: Odtwarzanie starej sesji bez migawki korekt
- **WHEN** odczytywana jest starsza sesja bez wersjonowanej migawki korekt
- **THEN** migawka pozostaje nieznana, a zapisane punkty i ich częstotliwości nie są przeliczane ani uzupełniane z bieżącego profilu

#### Scenario: Plugin bez jawnego pochodzenia przetwarzania
- **WHEN** host zapisuje skan sprzętowy źródła, które nie udostępnia użytych parametrów konwersji
- **THEN** migawka oznacza przetwarzanie jako nieznane i nie zapisuje placeholderów zgodności jako zastosowanych współczynników

### Requirement: Nazwane migawki zachowują pomiar i pochodzenie
The application SHALL save each named comparison snapshot as an immutable copy of a validated measurement session, including source/profile/provenance metadata and native sample frequencies.

#### Scenario: Porównanie siatek częstotliwości o różnej rozdzielczości
- **WHEN** the user overlays snapshots with different point counts or frequency grids
- **THEN** each trace is drawn from its own stored samples and no synthetic frequency samples are added

### Requirement: Nakładki porównawcze zachowują znaczenie danych
Comparison rendering SHALL use each snapshot's own known `Zref` and SHALL NOT interpolate samples, reinterpret an unconfirmed reactance sign, or modify the active measurement. A present neutral profile descriptor is authoritative when its `Zref` is unknown.

#### Scenario: Wybór wielkości na wykresie częstotliwościowym
- **WHEN** the user selects a frequency-domain metric for snapshot comparison
- **THEN** each visible snapshot renders that metric from its own samples and saved display offset; the active session remains unchanged

#### Scenario: Migawka bez impedancji odniesienia
- **WHEN** a snapshot does not contain a verified `Zref`
- **THEN** Smith rendering does not borrow the active device's `Zref` and omits quantities that require an unknown reference impedance

#### Scenario: Neutralny descriptor jawnie nie zna Zref
- **WHEN** a session has both a neutral descriptor with no `Zref` and an older profile snapshot with a numeric `Zref`
- **THEN** comparison rendering treats `Zref` as unknown and does not use the legacy value

### Requirement: Offset porównania jest tylko prezentacyjny
A return-loss display offset for a comparison snapshot SHALL be stored separately from its session and calibration provenance and SHALL NOT change source measurement values.

#### Scenario: Offset tylko prezentacyjny
- **WHEN** a snapshot records a return-loss chart offset
- **THEN** rendering applies it only to a derived display view and keeps the nested session samples and calibration provenance unchanged

### Requirement: Jawne dowody i role złączy profilu
Device profiles SHALL describe analysis modes, device commands, and physical connectors with separate availability/evidence metadata. A connector label SHALL NOT imply its electrical role unless that role has independent evidence.

#### Scenario: Niezweryfikowane FILTER i Generator
- **WHEN** VNA Studio ładuje znany profil VNA 1–65 MHz bez potwierdzonego toru FILTER lub komendy generatora
- **THEN** profil oznacza te funkcje jako zablokowane i nie przedstawia pamięci częstotliwości jako dowodu działania wyjścia

#### Scenario: Nazwy złączy na obudowie
- **WHEN** profil zapisuje oznaczenia `VNA` i `WOB` widoczne na urządzeniu
- **THEN** zachowuje etykiety i status ich obserwacji, a niepotwierdzone role elektryczne pozostają `Unknown`
