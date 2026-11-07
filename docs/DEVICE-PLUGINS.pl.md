# Kontrakt pluginów urządzeń

VNA Studio rejestruje pluginy urządzeń jawnie. Host komunikuje się z każdym pluginem przez neutralne kontrakty w `Vna.Core`; plugin odpowiada za komendy urządzenia, ramkowanie portu szeregowego, konfigurację transportu, dekodowanie próbek i przeliczenia właściwe dla modelu.

## Zgodność wersji API

Wersja API pluginu to para `(Major, Minor)`, niezależna od numeru wydania samego pluginu. Bieżąca wersja kontraktu hosta to **1.0**.

- Numer major musi być identyczny. Zmiana niezgodna wstecz wymaga zwiększenia major.
- Minor pluginu musi być nieujemny i nie może przekraczać minor obsługiwanego przez host. Zgodne rozszerzenia kontraktu zwiększają minor.
- Wersje ujemne, inny major lub minor pluginu nowszy niż host są odrzucane podczas rejestracji katalogu. Aplikacja zatrzymuje start z diagnostyką zamiast uruchamiać niezgodny plugin.

Politykę realizuje [`DevicePluginContract`](../src/Vna.Core/DevicePluginContracts.cs), a egzekwuje [`DevicePluginCatalog`](../src/Vna.Core/DevicePluginCatalog.cs).

## Odpowiedzialność pluginu i profilu

`IDevicePlugin` deklaruje stabilny identyfikator pluginu, nazwę wyświetlaną, wersję API i obsługiwane wpisy `DeviceProfileDescriptor`. Identyfikatory pluginów muszą być niepuste i unikalne w całym katalogu bez względu na wielkość liter. Identyfikatory profili muszą być niepuste i unikalne w obrębie danego pluginu. Descriptor profilu podaje stabilny identyfikator i wersję schematu, granice częstotliwości, domyślną liczbę punktów, opcjonalną impedancję odniesienia, złącza, możliwości i wskazówki prezentacji. Wartości protokołu oraz układ ramek nie należą do descriptora.

`CreateSource(profile)` przyjmuje wyłącznie profil zadeklarowany przez plugin i zwraca `IMeasurementSource`. Źródło implementuje protokół. Łączenie, rozłączanie i skanowanie są asynchroniczne oraz przyjmują `CancellationToken`; host przekazuje nazwę portu systemowego bez zmian w `DeviceConnectionRequest.PortName`. Brak impedancji w neutralnym descriptorze pozostaje nieznany w prezentacji i zapisanej migawce profilu; przejściowy profil zgodności UI nie jest źródłem prawdy. Jeśli źródło nie potrafi zgłosić użytego przetwarzania odbicia, pozostawia `AppliedReflectionCalibration` jako null, aby pochodzenie zapisu pozostało nieznane.

Możliwości opisują tryby analizy, komendy urządzenia lub narzędzia hosta. `Availability` należy oddzielać od `Evidence`: na przykład `Partial` nie potwierdza dokładności fizycznej, a `Blocked` nie może wysyłać komendy sprzętowej. Etykietę złącza i jego rolę elektryczną zapisuje się osobno, aby zaobserwowana etykieta nie oznaczała niepotwierdzonej ścieżki sygnałowej.

## Rejestracja i wybór

Pluginy są rejestrowane jawnie w composition root aplikacji. Host nie skanuje zapisywalnych katalogów, nie ładuje dowolnych assembly ani nie pobiera pluginów z sieci. Dodanie pluginu wymaga zatem przejrzanego odwołania do projektu i jawnej rejestracji DI, a jego zasoby muszą trafić do paczek release.

Aktywne identyfikatory pluginu i profilu są zapisane w sekcji `DeviceSelection` pliku `settings.json`; zobacz [wybór urządzenia](DEVICE-SELECTION.pl.md). Katalog w Pomocy wyświetla zarejestrowane descriptory, ale nie przełącza źródła podczas działania programu. Ścieżki COM/tty i wybrane źródło pomiaru są osobnymi ustawieniami.

Implementacja VNA 1–65 MHz znajduje się w [`Vna.Device.Vna65MHz`](../src/Vna.Device.Vna65MHz/). Wspólny bajtowy transport szeregowy znajduje się w `Vna.Devices.Transport`; komendy urządzenia i format ramek pozostają w pluginie. Przed migracją kolejnych pól starego `DeviceProfile` zobacz [mapę zależności projektów](DEVICE-PROFILE-DEPENDENCIES.md).
