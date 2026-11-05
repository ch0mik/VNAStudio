# Spec Delta

## Purpose

Automatyzuje kompilację i dołączanie paczek aplikacji dla wybranych RID do opublikowanego GitHub Release.

## ADDED Requirements

### Requirement: Wydanie z wersjonowanego taga

System SHALL uruchamiać pakowanie po zdarzeniu `release.published`, walidować tag `vX.Y.Z` i kompilować commit zdarzenia release. Wersja plików SHALL wynikać z taga.

#### Scenario: Opublikowanie release

- **WHEN** użytkownik publikuje release z tagiem `v1.2.3`
- **THEN** aplikacja jest publikowana z wersją 1.2.3, a assembly i plik wykonywalny mają wersję 1.2.3.0

### Requirement: Osobne paczki RID

System SHALL przygotowywać osobne archiwa dla `win-x86`, `win-x64`, `win-arm64`, `linux-x64`, `linux-arm`, `linux-arm64`, `linux-musl-x64`, `linux-musl-arm`, `linux-musl-arm64`, `osx-x64` i `osx-arm64`. Każda paczka SHALL zawierać runtime .NET, plik wykonywalny, profil urządzenia i zasoby polskie oraz angielskie. Archiwa Unix SHALL zachowywać prawo wykonywania. Walidacja SHALL sprawdzać wymagane biblioteki natywne, a dla Windows również ich architekturę PE.

#### Scenario: Zawartość wariantu

- **WHEN** powstaje paczka danego RID
- **THEN** jej nazwa i metadane wskazują wersję oraz RID, a walidator sprawdza zgodność zależności, wersji assembly i wymaganych plików

### Requirement: Sprawdzenie kompletności przed przesłaniem

System SHALL dołączać paczki do release po powodzeniu testów i wszystkich buildów. System SHALL weryfikować sumy kontrolne, wymagać wszystkich jedenastu wariantów i dołączać wspólny manifest SHA-256.

#### Scenario: Niekompletny zestaw

- **WHEN** brakuje paczki lub jej suma kontrolna jest niezgodna
- **THEN** krok przesłania załączników nie jest wykonywany
