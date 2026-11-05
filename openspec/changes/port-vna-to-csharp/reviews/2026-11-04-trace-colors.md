# Przegląd wyboru kolorów przebiegów

Dodano wybór koloru dla każdego przebiegu z palety dostępnych kolorów oraz reset do koloru profilu. Nadpisania zapisują się w `settings.json` przez `AppSettings.TraceColors` i `IOptionsMonitor`; profil oraz kolory sesji pozostają bez zmian. Dark nadal podnosi kontrast, a Night Vision wyłącza wybór i rysuje tylko czerwone przebiegi, zachowując nadpisania na powrót do innych motywów.

Weryfikacja: `dotnet test Vna.slnx --configuration Release --no-restore -p:UsedAvaloniaProducts=` — 63/63 zaliczone. Test sprawdza zapis/odczyt JSON, przełączanie trzech motywów, reset i zachowanie wybranego koloru. Opis EN/PL oraz instrukcja motywów zostały uaktualnione. Testowane są dane demonstracyjne i ustawienia lokalne.
