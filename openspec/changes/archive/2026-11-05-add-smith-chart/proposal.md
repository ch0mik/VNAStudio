# Proposal

## Why

Użytkownik chce dodatkowy wykres Smitha, umożliwiający ocenę dopasowania anteny i zmian impedancji w funkcji częstotliwości. Obecny widok pokazuje osobne przebiegi częstotliwościowe. Nowa reprezentacja powinna wykorzystywać te same próbki, bez kolejnego skanowania urządzenia.

## What Changes

- W trybie Antena pojawi się wybór: wykres częstotliwościowy, Smith albo oba widoki. Własna kontrolka Avalonia zachowa wspólne markery M1/M2 i zakres częstotliwości.
- Powstaną obliczenia zespolonego współczynnika odbicia, wektorowa siatka impedancji i odczyt częstotliwości, Z oraz Γ przy markerach.
- Wprowadzimy jawny status wiarygodności znaku fazy. Dla VNA 1–65 MHz znak reaktancji pozostaje niepotwierdzony: widok pokaże dwa lustrzane rozwiązania i informację o niepewności, zamiast arbitralnie rozstrzygać L/C.
- Widok obsłuży zoom geometryczny, przesuwanie, reset, motywy Light/Dark/Night Vision, PL/EN i dynamiczny rozmiar okna od 1024×768.
- Preferencje widoku będą zapamiętywane. Sesje zachowają informację o wiarygodności fazy i jej pochodzeniu, z bezpiecznym odczytem wcześniejszych wersji.

Etapy 1–5 obejmują zaimplementowany widok impedancyjny Smith S11, jego interakcje, zapamiętanie preferencji, podglądy i testy wydajności. Walidacja znaku fazy na znanych obciążeniach L/C pozostaje osobną pracą sprzętową. Projektowanie układów dopasowujących, siatka admitancji, de-embedding, Touchstone i eksport wykresu pozostają poza zakresem.

## Capabilities

### New Capabilities

- `smith-chart`: obliczenia, kontrolka, wspólne markery, interakcje i uczciwe przedstawienie niepewnej fazy.

### Modified Capabilities

Brak bazowych specyfikacji; zmiana rozszerza projekt opisany w `port-vna-to-csharp`. Nie zastępuje wymagań obecnego wykresu, plików ani komunikacji.

## Impact

`Vna.Core`: matematyka i status fazy. `Vna.App`: kontrolka i stan widoków. `Vna.IO`: wersjonowanie sesji i ustawienia. `Vna.Localization`: zasoby `.resx`. `Vna.Tests`: testy matematyczne, migracyjne i interakcji. Protokół COM i polecenia urządzenia pozostają bez zmian.

Punkt wyjścia: [artykuł DigiKey](https://www.digikey.pl/pl/blog/the-smith-chart-its-history-and-why-its-so-important). Zależności techniczne i źródła: [design.md](design.md).
