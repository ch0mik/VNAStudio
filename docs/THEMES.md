# Motywy aplikacji

Pierwsze uruchomienie wybiera **English** i **Dark**. Selektor **Motyw** przełącza wygląd od razu. Przy zwykłym zamknięciu aplikacja automatycznie zachowuje wybrany motyw, język, port i pozostałe obsługiwane preferencje w `settings.json` dostarczanym przez `IOptionsMonitor<AppSettings>`. **Zapisz ustawienia** pozwala zapisać je wcześniej. Plik jest odczytywany przy starcie; zapisane wartości mają pierwszeństwo przed domyślnymi.

| Nazwa | Pole Theme | Wygląd |
| --- | --- | --- |
| Jasny / Light | `light` | Jasny interfejs, kolory i tło wykresu z profilu sesji |
| Ciemny / Dark | `dark` | Ciemny interfejs i wykres, rozjaśnione zbyt ciemne kolory przebiegów |
| Night Vision | `night-vision` | Czarne i czerwone powierzchnie, czerwone etykiety, siatka, markery i przebiegi; różne wzory linii |

Brak ustawienia lub nieznana wartość wybierają `dark`; jawne `light` pozostaje obsługiwane. Brak języka wybiera `en-US`. Przykład:

```json
{
  "PortName": "/dev/ttyUSB0",
  "Language": "en-US",
  "Theme": "dark",
  "TraceColors": { "Swr": "#FF0000", "ReturnLoss": "#0000FF" }
}
```

Wartości RF, częstotliwości, widoczność przebiegów, markery oraz zoom są zachowywane przy zmianie motywu. Snapshot profilu w sesji przechowuje oryginalne kolory, a `TraceColors` jest osobnym ustawieniem aplikacji i nakłada się na profil. Ciemny motyw rozjaśnia wybrane zbyt ciemne kolory. Night Vision wymusza czerwoną paletę i różne wzory przebiegów, zachowując wybrane kolory dla pozostałych motywów. Natywne dialogi plikowe i dekoracje okna są obsługiwane przez system; ustawienie nie steruje jasnością monitora.

## Rozwój wyglądu

- `src/Vna.App/Theming/AppThemes.cs`: dostępne warianty i ich trwałe kody; Night Vision dziedziczy po ciemnym wariancie Fluent.
- `src/Vna.App/Themes/ThemeResources.axaml`: semantyczne zasoby `Vna.*` w słownikach Light, Dark i NightVision; XAML odwołuje się do nich przez `DynamicResource`.
- `src/Vna.App/Themes/NightControlResources.axaml`: czerwone zasoby kontrolek Fluent, również dla fokusu, zaznaczenia, popupów i stanów interakcji.
- `src/Vna.App/Plotting/PlotPalette.cs`: tło, siatka, opisy osi, kolory i wzory przebiegów, kolory markerów. Legenda i wykres korzystają z tej samej palety.
- Nazwy motywów i podpowiedzi są w zasobach `.resx` PL/EN.

## Podglądy README

Test `RenderThemePreviewsFromFullDemonstrationSweep` tworzy sesję z pełnego syntetycznego skanu demonstratora, przełącza trzy motywy i zapisuje PNG 1440×900 przez rzeczywisty renderer Skia w backendzie Avalonia Headless. Aby odświeżyć obrazy:

```powershell
dotnet test tests/Vna.Tests/Vna.Tests.csproj --configuration Release --filter FullyQualifiedName~RenderThemePreviewsFromFullDemonstrationSweep
```

Pliki powstają w `tests/Vna.Tests/bin/Release/net10.0/theme-previews/`. Skopiuj `light.png`, `dark.png`, `night-vision.png` do `docs/images/themes/`. Przy użyciu `--artifacts-path` katalog podglądów znajduje się obok wynikowej biblioteki `Vna.Tests.dll`. Testy pozostałych scenariuszy zapisują osobne obrazy `regression-*`.

Podglądy wymagają przeglądu po zmianie wyglądu. Testy sprawdzają też, że wyrenderowany obraz Night Vision nie zawiera dodatnich składowych G/B oraz że interakcje kontrolek nie przywracają standardowych jasnych kolorów.
