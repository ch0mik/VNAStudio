# Wybór pluginu i profilu urządzenia

VNA Studio przechowuje tożsamość urządzenia w sekcji `DeviceSelection` pliku `%LOCALAPPDATA%/VnaStudio/settings.json`. Preferencje aplikacji, takie jak język, motyw i opcje wykresu, pozostają osobnymi ustawieniami.

```json
{
  "DeviceSelection": {
    "PluginId": "iw3hev-vna65mhz",
    "ProfileId": "iw3hev-65mhz"
  }
}
```

Identyfikatory są stabilnymi identyfikatorami zarejestrowanych pluginów, a nie nazwami wyświetlanymi ani nazwami plików. Lista **Pomoc → Zarejestrowane profile urządzeń** pokazuje identyfikatory, zakres oraz deklarowane możliwości profilu.

Jeżeli oba identyfikatory są puste lub pominięte, aplikacja wybierze profil automatycznie tylko wtedy, gdy zarejestrowano dokładnie jeden plugin i jeden profil. Przy większej liczbie możliwości wpisz oba identyfikatory. Niepełna para, niepoprawne ustawienia, niezarejestrowany plugin albo niezadeklarowany profil zatrzymują start z komunikatem diagnostycznym; aplikacja nie przełącza się po cichu na Demo ani inne urządzenie. Po zmianie wyboru w pliku ustawień uruchom VNA Studio ponownie, aby utworzyć źródło wybranego pluginu.

Ścieżka portu szeregowego pozostaje osobnym ustawieniem aplikacji `PortName` i jest przekazywana bez zmian, zgodnie z nazwą systemową. Wybór pluginu nie wykrywa portu, nie otwiera go i niczego do niego nie wysyła. Operacje sprzętowe nadal podlegają bramkom obsługi i dowodów zadeklarowanym przez plugin.
