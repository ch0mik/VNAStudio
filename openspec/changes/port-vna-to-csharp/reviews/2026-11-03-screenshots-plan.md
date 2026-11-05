# Przegląd planu na podstawie zrzutów — 2026-11-03

Porównano oba obrazy z bieżącym kodem VNA Studio, trzema instrukcjami tekstowymi, oryginalnym INI i nazwami kontrolek/komunikatów w EXE 306b. Utworzono `roadmap-screenshots.md`: 29 pozycji z priorytetami, stanem implementacji, źródłami i kryteriami odbioru oraz etapy E0–E5. Rozbito dalsze zadania w sekcji 11; istniejące zadanie pełnej macierzy vna/J pozostało otwarte.

Najważniejsze ustalenia przeglądu:

- Save/Recall z obszaru zakresu ma inne znaczenie niż zapis/odczyt sesji. Instrukcja opisuje pamięć start/stop, a zasoby EXE mają osobne nazwy i tooltipy przycisków.
- DISPLAY Full (`chkMkrFull`) i Full range scan są różnymi kontrolkami. Znaczenie pierwszej wymaga sprawdzenia procedury zdarzenia.
- XTAL nie jest dowodem istnienia kompletnego automatu: instrukcja wprost nazywa tę zakładkę nieoprogramowaną i opisuje ręczny pomiar przez FILTER.
- Różnice markerów ze zrzutów wynoszą 3306000 i 2950000 Hz. Wyświetlone Q=2/7 zgadza się z jedną hipotezą obliczenia, ale nie stanowi potwierdzenia algorytmu lub fizycznego Q. Plan wymaga jawnej metody i f0.
- FILTER wymaga rozdzielenia transmisji i odbicia; Cable Loss wymaga potwierdzenia układu i liczby przejść. Checkbox 50 Ω nie może zmieniać kalibracyjnego Z0.
- Skale/ticki są jeszcze wstępne; szczególnie SWR i RL×2 nie mogą być wyprowadzone z domyślnego założenia. Predefiniowana kategoria VHF nie rozszerza zakresu sprzętu 65 MHz.
- Otwarcie portu COM nie zastępuje potwierdzonej odpowiedzi VNA, a błąd sprzętu nie może uruchamiać demonstratora.

Zaktualizowano specyfikacje wykresów, akwizycji i plików; dodano planowane wymagania `measurement-tools`. Zmiana dotyczy dokumentów planu, bez implementacji nowych funkcji programu. Kryteria obejmują wcześniejsze PL/EN, trzy motywy, skalowanie i nazwę VNA Studio/SQ7MRU.

Weryfikacja: 29 unikalnych identyfikatorów macierzy, poprawna liczba kolumn Markdown i istniejące lokalne odnośniki do źródeł. `openspec validate port-vna-to-csharp --strict` zakończył się powodzeniem.
