# Plan projektu VNA

Aktywne zmiany: [`port-vna-to-csharp`](changes/port-vna-to-csharp/proposal.md), [`add-smith-chart`](changes/add-smith-chart/proposal.md) oraz [`device-plugin-architecture`](changes/device-plugin-architecture/proposal.md).

Wykres Smitha: [`add-smith-chart`](changes/add-smith-chart/proposal.md), [projekt techniczny](changes/add-smith-chart/design.md), [małe etapy implementacji](changes/add-smith-chart/tasks.md). Etapy 1–5 zaimplementowane i zreviewowane. Niezależna walidacja znaku fazy na znanych obciążeniach L/C pozostaje osobnym etapem sprzętowym.

Plan architektury rozszerzeń urządzeń: [`device-plugin-architecture`](changes/device-plugin-architecture/proposal.md), [projekt techniczny](changes/device-plugin-architecture/design.md), [etapy](changes/device-plugin-architecture/tasks.md). Określa wydzielenie VNA 1–65 MHz wraz z profilem i protokołem do osobnego pluginu.

- [Cel i zakres](changes/port-vna-to-csharp/proposal.md)
- [Architektura, decyzje i ryzyka](changes/port-vna-to-csharp/design.md)
- [Lista zadań i wykonane analizy](changes/port-vna-to-csharp/tasks.md)
- [Pozostałe funkcje ze zrzutów: macierz 29 pozycji i etapy E0–E5](changes/port-vna-to-csharp/roadmap-screenshots.md)
- Wymagania: [urządzenie](changes/port-vna-to-csharp/specs/device-acquisition/spec.md), [obliczenia i kalibracja](changes/port-vna-to-csharp/specs/rf-calibration/spec.md), [wykresy](changes/port-vna-to-csharp/specs/legacy-plots/spec.md), [pliki i odtwarzanie](changes/port-vna-to-csharp/specs/measurement-files/spec.md).
- [Wielojęzyczność PL/EN](changes/port-vna-to-csharp/specs/localization/spec.md).
- [Paczki release dla RID](changes/port-vna-to-csharp/specs/release-packaging/spec.md).
- [Motywy Light, Dark i Night Vision](changes/port-vna-to-csharp/specs/app-themes/spec.md).
- [Narzędzia pomiarowe: BW/Q, FILTER, kable i generator](changes/port-vna-to-csharp/specs/measurement-tools/spec.md).

Weryfikacja dokumentów: `openspec validate port-vna-to-csharp --strict`.

Zmiana `port-vna-to-csharp` nadal zawiera pozostałe funkcje oryginalnego programu i prace weryfikacyjne. Wykres Smitha ma wdrożony widok z jawną niepewnością; jego niezależne potwierdzenie znaku fazy czeka na znane obciążenia L/C. Zmiana `device-plugin-architecture` jest planem, nie implementacją. `openspec/specs/` pozostaje bez bazowych specyfikacji do czasu zrealizowania i archiwizacji właściwej zmiany.
