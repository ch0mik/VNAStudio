# Proposal

## Why

VNA Studio ma obsługiwać więcej niż jeden model analizatora. Obecnie zachowanie urządzenia VNA 1–65 MHz, współczynniki profilu i szczegóły protokołu są związane z projektami aplikacji i wspólnego modelu. To utrudnia dodawanie kolejnych urządzeń i grozi przenikaniem ustawień konkretnego sprzętu do UI.

## What Changes

- Wprowadzić neutralny kontrakt pluginu urządzenia: identyfikacja, opis możliwości, profile oraz fabryka instancji urządzenia.
- Wydzielić VNA 1–65 MHz, jego profil i komunikację szeregową do dedykowanego projektu pluginu.
- Pozostawić Vna Studio, wspólne wykresy, sesje, ustawienia aplikacji i kontrakty pomiarowe niezależne od modelu urządzenia.
- Oddzielić profil jako dane/ustawienia wariantu pracy od pluginu zawierającego implementację protokołu i obliczeń sprzętowych.
- W pierwszej wersji użyć kompozycji pluginów znanych w czasie kompilacji; nie ładować dowolnych bibliotek z dysku.

To jest plan architektury; zmiana nie implementuje jeszcze nowego systemu pluginów ani nie zmienia działającego sterownika.

## Capabilities

### New Capabilities

- `device-plugin-architecture`: kontrakt, rejestracja, profile, izolacja zachowania urządzeń i kompatybilność pluginów.

### Modified Capabilities

Brak bazowych specyfikacji. Plan uszczegóławia architekturę projektu opisanego w `port-vna-to-csharp`.

## Impact

Plan obejmie `Vna.Core`, warstwę urządzeń, konfigurację DI w `Vna.App`, format sesji i ustawień, pakowanie release, testy oraz README i dokumentację protokołu. Szczegóły COM, ramki, kalibracja i zakresy VNA 1–65 MHz trafią do pluginu `Vna.Device.Vna65MHz`; nazwa `analyz.ini` nie będzie nazwą profilu widoczną dla użytkownika.
