# Przegląd lokalizacji PL/EN — 2026-11-03

## Implementacja

- `Vna.Localization` zawiera 103 klucze w `Resources/Strings.resx` (angielski, zasoby neutralne `en`) i `Resources/Strings.pl.resx` (polski). Standardowy `ResourceManager` odczytuje polską bibliotekę zasobów satelitarnych.
- `LocalizationService` udostępnia kultury `pl-PL` i `en-US` oraz powiadomienia dla indeksatorów wiązań Avalonia. Język można zmienić selektorem w panelu ustawień.
- `LocalizedMessage` przechowuje klucz i argumenty komunikatu. Statusy i własne błędy aplikacji mogą zostać ponownie przetłumaczone po zmianie języka. `LocalizedErrors` zachowuje standardowe typy wyjątków oraz argumenty zasobów.
- Lokalizacją objęto etykiety i przyciski, legendę, statusy, własne błędy Core/IO/Devices, tytuły i filtry dialogów plików oraz format liczb w polach, markerach i na osiach wykresu.
- `AppSettings.Language` jest obsługiwany przez istniejący `IOptionsMonitor<AppSettings>`. „Zapisz ustawienia” zapisuje port i język atomowo i asynchronicznie z CancellationToken. Starsze ustawienia bez języka przyjmują `pl-PL`; inne kultury są normalizowane do obsługiwanych języków, a nieobsługiwany język korzysta z angielskiego.
- Nowe sesje demonstracyjne zapisują stały identyfikator `demo`. Odczyt starszych etykiet demonstratora jest nadal obsługiwany. Pliki JSON, identyfikatory profilu, próbki i ścieżki portów są niezależne od języka.

## Wyniki przeglądu

1. Indeksator zasobów początkowo wysyłał wyłącznie `Item[]`, co nie odświeżało napisów w Avalonia. Dodano wymagane powiadomienie `Item`; test przełącza rzeczywiste powiązanie przycisku w oknie.
2. Formatowanie oparto na kulturze usługi lokalizacji. Procesowe `CurrentCulture` i `CurrentUICulture` pozostają bez zmian; format sesji JSON nie zależy od ustawień UI.
3. Pola częstotliwości są przeliczane między separatorami bez zmiany ich wartości. Nieprawidłowy, niedokończony wpis pozostaje dostępny do poprawienia; walidacja odrzuca też NaN i nieskończoności.
4. Przeładowanie opcji przekazuje zmianę na wątek UI przez Dispatcher. Subskrypcja lokalizacji jest zwalniana podczas asynchronicznego zamykania modelu widoku. Zmiana języka związanej z UI usługi wymaga wątku UI.
5. Własne błędy zachowują klucz zasobu, dzięki czemu wcześniejszy komunikat walidacji zmienia język wraz z interfejsem. Komunikaty otrzymane z systemu i bibliotek zewnętrznych są prezentowane w dostarczonej postaci. Natywne przyciski dialogów plikowych korzystają z języka systemu.

## Weryfikacja

Uruchomiona wcześniej aplikacja blokowała standardowe pliki DLL. Sprawdzenia wykonano w osobnym katalogu wynikowym:

```powershell
dotnet restore Vna.slnx -p:NuGetAudit=false -p:UseArtifactsOutput=true -p:ArtifactsPath=.artifacts/localization --nologo
dotnet test Vna.slnx --no-restore --artifacts-path .artifacts/localization --nologo -p:UsedAvaloniaProducts= --logger 'console;verbosity=minimal'
```

Wynik: **44/44 testy zaliczone**, w tym 14 nowych przypadków lokalizacji. Końcowa kompilacja wszystkich sześciu projektów przeszła bez ostrzeżeń i błędów.

Sprawdzono zgodność kluczy i argumentów obu zestawów zasobów, normalizację kodów kultury, działanie selektora PL→EN→PL w oknie o rozmiarze 1080×700, napisy przycisków i legendy, status już wczytanej sesji, komunikat walidacji po ponownej zmianie języka, format markerów, niezmienność próbek i portu, zachowanie źródła w sesji, odtworzenie ustawień przez IOptionsMonitor i przeładowanie konfiguracji. Zapis ustawień sprawdzono na pliku tymczasowym. Dotychczasowe testy geometrii, DPI, markerów i sesji nadal przechodzą.

## Rozszerzanie tłumaczeń

Nowy tekst otrzymuje ten sam klucz w obu plikach `.resx`. XAML używa indeksatora `LocalizationService.Instance`, kod używa `Text.Get`/`Text.Format`, a długotrwałe komunikaty używają `LocalizedMessage` lub metod `ReportLocalizedStatus`/`ReportLocalizedError`. Dane zapisane w sesjach nie powinny zawierać przetłumaczonych etykiet źródeł.
