# Spec Delta

## Purpose

Zapewnia polski i angielski interfejs aplikacji VNA Studio oparty na zasobach .NET RESX.

## ADDED Requirements

### Requirement: Zasoby polskie i angielskie

System SHALL przechowywać teksty interfejsu, własne komunikaty walidacji i komunikaty operacji w plikach `.resx`. Zasoby angielskie SHALL być zasobami neutralnymi, a polskie SHALL być dostępne jako zasoby kultury `pl`. Oba zestawy SHALL zawierać te same klucze i zgodne argumenty formatowania.

#### Scenario: Odczyt zasobów obu języków

- **WHEN** użytkownik wybiera język Polski lub English
- **THEN** aplikacja odczytuje właściwe zasoby, a etykiety i własne komunikaty są dostępne bez brakujących kluczy

### Requirement: Zmiana języka podczas pracy

System SHALL umożliwiać zmianę języka bez restartu. Zmiana SHALL aktualizować napisy, legendę, bieżące komunikaty aplikacji, odczyty markerów i format liczb na wykresie.

#### Scenario: Przełączenie języka istniejącej sesji

- **WHEN** użytkownik zmienia język otwartej sesji z polskiego na angielski
- **THEN** napisy i format liczb zmieniają się, a próbki, częstotliwości, markery i identyfikator źródła danych pozostają zachowane

### Requirement: Język w konfiguracji użytkownika

System SHALL zapisywać język razem z nazwą portu w ustawieniach użytkownika. `IOptionsMonitor<AppSettings>` SHALL dostarczać ustawienia przy uruchomieniu i po ich przeładowaniu. Starsze pliki bez pola języka SHALL uruchamiać aplikację po polsku.

#### Scenario: Zachowanie języka po uruchomieniu

- **WHEN** użytkownik wybiera English i zapisuje ustawienia
- **THEN** ustawienie `Language` ma wartość `en-US`, nazwa portu jest zachowana i ponowne uruchomienie stosuje język angielski

### Requirement: Niezależność danych od języka

System SHALL zapisywać dane pomiarowe i pliki JSON niezależnie od kultury interfejsu. Zmiana języka SHALL zachować wartość wprowadzonych częstotliwości przy zmianie separatora dziesiętnego i SHALL przekazywać ścieżkę portu bez zmian.

#### Scenario: Separator dziesiętny

- **WHEN** pole częstotliwości zawiera `1,5` MHz w języku polskim i użytkownik wybiera English
- **THEN** pole pokazuje `1.5`, a wartość częstotliwości pozostaje równa 1500000 Hz
