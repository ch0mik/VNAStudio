# Pozostałe funkcje VNA Studio — plan na podstawie zrzutów

Data: 2026-11-04. Zakres: dwa zrzuty dostarczone przez użytkownika, instrukcje lokalnego urządzenia oraz zasoby Analyser 306b. Nazwa aplikacji pozostaje **VNA Studio**, ze znakiem **SQ7MRU** po prawej. Wszystkie nowe elementy obejmują PL/EN, Light/Dark/Night Vision, zmianę rozmiaru i minimum 1024×768 DIP; interfejs używa mniejszych czcionek, by zachować czytelny układ przy minimalnym rozmiarze.

## Źródła i granice pewności

- **S1**: pierwszy zrzut oryginalnej aplikacji (plik źródłowy `original-page2-R19.png`, dostarczony poza repozytorium), V1.3.2: sześć przebiegów, tabela markerów, pasmo/Q, pamięć zakresu i ustawienia wyświetlania.
- **S2**: drugi zrzut (plik `original-page3-R26.png`, dostarczony poza repozytorium), V1.3.2: CAL, RL×2, Offset, Min.SWR, Full/Zoom/Save/Recall. Sam obraz nie potwierdza aktywnego trybu sprzętowego.
- **M1**: dostarczona instrukcja instalacji, strona 5: pomiary ruszają po wyłączeniu Hold.
- **M2**: dostarczona instrukcja rezonansów i Q, strony 2–4: `Q = Fres / (Fh − Fl)`, kryterium fazowe i odrębne układy pomiaru rezonansów.
- **M3**: dostarczona instrukcja pomiarów: Save/Recall, FILTER, długość kabla, baluny i generator. Tekst wyodrębniono z PDF z błędami OCR; przy niejasnych jednostkach trzeba wrócić do oryginalnego obrazu.
- **B**: lokalne zasoby zdekompilowanego Analyser 306b (`strings.tsv`, poza repozytorium): `chkMkrFull`, `chk50Ref`, `chkXJ`, `chkRLx2`, `cmdSaveSweep`, `cmdRecallSweep`, `mnuSaveCsv`, nazwy trybów i komunikaty kalibracji.
- **I**: dostarczony plik `analyz.ini` (poza repozytorium): zakres 1–65 MHz, zegar DDS, stałe kalibracji, VF i dziesięć pamięci generatora.

Oryginalne zrzuty, instalatory, instrukcje i wyniki dekompilacji są utrzymywane w katalogach obok `VnaStudio`; nie są kopiowane ani linkowane z repozytorium GitHub.

Zrzuty pokazują wcześniejszą wersję niż EXE 306b. Widoczna zakładka lub kontrolka potwierdza obecność interfejsu, ale nie kompletność jej algorytmu. Szczególnie M3 opisuje **XTAL jako jeszcze nieoprogramowane**: pełny automat parametrów kwarcu jest rozszerzeniem, a nie udowodnioną brakującą funkcją oryginału.

## Macierz braków

Statusy: **częściowo** = istnieje podstawa w VNA Studio; **brak** = funkcja nie jest zaimplementowana; **badanie** = znaczenie lub algorytm wymaga potwierdzenia. P0 dotyczy rzeczywistych pomiarów i poprawności danych, P1 codziennej obsługi, P2 narzędzi specjalistycznych, P3 rozszerzeń.

| ID | Element źródła | Stan teraz | Plan i kryterium odbioru | Priorytet / źródło |
| --- | --- | --- | --- | --- |
| C01 | Faktyczne pomiary zamiast demonstratora | Jawny wybór demo/sprzęt/odtwarzanie, Połącz/Rozłącz, trwałe ustawienie i blokada zmiany aktywnego portu/źródła. Odtwarzanie publikuje wyłącznie pełną, zgodną sesję bez interpolacji | Zakończyć fizyczne sprawdzenie sterownika; błędy nigdy nie przechodzą automatycznie na demo | P0 / M1, B |
| C02 | Hold i skanowanie ciągłe | Pojedynczy i ciągły cykl nad kontraktem źródła; Hold kończy bieżący pełny skan, Stop anuluje i odrzuca częściowy wynik | Hold zachowuje ostatni kompletny wynik; sprzętowe timeouty, przerwanie ramki i ponowne połączenie wymagają potwierdzonego sterownika | P0 / S1–S2, M1 |
| C03 | Punkty i czas skanu | Pasek liczby/limitu i czas ostatniego kompletnego skanu | Postęp aktualizuje się w trakcie akwizycji; czas pełnego skanu nie jest podmieniany przez wynik przerwany. Bieżący demonstrator publikuje próbki progresywnie | P1 / S1–S2 |
| U01 | Tabela VALUES: M1/M2 × FREQ, SWR, \|Z\|, RL, faza, Rs, \|Xs\| | Zaimplementowana tabela pod wykresem | Dwa wyrównane wiersze ze wspólnej próbki, jednostki i brakujące wartości „—”; nie wypełniać nieobliczalnych kolumn zerem | P1 / S1–S2 |
| U02 | Osobne checkboxy Marker 1/2 | Zaimplementowane niezależne przełączniki | Niezależne pokazywanie i ukrywanie bez utraty pozycji; zoom wymaga dwóch dostępnych, różnych pozycji | P1 / S1–S2, B |
| U03 | Strzałki markerów | Strzałki UI, przeciąganie i lewo/prawo po fokusie wykresu | Przesuwanie o sąsiednią rzeczywistą próbkę; pozostaje w granicach danych i aktywnego zakresu | P1 / S1–S2, B |
| U04 | Min.SWR | Zaimplementowano checkbox i akcję M1 → minimum | Rysowanie można wyłączyć bez zmiany wyniku; akcja ustawia M1 na prawidłowe minimum, przywraca pełen widok gdy trzeba i zachowuje poprzedni zoom w historii | P1 / S2, M3 |
| U05 | Trace enable, All, None | Checkboxy i polecenia Wszystkie/Żadne zaimplementowane | Włącz wszystkie/wyłącz wszystkie, bez kasowania danych i pozycji markerów | P1 / S1–S2 |
| U06 | Edytowalne kolory przebiegów | Paleta wyboru, reset do profilu i zapis preferencji aplikacji | Wybór koloru bazowego, reset i zapis preferencji widoku; legenda zgodna z wykresem; Night Vision zachowuje czerwoną prezentację i inne wzory linii | P1 / S1–S2, I |
| U07 | Rzeczywiste ticki i skale | SWR 1/2.5/5/7.5/10; faza 0/45/90/135/180; Ω 0/75/150/225/300; RL pokazuje dodatnią wartość straty na danych ujemnych | Wartości etykiet odtworzono ze zrzutów; ticki są pozycjonowane tą samą liniową transformacją co krzywe. Linia 50 Ω pozostaje niezależna | P1 / S1–S2, B |
| U08 | DISPLAY: 50 ohm | Przełączana przerywana linia odniesienia 50 Ω na skali impedancji; preferencja zapisywana w ustawieniach | Nie modyfikuje impedancji odniesienia `Z0` ani kalibracji. Powiązanie kontrolki potwierdzone identyfikatorem `chk50Ref` | P1 / S1, B |
| U09 | DISPLAY: Full | Pełna i skrócona tabela markerów | W pełnym widoku pokazuje FREQ, SWR, |Z|, RL, PHASE, R i |X|; skrócony pozostawia FREQ i RL. Nie utożsamiać z przyciskiem Full zakresu | P1 / S1–S2, B |
| U10 | DISPLAY: Xc/Xl | Przełączana kolumna markerów przelicza signed X na C (pF) lub L (µH) | Identyfikatory `chkXJ`, `txtMkr1XJ`, `txtMkr2XJ` oraz teksty `Xc to pf`/`Xl to uH` wskazują na przelicznik markerów. Standardowe wzory są opisane w kodzie; hardware results require verified sign/calibration | P2 / S1, B |
| R01 | Full zakresu pomiarowego | Osobna akcja pełnego zakresu widoku i pełnego zakresu skanu | Pełny widok ustawia oś wykresu; pełny skan ustawia start/stop na granice profilu. Żadna z akcji widoku nie zmienia skanu | P1 / S2, B |
| R02 | Save/Recall zakresu | Pamięć zakresu widoku w bieżącej sesji oraz historia zoomu | Osobne Zapamiętaj / Przywróć; Save zakresu nie zapisuje pliku pomiarowego. Pamięć zakresu jest tymczasowa i resetuje się po zamknięciu aplikacji | P1 / S2, M3, B |
| R03 | Bands, HF, VHF | Presety polskich pasm amatorskich z UKE, filtrowane według zakresu profilu | HF i VHF wybierają grupę, preset ustawia tylko start/stop; 2 m nie pojawia się dla profilu 65 MHz | P1 / S1–S2, B |
| A01 | BANDWIDTH | Zaimplementowana szerokość między markerami | `abs(f2−f1)` wyświetlane w MHz, niezależnie od kolejności markerów | P1 / S1–S2 |
| A02 | Q | Szacunek z markerów oraz odrębny ręczny kalkulator rezonansu | Szacunek round((f1+f2)/2/abs(f2−f1)) pasuje do S1/S2, ale nie jest potwierdzonym algorytmem legacy. Kalkulator Xtal przyjmuje Fres/Fl/Fh i stosuje metodę ±45° fazy oraz wzór Q=Fres/(Fh−Fl) z M2; granice wprowadza użytkownik, program ich automatycznie nie wykrywa | P2 / S1–S2, M2 |
| K01 | CAL / Offset | Stałe tylko zachowane | Kalibracja dla trybu, zakresu i profilu; Through/LOOP 0 dB tam, gdzie potwierdzony. Pokazać stan i pochodzenie, a nie tylko checkbox | P0 / S2, M3, B, I |
| K02 | RL×2 / zakres dB | Częściowo potwierdzone | Zrzut etykietuje opcję `RL×2`; handler ma gałęzie osi 0–30 i 0–60 dB. VNA Studio przełącza wyłącznie zakres osi i nie zmienia próbek ani kalibracji; porównanie podziałki z oryginałem pozostaje otwarte | P1 / S2 + Ghidra `VB6_0043094f`, I |
| K03 | RL Offset | Regulowana korekta widoku | Ustawienie dodaje offset dB do signed RL, a SWR i impedancja są wyliczane ponownie; nie zmienia próbek sesji ani kalibracji profilu. Wartość zapisywana w preferencjach aplikacji | P1 / S2 + `RL_OFFSET_vna` z INI, B |
| M01 | Antenna | Demo i kalkulator RF | Sterownik odbicia, potwierdzona konwencja RL/fazy i kalibracja; porównania 50 Ω, OPEN, SHORT i obciążenia reakcyjnego | P0 / S1, M3 |
| M02 | Filter | Brak | Osobne dane transmisji i kalibracja Through; widok poziomu/strat w dB. Nie nazywać amplitudy transmisji return loss ani zakładać pełnego zespolonego S21 | P0/P2 / S2, M3, B |
| M03 | Cable Length | VF zachowane w profilu | Edytowalny VF, wybór porównywalnych rezonansów i długość; OPEN/SHORT oraz przykład znanego kabla do walidacji. Instrukcja ostrzega o słabych wynikach do 60 MHz i zaleca rezonanse powyżej tej granicy (w profilu 65 MHz zostaje wąski użyteczny zakres) | P2 / M3 s.5–6, I |
| M04 | Cable Loss | Brak | Zidentyfikować układ pomiaru i algorytm; odróżnić stratę jednokierunkową od pomiaru z odbiciem. Bez arbitralnego dzielenia odczytu przez dwa | P2 / S1–S2, B |
| M05 | Generator | Brak | ON/OFF, zadana częstotliwość i dziesięć pamięci z INI, walidacja zakresu, potwierdzona obsługa sondy dB | P2 / M3 s.9, I, B |
| M06 | Xtal / trapy | Zakładka widoczna tylko w źródle | Najpierw pomiar w FILTER, oznaczenie rezonansów i ręczne Q. Automat parametrów kwarcu jako odrębne rozszerzenie | P2/P3 / M2, M3 s.2–5 |
| F01 | Nazwa/ścieżka pliku w widoku | Tylko komunikat po otwarciu/zapisie | Bieżąca sesja, ostatni plik i informacja o niezapisanym wyniku; oddzielić od źródła i pamięci zakresu | P1 / S1, B |
| F02 | Pliki .vec i CSV dla Z-plot | Własny eksport jednostkowy oraz import własnego CSV i rozpoznanego układu Analyser antenna | Rozpoznane pola Frequency/Return Loss i opcjonalnie Phase są importowane bez dopowiadania brakujących wartości. Brak próbki użytkownika; nie deklarować pełnej zgodności. Binarny .vec wymaga rozpoznania i wzorca | P2 / S1, B
| F03 | File / About | Brak wspólnego menu | Menu operacji sesji i ustawień, About z VNA Studio, SQ7MRU, wersją i stanem zgodności; dostępność funkcji według trybu | P1 / S1–S2 |
| U11 | Pasek trybów Antenna / Cable Loss / Cable Length / Xtal / Filter / Generator | Pasek działa; dane dostępne wyłącznie dla Antenna | Wybrany tryb aktualizuje kontekst. Gdy brak potwierdzonego źródła lub algorytmu, ekran ukrywa wykres z danymi Antenna i wyjaśnia ograniczenie. Nie symuluje przełączenia firmware | P1, zależność E0 / S1 |
| U12 | Controls: Hold, Bands i HF / F/F / VHF | Hold zatrzymuje ciągły cykl po kompletnym sweepie; HF/VHF ustawiają profilowane presety | Wyjaśnić wyłącznie napis `F/F` w oryginalnym obrazie/kodzie przed dodaniem opcji; nie przypisywać mu arbitralnego znaczenia ani nie rozszerzać zakresu urządzenia 65 MHz | P0/P1, zależność E0 / S1–S2, B |

## Najważniejsze rozróżnienia

### Zakres pomiaru, widok i pamięć

Trzy osobne stany: `SweepSettings` określają pomiar; `PlotViewState` określa zoom, widoczność i markery; `SavedSweepRange` przechowuje zapamiętane start/stop. Przywrócenie zakresu nie zastępuje sesji ani nie jest operacją otwierania JSON. M3 opisuje jednopoziomowy Recall związany także z zoomem, ale zasoby 306b mają osobne przyciski pamięci zakresu. W VNA Studio zachowujemy istniejącą wielopoziomową historię i dodajemy jednoznaczne nazwy akcji pamięci.

### Źródło i rzeczywisty COM

Domyślne istniejące demo pozostaje jawnym źródłem do pracy bez urządzenia. Wybranie Urządzenia i otwarcie portu nie oznaczają jeszcze identyfikacji VNA. Gotowość potwierdza udokumentowana odpowiedź protokołu lub pierwszy kompletny skan, jeśli protokół nie oferuje osobnego sprawdzenia. Nie dodawać wymyślonych poleceń identyfikacji. Brak odpowiedzi nie uruchamia demonstratora.

Stan akwizycji: Rozłączony → Łączenie → Gotowy → Skan → Gotowy/Hold, z osobnym stanem błędu. Tylko jeden skan naraz. Kompletna ramka i skan muszą być poprawnie złożone przed prezentacją jako końcowy wynik. Przerwanie i synchronizacja następnego skanu zależą od potwierdzonego protokołu; samo zatrzymanie UI nie uprawnia do ponownego użycia reszty bufora.

### BW i Q

Zrzuty pozwalają potwierdzić szerokość między markerami:

| Źródło | M1 [Hz] | M2 [Hz] | Δf [Hz] | Q pokazane w oryginale |
| --- | ---: | ---: | ---: | ---: |
| S1 | 3 842 000 | 7 148 000 | 3 306 000 | 2 |
| S2 | 18 582 000 | 21 532 000 | 2 950 000 | 7 |

Wartości Q są zgodne z hipotezą zaokrąglonego `((f1+f2)/2)/Δf`, ale dwa obrazy nie dowodzą użytego algorytmu. Nie traktować tej hipotezy jako automatycznego fizycznego Q anteny, filtra lub kwarcu. M2 opisuje metodę `Q=fres/BW` z określonym kryterium fazy i układem pomiarowym. Wdrożenie Q wymaga nazwania metody, źródła f0 i granic pasma; przy braku tych danych wynik ma być niedostępny.

### Tryb urządzenia a narzędzie analizy

Nie zakładać, że każda zakładka wysyła osobne polecenie do firmware. Sterownik udostępnia potwierdzone zdolności pomiaru, a Cable Length/Loss i analiza kwarcu mogą być narzędziami korzystającymi z odbicia lub transmisji. Generator wymaga osobnego kontraktu wyjścia. Tryb FILTER może dostarczać wyłącznie skalarną amplitudę; dostępność fazy transmisji musi wynikać z dowodu.

### Kalibracja i 50 Ω

Przełącznik linii odniesienia 50 Ω nie zmienia impedancji odniesienia `Z0`. RL Offset i CAL należą do potwierdzonych przeliczeń właściwego trybu; zakres dB i RL×2 nie mogą ich ukrycie zastępować. Zmiana trybu unieważnia niezgodną kalibrację. Stałe INI pozostają zapisane nawet wtedy, gdy algorytm ich zastosowania nie jest jeszcze potwierdzony.

## Kolejność realizacji

### Docelowy układ ekranu

Zachować stały nagłówek VNA Studio/SQ7MRU. Pod nim umieścić poziomy wybór sześciu trybów z S1, główny obszar wykresu oraz tabelę wartości M1/M2. Panel boczny grupuje zakres skanu i sterowanie (Start/Stop, liczba punktów, Hold, potwierdzone pasmo), kalibrację właściwą trybowi, markery, widoczność/kolory przebiegów i pamięć zakresu. Przy małym oknie panel przewija się, tabela i wykres dopasowują się do szerokości; żaden tryb nie zakłada stałej geometrii zrzutu 604×379.

Wybór zakładki zmienia kontekst funkcji, nie źródło danych sam z siebie. Antenna korzysta z potwierdzonego pomiaru odbicia. Filter wymaga odrębnego pomiaru transmisji. Cable Loss, Cable Length i Xtal najpierw uruchamiają jawne narzędzie analizy nad danymi odbicia/transmisji. Generator ma osobny stan wyjścia i potwierdzone zdolności sterownika. Gdy dane lub sprzęt nie obsługują trybu, UI wyjaśnia brak możliwości i nie pokazuje nieaktualnych wartości z poprzedniego trybu jako bieżących.

### E0 — kontrakty i referencje

Ustalić protokół odbicia/transmisji/generatora, mapę możliwości urządzenia, znaki fazy/X oraz Xc/Xl. Zebrać fragmenty kodu, odpowiedzi i przykłady plików. Przygotować skalę/ticki z S1/S2 i parametry porównań przed oceną wyniku. To ścieżka P0; jej badanie może trwać równolegle z niezależnym E1.

**Odbiór:** dokument kontraktu z lokalizacją dowodów i listą niepotwierdzonych pól; brak zgadywanych komend sprzętowych. Tabela tej mapy jest macierzą dla zrzutów, nie pełną macierzą wszystkich funkcji vna/J.

### E1 — komplet obsługi istniejących danych

U01–U06, R01–R03, A01, F01/F03: tabela markerów, checkboxy, kroki, minimum, All/None, edycja kolorów, pamięć zakresów, presety, BW, czas/postęp i menu. U07–U10 po wyjaśnieniu odpowiednich semantyk. Wszystko można sprawdzać na sesjach i demonstratorze.

**Odbiór:** dwa poprawnie zsynchronizowane wiersze tabeli; przypadki odwróconych/równych markerów; BW z obu zrzutów; niezależne Save/Recall i historia zoomu; brak granic poza profilem. Dla 50 MHz profil pozwala ustawić zakres w granicach 65 MHz, natomiast 144 MHz jest odrzucane, niezależnie od kategorii VHF.

### E2 — sterownik antenowy i sterowanie źródłem

C01–C03, M01 oraz kalibracja odbicia K01: źródło/port, połączenie, skan pojedynczy i ciągły, Hold, Stop i utrata kabla. Wspólny mechanizm akwizycji z anulowaniem, timeoutem i publikowaniem pełnych skanów. Zachować ostatni kompletny wynik po Hold i oznaczać dane częściowe.

**Odbiór:** zgodne ramki i ADC na zapisach referencyjnych; OPEN/SHORT/50 Ω oraz znane obciążenie; brak nakładania skanów, brak powrotu na demo po błędzie, ponowne połączenie bez restartu. Osobno oznaczyć test symulatora i test egzemplarza użytkownika.

### E3 — FILTER i kalibracja transmisji

M02, K01/K02: osobny model amplitudy transmisji, Through/LOOP 0 dB, stan CAL, offset i zweryfikowane zakresy osi. Aktualizacja skali i tabeli do dostępnych wielkości. Powiązanie kalibracji z profilem, trybem, zegarem i zakresem.

**Odbiór:** kabel Through i znany tłumik/filtr; potwierdzony znak i jednostka dB; brak SWR/R/X pozornie wyliczonych z samej amplitudy transmisji; wygaszenie niezgodnej kalibracji po zmianie trybu.

### E4 — narzędzia specjalistyczne

A02, M03–M06 i potwierdzone U10. Kolejność: ręczne Q z jawną metodą → długość kabla z VF → straty kabla → generator z pamięciami → pomocnik rezonansów kwarcu/trapów. Zaawansowany automat kwarcu jest osobnym rozszerzeniem po uzyskaniu wzorców.

**Odbiór:** znany kabel OPEN/SHORT, znane VF i straty, wzorcowy rezonator; ON/OFF i rzeczywista częstotliwość generatora sprawdzone odpowiednim przyrządem. Generator uruchamia wyjście wyłącznie po jawnej akcji ON; zachowanie podczas rozłączenia musi wynikać z protokołu.

### E5 — pliki, regresja i zgodność

F02 oraz pełna ocena powyższych funkcji. Sesja następnej wersji przechowuje tryb, dostępne wielkości, pochodzenie i status kalibracji oraz kontekst analizy. Odczyt v1 zachowuje istniejące wartości, nie przelicza ich ponownie niepotwierdzoną kalibracją i nie nadaje statusu zweryfikowanego pomiaru sprzętowego. Nieobsługiwane przyszłe wersje pozostają odrzucane.

**Odbiór:** round-trip własnego formatu, CSV z opisanymi jednostkami, zweryfikowane przykłady .vec/CSV legacy; raport zgodności liczb i geometrii dla tych samych danych. Light/Dark/Night Vision, PL/EN, okno 1024×768/przywrócone/maksymalizowane i DPI 100/125/150% pozostają w macierzy kontroli.

## Architektura i zasady realizacji

- `Vna.Core`: obliczenia BW/Q/kabla i jawne modele dostępnych wielkości; kalibracja i analiza niezależne od UI. Wynik zawiera metodę, jednostkę i stan braku danych.
- `Vna.Devices`: zdolności urządzenia, potwierdzony sterownik i lifecycle akwizycji; wszystkie operacje I/O są asynchroniczne i przyjmują CancellationToken.
- `Vna.IO`: wersjonowane sesje, kalibracje, pamięci i import/eksport; zapis atomowy i niezależny od kultury. `IOptions`/`IOptionsMonitor` dla preferencji i profili.
- `Vna.App`: tabela markerów, stany narzędzi, menu i podgląd. Elementy nieobsługiwane mają widoczny powód niedostępności, a nie działające pozornie przyciski.
- Każdy etap kończy się przeglądem zmian, odpowiednimi testami i uzupełnieniem OpenSpec. Obliczenia sprawdzać analitycznie i na referencjach, sprzęt osobno; zrzut UI nie potwierdza dokładności pomiaru.
- Zachować aktualne funkcje: PL/EN, motywy, markery/zoom, stare sesje, nazwa VNA Studio/SQ7MRU i workflow release RID.

## Weryfikacja niejasności przed implementacją

1. Xc/Xl: typ wyniku, jednostka i dostępność znaku X.
2. RL×2: wybór osi 0–30/0–60 jest oparty na odzyskanym handlerze i działa jako ustawienie widoku; porównanie krzywej i etykiet z oryginałem na tych samych próbkach pozostaje wymagane.
3. Q: algorytm kontrolki legacy oraz rozróżnienie wskaźnika markerowego i Q z kryterium rezonansu.
4. Cable Loss: układ podłączenia i jedno-/dwukierunkowość odczytu.
5. CAL: zakres działania, sposób odejmowania referencji oraz unieważnianie przy zmianie trybu.
6. Presety HF/VHF: zatwierdzona konfiguracja zakresów, bez domniemywania obsługi częstotliwości spoza profilu.
7. XTAL: funkcje rzeczywiście obecne w 306b wobec nieoprogramowanej zakładki opisywanej w M3.

Wykres Smitha i dalsze narzędzia vna/J pozostają w dotychczasowym zadaniu 6.3. Nie wynikają z tych dwóch ekranów i nie blokują pierwszego kompletnego przepływu dla VNA 65 MHz.







