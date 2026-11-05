# Spec Delta

## Purpose

Umożliwia wykonywanie pomiarów na urządzeniu użytkownika z konfigurowalnym profilem oraz przewidywalnym zachowaniem podczas zatrzymania i awarii połączenia.

## ADDED Requirements

### Requirement: Konfigurowalny profil urządzenia

System SHALL pozwalać wybrać port i profil, w tym zakres częstotliwości, zegar DDS i liczbę próbek. Profil początkowy SHALL odzwierciedlać zapis INI: 1–65 MHz, DDS 191993220 Hz i 649 próbek; numer COM SHALL być wybierany niezależnie.

#### Scenario: Użycie profilu użytkownika

- **WHEN** użytkownik wybiera profil VNA 65 MHz i dostępny port
- **THEN** aplikacja pokazuje parametry tego profilu bez zastępowania zegara wartością 400 MHz ani wymuszania COM5

### Requirement: Walidacja zakresu skanu

System SHALL odrzucać parametry skanu niezgodne z ograniczeniami wybranego profilu przed wysłaniem polecenia do urządzenia.

#### Scenario: Nieprawidłowe granice

- **WHEN** częstotliwość początkowa przekracza końcową albo granice wykraczają poza profil
- **THEN** aplikacja wyświetla przyczynę błędu i nie uruchamia skanu

### Requirement: Zakresy pasm z profilu legacy

System SHALL read optional paired start/end frequency limits from `[BANDS_LIMIT]` in `analyz.ini`, interpreting values as Hz. It SHALL reject incomplete, non-positive or reversed pairs and SHALL hide ranges outside the selected device profile. When the section is absent or empty, the configured regional presets SHALL remain available.

#### Scenario: Legacy range outside device capability

- **WHEN** `[BANDS_LIMIT]` contains a syntactically valid range outside the active profile
- **THEN** the range is retained in the profile snapshot but omitted from the scan shortcut selector

### Requirement: Sterowanie skanowaniem

System SHALL obsługiwać pojedynczy skan, skanowanie ciągłe i zatrzymanie zgodnie ze zweryfikowanym kontraktem protokołu wybranego urządzenia.

#### Scenario: Pomiar pojedynczy

- **WHEN** urządzenie odpowiada kompletnym zestawem próbek na żądanie pojedynczego skanu
- **THEN** aplikacja udostępnia jeden spójny wynik wraz z częstotliwościami i kończy pomiar

#### Scenario: Przerwanie pracy ciągłej

- **WHEN** użytkownik zatrzymuje pomiar ciągły
- **THEN** aplikacja kończy bieżącą operację według kontraktu protokołu i nie rozpoczyna kolejnego skanu

### Requirement: Obsługa błędów transportu

System SHALL wykrywać timeout i utratę połączenia oraz odróżniać niepełny wynik od kompletnego pomiaru. System SHALL umożliwiać ponowne połączenie bez restartu aplikacji.

#### Scenario: Fragmentowane dane

- **WHEN** poprawna odpowiedź dociera w kilku fragmentach przed limitem czasu
- **THEN** aplikacja składa ją zgodnie z kontraktem i publikuje wynik dopiero po uzyskaniu kompletnej odpowiedzi

#### Scenario: Odłączenie podczas skanu

- **WHEN** połączenie zostaje utracone przed odebraniem wszystkich próbek
- **THEN** aplikacja zgłasza przerwany pomiar, nie przedstawia go jako kompletnego i pozwala ponownie wybrać połączenie

### Requirement: Jawny wybór źródła

System SHALL rozróżniać Demonstrator, Urządzenie i Odtwarzanie. System SHALL NOT automatycznie przełączać źródła ani potwierdzać zgodności VNA na podstawie samego wyboru lub pojawienia się portu. System SHALL NOT uruchamiać danych demonstracyjnych po błędzie pomiaru urządzenia.

#### Scenario: Port istnieje, ale nie odpowiada zgodny instrument

- **WHEN** użytkownik wybiera Urządzenie, a port otwiera się bez potwierdzonej odpowiedzi protokołu
- **THEN** aplikacja pokazuje stan oczekiwania lub błąd, nie oznacza połączenia jako zweryfikowanego i nie podstawia demonstracji

### Requirement: Hold i możliwości trybu

System SHALL zachowywać ostatni kompletny wynik w Hold i zatrzymywać rozpoczynanie kolejnych skanów. System SHALL udostępniać tryby na podstawie zdolności sterownika i walidować zakresy presetów względem profilu urządzenia.

#### Scenario: Preset poza zakresem

- **WHEN** profil kończy się na 65 MHz, a preset wymaga pomiaru przy 144 MHz
- **THEN** aplikacja wskazuje niedostępny zakres i nie wysyła żądania do urządzenia
