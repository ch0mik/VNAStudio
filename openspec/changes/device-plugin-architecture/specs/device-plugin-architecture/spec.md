# Capability: Architektura pluginów urządzeń

## ADDED Requirements

### Requirement: Izolacja zachowania urządzeń
The application SHALL depend only on versioned, model-neutral device contracts for enumeration, capabilities, profiles, measurement acquisition, and lifecycle; device-specific protocol details SHALL remain inside the device plugin.

#### Scenario: Dodanie kolejnego modelu
- **WHEN** nowy zatwierdzony plugin zostanie zarejestrowany w DI
- **THEN** aplikacja udostępnia jego modele i funkcje na podstawie descriptorów bez odwołań UI do pól protokołu konkretnego urządzenia

### Requirement: Rozdzielenie profilu i pluginu
The application SHALL treat a device profile as versioned declarative model data and a device plugin as the implementation of communication and model-specific behavior.

#### Scenario: Wyświetlenie modelu
- **WHEN** użytkownik wybiera profil urządzenia VNA 1–65 MHz
- **THEN** UI pokazuje nazwę sprzętu, a nie nazwę pliku źródłowego lub flagę protokołu

### Requirement: Bezpieczna rejestracja
The application SHALL register approved plugins explicitly at composition time, verify unique identifiers and supported contract versions, and SHALL NOT load arbitrary plugin assemblies from writable directories or the network.

#### Scenario: Niezgodna wersja API
- **WHEN** plugin deklaruje nieobsługiwaną wersję kontraktu albo zduplikowany identyfikator
- **THEN** rejestracja odrzuca ten plugin z widocznym, diagnostycznym błędem

### Requirement: Izolowany cykl życia urządzenia
Device plugin operations SHALL be asynchronous, accept cancellation for scans and connections, and release the active transport when the source is stopped, changed, or disposed.

#### Scenario: Zatrzymanie skanu
- **WHEN** użytkownik zatrzymuje pomiar
- **THEN** anulowanie dociera do aktywnego pluginu i urządzenie jest gotowe do bezpiecznego ponownego skanu

### Requirement: Trwałe pochodzenie pomiaru
New measurement sessions SHALL preserve plugin ID, profile ID, and the profile snapshot needed to interpret the measurement; playback SHALL remain available when the originating plugin is not installed.

#### Scenario: Odtwarzanie historycznej sesji
- **WHEN** otwierana jest sesja bez obecnego pluginu źródłowego
- **THEN** aplikacja odtwarza zapisane próbki i migawkę bez podstawiania aktualnego profilu urządzenia
