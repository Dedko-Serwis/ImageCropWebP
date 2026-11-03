# ImageCrop WebP — WPF .NET 10

Prosty lokalny program do kadrowania obrazów i eksportu do WebP.

## Uruchomienie
1. Otwórz `ImageCropWebP.csproj` w Visual Studio 2026 (Windows, workload .NET desktop development).
2. Przywróć pakiety NuGet i uruchom F5.
3. Alternatywnie: `dotnet restore` oraz `dotnet run` z folderu projektu.

## Obsługa
- Otwórz obraz przyciskiem, Ctrl+O albo przeciągnij plik na okno.
- Ctrl+V wkleja bitmapę ze schowka.
- Wybierz proporcje; przeciągnij myszą poza aktualnym kadrem, aby utworzyć nowy. Przeciągnij wewnątrz kadru, aby go przesunąć.
- Ustaw szerokość wynikową i jakość; wysokość obliczana jest automatycznie.
- Kliknij `Sprawdź wielkość pliku` albo `Zapisz jako WebP` (Ctrl+S).

## Uwagi
- Kadrowanie pracuje w pikselach oryginału, niezależnie od rozmiaru podglądu.
- Wariant bezstratny może tworzyć znacznie większe pliki.
- Obraz jest skalowany do rozmiaru okna w podglądzie; źródło zachowuje pełną rozdzielczość.
- Pierwsza wersja nie zawiera obracania i uchwytów zmiany rozmiaru już istniejącej ramki; nową ramkę można narysować ponownie.
- Projekt nie został skompilowany w tym środowisku (wymaga Windows i .NET 10 SDK).

## License

This project is licensed under the MIT License.

## Third-party libraries

- SkiaSharp 3.119.0
  Copyright its respective contributors.
  Licensed under the MIT License.
  https://github.com/mono/SkiaSharp