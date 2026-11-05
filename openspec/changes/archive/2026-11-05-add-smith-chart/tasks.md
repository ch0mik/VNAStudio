# Tasks

## 1. Kontrakt danych i niepewność

- [x] 1.1 Sprawdzić przepływ korekty RL, pochodzenie fazy, profil sesji i limit 4000 próbek; ustalenia zapisano w design.md.
- [x] 1.2 Dodać status i pochodzenie wiarygodności fazy z zachowawczymi regułami dla sterownika, demonstracji i importu.
- [x] 1.3 Dodać sesję v3 i migrację v1/v2, sprawdzić round-trip bez podnoszenia wiarygodności oraz zachowanie nieznanego Z0 bez migawki profilu.
- [x] 1.4 Wykonać review subagenta i poprawić trzy uwagi. Dowód: `reviews/2026-11-05-stage-1.md`.

## 2. Matematyka i geometria

- [x] 2.1 Dodać konwersje Z↔Γ i testy wektorów 50/75 Ω, osobliwości oraz signed S11.
- [x] 2.2 Przygotować znormalizowaną siatkę impedancji i transformacje ekranowe niezależne od UI.
- [x] 2.3 Dodać testy położenia poza kołem, klasyfikacji granicy i dokładności near-open; pełne lustrzane rozwiązania powstaną w etapie widoku.
- [x] 2.4 Wykonać review subagenta i poprawić obliczenia oraz sampling siatki. Dowód: `reviews/2026-11-05-stage-2.md`.

## 3. Kontrolka i wygląd

- [x] 3.1 Dodać SmithPlotControl z kołową siatką, śladem, Z0 i komunikatami o jakości danych.
- [x] 3.2 Dodać zasoby PL/EN i motywy Light/Dark/Night Vision; niepewne gałęzie różnią się także stylem linii.
- [x] 3.3 Sprawdzić kontrolkę w oknie 1024×768 i większym, po resize oraz z DPI 100/125/150%; kontrola zachowuje koło dzięki transformacji z mniejszego wymiaru.
- [x] 3.4 Wykonać review subagenta i poprawki. Dowód: `reviews/2026-11-05-stage-3.md`.

## 4. Integracja i interakcje

- [x] 4.1 Dodać wybór Frequency/Smith/Both w Antenie i responsywny układ dwóch wykresów.
- [x] 4.2 Współdzielić projekcję danych, korektę RL, markery i okno częstotliwości bez nowego skanowania COM.
- [x] 4.3 Dodać wybór/przeciąganie M1/M2 i klawiaturę z testami przecięć przebiegu.
- [x] 4.4 Dodać geometryczne +/−, kółko, pan i reset Smitha; oddzielić je w UI od zoomu częstotliwości.
- [x] 4.5 Wykonać review subagenta i poprawki. Dowód: `reviews/2026-11-05-stage-4.md`.

## 5. Trwałość, wydajność i odbiór

- [x] 5.1 Zapamiętać wybór widoku przez istniejące ustawienia IOptions i automatyczny zapis.
- [x] 5.2 Sprawdzić Demo/Hardware/Playback, brak fazy, stare sesje i bieżącą sesję z profilem innym niż aktywny.
- [x] 5.3 Zmierzyć przygotowanie i renderowanie aktualnego oraz maksymalnego zbioru; zastosować cache i anulowanie tam, gdzie potrzebne.
- [x] 5.4 Dodać podglądy wykresu i opis EN/PL, wykonać wymagane testy i końcowe review subagenta. Dowód: `reviews/2026-11-05-stage-5.md`.

## 6. Osobna weryfikacja sprzętowa znaku

- [x] 6.1 Sprawdzić dostępny terminator 50 Ω na COM9 i zapisać ograniczenie: brak znanych obciążeń L/C oraz niezależnego odniesienia nie pozwala potwierdzić znaku fazy. Dalsza walidacja sprzętowa jest osobną pracą i nie blokuje widoku pokazującego oba warianty.
- [x] 6.2 Zapisać zakres potwierdzenia lub trwałe ograniczenie toru; nie podnosić statusu historycznych pomiarów. Do potwierdzenia obciążeń L/C urządzenie pozostaje `Ambiguous`.
- [x] 6.3 Wykonać review raportu. Review potwierdził poprawne pozostawienie statusu fazy `Ambiguous`; dowód: `reviews/2026-11-05-stage-6.md`. Wydanie widoku z jawną niepewnością nie wymaga pozytywnego rozstrzygnięcia tego etapu.
