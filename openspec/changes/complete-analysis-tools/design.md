# Design

## Kontekst

Pasek trybów ustawia `SelectedMode`. Aktualnie Cable Loss udostępnia oznaczone oszacowanie jednowrotnych strat pod warunkiem danych sprzętowych i potwierdzenia open/short; Cable Length liczy długość z sąsiednich rezonansów; Xtal liczy Q z ręcznie podanych częstotliwości; Filter obsługuje osobne skalarne dane Demo/Playback, ale blokuje sprzętowy skan; Generator pokazuje pamięci i waliduje częstotliwość, ale nie steruje wyjściem. Oryginalny handler trybu jest zdekompilowany częściowo, a nie każdy tryb ma potwierdzony algorytm. Pełna macierz statusu i pozostałych luk jest w [implementation-status.md](implementation-status.md).

## Potwierdzone źródła i ograniczenia

- `reference/software/analyz.ini`: `RL_OFFSET_vna=0`, `RL_OFFSET_filter=15.5`, VF domyślne 0.66, 10 częstotliwości pamięci generatora.
- Instrukcja kablowa (`reference/hardware/manual-3.txt`, strony 5–6): otwarty lub zwarty kabel; dwa sąsiednie maksima albo dwa sąsiednie minima tego samego rodzaju; VF ustalany dla danego kabla. Nie określa pomiaru strat kabla.
- Instrukcja filtra/balunu (strona 7): tryb FILTER, uzwojenia między portami, ocena pasma przenoszenia. INI opisuje punkt zerowy OUT–IN w FILTER. To jest wskazówka scalarnej odpowiedzi amplitudowej, nie dowód dostępności zespolonego S21 ani kalibracji fazy.
- Instrukcja XTAL (strona 5) mówi, że automatyczna zakładka XTAL nie była zaprogramowana. Obecny ręczny Q jest dodatkiem zgodnym z dalszą procedurą instrukcji, nie portem automatu legacy.
- Brak odzyskanego kontraktu komend generatora. Pamięci częstotliwości nie upoważniają do włączenia nadajnika.

## Decyzje

1. Zachować istniejące surowe próbki sesji. Tryby mogą tworzyć wyłącznie projekcję do wyświetlenia i nie nadpisują danych źródłowych.
2. Kabel Loss i Filter muszą mieć odrębne wielkości i opis kalibracji. Nie wyliczać SWR/Z z odpowiedzi transmisyjnej ani S21 z ReturnLossDb bez potwierdzonego kontraktu.
3. Dla funkcji niepotwierdzonej pokazać dokładnie czego brakuje; nie używać demonstracyjnego przebiegu jako wyniku urządzenia.
4. Generator pozostaje bez komend wyjściowych, dopóki sterownik VNA 1–65 MHz nie ma testów ramek i potwierdzenia na sprzęcie.
5. Każdy etap kończy się niezależnym review subagenta; uwagi blokujące naprawić przed następnym etapem.

## Plan etapów

1. Inwentaryzacja kontraktów i źródeł prawdy dla każdego przycisku.
2. Cable Length: walidacja sąsiedniej pary rezonansów i przepływu VF.
3. Xtal: zachowanie ręcznego pomocnika Q bez przypisywania go automatyce legacy.
4. Cable Loss: osobny estymator, ograniczenie źródła i jawny fixture.
5. Filter: osobny tor skalarnej odpowiedzi; sprzęt zablokowany do potwierdzenia protokołu.
6. Generator: utrzymanie kontroli sprzętowej wyłączonej, dopóki pełny kontrakt nie zostanie odzyskany.
7. Integracja PL/EN, sesji/CSV, pełny zestaw testów i review.

Pozostałe walidacje fizyczne/protokołowe są zaplanowane osobno w [implementation-status.md](implementation-status.md); nie są uznawane za wykonane przez same testy demo.
