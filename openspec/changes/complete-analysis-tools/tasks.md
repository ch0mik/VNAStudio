# Tasks

## 1. Kontrakt i inwentaryzacja

- [x] 1.1 Zmapować każdy przycisk na obecny command, widok, obliczenie i stan hardware.
- [x] 1.2 Udokumentować fakty źródłowe z VB6, INI i instrukcji oraz oznaczyć hipotezy jako niepotwierdzone.
- [x] 1.3 Zdefiniować osobne semantyki S11, odpowiedzi FILTER i jednostronnych strat kabla; nie mieszać z SWR/Z.
- [x] 1.4 Zrobić review subagenta; rozstrzygnąć uwagi blokujące przed etapem 2. Review: `reviews/2026-11-05-stage-1.md` (bez blockerów).

## 2. Cable Length

- [x] 2.1 Walidować, że M1/M2 wskazują sąsiednie rezonanse tego samego rodzaju w bieżącym sweepie.
- [x] 2.2 Wyjaśnić wymagania zakończenia kabla i ograniczenie rekomendowanego zakresu >60 MHz.
- [x] 2.3 Zachować edycję i zapis VF oraz reagowanie wyniku na zmianę markerów.
- [x] 2.4 Dodać testy przypadków: poprawne sąsiednie maksima/minima, różne rodzaje, rezonans pośredni, brak danych i zmiana zakresu.
- [x] 2.5 Review subagenta i poprawki. Review: `reviews/2026-11-05-stage-2.md` (bez blockerów; hałas lokalny opisano jako ograniczenie).

## 3. Xtal

- [x] 3.1 Potwierdzić, że automatyczna zakładka XTAL oryginału była pusta; utrzymać ręczny kalkulator jako osobną funkcję.
- [x] 3.2 Sprawdzić walidację Q, jednostek, krawędzi i lokalizowanych instrukcji metody.
- [x] 3.3 Dodać testy regresji przykładu z instrukcji i błędnych wejść.
- [x] 3.4 Review subagenta i poprawki. Review: `reviews/2026-11-05-stage-3.md` (bez blockerów).

## 4. Cable Loss

- [x] 4.1 Dodać oszacowanie strat jednowrotnych `−ReturnLossDb / 2`, oddzielne od SWR i skalibrowanego S21.
- [x] 4.2 Wymagać jawnego potwierdzenia kabla otwartego/zwartego na końcu przed odsłonięciem wykresu; 50 Ω nie jest poprawnym fixture.
- [x] 4.3 Dodać ograniczenia metody, lokalizowane wskazówki, testy domeny wartości i bramkę pochodzenia wykluczającą demo z pomiaru kabla.
- [x] 4.4 Uruchomić review subagenta; wynik bez blockerów w `reviews/2026-11-05-stage-4.md`.

## 5. Filter

- [x] 5.1 Dodać odrębną wartość skalarnej transmisji i offset FILTER; nie wyliczać SWR/Z/fazy.
- [x] 5.2 Udokumentować układ ANT–WOB, obsługę Demo/Playback i odrzucać hardware Filter przed COM, dopóki komenda wariantu VNA 1–65 MHz nie zostanie potwierdzona.
- [x] 5.3 Dodać testy konwersji, offsetu i migracji sesji.
- [ ] 5.4 Wykonać pomiar sprzętowy po podłączeniu właściwego fixture i potwierdzić komendę na docelowym wariancie urządzenia (obecnie fixture ANT–50 Ω, WOB wolne nie nadaje się do Filter; driver odrzuca tryb przed COM).
- [x] 5.5 Review subagenta i poprawki; wynik w `reviews/2026-11-05-stage-5.md`.

## 6. Generator

- [x] 6.1 Zbadać handler VB6, instrukcję i profil: jest fragment `&OFF`, ale brak pełnej sekwencji start/stop/częstotliwość.
- [x] 6.2 Nie dodawać async komend sprzętowych bez kompletnego kontraktu; zachować edycję/walidację pamięci oraz zablokowane wyjście.
- [x] 6.3 Opisać nieznany stan fizycznego wyjścia i ograniczenie w PL/EN; dodać testy zakresu i blokady.
- [x] 6.4 Review subagenta i poprawki; wynik w `reviews/2026-11-05-stage-6.md`.

## 7. Integracja

- [x] 7.1 Sprawdzić język PL/EN, ustawienia IOptions, format sesji v4/CSV, responsywność i rozdzielenie źródłowych próbek.
- [x] 7.2 Wykonać pełny zestaw testów (202/202), build całego solution (0 błędów) i strict OpenSpec validation.
- [x] 7.3 Wykonać końcowe review subagenta; wynik w `reviews/2026-11-05-stage-7.md`.

## 8. Dalsza walidacja zgodności z urządzeniem

- [x] 8.1 Dodać macierz aktualnego stanu i pozostałych luk do `implementation-status.md`.
- [x] 8.2a Zbadać manual i dekompilację Cable Loss; potwierdzić, że odzyskano frame/etykietę trybu, ale nie wzór ani kalibrację.
- [x] 8.2b Opisać `−ReturnLossDb / 2` jako estymator VNA Studio, nie port legacy, w dokumentacji EN/PL.
- [x] 8.2c Uruchomić review subagenta i zapisać wynik w `reviews/2026-11-05-stage-8-2.md`.
- [ ] 8.2d Porównać estymator na kablu fizycznym open/short o znanym tłumieniu.
- [ ] 8.3 Zweryfikować Cable Length na kablu o znanej długości i współczynniku skrócenia.
- [x] 8.4a Przejrzeć odzyskany kod i asembler handlera trybów pod kątem komendy FILTER; `VB6_00425e1d` nie zdekompilował się, a eksport nie potwierdza kompletnej komendy dla VNA 1–65 MHz.
- [x] 8.4b Zachować blokadę sprzętowego FILTER, opisać granicę dowodów i wykonać review subagenta; wynik w `reviews/2026-11-05-stage-8-4.md`.
- [ ] 8.4c Potwierdzić komendę FILTER na dokładnym wariancie VNA 1–65 MHz i właściwym fixture ANT–WOB. Aktualnie ANT ma 50 Ω, WOB jest wolne; bez właściwego fixture test nie jest możliwy.
- [x] 8.5a Sprawdzić handler Generatora `VB6_0042399d` i odwołania do `&OFF`; odzyskana procedura potwierdza zmianę stanu/etykiety UI, ale nie pełną ramkę komunikacyjną ON/OFF ani strojenia.
- [x] 8.5b Porównać dostępne odzyskane materiały komunikacji i pozostawić komendy sprzętowe wyłączone; nie przenosić niepotwierdzonego protokołu z pokrewnego urządzenia.
- [x] 8.5c Zaktualizować dokumentację stanu i wykonać review subagenta; wynik w `reviews/2026-11-05-stage-8-5.md`.
- [ ] 8.5d Odzyskać pełny protokół generatora ON/OFF i strojenia, dodać testy ramek i zweryfikować na obciążeniu.
