# Tasks

## 1. Inwentaryzacja i kontrakt

- [ ] 1.1 Sporządzić mapę zależności Core/App/Devices/IO i pól `DeviceProfile`, które są używane w każdym projekcie.
- [ ] 1.2 Zdefiniować wersjonowane DTO możliwości, opis pluginu/profilu, wspólny interfejs urządzenia/źródła oraz fabrykę niezależną od transportu.
- [ ] 1.3 Dodać zgodnościowy test kontraktu z testowym pluginem bez sprzętu.
- [ ] 1.4 Udokumentować politykę zgodności major/minor i dodać test akceptacji/odrzucenia wersji.
- [ ] 1.5 Dodać test architektury potwierdzający brak odniesień App/Core do pól protokołu VNA 1–65 MHz.
- [ ] 1.6 Wykonać review subagenta i poprawić kontrakt przed migracją sterownika.

## 2. Projekt pluginu VNA 1–65 MHz

- [ ] 2.1 Utworzyć `Vna.Device.Vna65MHz` i przenieść do niego profil „VNA 1–65 MHz” wraz z walidacją i opcjami `IOptions`.
- [ ] 2.2 Przenieść protokół, ramki, komendy, przeliczenia ADC i parametry DDS bez zmiany zachowania transportu COM.
- [ ] 2.3 Przenieść do pluginu pamięci generatora i parametry specyficzne dla urządzenia; usunąć zależność UI/Core od pól legacy.
- [ ] 2.4 Uruchomić testy znanych ramek, częstotliwości i pomiaru oraz review subagenta.

## 3. Rejestracja i wybór

- [ ] 3.1 Rejestrować zatwierdzone pluginy jawnie przez DI; walidować zgodność wersji API oraz unikalność identyfikatorów.
- [ ] 3.2 Udostępnić listę modeli i profili w UI według descriptorów, a funkcje pokazywać według deklarowanych możliwości.
- [ ] 3.3 Zachować konfigurację ścieżki COM/tty jako wartość systemową przekazywaną bez zmian; używać wyłącznie opcji pluginu dla ustawień jego protokołu.
- [ ] 3.4 Potwierdzić błędy, zmianę źródła, Stop, anulowanie i async disposal; wykonać review subagenta.

## 4. Sesje, ustawienia i starsze dane

- [ ] 4.1 Zapisać ID pluginu/profilu w nowych sesjach i zachować migawkę historyczną niezbędną do interpretacji wykresu.
- [ ] 4.2 Migrować dotychczasowe `iw3hev-65mhz` oraz starsze sesje bez utraty próbek, Z0, fazy, kolorów i zakresu.
- [ ] 4.3 Rozdzielić wybór pluginu/profilu od ogólnych preferencji aplikacji i dodać walidację ustawień.
- [ ] 4.4 Odtwarzać sesję z zapisanych sweepu i migawki bez wymaganego pluginu; dodać test nieznanego/uninstaliowanego plugin ID.
- [ ] 4.5 Potwierdzić testy migracji i playback bez pluginu; wykonać review subagenta.

## 5. Release i dokumentacja

- [ ] 5.1 Uwzględnić assembly i zasób profilu pluginu we wszystkich dostępnych RID; sprawdzać je w workflow release.
- [ ] 5.2 Zaktualizować README EN/PL oraz dokumentację protokołu, opisując architekturę i kontrakt dodawania modeli.
- [ ] 5.3 Uruchomić pełne testy i strict OpenSpec validation; wykonać finalne review subagenta.
