# Przegląd minimum SWR

Dodano checkbox widoczności znacznika minimum SWR i akcję ustawienia M1 na prawidłowe minimum. Wyłączenie znacznika nie zmienia wyniku pomiaru. Gdy minimum leży poza bieżącym zoomem, akcja przełącza wykres na pełny skan i zachowuje poprzedni zoom w historii. Polecenie jest niedostępne bez prawidłowych próbek i podczas skanu.

Weryfikacja: `dotnet test Vna.slnx --configuration Release --no-restore -p:UsedAvaloniaProducts=` — 62/62 zaliczone. Test obejmuje brak minimum, ukrywanie znacznika, ustawienie częstotliwości M1 i przywracanie zoomu. Funkcja zweryfikowana na danych testowych, bez urządzenia.
