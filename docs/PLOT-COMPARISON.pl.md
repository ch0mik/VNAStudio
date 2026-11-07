# Uwagi z porównania wykresów

To porównanie strukturalne dostarczonego zrzutu starszej aplikacji z rodziny VNA w wersji V1.3.2 (`original-page2-R19.png`) z renderami Avalonia headless VNA Studio (`minimum-1024x768.png` / `plot-649.png`). Jak wyjaśniono w [PLOTS.md](../PLOTS.md), zrzut starszej rodziny nie potwierdza dokładnego układu kontrolek Analyser 306b. Oryginalne obrazy i binaria aplikacji pozostają w sąsiednim katalogu badawczym `VNA-ReverseEngineering`; obrazy headless w ignorowanym katalogu `bin` projektu testowego są lokalnie wygenerowane.

| Element | Zrzut starszej aplikacji (V1.3.2) | Render VNA Studio |
| --- | --- | --- |
| Przebiegi | Sześć kolorowych wielkości w jednym polu wykresu z osobnymi, opisanymi skalami. | Sześć niezależnie włączanych przebiegów; każda wielkość ma własną skalę i odpowiadające jej etykiety przy krawędziach. |
| Markery | Czerwony M1 i niebieski M2 jako pionowe linie z etykietami pod przebiegiem. | Czerwone i niebieskie linie M1/M2, można je przeciągać lub ustawić kliknięciem; odczyty są w tabeli pod wykresem. |
| Układ wykresu | Zrzut pokazuje kompaktowy wykres pośrodku widocznego okna aplikacji. | Wykres dopasowuje się do okna obok przewijanego panelu ustawień skanu; minimalny rozmiar to 1024×768 DIP. |
| Sterowanie i wyniki | Przyciski trybów nad wykresem, tabela markerów pod nim, ustawienia skanu i kolory przebiegów na dole. | Tryby analizy są nad wykresem; sterowanie zoomem/zakresem i porównaniem znajduje się między paskiem trybów a wykresem; ustawienia są w panelu bocznym, a podsumowania markerów pod wykresem. |
| Pochodzenie danych | Wartości i przebiegi widoczne na dostarczonym zrzucie; surowy pomiar i jego pochodzenie nie są dostępne. | `minimum-1024x768.png` zawiera pięć punktów z `TestData.Session()`; `plot-649.png` zawiera 649 syntetycznych punktów `DemoVnaDevice` z testu wydajności. Żaden z wygenerowanych obrazów nie pochodzi z urządzenia. |

Dostarczony zrzut i wygenerowane rendery mają różne wymiary, dane, typografię i układ kontrolek. Do oryginalnego zrzutu nie dołączono pasującego surowego pomiaru/pliku sesji, a oryginalnego programu nie uruchamiano w celu wykonania kontrolowanego drugiego zrzutu. Porównanie potwierdza więc odpowiedniki koncepcji wykresu i opisuje różnice układu; nie potwierdza zgodności pikselowej, położenia identycznych krzywych, reguł ticków dla każdego zakresu ani dokładności fizycznego pomiaru.

Obsługę i lokalny pomiar renderera headless opisuje [instrukcja wykresów](PLOT-GUIDE.pl.md). Zgodnie z ustalonym podziałem zrzuty źródłowe są archiwizowane poza kodem VNA Studio.
