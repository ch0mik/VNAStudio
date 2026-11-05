# Pamięci częstotliwości generatora

Profil wczytuje dziesięć częstotliwości generatora z sekcji `MEMORY` dostarczonego pliku `analyz.ini`. Dla profilu 65 MHz są to:

| Pamięć | Częstotliwość |
| --- | ---: |
| M0 | 3,600 MHz |
| M1 | 7,050 MHz |
| M2 | 10,500 MHz |
| M3 | 14,100 MHz |
| M4 | 18,050 MHz |
| M5 | 21,050 MHz |
| M6 | 24,550 MHz |
| M7 | 27,195 MHz |
| M8 | 28,050 MHz |
| M9 | 29,050 MHz |

W trybie Generator wybierz komórkę pamięci lub zmień jej częstotliwość w zakresie aktywnego profilu. Dostarczona instrukcja urządzenia mówi, że przycisk `OFF` pokazuje bieżący stan generatora i kliknięcie włącza generator. Drugi port BNC jest wtedy używany jako sonda RF. Nie odzyskano jednak komend urządzenia, które uruchamiają, zatrzymują lub przestrajają to wyjście. Sterowanie pozostaje wyłączone, VNA Studio nie wysyła komendy generatora, a fizyczny stan wyjścia jest nieznany. Nie zakładaj, że aplikacja wyłączyła wyjście.

Analiza zdarzenia VB6 `VB6_0042399d` odzyskała gałąź zmiany stanu kontrolki i tekst przycisku `&OFF`; samo odwołanie do etykiety nie dowodzi wysłania takiej komendy przez COM. W dostępnej procedurze nie odzyskano pełnej, zweryfikowanej sekwencji włączania, wyłączania i ustawiania częstotliwości. Ten fragment nie wystarcza do bezpiecznego zaimplementowania sterowania generatorem.
