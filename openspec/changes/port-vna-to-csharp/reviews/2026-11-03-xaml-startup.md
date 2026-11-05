# Poprawka ładowania XAML — 2026-11-03

Zgłoszenie: `dotnet run --project src\VNA.App` kończyło się CS0103 dla `InitializeComponent` i `Plot`. Kolejna próba użytkownika pokazywała AVLN3001 o braku konstruktora okna dla dynamicznego loadera XAML.

## Zmiany i przegląd

- Wyłączono generator nazw Avalonia w projekcie aplikacji. `MainWindow` jawnie ładuje skompilowany XAML przez `AvaloniaXamlLoader.Load(this)` i wyszukuje `VnaPlotControl` przez `FindControl("Plot")`. Kod nie wymaga wygenerowanych członków `InitializeComponent` ani `Plot`.
- Dodano publiczny konstruktor bezargumentowy, używany do ładowania widoku, oraz zachowano konstruktor przyjmujący model widoku. Usuwa to AVLN3001; konstruktor aplikacji nadal przypisuje model przez `DataContext`.
- Obsługa markerów pobiera bieżący model z `DataContext`; działa również po przypisaniu modelu do okna utworzonego konstruktorem bezargumentowym.
- Dodano regresyjny test utworzenia okna bez modelu i obecności nazwanej kontrolki wykresu. Pozostałe testy interakcji używają konstruktora z modelem widoku.

## Sprawdzenie

```powershell
dotnet test Vna.slnx --no-restore --nologo -p:UsedAvaloniaProducts= --logger 'console;verbosity=minimal'
dotnet run --project src\VNA.App --no-build --no-restore
```

Wynik: wszystkie projekty skompilowane; **30/30 testów zaliczonych**. Końcowa kompilacja nie zgłaszała CS0103 ani AVLN3001. Parametr `UsedAvaloniaProducts=` ma takie samo znaczenie jak w poprzednim przeglądzie: pomija zadanie statystyk kompilacji zapisujące poza katalogiem roboczym.

Aplikację uruchomiono na pulpicie Windows. Potwierdzono widoczne okno procesu `Vna.App` z tytułem „VNA Studio · 65 MHz”; proces odpowiadał. Okno pozostawiono uruchomione.
