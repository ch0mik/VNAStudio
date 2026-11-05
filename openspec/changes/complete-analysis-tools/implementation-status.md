# Stan funkcji względem oryginału

Ta macierz aktualizuje ocenę początkową po wykonaniu etapów implementacji. Tryb może być widoczny w pasku i nadal mieć ograniczenia względem starego programu.

| Przycisk | Działa obecnie w VNA Studio | Ocena i pozostała luka |
| --- | --- | --- |
| **Cable Loss** | Szacuje stratę jednokierunkową jako `−ReturnLossDb / 2`; wymaga danych ze sprzętowego pomiaru Cable Loss oraz świeżego potwierdzenia kabla otwartego/zwartego na ANT. Dane demo i sesje z nieznanym źródłem nie pokazują wyniku. | **Częściowo** — metoda jest oszacowaniem, nie odtworzono pełnej kalibracji/algorytmu oryginalnego programu i nie zweryfikowano jej na kablu fizycznym. Terminator 50 Ω nie jest odpowiednim zakończeniem. |
| **Cable Length** | Wylicza długość z dwóch sąsiednich rezonansów tego samego rodzaju i współczynnika VF; sprawdza próbki i położenie markerów. | **Częściowo** — dokładność nie była sprawdzona na kablu fizycznym. Instrukcja ostrzega o gorszych wynikach poniżej 60 MHz. |
| **Xtal** | Ręcznie liczy Q z częstotliwości rezonansu oraz krawędzi pasma. | **Dodatek VNA Studio** — nie wykrywa automatycznie rezonansu ani nie dopasowuje parametrów kwarcu. Dostarczona instrukcja mówi, że automatyczna zakładka XTAL oryginału nie była zaprogramowana. |
| **Filter** | Oddzielny typ próbek `TransmissionDb`, offset FILTER, wykres amplitudy skalarnej oraz obsługa Demo, sesji i CSV. Sprzętowy skan jest odrzucany przed komunikacją COM. | **Częściowo** — komenda trybu nie jest potwierdzona dla VNA 1–65 MHz; nie ma zweryfikowanej fazy ani skalibrowanego zespolonego S21. Wymaga testu właściwym fixture ANT–WOB. |
| **Generator** | Wczytuje dziesięć pamięci profilu, pozwala wybrać/edytować częstotliwość i sprawdza zakres. | **Częściowo** — komenda sprzętowa jest wyłączona. Dekompilacja pokazała fragment `&OFF`, ale nie pełny protokół start/stop/ustawiania częstotliwości. Aplikacja nie wysyła komend, a fizyczny stan wyjścia jest nieznany. |

## Plan domknięcia pozostałych luk

1. **Cable Loss:** dekompilacja nie odzyskała wzoru ani kalibracji; VNA Studio zachowuje `−ReturnLossDb / 2` tylko jako jawnie nowy estymator, bez roszczenia do zgodności z oryginałem. Nadal trzeba porównać go na kablu fizycznym z otwartym/zwartym końcem i znanym tłumieniem.
2. **Cable Length:** pomierzyć kabel o znanym współczynniku skrócenia i długości, zapisać błąd w funkcji pasma oraz potwierdzić praktyczną granicę dokładności dla VNA 1–65 MHz.
3. **Filter:** przegląd zachowanego handlera `VB6_00425e1d` nie odzyskał komendy: pseudokod handlera nie powstał (timeout), a asembler i odwołania do tekstów potwierdzają wybór trybu/kontrolki, nie kompletną ramkę komunikacyjną. Wartość `1=Wobuloscope` pochodzi z pokrewnego MAX4 i nie potwierdza wariantu VNA 1–65 MHz. Potwierdzić komendę na dokładnym modelu, a następnie podłączyć przewód referencyjny ANT–WOB i filtr między portami. Do tego czasu sprzętowy skan pozostaje zablokowany.
4. **Generator:** handler `VB6_0042399d` odzyskano jako pseudokod, ale widoczna logika dotyczy przełączania stanu i tekstu przycisku (`&OFF`), bez dowodu na komendy szeregowe ON/OFF lub programowanie częstotliwości. Sprawdzić oryginalny handler/firmware lub wiarygodny protokół dla tego wariantu; dodać testy ramek i bezpieczną weryfikację na właściwym obciążeniu przed odblokowaniem.
5. **Xtal:** zachować jako ręczną pomoc, nie przedstawiać go jako portu nieistniejącej automatyki legacy.

Aktualne podłączenie użytkownika — terminator 50 Ω na ANT i wolne WOB — pozwala na bazowy pomiar odbicia. Nie jest fixture dla Cable Loss ani Filter. Nie należy uruchamiać generatora, dopóki komenda wyjściowa i obciążenie nie będą potwierdzone.

Szczegóły metod: [Cable Loss](../../../docs/CABLE-LOSS.pl.md), [Cable Length](../../../docs/CABLE-LENGTH.pl.md), [Filter](../../../docs/FILTER-MODE.pl.md), [Generator](../../../docs/GENERATOR.pl.md) oraz [Q rezonansu](../../../docs/RESONANCE-Q.md). Dowód, że automatyka XTAL była pusta: `openspec/changes/port-vna-to-csharp/reviews/2026-11-04-resonance-q.md`. Ograniczenia zdekompilowanego handlera trybów: `openspec/changes/port-vna-to-csharp/reviews/2026-11-04-targeted-mode-handler.md`.
