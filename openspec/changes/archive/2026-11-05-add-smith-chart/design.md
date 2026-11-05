# Design

## Context

Obecne `MeasurementPoint` zawiera częstotliwość, podpisane `ReturnLossDb` (w praktyce S11 w dB), fazę i pochodne impedancji. Sesja v2 przechowuje migawkę profilu. Źródłem może być urządzenie, demonstracja albo odtwarzanie. Nowy widok jest reprezentacją pomiaru Antena, a nie nowym trybem sprzętowym.

Oryginalny program posługuje się modułem reaktancji i fazą 0–180°. Obecne przeliczenie RF nie potwierdza fizycznego znaku X. Zdjęcia obudowy identyfikują VNA 1–65 MHz, lecz nie potwierdzają modelu detektora. Nie wolno utożsamiać dodatniej wartości liczbowej fazy z potwierdzoną indukcyjnością.

## Goals / Non-Goals

**Cele:** dodatkowy wykres impedancyjny S11, wspólna obsługa danych i markerów, poprawne Z0, czytelna niepewność fazy, płynne skalowanie, dostępność klawiaturą, zapamiętywanie preferencji.

**Poza zakresem:** projektowanie dopasowania L/C, nowe komendy COM, kalibracja OSL, siatka admitancji, pomiary S21 na Smithie, import Touchstone, eksport SVG/PNG. Nie zmieniamy domyślnego języka angielskiego ani motywu Dark.

## Decisions

### 1. Dane i wiarygodność fazy

Wprowadzić status `Verified`, `Ambiguous`, `Missing` oraz pochodzenie informacji. Status należy do pomiaru; model powinien pozwalać obniżyć wiarygodność pojedynczej próbki. `Verified` oznacza potwierdzoną orientację fazy, nie deklarację pełnej kalibracji RF.

- Obecny sterownik VNA 1–65 MHz: `Ambiguous`, dopóki odpowiednie pomiary kontrolne nie potwierdzą możliwości wyznaczania znaku w używanym torze.
- Sesje v1/v2 i pliki bez jawnej informacji: zachowawczo `Ambiguous`, a przy braku użytecznej fazy `Missing`. Sam ujemny X w starszym pliku nie jest dowodem prawidłowej konwencji.
- Demonstracja analityczna: znana konwencja, ale zawsze zachowana etykieta danych demonstracyjnych.
- Import może otrzymać `Verified` wyłącznie na podstawie rozpoznanego kontraktu źródła. Zapis i odczyt nie podnoszą statusu.

Przy `Ambiguous` rysować Γ oraz jego sprzężenie jako dwa równorzędne, przerywane przebiegi. Odczyt impedancji: `R ± j|X|`; faza analogicznie `±|φ|`. Nie opisywać próbki jako indukcyjnej lub pojemnościowej. Na osi rzeczywistej oba rozwiązania pokrywają się. Brak fazy oznacza brak przebiegu i jasny komunikat, zamiast sztucznego umieszczania danych na osi.

### 2. Obliczenia niezależne od Avalonia

Dla podpisanego S11 w dB: `ρ = 10^(S11_dB/20)`, `Γ = ρ·exp(jφ)`. Dla pełnej, wiarygodnej impedancji: `Γ = (Z−Z0)/(Z+Z0)`. Konwersja odwrotna: `Z = Z0·(1+Γ)/(1−Γ)`. Nie zmieniać znaku obecnego pola `ReturnLossDb` przez pomylenie go z dodatnią stratą odbiciową.

Z0 pochodzi z migawki profilu pomiaru, nie z aktualnie wybranego urządzenia. Pierwsza wersja wyświetla Z0 bez edycji. Ewentualna późniejsza renormalizacja wymaga pełnego przeliczenia, a nie samej zmiany etykiety. Aktywna korekta wyświetlania RL ma być stosowana dokładnie raz, przez wspólną projekcję danych obu wykresów; wartości źródłowe sesji pozostają zachowane.

Jeżeli starsza sesja nie ma `ProfileSnapshot` ani innego wiarygodnego historycznego Z0, nie podstawiać aktualnego profilu ani domyślnych 50 Ω. Wyświetlić `Z0 nieznane`: można pokazać Γ z zapisanego S11/fazy na siatce znormalizowanej, ale odczyty impedancji w omach i konwersja z zapisanego Z są niedostępne. Brak Z0 zachować także przy migracji i ponownym zapisie.

Wartości Missing/Invalid/NaN przerywają przebieg. Punkty `|Γ|>1` oznaczać jako poza zakresem, pomijać w podstawowym widoku i zliczać; nie przycinać numerycznie do jedynki. Nie łączyć odcinkiem luk po takich punktach. Γ=1 oznacza osobliwość konwersji na Z: można zaznaczyć rozwarcie, ale bez dzielenia przez zero. Dokładne dopasowanie Γ=0 jest poprawnym wejściem modelu matematycznego z Z=Z0; nie wymaga dopuszczania nieskończonego S11 w istniejących plikach.

### 3. Własna siatka wektorowa

`SmithPlotControl` rysuje siatkę i dane; funkcje konwersji i geometria są w Core. Normalizacja `z=r+jx=Z/Z0`:

- stałe r: środek `(r/(r+1),0)`, promień `1/(r+1)`;
- stałe x≠0: środek `(1,1/x)`, promień `1/|x|`; x=0 to oś rzeczywista;
- linie r: 0, 0.2, 0.5, 1, 2, 5; linie x: ±0.2, ±0.5, ±1, ±2, ±5;
- łuki ograniczone do koła jednostkowego, etykiety bez nakładania na markery;
- środek Γ=0: dopasowanie; lewy skraj Γ=−1: zwarcie; prawy Γ=1: rozwarcie.

Mapowanie ekranu odwraca oś Y, aby dodatnia część urojona była u góry. Koło zachowuje proporcje przy każdym rozmiarze; promień wynika z mniejszego wymiaru dostępnego pola. Wszystkie rysunki generowane z geometrii, bez kopiowania ilustracji artykułu.

### 4. Widoki, markery i zoom

W Antenie: `Frequency / Smith / Both` z tłumaczeniami. Przełączenie nie wykonuje pomiaru ani nie zmienia próbek. W innych trybach wybór Smitha jest niedostępny, a preferencja Anteny pozostaje zapamiętana. Przy małej szerokości Both układa wykresy pionowo; zawartość pozostaje dostępna w oknie 1024×768.

M1 wybierany lewym przyciskiem, M2 prawym, przeciąganie aktualizuje wspólny indeks próbki. Wybór na podstawie odległości w pikselach od przebiegu, następnie najbliższej rzeczywistej próbki na wybranym odcinku. Przy przecięciu przebiegu remis rozstrzyga bliskość poprzedniego indeksu, a następnie niższy indeks. Ten sam indeks dotyczy obu lustrzanych wariantów. Strzałki przechodzą po kolejnych poprawnych próbkach w porządku częstotliwości. Nie interpolować pozornej próbki w punkcie przecięcia.

Dotychczasowe przyciski zakresu i zoom +/− sterują wspólnym oknem częstotliwości i wyborem próbek dla obu widoków. Smith ma osobno opisane `powiększenie wykresu`, przesuwanie i `reset widoku`, zmieniające transformację geometryczną, bez modyfikacji zakresu skanu. Przesuwanie środkowym przyciskiem; zoom geometryczny przyciskami i kółkiem nad kontrolką z punktem kotwiczenia pod kursorem. Dodatnia skala ograniczona do 1–8×; reset przywraca 1× i środek Γ=0. Przeliczanie wyboru markerów korzysta z tej samej transformacji.

### 5. Stan i integracja

Preferencja rodzaju widoku Anteny w `AppSettings`, przez istniejące IOptions i automatyczny zapis. Początkowo zachować obecny wykres Frequency. Geometryczny zoom/pan jest stanem bieżącego widoku; nie zapisywać go jako cechy pomiaru. Nowa wersja sesji v3 zapisuje status fazy i jego pochodzenie, czyta v1/v2 zgodnie z zachowawczą regułą powyżej. Nie zmieniać po cichu kontraktu istniejącego CSV; odczyt CSV bez metadanych pozostaje niezweryfikowany.

Wspólna projekcja danych zapewnia identyczne korekty i źródła w obu wykresach. Siatka buforowana według rozmiaru, DPI, motywu i transformacji. Brak I/O w Render. Operacje plikowe pozostają async z CancellationToken; kosztowniejsze przygotowanie danych może być anulowane po nadejściu nowszego skanu. Nie tworzyć osobnego zadania dla każdej próbki.

### 6. Kryteria odbioru

- Wektory przy Z0=50 Ω: Z=50→Γ=0, 0→−1, rozwarcie→1, 100→1/3, 25→−1/3, 50±j50→0.2±j0.4. Osobno Z0=75 Ω. Tolerancja konwersji dla niesingularnych wektorów: 1e−12 względnie/bezwzględnie.
- Zmiana bieżącego profilu nie zmienia Z0 odtwarzanej sesji. Brak podwójnej korekty RL.
- Sesja bez migawki nie przejmuje Z0 aktualnego profilu; widok znormalizowany nie prezentuje niepotwierdzonych odczytów w omach.
- Oba warianty niepewnej fazy, prawidłowe luki, osobliwości i wartości spoza zakresu mają testy.
- Wspólne markery i zakres działają po zoomie, panie, resize i przy przecięciach przebiegu.
- Sprawdzenie 1024×768 i większych okien, minimalizacji/maksymalizacji oraz skali DPI 100/125/150%; różnica średnic koła ≤1 DIP.
- Motywy Light/Dark/Night Vision i PL/EN; niepewność wyróżniona także stylem linii, nie tylko kolorem.
- Test obciążenia 649 i 4000 próbek (aktualny limit `SweepSettings`), także dla dwóch lustrzanych śladów. Brak dodatkowych wywołań COM wskutek zmiany widoku.
- Pomiar terminatora 50 Ω powinien dawać ślad blisko środka z uwzględnieniem tolerancji i kalibracji. Sam terminator nie potwierdza znaku fazy. Testy znanych obciążeń L/C są osobną weryfikacją możliwości toru; nie odblokowują automatycznie pełnego zakresu na podstawie jednego punktu.

## Risks / Trade-offs

Podwójny ślad jest mniej wygodny niż pojedynczy, lecz zachowuje uczciwą interpretację danych. Funkcja może zostać udostępniona z oznaczoną niepewnością; potwierdzenie znaku jest osobnym wynikiem badań sprzętu. Wykres nie zastępuje kalibracji. Jeśli tor nie rozróżnia znaku, sama zmiana UI tego nie naprawi.

## Migration Plan

Najpierw kontrakt danych i migracja bez zmiany UI, następnie matematyka, kontrolka, integracja i interakcje. Każdy etap kończy się testami odpowiednimi do zmiany oraz niezależnym review subagenta. Wszystkie etapy implementacyjne w tasks.md pozostają otwarte.

## Open Questions

Do pomiarowego ustalenia: czy tor urządzenia potrafi rozróżnić znak fazy i w jakim zakresie. Nie blokuje to zaplanowanego widoku z dwoma rozwiązaniami. Dokładny sposób zapisu metadanych v3 zostanie doprecyzowany przy implementacji po przeglądzie serializerów.

## References

- [DigiKey: wprowadzenie do wykresu Smitha](https://www.digikey.pl/pl/blog/the-smith-chart-its-history-and-why-its-so-important): zastosowanie reprezentacji impedancji i odbicia.
- [Keysight: Complex Impedance](https://helpfiles.keysight.com/csg/m9485a/tutorials/comp_imped.htm): konwersja Z↔Γ i odniesienie do Z0.
- [Analog Devices: AD8302, rozróżnienie ±180°](https://ez.analog.com/rf/a/documents/c/ad8302-faq/DO24246/ad8302-hot-to-measure---180deg-phase): przykład ograniczenia detektora; nie jest dowodem obecności tego układu w posiadanym urządzeniu.
- [Lokalny opis komunikacji i ograniczeń RF](../../../docs/VNA-COMMUNICATION.pl.md).
