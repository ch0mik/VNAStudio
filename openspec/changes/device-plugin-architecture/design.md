# Design

## Context

Aktualny kod ma wspólny projekt `Vna.Devices` i profil `DeviceProfile.Vna65MHz` w `Vna.Core`. Profil zawiera zarówno ogólne metadane, jak i pola legacy: wariant ramki, zegar DDS, pauzy, współczynniki ADC, kalibrację i pamięci generatora. UI i przepływ skanu korzystają z konkretnych rejestracji VNA. Port jest już konfigurowany jako ścieżka systemowa, a sesje zachowują migawkę profilu.

## Goals / Non-Goals

**Cele:** jawna granica aplikacja–urządzenie, możliwość dodawania innych modeli bez zmiany wykresów, izolacja protokołu i profili, zachowanie dotychczasowych sesji i ustawień, testowalność pluginu bez sprzętu.

**Poza zakresem pierwszej iteracji:** pobieranie pluginów z Internetu, ładowanie dowolnych DLL, sandboxing nieufnego kodu, marketplace, migracja wszystkich kalibracji do wspólnego formatu ani usunięcie dotychczasowych funkcji UI.

## Decisions

### 1. Granice projektów

- `Vna.Core` definiuje wyłącznie kontrakty stabilne dla UI i obliczeń wspólnych: metadane urządzenia, możliwości, opis profilu oraz interfejs źródła pomiarów.
- Nowy `Vna.Device.Vna65MHz` (Class Library) zawiera implementację urządzenia, algorytm konfiguracji DDS, serial framing, przeliczenia specyficzne dla detektora, opcje IOptions, parser/adapter profilu i zasoby profilu. Projekt może zależeć od Core i transportu, lecz Core nie zależy od pluginu.
- `Vna.App` zawiera UI, wspólny przepływ pomiaru, wybór źródła i rejestrację znanych pluginów. Nie odwołuje się do pól protokołu specyficznych dla VNA.
- `Vna.Devices.Transport` pozostaje neutralną implementacją portu szeregowego (systemowa ścieżka, timeout, asynchroniczny odczyt i anulowanie); nie interpretuje komend konkretnego urządzenia.
- `Vna.Tests` testuje kontrakt, zachowanie każdego pluginu i testową implementację pluginu niezależnie od sprzętu.

### 2. Kontrakt pluginu

Projekt Core udostępnia kontrakt w wersjonowanej przestrzeni nazw, np. `IVnaDevicePlugin`, z `DevicePluginDescriptor` (`Id`, nazwa, wersja kontraktu, możliwości), kolekcją opisów profili i rejestracją usług do DI. Fabryka urządzenia przyjmuje wybrany `DeviceProfileDescriptor` oraz ogólne połączenie/ścieżkę portu i tworzy instancję `IVnaDevice`/źródło pomiaru. Operacje I/O są asynchroniczne, przyjmują `CancellationToken`, poprawnie zwalniają zasoby i nie blokują wątku UI.

Nie przekazywać do Core klas opcji VNA65, surowych bajtów ramek, rejestrów DDS ani pól legacy INI. Możliwości (`Sweep`, `Impedance`, `Calibration`, `GeneratorOutput`) deklaruje plugin; UI może ukrywać niedostępne funkcje na podstawie stabilnych możliwości, nie identyfikatora konkretnego modelu.

### 3. Profil urządzenia i konfiguracja

Profil to wersjonowany zbiór deklaratywnych parametrów danego wariantu urządzenia: granice i domyślne parametry skanu, impedancja odniesienia, etykieta, lista dostępnych funkcji oraz dane widoku. Profil nie zawiera wykonywalnego kodu. Plugin odpowiada za jego interpretację i walidację. Wbudowany profil będzie nazywał się „VNA 1–65 MHz”; `analyz.ini` może służyć jako źródło migracji podczas przeniesienia danych, ale nie pozostaje jego nazwą w UI.

Ogólne ustawienia użytkownika (port, język, motyw, preferencje wykresu) pozostają w ustawieniach aplikacji. Ustawienia protokołu, tolerancje i parametry sprzętowe należą do przestrzeni nazw/opcji pluginu. Ścieżka COM/tty przekazywana jest bez normalizacji pluginowi, zgodnie z systemem.

### 4. Odkrywanie i zaufanie

Pierwsze wydanie rejestruje pluginy statycznie przez `IServiceCollection` i jawne wywołania rejestracji z referencjonowanych projektów. Każdy plugin zgłasza major/minor wersji kontraktu. Host akceptuje zgodny major oraz minor pluginu nie większy od obsługiwanego minor; niezgodny plugin jest pomijany z widocznym błędem. Przy niekompatybilnej zmianie host zwiększa major i może jawnie wspierać dwa majory w okresie migracji. Plugin z nowszym, nieznanym majorem/minorem nie jest ładowany. Nie używamy automatycznego skanowania katalogów, `Assembly.LoadFrom` ani pluginów pobranych z sieci. To upraszcza podpisywanie/pakowanie i nie uruchamia nieufnego kodu.

### 5. DI, cykl życia i anulowanie

Rejestr pluginu dodaje jego fabryki i opcje (`IOptions<T>`/walidację). Każda aktywna instancja sprzętu jest scoped do połączenia, serializuje równoległy dostęp do portu, przyjmuje anulowanie przy skanie i zatrzymaniu, a przy zamknięciu/zmianie źródła jest asynchronicznie zwalniana. Przepływ UI zna tylko interfejs wspólny. Błędy pluginu są przekładane na wspólny model wyniku/błędu; nie wolno w razie błędu urządzenia po cichu przełączyć na demo.

### 6. Sesje, ustawienia i zgodność

Sesja zapisuje identyfikator pluginu, identyfikator profilu i migawkę niezbędną do odczytania historycznego pomiaru. `SessionPlaybackDevice` buduje zakres i opis wyłącznie z `MeasurementSession.Sweep` oraz `ProfileSnapshot`; nie tworzy źródła przez resolver aktywnego pluginu i nie wymaga, aby profile ID istniało w katalogu pluginów. Odtwarzanie działa bez zainstalowanego pluginu pomiarowego. Brakujące/stare identyfikatory migrują do jawnego `legacy-vna65` i zachowują dane; migawka nie jest nadpisywana aktualnym profilem. Ustawienia aplikacji przechowują wybór pluginu/profilu niezależnie od portu. W starszych JSON i sesjach zachować bieżące mapowanie dotychczasowego `iw3hev-65mhz`.

### 7. Pakowanie i dokumentacja

Każdy zatwierdzony plugin jest normalną zależnością projektu i trafia wraz z zasobami profilu do paczek RID w GitHub Actions. Test release sprawdza obecność assembly i profilu. Dokumentacja protokołu opisuje VNA 1–65 MHz jako implementację jednego pluginu; osobna strona opisuje publiczny kontrakt dla autorów kolejnych pluginów.

## Risks / Trade-offs

Stabilność kontraktu ogranicza tempo zmian w Core; wersjonowanie API i test kontraktowy redukują ryzyko. Statyczna rejestracja wymaga zmiany/publikacji aplikacji przy dodaniu pluginu, ale jest prostsza i bezpieczniejsza od ładowania obcego kodu. Migracja profilu legacy wymaga ostrożnego zachowania sesji, kolorów i ustawień użytkownika.

## Migration Plan

Najpierw zinwentaryzować zależności i zamrozić testami dotychczasowe zachowanie, następnie wprowadzić neutralne kontrakty i atrapę pluginu, wydzielić transportową logikę VNA 1–65 MHz, migrować rejestrację i ustawienia, potem format sesji/pakowanie/dokumentację. Każdy etap kończy się testami i review niezależnego subagenta. Obecny działający sterownik należy zachować, aż test integracyjny udowodni identyczne komendy, bajty, konwersje, anulowanie i zwalnianie COM.

## Open Questions

- Czy `IVnaDevice` ma reprezentować urządzenie sprzętowe, czy dostarczać `IMeasurementSource`/fabrykę skanu? Ustalić na etapie kontraktu po sprawdzeniu aktualnych interfejsów źródła, bez przenoszenia logiki protokołu do Core.
- Czy profile będą JSON-em czy typowanymi rekordami dostarczanymi przez plugin? Preferowany jest model typowany w runtime i deklaratywny, wersjonowany JSON jako zasób profilu, jeśli nie komplikuje walidacji.
- Które możliwości występują niezależnie od `MeasurementMode` i jak mapować dotychczasowy CSV/session bez fałszywej identyfikacji sprzętu?
