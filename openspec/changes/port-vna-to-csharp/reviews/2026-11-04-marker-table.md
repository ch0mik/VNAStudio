# Przegląd tabeli markerów

Dodano pod wykresem wiersze M1/M2 i kolumny częstotliwości, SWR, |Z|, RL, fazy, R oraz |X|. Dane każdego wiersza pochodzą z tej samej najbliższej próbki. Wartości formatują się według wybranego języka; brakująca, niepoprawna lub nieobliczalna wielkość wyświetla kreskę. Panel zachowuje minimum okna 1080 × 700 DIP.

Weryfikacja: `dotnet test Vna.slnx --configuration Release --no-restore -p:UsedAvaloniaProducts=` — 60/60 zaliczonych. Nowy test obejmuje wszystkie kolumny, kulturę polską i brakujące próbki. Zrzut Avalonia Headless przy 1080 × 700 sprawdzono wizualnie. Test dotyczy demonstracyjnych danych; odczyt sprzętowy pozostaje zależny od niepotwierdzonego protokołu.
