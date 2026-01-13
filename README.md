# NSGA-II Smart Home Scheduler

Planificator pentru electrocasnice de tip smart-home, construit cu NSGA-II pentru a echilibra costul energiei și disconfortul utilizatorului. Aplicația Windows Forms oferă control interactiv asupra simulării și vizualizare în timp real a frontului Pareto.

## Funcționalități
- **Algoritm Evolutiv:** Implementare NSGA-II (Non-dominated Sorting Genetic Algorithm II) cu elitism și menținerea diversității.
- **Control Execuție:** Interfață cu butoane **Start**, **Pauză/Reluare** și **Stop**.
- **Vizualizare Live:**
  - Scatter plot dinamic: Cost (X) vs Disconfort (Y).
  - Coduri de culoare: Populație (Gri), Front Pareto (Roșu), Selecție (Albastru).
- **Interacțiune & Export:**
  - **Click pe grafic/tabel:** Selectează o soluție și afișează detalii.
  - **Export PNG:** Salvează graficul curent ca imagine.
  - **Export CSV:** Salvează detaliile soluției selectate (orar, costuri per aparat, tarife).
- **Monitorizare:** Status bar cu progres (ProgressBar) și indicatori live pentru cel mai bun cost și cel mai mic disconfort.

## Logică de bază
- **Cromozom:** `int[] StartTimes` (vector de ore de start 0–23, câte una per aparat).
- **Obiective:**
  1. **Minimizare Cost:** $\sum (Putere \times Tarif_{oră})$
  2. **Minimizare Disconfort:** $\sum |Ora_{programată} - Ora_{preferată}|$
- **Parametri NSGA-II:** Populație 50, Generații 100, Crossover 0.9, Mutație 0.05.

## Structură Proiect
- `Core/Models.cs`: Definiții pentru Aparat, Tarif, Scenariu, Individ.
- `Algorithm/NSGAIIEngine.cs`: Motorul algoritmului (include logica de pauză și sortare nedomintată).
- `UI/MainForm.cs`: Interfața grafică, desenarea custom a graficului și gestionarea stărilor.
- `Program.cs`: Entry point (configurat cu `[STAThread]` pentru suport dialoguri de salvare).

## Scenariu Demo
- **Electrocasnice:** Washer (2h, 18:00), Dryer (1h, 18:00), EV Charger (4h, 18:00), Dishwasher (2h, 20:00), Boiler (3h, 07:00).
- **Tarife:** Diferențiate (0.3 RON noaptea 00-05 / 0.9 RON ziua).
- **Comportament:** Algoritmul va căuta compromisuri între a rula noaptea (ieftin, dar disconfort mare) și a rula seara (scump, dar comod).

## Build și Rulare
1. Necesită **.NET 8 SDK**.
2. Deschide soluția în Visual Studio 2022+ sau VS Code.
3. Build: `dotnet build`
4. Run: `dotnet run --project NSGA-II-SmartHome`

## Instrucțiuni de Utilizare
1. Apasă **Start** pentru a lansa optimizarea.
2. Folosește **Pause** dacă vrei să analizezi o generație intermediară.
3. Dă **Click** pe un punct roșu (Pareto) din grafic pentru a vedea orarul propus.
4. Apasă **Export Selection (CSV)** pentru a salva datele soluției alese.
5. Apasă **Export Graph (PNG)** pentru a salva imaginea graficului.
