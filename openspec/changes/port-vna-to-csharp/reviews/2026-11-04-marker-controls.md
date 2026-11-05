# Przegląd sterowania markerami i przebiegami

Dodano niezależne checkboxy widoczności M1/M2, strzałki poprzednia/następna próbka i nawigację lewo/prawo po wskazaniu markera na wykresie. Ruch zatrzymuje się na granicach danych i bieżącego zoomu. Ukrycie elementu graficznego nie usuwa częstotliwości markerów, danych tabeli ani próbek sesji. Nazwy i podpowiedzi mają zasoby PL/EN. Dodano również polecenia Wszystkie/Żadne dla sześciu przebiegów.

Weryfikacja: pełna bateria `dotnet test Vna.slnx --configuration Release --no-restore -p:UsedAvaloniaProducts=` — 61/61 zaliczonych. Test sprawdza ruch z klawiatury, granice, zoom, zachowanie pozycji markerów, próbki i maski widoczności. Podgląd jasnego motywu sprawdzony; kontrolki pozostają w przewijanym panelu przy minimalnym rozmiarze.
