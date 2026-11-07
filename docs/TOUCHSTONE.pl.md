# Import i eksport Touchstone

Biblioteka `Vna.IO` odczytuje i zapisuje podzbiór Touchstone v1.x dla parametrów S i plików jedno- oraz dwuportowych (`.s1p` i `.s2p`). Aplikacja nie udostępnia jeszcze poleceń importu/eksportu w interfejsie. Próbki są przechowywane jako wartości zespolone, z częstotliwością w Hz i jednym jawnym, wspólnym oporem odniesienia (`Zref`). Program nie wykonuje renormalizacji impedancji.

Czytnik obsługuje formaty zespolone `RI`, `MA` i `DB`, jednostki częstotliwości `Hz`, `kHz`, `MHz` lub `GHz`, komentarze zaczynające się od `!` oraz wspólny opór z opcji `R`. Gdy wartości w linii opcji są pominięte, stosuje domyślne wartości v1: GHz, parametry S, MA i 50 omów. Referencje per port są przyjmowane tylko wtedy, gdy ich wartości liczbowe są równe, ponieważ obecny model przechowuje jedno wspólne `Zref`; zgodnie z Touchstone v1.1 wielowartościowy wpis `R` musi być ostatni w linii opcji.

Zapis tworzy nagłówek `# Hz S RI R <Zref>`. Wartości S2P zapisuje w kolejności v1: `S11, S21, S12, S22`. Dla danych jednoportowych użyj rozszerzenia `.s1p`, a dla dwuportowych `.s2p`.

Pliki Touchstone 2.0/2.1 z sekcjami słów kluczowych, parametry inne niż S, tryb mieszany, różne referencje portów i więcej niż dwa porty są odrzucane. Limit wynosi 1 000 000 próbek. Skalarna amplituda transmisji nie jest zamieniana na S21 ani inny parametr zespolony.

Domyślne wartości linii opcji, jednostki i formaty częstotliwości, komentarze oraz kolejność parametrów dwuportowych są zgodne ze [specyfikacją Touchstone 2.1 IBIS Open Forum](https://ibis.org/touchstone_ver2.1/touchstone_ver2_1.pdf). Specyfikacja opisuje też słowa kluczowe zależne od wersji i referencje per port, których ten adapter jeszcze nie obsługuje.
