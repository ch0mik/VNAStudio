# Wydania dla RID

Workflow `.github/workflows/release.yml` uruchamia się po **opublikowaniu GitHub Release**. Release musi wskazywać tag w dokładnym formacie `vX.Y.Z`, np. `v1.2.3`. Sam push taga oraz zapisanie szkicu release nie uruchamiają pakowania. Tag z sufiksem `-beta`, zerami wiodącymi lub niepoprawną wersją zatrzyma walidację przed kompilacją.

Workflow i katalog `scripts/` muszą znajdować się w commicie wskazywanym przez tag. Każdy job pobiera `github.sha` zdarzenia release, dzięki czemu pakowanie dotyczy tego samego commita.

Zapisz zmiany w repozytorium, utwórz i wypchnij tag (`git tag v1.2.3`, następnie `git push origin v1.2.3`), a w GitHub utwórz release wskazujący ten tag i wybierz **Publish release**. Wyniki pojawią się w sekcji Assets po zakończeniu workflow.

## Publikowane pliki

| RID | Runner | Załącznik dla v1.2.3 |
| --- | --- | --- |
| win-x86 | windows-2022 | VnaStudio-1.2.3-win-x86.zip |
| win-x64 | windows-2022 | VnaStudio-1.2.3-win-x64.zip |
| win-arm64 | windows-2022 | VnaStudio-1.2.3-win-arm64.zip |
| linux-x64 | ubuntu-24.04 | VnaStudio-1.2.3-linux-x64.tar.gz |
| linux-arm | ubuntu-24.04 | VnaStudio-1.2.3-linux-arm.tar.gz |
| linux-arm64 | ubuntu-24.04 | VnaStudio-1.2.3-linux-arm64.tar.gz |
| linux-musl-x64 | ubuntu-24.04 | VnaStudio-1.2.3-linux-musl-x64.tar.gz |
| linux-musl-arm | ubuntu-24.04 | VnaStudio-1.2.3-linux-musl-arm.tar.gz |
| linux-musl-arm64 | ubuntu-24.04 | VnaStudio-1.2.3-linux-musl-arm64.tar.gz |
| osx-x64 | macos-15-intel | VnaStudio-1.2.3-osx-x64.tar.gz |
| osx-arm64 | macos-15 | VnaStudio-1.2.3-osx-arm64.tar.gz |

Dwunasty załącznik, `SHA256SUMS.txt`, zawiera sumy SHA-256 wszystkich jedenastu archiwów. Po pobraniu rozpakuj cały katalog aplikacji. Windows uruchamia `Vna.App.exe`, a Linux/macOS `./Vna.App`. Paczki Unix zachowują prawo wykonywania. Dla macOS jest to przenośny katalog programu uruchamiany z terminala, bez instalatora i pakietu `.app`.

Macierz obejmuje warianty desktopowe mające runtime .NET 10 oraz natywne biblioteki używanych pakietów SkiaSharp, HarfBuzzSharp i System.IO.Ports. Sam wpis w grafie RID nie wystarcza do dodania platformy. `win-x86` to aplikacja 32-bitowa dla systemów obsługiwanych przez .NET 10; nie oznacza zgodności z dawnymi Windows XP/7. Warianty `linux-musl-*` są przeznaczone dla systemów z musl, np. Alpine, i wymagają systemowych zależności okienkowych oraz fontconfig. Dostępność plików natywnych nie zastępuje próby uruchomienia na docelowej dystrybucji.

Każda paczka zawiera własny runtime .NET (`--self-contained true`), biblioteki dla wybranego RID, profil `Assets/analyser306b-65mhz.ini`, polskie zasoby `.resx` w bibliotece satelitarnej oraz angielskie zasoby neutralne. Zależności okienkowe systemu operacyjnego nadal muszą być dostępne. Włączono apphost; wyłączono trimming i single-file. Ustawienia użytkownika nie są dołączane do paczek.

Tag `v1.2.3` ustawia `Version=1.2.3`, `AssemblyVersion=1.2.3.0` i `FileVersion=1.2.3.0`. `release-info.json` zawiera tag, wersję, RID oraz informację o runtime. Komponenty wersji muszą mieścić się w zakresie 0–65534 wymaganym przez wybrane wersjonowanie assembly.

## Kolejność działania

1. Walidacja taga.
2. Jedenaście jobów macierzy uruchamia sprawdzenia skryptów i testy aplikacji na swoim runnerze.
3. `dotnet publish` przygotowuje wariant RID. Walidator sprawdza runtime, RID w `.deps.json`, wersję assembly, profil, zasoby językowe oraz wymagane biblioteki natywne; dla Windows sprawdza także architekturę PE apphost, coreclr, SkiaSharp i HarfBuzzSharp. Następnie tworzy archiwum i jego sumę kontrolną.
4. Osobny job czeka na powodzenie całej macierzy, pobiera paczki i weryfikuje kompletność, nazwy oraz hashe.
5. `gh release upload` dodaje jedenaście archiwów i wspólny manifest do istniejącego release. Ponowienie workflow zastępuje wygenerowane załączniki o tych samych nazwach (`--clobber`).

Przed uploadem wszystkie buildy muszą się powieść. Samo przesłanie plików przez GitHub API nie jest transakcją: po błędzie sieci można ponowić workflow, aby uzupełnić załączniki. Artefakty pośrednie Actions są przechowywane przez 7 dni; załączniki release mają cykl życia release.

Uprawnienia domyślne to `contents: read`; jedynie job dodający pliki ma `contents: write`. Używany jest wbudowany `GITHUB_TOKEN`, bez osobistego PAT. Checkout nie zapisuje poświadczeń Git.

## Sprawdzenie lokalne

Wymagane: SDK .NET 10, PowerShell 7 oraz `tar` dla paczek Unix. Paczki Unix twórz na Linux/macOS, aby zachować uprawnienia. Przykład Windows:

```powershell
./tests/release/Release.Tests.ps1
./scripts/Publish-Release.ps1 -Tag v1.2.3 -Rid win-x64
```

Wyniki powstają w `.artifacts/release/packages/`. Istniejący katalog publikacji lub archiwum powoduje błąd, aby zapobiec zapakowaniu starych plików. Do niezależnej próby użyj `-ArtifactsDirectory` z inną ścieżką. W CI każdy job otrzymuje świeży katalog roboczy.

Przełącznik `-PublishOnly` wykonuje publikację i walidację katalogu, bez archiwum ani sumy kontrolnej. Pozwala sprawdzać publikacje linuksowe na Windows; nie zastępuje pakowania Unix na odpowiednim runnerze.

Warianty Windows ARM64, Linux ARM/ARM64 i musl są publikowane przez SDK na runnerze x64; testy aplikacji wykonują się na architekturze runnera. Zgodność uruchomieniowa na platformach docelowych i pomiary rzeczywistego VNA wymagają osobnego sprawdzenia. Lokalnie 2026-11-05 potwierdzono publikację Windows x86 i uruchomiono cały zestaw 140 testów aplikacji pod 32-bitowym .NET 10. Potwierdzono także cross-publish Linux ARM oraz musl ARM, bez uruchamiania tych programów na Windows.

Dokumentacja źródłowa: [zdarzenie release](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows#release), [katalog RID](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog), [upload załączników GitHub CLI](https://cli.github.com/manual/gh_release_upload).

Dostępność platform: [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), [Avalonia](https://docs.avaloniaui.net/docs/supported-platforms), [pakiety natywne SkiaSharp](https://github.com/mono/SkiaSharp/blob/main/documentation/dev/packages.md). Dla bieżących wersji sprawdzono także faktyczne pliki `runtimes/` lokalnie przywróconych pakietów NuGet.
