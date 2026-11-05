# Spec Delta

## Purpose

Zapewnia motywy jasny, ciemny i Night Vision dla interfejsu oraz własnego wykresu.

## ADDED Requirements

### Requirement: Motywy zmieniane podczas pracy

System SHALL umożliwiać wybór Light, Dark i Night Vision bez restartu. Motyw SHALL obejmować powierzchnie, tekst, formularze, legendę, tło wykresu, osie, siatkę i markery.

#### Scenario: Zmiana na ciemny motyw

- **WHEN** użytkownik wybiera Dark
- **THEN** okno i wykres zmieniają kolory na ciemne, a zbyt ciemne przebiegi zostają rozjaśnione na potrzeby wyświetlania

### Requirement: Czerwony Night Vision

System SHALL udostępniać Night Vision z czerwonymi i czarnymi kolorami obszaru klienta. Przebiegi SHALL być rozróżniane również wzorami linii. Własne kontrolki i stany interakcji SHALL używać zasobów wariantu.

#### Scenario: Night Vision podczas interakcji

- **WHEN** użytkownik ustawia fokus, zaznacza tekst lub otwiera listę w Night Vision
- **THEN** kontrolki zachowują czerwone kolory i korzystają z ciemnego wariantu jako podstawy szablonów

### Requirement: Trwały wybór motywu

System SHALL zachowywać kod motywu w AppSettings razem z portem i językiem. IOptionsMonitor SHALL stosować wybór przy uruchomieniu i po przeładowaniu. Starsze pliki ustawień SHALL wybierać jasny motyw.

#### Scenario: Ponowne uruchomienie

- **WHEN** zapisane ustawienia zawierają `Theme=night-vision`
- **THEN** model widoku wybiera Night Vision przed wyświetleniem okna

### Requirement: Zachowanie danych i profilu

System SHALL zachowywać dane sesji, markery, zakres powiększenia i oryginalne kolory snapshotu przy zmianie motywu. Motyw SHALL być ustawieniem wyświetlania niezależnym od pliku pomiarowego.

#### Scenario: Odtworzenie kolorów profilu

- **WHEN** użytkownik wczytuje sesję w Night Vision, a następnie wybiera Light
- **THEN** aplikacja wyświetla oryginalne kolory profilu sesji bez przeliczania danych RF
