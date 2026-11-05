# Spec Delta

## Purpose

Zapewnia zachowanie i odtwarzanie pomiarów wraz z kontekstem urządzenia oraz jawne określenie zgodności importowanych plików starszych aplikacji.

## ADDED Requirements

### Requirement: Kompletny zapis sesji

System SHALL zapisywać wersję formatu, jednostki, profil urządzenia, parametry skanu, próbki, stany poprawności i odniesienie do użytej kalibracji, tak aby odtworzenie nie wymagało zgadywania brakujących jednostek lub profilu.

#### Scenario: Zapis i odczyt pomiaru

- **WHEN** użytkownik zapisuje sesję i ponownie ją otwiera
- **THEN** aplikacja odtwarza próbki i ich kontekst bez zmiany częstotliwości, jednostek ani statusów poprawności

### Requirement: Odtwarzanie bez urządzenia

System SHALL pozwalać wyświetlać zapisane sesje i dane demonstracyjne bez połączenia ze sprzętem. Źródło danych SHALL być widoczne dla użytkownika.

#### Scenario: Demonstracja offline

- **WHEN** użytkownik otwiera sesję demonstracyjną bez dostępnego portu COM
- **THEN** wykresy i markery działają, a aplikacja oznacza dane jako demonstracyjne lub odtwarzane

### Requirement: Jawna obsługa niezgodnych plików

System SHALL informować o nieobsługiwanej wersji, uszkodzonym pliku lub brakujących danych wymaganych do jego interpretacji, bez przedstawiania częściowego odczytu jako kompletnej sesji.

#### Scenario: Nieobsługiwana wersja formatu

- **WHEN** użytkownik otwiera plik o wersji, której aplikacja nie obsługuje
- **THEN** aplikacja podaje przyczynę i nie nadpisuje pliku wejściowego

### Requirement: Potwierdzona zgodność starszych formatów

System SHALL deklarować obsługę formatu pomiaru lub kalibracji starszej aplikacji dopiero po weryfikacji na zapisanych przykładach. Raport zgodności SHALL wymieniać obsługiwane warianty i ograniczenia.

#### Scenario: Import starszej kalibracji

- **WHEN** format pliku kalibracji nie został zweryfikowany dla wybranego profilu
- **THEN** aplikacja nie traktuje go jako zgodnej kalibracji i wskazuje brak potwierdzonej obsługi

### Requirement: Kontekst nowych narzędzi i zgodność sesji

System SHALL rozszerzać wersjonowany zapis o tryb, dostępne wielkości i kontekst analizy wraz ze statusem kalibracji. Odczyt wcześniejszych sesji SHALL zachować istniejące wyniki bez ponownego stosowania niepotwierdzonej kalibracji i bez nadawania im statusu zweryfikowanych pomiarów sprzętowych.

#### Scenario: Odczyt sesji wersji 1

- **WHEN** użytkownik otwiera istniejącą sesję v1 po wprowadzeniu nowych narzędzi
- **THEN** wartości próbek pozostają zachowane, a brakujący kontekst i status zgodności są jawnie oznaczone
