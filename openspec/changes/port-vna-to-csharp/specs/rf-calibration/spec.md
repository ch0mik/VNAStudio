# Spec Delta

## Purpose

Zapewnia jednoznaczne przeliczanie surowych próbek na wielkości RF i sprawdzalną kalibrację właściwą dla wybranego urządzenia oraz trybu pomiarowego.

## ADDED Requirements

### Requirement: Jawne jednostki i impedancja odniesienia

System SHALL zachowywać jednostki częstotliwości, fazy, impedancji i strat w całym przepływie danych. System SHALL używać impedancji odniesienia zapisanej w profilu; dla profilu początkowego wynosi ona 50 Ω.

#### Scenario: Prezentacja częstotliwości

- **WHEN** próbka ma częstotliwość 7100000 Hz
- **THEN** obliczenia i pliki zachowują jednoznaczną jednostkę, a etykieta wykresu w MHz przedstawia tę samą częstotliwość jako 7,1 MHz

### Requirement: Kalibracja zgodna z profilem i trybem

System SHALL stosować kalibrację właściwą dla profilu, trybu i udokumentowanego zakresu częstotliwości. System SHALL informować o braku odpowiedniej kalibracji zamiast po cichu używać niezgodnego zestawu.

#### Scenario: Niezgodna kalibracja

- **WHEN** użytkownik wybiera kalibrację dla innego urządzenia lub nieobsługiwanego zakresu
- **THEN** aplikacja wskazuje niezgodność i nie przedstawia wyników jako poprawnie skalibrowanych

### Requirement: Zgodność obliczeń z referencjami

System SHALL odtwarzać wyniki zweryfikowanego algorytmu dla identycznych próbek i kalibracji w granicach wcześniej zapisanych tolerancji. Porównanie SHALL obejmować wartości zespolone i wielkości pochodne, w tym SWR, RL, fazę, R i X.

#### Scenario: Porównanie wektora referencyjnego

- **WHEN** ten sam zestaw surowych próbek i kalibracji jest przeliczany w aplikacji oraz w źródle referencyjnym
- **THEN** raport wskazuje różnice każdego badanego parametru względem ustalonej tolerancji

### Requirement: Afiniczna korekta skalowanych wskazań legacy

System SHALL provide a pure conversion applying the affine `y=ax+b` coefficients documented in `[SPECIAL_CAL]` of `analyz.ini` to already-scaled signed return loss and phase readings, then use the same RF derivation as the direct return-loss/phase API. It SHALL NOT decode raw ADC counts or apply `RL_OFFSET_vna`/`RL_OFFSET_filter` until their acquisition-flow semantics are verified.

#### Scenario: Profiled ADC conversion

- **WHEN** already-scaled signed return-loss and phase readings are converted using a profile with known affine coefficients
- **THEN** return loss and phase equal those readings transformed by `y=ax+b`, and SWR/impedance are derived from those converted values

#### Scenario: Legacy RL offset remains unapplied

- **WHEN** scaled readings are converted with a profile containing legacy RL offsets
- **THEN** the affine conversion does not add or subtract either offset and does not label physical calibration as verified

### Requirement: Obsługa punktów osobliwych i nieprawidłowych danych

System SHALL jawnie oznaczać próbki, dla których wynik jest nieokreślony lub niefinitywny, oraz nie zastępować ich arbitralnie pozornie poprawnymi wartościami RF.

#### Scenario: Dzielenie przez zero

- **WHEN** przeliczenie próbki prowadzi do dzielenia przez zero lub wartości nieokreślonej
- **THEN** wynik zawiera stan tej próbki, a aplikacja nie prezentuje go jako prawidłowego pomiaru skończonego

### Requirement: Jawny status zgodności sprzętowej

System SHALL odróżniać wynik zweryfikowany na urządzeniu od wyniku sprawdzonego wyłącznie na symulatorze lub zapisanych próbkach.

#### Scenario: Sprawdzenie bez urządzenia

- **WHEN** scenariusze transportu i obliczeń przeszły tylko w trybie odtwarzania
- **THEN** raport oznacza zgodność sprzętową jako niepotwierdzoną
