# Tryb Filter

Tryb Filter jest opisany jako pomiar odpowiedzi skalarnej, a próbki zapisuje w `MeasurementPoint.TransmissionDb`. Nie wypełnia SWR, return loss, fazy ani impedancji. Wykres nazywa wynik skalarną transmisją w dB, a nie S21, ponieważ odzyskane dane urządzenia nie potwierdzają fazy ani pełnej kalibracji dwuportowej.

Wskazówka w aplikacji opisuje wymagany układ: jako punkt odniesienia połącz ANT z WOB przewodem przelotowym, a następnie zastąp przewód filtrem. Nie uruchamiaj pomiaru sprzętowego Filter, gdy WOB jest wolne albo terminator 50 Ω znajduje się tylko na ANT.

Profil zawiera starszą wartość `ReturnLossOffsetFilterDb` z `analyz.ini`, którą konwersja stosuje do skalarnego odczytu ADC. Pokrewna dokumentacja SP3SWJ VNA3p2 podaje, że offset ten ustawia się tak, aby przewód OUT–IN pokazywał 0 dB. To potwierdzenie dla rodziny urządzeń, nie dla każdej rewizji VNA 1–65 MHz: [dokumentacja VNA3p2](https://sp2swj.sp-qrp.pl/VNA3p2/VNA3p2.htm). Pokrewna instrukcja MAX4 opisuje komendy `0=VNA` i `1=Wobuloscope`: [instrukcja VNA MAX4](https://sp2swj.sp-qrp.pl/SP3SWJ/VNA_MAX4_pliki/MANUAL_draft/VNA_MAX4_md_04.pdf). Komenda `1` nie została potwierdzona dla VNA 1–65 MHz, więc profil blokuje sprzętowy skan Filter przed połączeniem i wysłaniem komendy. Pozostają dostępne dane demo i odtwarzanie zapisanej sesji.

Tryb można więc oceniać na danych demo lub sesji, dopóki komenda urządzenia nie zostanie potwierdzona dla tego modelu. Nie wyliczaj fazy ani zespolonego S21 z odpowiedzi skalarnej.
