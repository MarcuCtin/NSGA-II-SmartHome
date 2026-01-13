# NSGA-II Smart Home Scheduler

Planificator pentru electrocasnice de tip smart-home, construit cu NSGA-II pentru a echilibra costul energiei și disconfortul utilizatorului. UI Windows Forms afișează live frontul Pareto și permite inspecția soluțiilor candidate.

## Funcționalități
- Implementare NSGA-II cu elitism, sortare non-dominată, distanță de aglomerare, selecție prin turneu binar, crossover single-point și mutație prin resetare aleatorie.
- Scenariu configurabil: electrocasnice cu durată, putere și oră preferată; tarif orar pe 24h.
- Scatter live (cost vs disconfort) desenat pe formular; punctele Pareto sunt evidențiate și clicabile.
- Grid cu primele soluții Pareto și orele lor de start.

## Logică de bază
- Cromozom: `int[] StartTimes` (câte un gene per aparat, oră 0–23).
- Obiective per individ:
  - **Cost:** sumă `putere_kW * tarif[ora]` pe durata fiecărui aparat (ore mod 24).
  - **Disconfort:** sumă `|ora_programată - ora_preferată|`.
- Parametri NSGA-II (implicit): populație 50, generații 100, crossover 0.9, mutație 0.05.
- Fișiere relevante:
  - Modele: [NSGA-II-SmartHome/Core/Models.cs](NSGA-II-SmartHome/Core/Models.cs)
  - Motor NSGA-II: [NSGA-II-SmartHome/Algorithm/NSGAIIEngine.cs](NSGA-II-SmartHome/Algorithm/NSGAIIEngine.cs)
  - UI: [NSGA-II-SmartHome/UI/MainForm.cs](NSGA-II-SmartHome/UI/MainForm.cs)
  - Intrare: [NSGA-II-SmartHome/Program.cs](NSGA-II-SmartHome/Program.cs)

## Scenariu implicit (demo)
- Electrocasnice: Washer (2h, 1.2 kW, preferă 18), Dryer (1h, 1.0 kW, 18), EV Charger (4h, 7.0 kW, 18), Dishwasher (2h, 1.4 kW, 20), Boiler (3h, 2.0 kW, 7).
- Tarife: 0.3 RON/kWh pentru orele 00–05; 0.9 RON/kWh în rest.
- UI: populația în gri, frontul Pareto în roșu; clic pe punct roșu pentru detalii și orele de start.

## Build și rulare
- Necesită .NET 8 SDK pe Windows (WinForms target `net8.0-windows`).
- Build: `dotnet build NSGA-II-SmartHome.sln`
- Run (Windows): `dotnet run --project NSGA-II-SmartHome/NSGA-II-SmartHome.csproj`
- Pe macOS/Linux se poate compila, dar UI WinForms necesită Windows pentru afișare.

## Cum modifici
- Ajustează electrocasnicele sau tarifele în `BuildDefaultScenario()` din [NSGA-II-SmartHome/UI/MainForm.cs](NSGA-II-SmartHome/UI/MainForm.cs).
- Tunează parametrii NSGA-II prin `NSGAIIParameters` la construirea `NSGAIIEngine`.

## Note
- Randare custom pe panel pentru scatter; nu sunt dependențe NuGet suplimentare față de framework.
- UI rămâne responsiv: motorul rulează pe task de fundal și raportează progresul către formular.
