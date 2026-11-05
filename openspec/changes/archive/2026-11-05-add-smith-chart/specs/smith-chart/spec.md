# Spec Delta

## ADDED Requirements

### Requirement: Dodatkowa reprezentacja pomiaru antenowego

System SHALL udostępniać widoki Frequency, Smith i Both dla pomiaru Antena, wykorzystując ten sam zbiór danych bez dodatkowej akwizycji.

#### Scenario: Przełączenie widoku

- **WHEN** użytkownik przełącza ukończony pomiar na Smith lub Both
- **THEN** próbki i markery pozostają zachowane, a urządzenie nie otrzymuje nowego polecenia skanu

### Requirement: Poprawna konwersja i odniesienie impedancji

System SHALL obliczać Γ z podpisanego S11 i fazy lub pełnej impedancji zespolonej, z Z0 pochodzącym z migawki pomiaru. System SHALL stosować korekty wyświetlania tak samo w obu reprezentacjach, bez powtórnego naliczania.

#### Scenario: Odtworzenie sesji 75 Ω

- **WHEN** otwierana jest sesja z Z0=75 Ω przy aktywnym profilu 50 Ω
- **THEN** wykres i odczyty używają 75 Ω i pokazują tę wartość odniesienia

#### Scenario: Brak historycznego odniesienia impedancji

- **WHEN** starsza sesja nie zawiera migawki ani wiarygodnego Z0
- **THEN** system pokazuje Z0 nieznane, nie przejmuje wartości bieżącego profilu i pozwala jedynie na widok Γ z S11/fazy bez niepotwierdzonych odczytów impedancji w omach

### Requirement: Jawna niepewność znaku fazy

System SHALL zachowywać status i pochodzenie wiarygodności fazy. Dla fazy niejednoznacznej SHALL pokazywać dwa sprzężone rozwiązania, komunikat niepewności i odczyt R ± j|X|, bez rozstrzygania charakteru L/C. Dane bez użytecznej fazy SHALL nie tworzyć pozornego śladu.

#### Scenario: Pomiar obecnego urządzenia

- **WHEN** dane pochodzą z VNA 1–65 MHz bez potwierdzonej orientacji fazy
- **THEN** widoczne są oznaczone warianty lustrzane, a dodatnia liczba fazy nie nadaje statusu Verified

#### Scenario: Starsza sesja

- **WHEN** sesja v1/v2 albo import nie podaje wiarygodności znaku
- **THEN** dane otrzymują status Ambiguous lub Missing, a ponowny zapis nie podnosi ich wiarygodności

### Requirement: Geometria i jakość danych

System SHALL rysować wektorową siatkę impedancji w kole o zachowanych proporcjach. Invalid/Missing i punkty spoza zakresu SHALL przerywać przebieg; system SHALL nie zmieniać |Γ|>1 na 1 i SHALL bezpiecznie obsługiwać osobliwość Γ=1.

#### Scenario: Luka w danych

- **WHEN** między poprawnymi próbkami występuje próbka niepoprawna lub poza zakresem
- **THEN** ślad zostaje przerwany, a licznik lub komunikat wskazuje pominięte dane

### Requirement: Wspólne markery i rozdzielone rodzaje zoomu

System SHALL współdzielić indeksy M1/M2 i zakres częstotliwości między wykresami. Wybór markerów SHALL uwzględniać transformację ekranu i rzeczywiste próbki. Geometryczny zoom/pan Smitha SHALL pozostawać oddzielony od zakresu skanu.

#### Scenario: Marker po powiększeniu

- **WHEN** użytkownik powiększa i przesuwa Smitha, po czym wybiera próbkę
- **THEN** oba wykresy pokazują ten sam marker, a częstotliwości skanu nie zmieniają się wskutek geometrycznego zoomu

### Requirement: Integracja z aplikacją i zapamiętywanie

System SHALL obsługiwać PL/EN, Light/Dark/Night Vision, dynamiczny rozmiar okna od 1024×768 oraz klawiaturę. System SHALL zapamiętywać rodzaj widoku w ustawieniach i zachowywać status fazy w sesji v3, z odczytem v1/v2.

#### Scenario: Ponowne uruchomienie

- **WHEN** użytkownik zamyka aplikację po wyborze Smitha i uruchamia ją ponownie
- **THEN** wybór widoku zostaje przywrócony bez utraty zapisanych preferencji języka i motywu
