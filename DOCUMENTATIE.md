Student: [Numele Tău]  
Grupa: [Grupa Ta]  
Anul: IV  
Specializarea: Calculatoare și Tehnologia Informației  
Profesor coordonator: [Numele Profesorului]  
Iași, 2026  

---

# (PAGINA 2 – ÎNCEPUT DOCUMENTAȚIE)

## 1. Introducere și context

În era digitalizării și a caselor inteligente (Smart Homes), gestionarea eficientă a resurselor a devenit o prioritate. Utilizatorii casnici se confruntă cu o dilemă constantă: dorința de a reduce costurile la energie (profitând de tarife dinamice/nocturne) versus dorința de confort (utilizarea electrocasnicelor la ore convenabile).

Acest proiect propune o aplicație desktop interactivă (Windows Forms) care utilizează tehnici de Inteligență Artificială pentru a asista utilizatorul în luarea acestei decizii. Aplicația implementează algoritmul evolutiv multi-obiectiv **NSGA-II** pentru a genera automat orare de funcționare, oferind o vizualizare grafică a compromisului dintre:

- **Cost (RON)**
- **Disconfort (ore)**

Spre deosebire de soluțiile clasice care oferă un singur rezultat rigid, aplicația generează un **Front Pareto** de soluții, permițând utilizatorului să aleagă vizual varianta optimă dintr-un grafic interactiv.

### 1.1. Definirea problemei

Avem un set de electrocasnice/consumatori care trebuie programați în intervalul unei zile (24h). Pentru fiecare aparat se cunosc:

- **Durata** de funcționare (în ore)
- **Puterea** (kW)
- **Ora preferată** de pornire (preferința utilizatorului)

De asemenea, există un **tarif orar** (RON/kWh) ce poate varia de la o oră la alta (ex.: noaptea mai ieftin).

Scopul nu este să găsim „o singură soluție perfectă” (care de obicei nu există), ci o **mulțime de soluții** care ilustrează diferite compromisuri între cele două obiective conflictuale.

### 1.2. Obiective și criterii de evaluare

În aplicație se optimizează simultan:

- $f_1$: **Costul total (RON)** — vrem minim.
- $f_2$: **Disconfortul total (ore)** — vrem minim.

Rezultatul final este un **front Pareto** (soluții nedominate), unde fiecare punct din grafic reprezintă o planificare completă (un orar pentru toate aparatele).

### 1.3. Scenariu demonstrativ (cerință)

Exemplu uzual de prezentare: utilizatorul dorește ca **Washer (2h)**, **Dryer (1h)** și **EV charge (4h)** să fie finalizate în jurul orei 18:00.

Tarife ilustrative:

- Noapte: 0.3 RON/kWh
- Seară: 0.9 RON/kWh

Soluțiile Pareto „așteptate” conceptual:

- **A (cost mic):** rulează totul ~02:00 (ieftin, disconfort mare)
- **B (disconfort mic):** rulează totul ~18:00 (comod, scump)
- **C (echilibrat):** EV noaptea, rufele seara

## 2. Fundamente teoretice: NSGA-II

Algoritmul Genetic de Sortare Nedomintată II (NSGA-II) reprezintă motorul decizional al aplicației. Este preferat în optimizarea multi-obiectiv datorită a trei mecanisme distincte:

1. **Sortarea nedomintată (Non-dominated sorting)**
   - Clasificarea soluțiilor în „fronturi” pe baza dominanței Pareto.
   - O soluție este nedomintată (Front 1) dacă nicio altă soluție nu este mai bună simultan la ambele obiective.

2. **Menținerea diversității (Crowding distance)**
   - Pentru a oferi utilizatorului o paletă largă de opțiuni (ieftine și comode), algoritmul favorizează soluțiile situate în zone mai puțin aglomerate ale graficului.

3. **Elitism**
   - Cei mai buni indivizi din populația combinată (părinți + copii) sunt păstrați automat, garantând păstrarea soluțiilor de calitate.

### 2.1. Dominanța Pareto (definiție)

Presupunem o problemă de minimizare cu două obiective $(f_1, f_2)$. Un individ $A$ **domină** un individ $B$ dacă:

- $f_1(A) \le f_1(B)$ și $f_2(A) \le f_2(B)$
- și cel puțin una dintre inegalități este strictă

Un individ este **nedominat** dacă nu există alt individ care îl domină.

### 2.2. Pașii NSGA-II (nivel înalt)

În fiecare generație, motorul NSGA-II aplică următorii pași:

1. Inițializare populație aleatoare.
2. Evaluare (Cost, Disconfort) pentru fiecare individ.
3. Sortare nedomintată → atribuirea rangurilor (Front 1, Front 2, ...).
4. Calcul crowding distance în fiecare front.
5. Selecție (turneu) pe baza (Rank, CrowdingDistance).
6. Crossover + mutație → copii.
7. Elitism: combină părinți + copii, sortează și păstrează cei mai buni $N$ indivizi.

Aceste mecanisme produc o distribuție de puncte în planul (Cost, Disconfort), convergând către o curbă (frontul Pareto) și păstrând diversitatea.

### 2.3. Crowding Distance (intenție și interpretare)

Crowding distance măsoară „cât de izolat” este un individ în frontul său. Intuitiv:

- indivizii de la capete (min/max pe un obiectiv) primesc distanță infinită pentru a fi păstrați;
- indivizii din zone aglomerate primesc distanță mică;
- selecția favorizează distanța mare pentru a păstra diversitatea soluțiilor.

## 3. Arhitectura aplicației

Aplicația a fost dezvoltată în C# (.NET 8) folosind tehnologia Windows Forms pentru interfața grafică.

### 3.1. Structura proiectului (Model – Logică – UI)

Proiectul este organizat pe un model simplificat de tip MVC:

**Model (Core):**
- `Appliance`: definește caracteristicile unui aparat (durată, putere, oră preferată).
- `TariffSchedule`: definește tariful orar (24 valori).
- `Scenario`: grupează aparatele și tarifele.
- `Individual`: reprezintă o soluție completă (cromozom + valori de fitness + rank + crowding distance).

**Logică (Algorithm):**
- `NSGAIIEngine`: conține logica NSGA-II (inițializare, evaluare, crossover, mutație, sortare nedomintată, crowding distance, elitism).

**UI (View):**
- `MainForm`: fereastra principală care pornește optimizarea pe task de fundal, afișează scatter-ul Cost vs Disconfort și permite click pe soluții pentru detalii.

### 3.1.1. Fluxul de date (end-to-end)

1. UI construiește un `Scenario` (apparate + tarife).
2. UI pornește motorul `NSGAIIEngine` într-un task de fundal.
3. Motorul rulează generațiile și raportează periodic un `GenerationSnapshot`.
4. UI actualizează:
    - scatter plot (populație + front Pareto)
    - tabelul cu primele soluții Pareto
    - status bar (generație curentă și mărimea frontului)
5. Utilizatorul poate da click pe un punct Pareto pentru a vedea orele de start.

### 3.2. Interfața grafică (GUI)

Interfața este proiectată pentru a fi intuitivă și oferă următoarele funcționalități:

- **Zona de configurare/descriere scenariu:** listă de aparate și tarife.
- **Panou de control:** buton Start/Stop pentru simulare.
- **Vizualizare grafică (scatter plot):**
  - Axa X: Cost total (RON)
  - Axa Y: Disconfort total (ore)
  - Puncte gri: populația curentă
  - Puncte roșii: frontul Pareto
- **Grid (tabel) cu soluții Pareto:** afișează cost, disconfort și orele de start.

Notă: în implementarea actuală, scatter plot-ul este **desenat custom pe un `Panel`**, pentru a evita dependențe suplimentare; funcționalitatea rămâne aceeași (plot + click pentru detalii).

### 3.3. Responsivitate și thread-safety

Optimizarea poate fi computațională (100 generații × populație 50 × evaluări), iar UI trebuie să rămână responsiv. Din acest motiv:

- algoritmul rulează în fundal prin `Task.Run(...)`;
- progresul este livrat către UI prin `Progress<T>`.

În .NET, `Progress<T>` postează callback-urile pe contextul de sincronizare al UI-ului (dacă este creat pe thread-ul UI), astfel update-urile de UI rămân sigure.

## 4. Implementare și detalii tehnice

### 4.1. Reprezentarea soluției (cromozom)

- Cromozomul este un vector de întregi: `StartTimes[i] ∈ [0, 23]`.
- Fiecare genă reprezintă **ora de start** pentru aparatul `i`.

Observație: dacă un aparat are durată > 1h, consumul său afectează mai multe ore. În evaluare, orele sunt „wrap” cu modulo 24.

### 4.2. Funcția de evaluare (fitness)

Evaluarea fiecărui individ se face pe baza a două funcții obiectiv conflictuale:

- **Cost** ($f_1$): se calculează prin însumarea consumului pe intervalele active, înmulțit cu tariful specific fiecărei ore.
- **Disconfort** ($f_2$): se calculează ca suma diferențelor absolute dintre ora planificată și ora preferată de utilizator.

Pseudo-cod (corespunzător implementării):

```csharp
double cost = 0;
double discomfort = 0;

for (int i = 0; i < Appliances.Count; i++)
{
    var start = StartTimes[i];

    // cost: parcurgem durata aparatului, cu ore wrap mod 24
    for (int h = 0; h < Appliances[i].DurationHours; h++)
    {
        int hour = (start + h) % 24;
        cost += Appliances[i].PowerKw * Tariff[hour];
    }

    // discomfort: distanța față de ora preferată
    discomfort += Math.Abs(start - Appliances[i].PreferredStartHour);
}
```

#### 4.2.1. Exemplu de calcul cost (intuitiv)

Dacă un aparat are 2h, putere 1.2 kW, pornește la 23:00, atunci orele active sunt 23 și 0 (wrap). Costul include ambele ore:

$$Cost = 1.2 \cdot tarif[23] + 1.2 \cdot tarif[0]$$

Această abordare modelează corect situațiile în care un aparat trece peste miezul nopții.

### 4.3. Evoluție: selecție, crossover, mutație

- **Selecție:** turneu binar (se preferă rank mai mic; la egalitate crowding distance mai mare).
- **Crossover:** single-point cu probabilitate 0.9.
- **Mutație:** resetare aleatorie a genei (oră 0–23) cu probabilitate 0.05.
- **Elitism:** populația următoare se selectează din reuniunea părinți + copii, păstrând cele mai bune soluții după (rank, crowding).

#### 4.3.1. Selecția (turneu binar)

Se aleg aleator doi indivizi $A$ și $B$; câștigătorul este:

1) cel cu **Rank** mai mic; 
2) la egalitate de rank, cel cu **CrowdingDistance** mai mare.

Această regulă urmărește atât convergența spre frontul Pareto, cât și diversitatea soluțiilor.

#### 4.3.2. Crossover single-point

Se alege un punct de tăiere $p \in [1, n-1]$ și se schimbă segmentele (genele) după acel punct între doi părinți, generând doi copii.

#### 4.3.3. Mutația (random reset)

Pentru fiecare genă, cu probabilitatea $p_m = 0.05$, ora se resetează aleator în [0, 23]. Mutația previne blocarea prematură în soluții locale.

### 4.4. Sortarea nedomintată și calculul crowding distance

În implementare, sortarea nedomintată este realizată prin:

- numărarea dominărilor pentru fiecare individ (câți îl domină);
- construirea mulțimii indivizilor pe care îi domină;
- extragerea fronturilor iterativ (Front 1: cei cu 0 dominatori, apoi Front 2 etc.).

Crowding distance se calculează în fiecare front, normalizând diferențele pe fiecare obiectiv.

### 4.4. Integrarea cu interfața grafică (UI responsive)

Pentru a nu bloca interfața în timpul calculelor, optimizarea rulează pe un task de fundal, iar UI primește update-uri printr-un mecanism de progres.

Conceptual:

```csharp
var progress = new Progress<GenerationSnapshot>(snapshot =>
{
    // actualizează frontul, grila și redă scatter-ul
});

await Task.Run(() => engine.Run(progress, token), token);
```

#### 4.4.1. Interacțiune: click pe punct Pareto

Pentru a permite inspecția soluțiilor, UI caută punctul Pareto cel mai apropiat de click (în coordonate ecran) și, dacă distanța este sub un prag, afișează detaliile:

- Cost
- Disconfort
- orele de start pentru fiecare aparat (format HH:00)

## 5. Rezultate experimentale

Testele au fost efectuate utilizând un set de 5 consumatori casnici (Mașină de spălat, Uscător, Boiler, Încărcător EV, Mașină de vase) și un tarif diferențiat (Noapte: ieftin, Restul zilei: scump).

### 5.1. Starea inițială (Generația 0)

La lansarea aplicației și generarea primei populații, soluțiile sunt dispersate pe tot graficul.

[INSEREAZĂ AICI O CAPTURĂ DE ECRAN CU APLICAȚIA – PUNCTE ÎMPRĂȘTIATE]

**Fig 1.** Interfața aplicației afișând populația inițială neoptimizată.

### 5.2. Convergența către frontul Pareto

După rularea a 100 de generații (timp de execuție mic), punctele de pe grafic se organizează într-o curbă situată în colțul stânga-jos (minimizare cost, minimizare disconfort).

[INSEREAZĂ AICI O CAPTURĂ DE ECRAN CU APLICAȚIA LA FINAL – CURBĂ PARETO]

**Fig 2.** Frontul Pareto rezultat în urma optimizării.

### 5.3. Interpretarea rezultatelor

Analizând graficul din aplicație, utilizatorul poate identifica clar compromisurile:

- **Extrema stângă (cost minim):** soluții care programează consumatorii în intervale ieftine (de regulă noaptea). Disconfortul poate fi ridicat.
- **Extrema dreaptă (disconfort minim):** soluții care respectă strict orele preferate ale utilizatorului. Costul este mai mare.
- **Zona de mijloc („knee point”):** soluții echilibrate (ex.: doar consumatorii mari, precum încărcarea EV, sunt mutați noaptea).

### 5.4. Exemplu de raportare a unei soluții (pentru documentație)

O soluție aleasă din zona de mijloc poate fi prezentată astfel:

- **Cost (RON):** ~X
- **Disconfort (ore):** ~Y
- **Ore start:** EV ~02:00, Washer ~18:00, Dryer ~20:00 (exemplu)

Valoarea exactă depinde de scenariul setat și de tarifele orare. În aplicație, aceste detalii se văd prin click pe un punct roșu (frontul Pareto) sau din tabel.

## 6. Concluzii

Implementarea algoritmului NSGA-II într-o aplicație Windows Forms demonstrează viabilitatea utilizării Inteligenței Artificiale în sisteme Smart Home:

- **Interactivitate:** vizualizarea grafică permite înțelegerea intuitivă a compromisului Pareto.
- **Performanță:** NSGA-II rulează suficient de rapid pentru update-uri periodice ale UI.
- **Utilitate:** aplicația oferă un instrument decizional concret pentru reducerea costurilor, păstrând un nivel acceptabil de confort.

## 7. Limitări și posibile îmbunătățiri

Limitări ale modelului curent:

- Nu există constrângeri de tip „fereastră de finalizare” (deadline) sau „nu porni noaptea” per aparat; se optimizează doar cele două obiective.
- Disconfortul este definit simplu ca $|Start - Preferred|$; în realitate ar putea fi neliniar (penalizare mai mare după o limită).
- Nu sunt modelate conflicte de putere (ex.: limită maximă kW simultan) sau priorități.

Îmbunătățiri posibile:

- Adăugarea de constrângeri (hard/soft) și penalizări.
- Integrare cu tarife dinamice (import din fișier/CSV).
- Export al soluției alese (text/CSV) și raport automat (capturi + tabel) pentru Word.
- Identificare automată a „knee point” (soluție compromis) și evidențiere în UI.

## 8. Bibliografie

- Deb, K., et al. (2002). *A fast and elitist multiobjective genetic algorithm: NSGA-II*. IEEE Transactions on Evolutionary Computation.
- Coello Coello, C. A. (2006). *Evolutionary multi-objective optimization: a historical view of the field*.
- Suport de curs Inteligență Artificială, Facultatea de Automatică și Calculatoare Iași, 2024–2025.

---

## Instrucțiuni practice pentru capturi (Fig 1 / Fig 2)

Recomandat (corect): rulează aplicația pe Windows și fă capturi reale:

- Fig 1: imediat după start (Generația 0)
- Fig 2: după finalizare (Generația 100)

Notă: pe macOS/Linux proiectul se poate compila, dar UI WinForms necesită Windows pentru afișare.
