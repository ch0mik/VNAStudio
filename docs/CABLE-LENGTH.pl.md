# Szacowanie długości kabla

Instrukcja opisuje pomiar kabla otwartego lub zwartego na końcu. Markery należy ustawić na sąsiednich maksimach albo na sąsiednich minimach tego samego rodzaju. Instrukcja ostrzega, że pomiar rezonansów przy częstotliwości do 60 MHz może być niedokładny, a zalecany obszar powyżej 60 MHz jest w tym urządzeniu wąski. Współczynnik skrócenia VF trzeba wyznaczyć dla danego kabla lub wziąć z jego specyfikacji. Wartość domyślna profilu to 0,66; przykłady instrukcji to około 0,63 dla skrętki LAN i 0,50 dla przedłużacza ogrodowego.

VNA Studio szacuje długość ze wzoru:

```text
długość = VF × c / (2 × |f2 − f1|)
```

gdzie `c` oznacza prędkość światła. Czynnik 2 uwzględnia drogę sygnału w obie strony. Instrukcja opisuje sposób pomiaru i VF, lecz nie podaje wzoru; wynika on z okresu fazy odbicia w linii.

Aby użyć narzędzia, ustaw M1 i M2 na wykresie Antenna na dwóch sąsiednich rezonansach tego samego rodzaju, wybierz **Cable Length** i sprawdź albo zmień VF. VF jest zapisywany w ustawieniach. Ostrzeżenie o 60 MHz jest wskazówką, a nie twardym ograniczeniem programu. To nie jest pomiar strat kabla ani potwierdzenie kalibracji urządzenia.

Program weryfikuje, czy oba markery są nie dalej niż dwie próbki skanu od lokalnego maksimum lub minimum SWR, czy wybierają ten sam rodzaj ekstremum oraz czy pomiędzy nimi nie leży kolejny rezonans tego rodzaju. Do wykrycia położenia rezonansu służy średnia z trzech próbek; długość jest liczona z częstotliwości markerów, bez interpolacji. Szum nadal może tworzyć lokalne ekstrema, dlatego wynik pozostaje szacunkiem i warto porównać go ze znanym kablem.

Źródło: instrukcja pomiarowa dostarczona dla urządzenia, strony 5–6. Oryginalne pliki instrukcji i profile pozostają poza katalogiem projektu.
