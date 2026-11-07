# Ręczny kalkulator Q rezonansu

Tryb Xtal zawiera ręczny kalkulator korzystający z metody opisanej w dostarczonej instrukcji rezonansowej:

    Q = Fres / (Fh − Fl)

Dla procedury rezonansu równoległego instrukcja zaleca ustawić dolny i górny marker na fazie 45° po obu stronach rezonansu. Wpisz `Fres` oraz odpowiadające im częstotliwości dolnej i górnej granicy pasma w MHz. Kalkulator sprawdza dodatnie, uporządkowane granice oraz zakres do 65 MHz. Nie wyszukuje rezonansu na wykresie i nie potwierdza, że wpisane granice spełniają kryterium fazy.

Przykład z instrukcji: `Fres = 14,400 MHz`, `Fl = 14,312 MHz`, `Fh = 14,4816 MHz`, co daje `Q ≈ 84,9`. Ten kalkulator jest oddzielony od podpowiedzi Q wyznaczanej z markerów na zrzutach ekranu.

Źródło: dostarczona przez użytkownika instrukcja pomiaru rezonatorów, strona 2. W instrukcji wskazano też, że automatyczna zakładka XTAL nie była zaprogramowana. VNA Studio udostępnia więc kalkulator ręczny, a nie automatyczne wyznaczanie parametrów zastępczych kwarcu.

Automatyczne wyszukiwanie rezonansu, wyznaczanie parametrów zastępczych kwarcu oraz analiza RLC/trapów/transformatorów/IF nie są zaimplementowane. Nie należy ich włączać jako funkcji pomiarowych przed walidacją dokładnego fixture/modelu, zachowania fazy i częstotliwości oraz niezależnych wektorów referencyjnych. Przyszły wynik eksperymentalny musi być tak oznaczony i zachowywać dane źródłowe.
