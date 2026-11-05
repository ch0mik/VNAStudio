# Plan przeniesienia vna/J 3.4.8 do C#

> Aktualny plan jest prowadzony w OpenSpec: [propozycja](openspec/changes/port-vna-to-csharp/proposal.md), [projekt](openspec/changes/port-vna-to-csharp/design.md), [zadania](openspec/changes/port-vna-to-csharp/tasks.md) i wymagania w `specs/` tej zmiany. Poniższy dokument zachowuje wcześniejsze ustalenia i materiały odniesienia.

## Cel i stan analizy

Docelowo samodzielna aplikacja C# odtwarzająca funkcje vna/J 3.4.8. Pierwszy działający etap obejmuje jeden model miniVNA; następne etapy rozszerzają zgodność do uzgodnionej listy funkcji i urządzeń oryginału.

Zbadano oryginalny pakiet https://download.dl2sba.com/vnaj/3.4.8/ oraz kod https://github.com/pa7n/miniVNA/blob/master/frmmain.pas.

- Oryginał i dokumentacja są w `../reference/vnaj-3.4.8/`.
- JAR zawiera 656 plików klas; spis: `jar-entries.txt`.
- Punkt wejścia: `krause.vna.gui.VNAMain`; interfejs Swing, biblioteka szeregowa purejavacomm, biblioteki wykresów i obliczeń w zagnieżdżonych JAR.
- Widoczne osobne sterowniki m.in. std, std2, pro i pro2 oraz klasy kalibracji i przeliczania próbek.
- SHA256 JAR: `101BE57C1984D2206CCF5623B5F71FDF80B58E244141EA6378E0DFB741FE3E70`.
- To inwentaryzacja archiwum, nie zakończona dekompilacja. Implementacja C# jeszcze nie powstała.

## 1. Odtworzenie kodu i specyfikacji

Użyć dekompilatora Java, np. CFR, i zachować wersję narzędzia oraz polecenia pozwalające powtórzyć wynik. Dekompilować kod aplikacji oddzielnie od zależności. Zachować zasoby, konfiguracje sterowników i dokumentację. Sporządzić raport błędów dekompilacji; trudne metody sprawdzić przez `javap -c -p`, a w razie potrzeby drugim dekompilatorem.

W pierwszej kolejności przeanalizować IVNADriver, VNADriverSerialBase, wybrany sterownik i jego MathHelper, VNACalibrationContext, VNACalibrationBlock oraz klasy surowych i skalibrowanych próbek. Wynik: opis ramek, kolejności bajtów, jednostek, zakresów, trybów skanowania, kalibracji, interpolacji i formatów plików.

Utworzyć macierz funkcji na podstawie klas, zasobów i zachowania uruchomionego oryginału: pomiary odbicia i transmisji, kalibracja, wykresy, markery, import/eksport, narzędzia dodatkowe i obsługiwane urządzenia. Każda pozycja otrzymuje status oraz kryterium zgodności. Nie zakładać, że wszystkie modele mają protokół podstawowego miniVNA.

## 2. Architektura C#

Decyzja użytkownika: Avalonia UI z własną kontrolką wykresu. Pierwsze wdrożenie dotyczy Windows; wydania Linux/macOS wymagają osobnej weryfikacji. Docelowe wersje .NET i Avalonia wybrać przed implementacją według zainstalowanego SDK i wspieranych wydań.

- `Vna.Core`: jednostki, próbki, liczby zespolone, kalibracja, interpolacja i obliczenia RF; bez zależności od UI i portów.
- `Vna.Devices`: kontrakt sterownika, transport szeregowy, implementacje dla poszczególnych modeli, timeouty i anulowanie.
- `Vna.IO`: pliki pomiarów, kalibracji i formaty eksportu potwierdzone w analizie.
- `Vna.App`: MVVM, konfiguracja, wykresy, markery i komunikaty błędów.
- `Vna.Tests`: wektory referencyjne z Javy i symulator transportu.

Dekompilacja Java jest materiałem wejściowym; przejście do C# wymaga przeprojektowania Swing, obsługi wątków i komunikacji sprzętowej.

## 3. Pierwszy działający zakres

Najpierw jeden potwierdzony model urządzenia: wybór portu, parametry skanu, pojedynczy i ciągły pomiar, przerwanie, odłączenie i ponowne połączenie. Następnie kalibracja właściwa temu sterownikowi, przeliczenie próbek, wykresy SWR/RL/fazy/R/X oraz markery. Dodać tryb demonstracyjny oparty na zapisanych danych, aby rozwijać UI bez sprzętu.

Kod PA7N pokazuje 115200 baud, 8N1, cztery parametry tekstowe zakończone CR i odpowiedź o rozmiarze `liczba_próbek * 4`. To hipoteza dla zgodnego modelu do porównania ze sterownikiem vna/J i urządzeniem. Nie przenosić bez sprawdzenia uproszczonej kalibracji PA7N ani jego założeń o fazie i znaku reaktancji.

## 4. Sprawdzenie zgodności

Zbudować wektory porównawcze Java/C# dla identycznych surowych próbek i kalibracji. Ustalić tolerancje według rozdzielczości urządzenia i algorytmów; sprawdzić końce zakresu, dzielenie przez zero, dopasowanie 50 Ω, OPEN/SHORT/LOAD oraz interpolację. Porównać częstotliwości punktów, wartości zespolone i wielkości pochodne.

Transport sprawdzić dla fragmentowanych odpowiedzi, timeoutów, nadmiarowych bajtów, anulowania i utraty połączenia. Zgodność ze sprzętem potwierdzić pomiarami wzorców i tego samego badanego obiektu w obu aplikacjach. Symulator nie potwierdza zgodności sprzętowej.

## 5. Rozszerzenie do pełnego programu

Po zgodności pierwszego sterownika realizować kolejne pozycje macierzy: dalsze urządzenia i tryby, wykres Smitha, formaty plików i narzędzia obecne w oryginale. Zgodność istniejących plików kalibracji zadeklarować dopiero po odczycie formatu i testach. Każdy etap kończyć działającą kompilacją i raportem pozostałych różnic. Pełnego portu nie uznawać za gotowy na podstawie samego pierwszego zakresu.

## Licencje i decyzje do ustalenia

`lizenz.html` z JAR deklaruje CC BY-NC-ND, w tym wariant 3.0 Polska. Warunki wykorzystania i rozpowszechniania pochodnego portu wymagają wyjaśnienia z autorem przed publikacją; ten plan nie stanowi oceny prawnej. W przejrzanym pliku PA7N jest informacja copyright, a główny widok repozytorium nie pokazuje pliku LICENSE: warunki kopiowania kodu również trzeba ustalić. Osobno zinwentaryzować licencje zależności.

Najważniejsze informacje od użytkownika: wersja firmware, dostępność sprzętu do testów, dalsze docelowe systemy oraz priorytet zgodności funkcji. Przyjęto Avalonia UI i własną kontrolkę wykresu, pozostawiając architekturę na dalsze sterowniki.

## Ustalenia z pakietu użytkownika — 2026-11-02

Przejrzano lokalne materiały urządzenia przekazane do analizy: instrukcję instalacji, dokumenty pomiarowe, `analyz.ini` i `SETUP.LST`. Tekst PDF zapisano w `../reference/hardware/manual-*.txt`. Nie uruchamiano instalatorów ani programów EXE.

Pakiet wskazuje na konstrukcję VNA z rodziny IW3HEV / modyfikacji SP3SWJ i SP7DPT, obsługiwaną przez Analyser 306b. Nie ustalono dokładnej rewizji sprzętu. Instrukcja nakazuje używać `analyser306b.exe`; miniVNA230 służy jedynie instalacji zależności VB6. Sama obecność miniVNA230 nie identyfikuje sprzętu jako fabrycznego miniVNA Standard.

Potwierdzone ustawienia dostarczonego INI (nie są odczytem aktualnego urządzenia):

| Parametr | Wartość |
|---|---|
| Zakres | 1–65 MHz |
| DDS_CLOCK | 191993220 Hz |
| Transport | RS232 przez FT232 / wirtualny COM |
| Numer portu w pliku | COM5 — ustalić rzeczywisty przy połączeniu |
| VNA_type | 1, opisane jako miniVNA lub VNA 40 dB |
| Liczba próbek Champ | 649 |
| Impedancja odniesienia | 50 Ω |
| Współczynnik skrócenia kabla | 0,66 |

Komentarze INI łączą zegar około 192 MHz z AD9851 i oscylatorem 32 MHz ×6. To wskazówka co do sprzętu, nie potwierdzenie typu układu. Dokumentacja pomiarowa opisuje tryby anteny, FILTER i generatora oraz pomiar kabli.

Pierwszy sterownik C# ma obsługiwać ten wariant 65 MHz, z konfigurowalnym zegarem DDS i skalowaniem ADC z INI. Przed wyborem sterownika std/std2 vna/J porównać protokół Analyser 306b, PA7N i zdekompilowanych sterowników. Zweryfikować prędkość portu, komendy przełączania trybów, pakowanie danych ADC oraz obsługę przekaźnika. Nie przyjmować automatycznie zegara 400 MHz ani zgodności komend fabrycznego miniVNA.

## Analiza lokalnego oprogramowania VB6

Na żądanie użytkownika wykonano częściową dekompilację Analyser 306b i miniVNA230 z lokalnego pakietu. Oba są PE32 x86 / VB6 native. CAB rozpakowano bez instalowania. Uzyskano pseudokod dla 74/78 i 135/142 wykrytych kandydatów procedur VB6; pozostałe fragmenty zachowano w asemblerze. Wyniki, ograniczenia, skrypty powtórzenia oraz raporty pokrycia są w `../decompiled/README.md`; robocza specyfikacja w `../decompiled/SPEC.md`. Materiały referencyjne i wyniki dekompilacji znajdują się poza katalogiem `VnaStudio`, w katalogu nadrzędnym. To osobny etap względem niezrealizowanej jeszcze dekompilacji Java vna/J.
