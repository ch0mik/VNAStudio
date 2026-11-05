# Szacowanie strat kabla metodą jednowrotną

Widok Cable Loss używa pomiaru odbicia na ANT. Kabel podłącza się do ANT, a jego dalszy koniec pozostawia otwarty lub zwarty. Dla każdej poprawnej próbki wyświetlane oszacowanie wynosi:

```text
strata w jedną stronę (dB) = − signed ReturnLossDb / 2
```

`ReturnLossDb` używa konwencji ze znakiem `20 log10(|Γ|)` i jest wartością zerową lub ujemną. Fala pokonuje kabel do jego końca i z powrotem, dlatego tłumienie jest dwukrotnością straty jednokierunkowej. Zależność zakłada, że koniec kabla odbija niemal całą moc, a punkt odniesienia pomiaru jest stabilny.

To oszacowanie, a nie skalibrowany pomiar tłumienia wtrąceniowego. W wyniku pozostają niedopasowanie złączy, straty osprzętu, promieniowanie, jakość zakończenia kabla i błędy zależne od częstotliwości. Terminator 50 Ω na końcu kabla nie jest poprawnym układem dla tej metody: pochłania sygnał zamiast go odbijać. Wykres pozostaje ukryty, dopóki operator nie potwierdzi otwartego lub zwartego końca kabla. Dane demonstracyjne ani sesje bez sprzętowego źródła Cable Loss nie są uznawane za pomiary kabla.

Dostarczona instrukcja sprzętu nie opisuje algorytmu ani kalibracji Cable Loss. Zewnętrzna dekompilacja VB6 pokazuje ramkę `fraCableLoss` i etykietę `CABLE LOSS dB`; odzyskany handler trybu przełącza kontrolki, ale dostępny pseudokod nie ujawnia wzoru strat ani punktu odniesienia kalibracji. Dlatego `−ReturnLossDb / 2` jest oszacowaniem jednowrotnym dodanym w VNA Studio, a nie potwierdzonym portem algorytmu oryginału.

Zależność połowy strat odbiciowych jest standardową metodą jednowrotną (zob. [Anritsu MS2034A Measurement Guide](https://eetac.upc.edu/ca/lescola/Equipaments/laboratoris/manuals/analitzador-espectres/anritsu-ms2034a-measurement-guide-10580-00289f.pdf)). Porównanie fizyczne z kablem o znanych parametrach pozostaje do wykonania.

Bezpośredni skalibrowany pomiar transmisji może używać konfiguracji dwuportowej: ANT → kabel → WOB. To odrębny tryb i inny układ pomiarowy.
