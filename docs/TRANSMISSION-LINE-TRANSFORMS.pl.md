# Transformacje linii transmisyjnej: projekt i warunki

**Status: tylko projekt.** VNA Studio nie dodaje obecnie ani nie usuwa wpływu linii transmisyjnej.

## Dopuszczalne dane wejściowe

Transformacja działa na zespolonych próbkach `NetworkDataSet` z plików Touchstone S1P/S2P lub z przyszłego pluginu, który jawnie dostarcza zespolone parametry S o znanym znaczeniu. To warunek konieczny, ale niewystarczający do korekcji fizycznej. Skalarna amplituda FILTER nie jest zespolonym S21. Próbki odbiciowe VNA z niepotwierdzonym znakiem fazy/reaktancji nie wystarczają do zbudowania zespolonego S11.

Obecny `NetworkDataSet` nie zapisuje stanu kalibracji. Przyszły widok/transformacja musi więc otrzymywać jawne pochodzenie kalibracji i korekt zarówno dla zbioru DUT/źródła, jak i dla zmierzonego fixture; aplikacja nie może wnioskować o kalibracji z rozszerzenia pliku ani samej obecności wartości zespolonych. Transformacja nie zastępuje kalibracji pomiaru. Źródło i zmierzony fixture jawnie oznaczone jako skalibrowane/skorygowane mogą dać fizycznie skorygowany wynik tylko wtedy, gdy oba zapisy kalibracji i model linii są właściwe dla tych samych portów, płaszczyzn odniesienia i zakresu częstotliwości. Nieskalibrowany operand może być użyty wyłącznie do jawnie eksperymentalnego wyniku matematycznego, a nieznany stan kalibracji musi pozostać widoczny jako nieznany; żadnego z tych wyników nie wolno przedstawiać jako skalibrowanej korekcji sprzętu. Keysight również opisuje przesunięcie płaszczyzny odniesienia jako obróbkę po korekcji pomiaru w [przeglądzie regulacji płaszczyzny odniesienia](https://helpfiles.keysight.com/csg/N1930xB/ToolsAndUtilities/Removing_Unwanted_Effects_from_the_Measurement.html).

Każdy zbiór danych sieciowych przechowuje własną, skończoną i dodatnią impedancję odniesienia portu `Zref`. Model linii osobno przechowuje impedancję charakterystyczną `Zc` i parametry propagacji. `Zc` nie może być po cichu zastąpione przez `Zref`; każda renormalizacja impedancji musi być jawna.

## Model linii

Pierwsza implementacja powinna przyjmować jeden z dwóch wariantów:

- zmierzony dwuportowy plik Touchstone linii/fixture z określonym kierunkiem portów i zgodnymi częstotliwościami próbek albo
- jawnie wybrany model jednorodnej linii z długością fizyczną oraz udokumentowanym modelem propagacji i tłumienia.

Idealne przedłużenie płaszczyzny odniesienia opisuje wyłącznie dopasowaną, jednorodną linię przy założeniu stałej impedancji, liniowej fazy i płaskiej amplitudy. Korekcja niedopasowania lub strat wymaga dokładnego modelu fixture albo zmierzonych parametrów S fixture. Zobacz dokumentację Keysight o [przesuwaniu płaszczyzny odniesienia](https://helpfiles.keysight.com/csg/N1930xB/ToolsAndUtilities/Removing_Unwanted_Effects_from_the_Measurement.html) oraz [zależność macierzy S dla jednorodnej linii](https://edadownload.software.keysight.com/eedl/ads/2012_08/pdf/em.pdf).

## Kontrakt transformacji

- Udostępnić jawne operacje **dodaj linię** i **usuń linię**, z kierunkiem i długością dla każdego portu.
- Dla zmierzonego fixture używać kaskadowania/odwracania kaskady pełnego zespolonego modelu; nie sprowadzać działania do samego przesunięcia fazy, gdy model uwzględnia niedopasowanie lub straty.
- Zachować źródłowy zbiór i jego siatkę próbek. Wynik pochodny jest nową, niezmienną wartością. Nie interpolować po cichu między niezgodnymi siatkami fixture i pomiaru.
- Jawnie zachowywać `Zref` wyniku i oddzielać je od `Zc` linii.
- Odrzucać nieskończone lub niepoprawne parametry modelu, niezgodną liczbę portów, niezgodne siatki częstotliwości oraz punkty osobliwe/źle uwarunkowane, podając zlokalizowany komunikat.

## Pochodzenie

Każdy wynik pochodny ma identyfikować zbiór źródłowy i jego hash gdy jest dostępny, stan i pochodzenie kalibracji/korekt źródła, a także — jeśli użyto — zmierzony zbiór fixture, jego hash oraz stan i pochodzenie kalibracji/korekt. Należy też zachować kierunek transformacji, mapowanie portów, wersję/model linii, `Zc`, `Zref`, długość, parametry propagacji/tłumienia i czas utworzenia. Rekord pochodny ma odróżniać fizyczną korekcję skalibrowanego pomiaru od eksperymentalnego przekształcenia matematycznego. Eksport do Touchstone zachowuje `Zref` zbioru pochodnego; źródło i fixture pozostają dostępne bez zmian.

## Warunek walidacji

Przed udostępnieniem transformacji w interfejsie należy przygotować niezmienne wektory wejścia/wyjścia z niezależnego wzorca analizy sieci i sprawdzić oba kierunki dla dopasowanej linii bezstratnej, różnego `Zc`/`Zref`, linii stratnej i przypadków osobliwych. Następnie porównać de-embedding zmierzonego fixture z niezależnym fixture lub skalibrowanym wzorcem oraz sprawdzić, czy pochodzenie kalibracji DUT i fixture jest zachowane od wejścia do wyniku. Do tego czasu projekt pozostaje udokumentowany, ale nie deklaruje się implementacji transformacji ani walidacji dla sprzętu VNA 1–65 MHz.

Powiązana implementacja: [import/eksport Touchstone](TOUCHSTONE.pl.md), [zespolone dane sieciowe](../src/Vna.Core/NetworkDataSet.cs).
