# Proposal

## Why

Przyciski Cable Loss, Cable Length, Xtal, Filter i Generator są widoczne jak tryby aplikacji, lecz ich zakres działania jest nierówny. Użytkownik potrzebuje jasnej, działającej logiki zgodnej z oryginałem dla VNA 1–65 MHz.

## What Changes

- Zweryfikować każdą funkcję względem zdekompilowanego programu, `analyz.ini` i instrukcji; odróżnić funkcję zaimplementowaną od pustego trybu UI.
- Dokończyć Cable Length i ręczną analizę Xtal, w tym walidację danych wejściowych i użyteczne przejście z markerów do wyniku.
- Dodać Cable Loss/Filter tylko w zakresie wynikającym z udokumentowanego toru i kalibracji. Nie przedstawiać odbicia S11 jako transmisji S21.
- Generator udostępniać sprzętowo tylko po odzyskaniu i sprawdzeniu kontraktu komend dla tego modelu.
- Niewspierane fragmenty pozostają jawnie zablokowane z opisem wymaganych danych; nie wysyłać niezweryfikowanych komend RF.

## Capabilities

### Modified Capabilities

- `measurement-tools`: tryby pomiarowe, metody obliczeń, kalibracja i jawny status funkcji zależnych od sprzętu.

## Impact

`Vna.Core`, `Vna.App`, `Vna.Devices`, `Vna.Localization`, testy i dokumentacja. Port COM i dotychczasowy format zapisanych próbek nie mogą zmienić znaczenia bez jawnej migracji.
