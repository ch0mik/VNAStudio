# Migawki porównawcze

VNA Studio może zapisać kompletny pomiar jako nazwaną migawkę `.vnasnapshot.json` i wczytać jedną lub więcej takich migawek jako nakładki na wykresie Smitha i częstotliwościowym. Jest to funkcja offline: zapisanie lub otwarcie migawki nie łączy się z urządzeniem ani nie otwiera portu szeregowego.

## Dane i pochodzenie

Migawka zawiera zwalidowaną sesję pomiarową. Zachowuje dokładne częstotliwości i wartości zarejestrowanych próbek, ustawienia skanu, źródło, identyfikatory pluginu/profilu, metadane prezentacji profilu, tryb analizy, rodzaj danych, konwencję siatki częstotliwości, czas pomiaru oraz dostępne metadane kalibracji i pochodzenia. Osobny, skończony offset wyświetlania RL zapisuje widok wykresu; nie zmienia próbek sesji ani nie jest kalibracją. Wczytanie migawki korzysta z tej samej walidacji i migracji co otwarcie pliku `.vnasession.json`.

Migawka w pamięci kopiuje próbki i kolekcje metadanych do kolekcji tylko do odczytu. Nie zmienia pomiaru źródłowego. Migawki o różnej liczbie punktów lub różnych siatkach częstotliwości są rysowane z własnych zapisanych próbek; wykres ich nie próbkowuje ponownie ani nie interpoluje.

## Rysowanie na wykresie Smitha

Bieżący skan pozostaje głównym przebiegiem. Na wykresie Smitha migawki są rysowane pod nim liniami przerywanymi i używają własnego, zapisanego `Zref`. Jeśli istnieje neutralny descriptor profilu, jego `Zref` jest rozstrzygający także wtedy, gdy jest nieznany; wykres nie używa wtedy starszej migawki profilu ani impedancji aktywnego urządzenia. Niejednoznaczny znak reaktancji nadal pokazuje oba sprzężone warianty z różnymi wzorami linii. Na wykresie częstotliwościowym wybierz jedną wielkość do porównania; migawki są rysowane pod aktywnym przebiegiem i każda ma własny kolor. Obie nakładki stosują offset RL zapisany dla migawki, a sesja źródłowa pozostaje bez zmian. Skalarne sesje transmisji nie są przyjmowane do tych widoków odbiciowych.

Gdy nie ma aktywnego skanu, podpis wykresu Smitha opisuje impedancję odniesienia porównań jako właściwą dla każdego przebiegu, zamiast pokazywać `Zref` aktywnego profilu. Dymek migawki podaje impedancję właściwą dla danego przebiegu. Odrzucane są również starsze sesje FILTER z nieznanym rodzajem danych, ponieważ mogą zawierać skalarne wartości transmisji bez zapisanej informacji o ich znaczeniu.

Wpisz nazwę i wybierz **Zapisz bieżący**, aby utworzyć plik migawki. Przycisk **Otwórz migawki** wczytuje wiele plików; pola wyboru pokazują lub ukrywają poszczególne przebiegi. **Wyczyść** usuwa bieżące nakładki z widoku; zapisane pliki można ponownie otworzyć.

Lista nakładek nie jest obecnie zapisywana jako globalne ustawienie aplikacji. Po ponownym uruchomieniu VNA Studio można wczytać migawki z plików. Ta funkcja nie obejmuje transformacji linii ani konwersji skalarnych danych FILTER do zespolonych parametrów S; projekt z warunkami jest w [TRANSMISSION-LINE-TRANSFORMS.pl.md](TRANSMISSION-LINE-TRANSFORMS.pl.md).
