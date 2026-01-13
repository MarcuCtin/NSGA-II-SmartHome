using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NSGA_II_SmartHome.Core;

namespace NSGA_II_SmartHome.Algorithm
{
    public record NSGAIIParameters(int PopulationSize = 50, int Generations = 100, double CrossoverRate = 0.9, double MutationRate = 0.05);

    public record GenerationSnapshot(int Generation, IReadOnlyList<Individual> ParetoFront, IReadOnlyList<Individual> Population);

    public class NSGAIIEngine
    {
        private readonly Scenario _scenario;
        private readonly NSGAIIParameters _parameters;
        private readonly Random _random;

        // Eveniment pentru Pauz? (Set = ruleaz?, Reset = pauz?)
        private readonly ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true);

        public NSGAIIEngine(Scenario scenario, NSGAIIParameters? parameters = null, int? seed = null)
        {
            _scenario = scenario;
            _parameters = parameters ?? new NSGAIIParameters();
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        // Metode pentru control extern
        public void Pause() => _pauseEvent.Reset();
        public void Resume() => _pauseEvent.Set();

        public IReadOnlyList<Individual> Run(IProgress<GenerationSnapshot>? progress = null, CancellationToken cancellationToken = default)
        {
            // Ne asigur?m c? nu este în pauz? la startul unei noi rul?ri
            _pauseEvent.Set();

            var population = InitializePopulation();
            EvaluatePopulation(population);

            ReportProgress(progress, population, 0);

            for (var generation = 0; generation < _parameters.Generations; generation++)
            {
                // 1. Verific?m dac? s-a cerut STOP
                cancellationToken.ThrowIfCancellationRequested();

                // 2. Verific?m dac? s-a cerut PAUZ? (a?teapt? aici dac? e cazul)
                // Dac? token-ul de anulare vine în timp ce e în pauz?, va arunca excep?ie ?i va opri totul corect.
                _pauseEvent.Wait(cancellationToken);

                // 3. [IMPORTANT] Întârziere artificial? pentru a putea vedea progresul ?i a folosi butoanele
                // Po?i ajusta valoarea (ex: 20ms = foarte rapid, 100ms = lent)
                Thread.Sleep(50);

                var offspring = CreateOffspring(population);
                EvaluatePopulation(offspring);

                var combined = population.Concat(offspring).ToList();
                var fronts = NonDominatedSort(combined);

                var nextPopulation = new List<Individual>(_parameters.PopulationSize);
                foreach (var front in fronts)
                {
                    CalculateCrowdingDistance(front);
                    if (nextPopulation.Count + front.Count <= _parameters.PopulationSize)
                    {
                        nextPopulation.AddRange(front);
                    }
                    else
                    {
                        var ordered = front.OrderByDescending(i => i.CrowdingDistance).ToList();
                        var remaining = _parameters.PopulationSize - nextPopulation.Count;
                        nextPopulation.AddRange(ordered.Take(remaining));
                        break;
                    }
                }

                population = nextPopulation;
                ReportProgress(progress, population, generation + 1);
            }

            return population;
        }

        private List<Individual> InitializePopulation()
        {
            var individuals = new List<Individual>(_parameters.PopulationSize);
            for (var i = 0; i < _parameters.PopulationSize; i++)
            {
                var individual = new Individual(_scenario.Appliances.Count);
                for (var gene = 0; gene < individual.StartTimes.Length; gene++)
                {
                    individual.StartTimes[gene] = _random.Next(0, 24);
                }

                individuals.Add(individual);
            }

            return individuals;
        }

        private void EvaluatePopulation(IEnumerable<Individual> population)
        {
            foreach (var individual in population)
            {
                EvaluateIndividual(individual);
            }
        }

        private void EvaluateIndividual(Individual individual)
        {
            double cost = 0;
            double discomfort = 0;
            double[] hourlyLoad = new double[24];

            int washerIndex = -1;
            int dryerIndex = -1;

            for (var i = 0; i < _scenario.Appliances.Count; i++)
            {
                var appliance = _scenario.Appliances[i];

                if (appliance.Name.Contains("Washer", StringComparison.OrdinalIgnoreCase)) washerIndex = i;
                if (appliance.Name.Contains("Dryer", StringComparison.OrdinalIgnoreCase)) dryerIndex = i;

                var start = individual.StartTimes[i];

                for (var h = 0; h < appliance.DurationHours; h++)
                {
                    var hour = (start + h) % 24;

                    cost += appliance.PowerKw * _scenario.Tariffs.RateForHour(hour);
                    hourlyLoad[hour] += appliance.PowerKw;

                    if (hour >= 0 && hour < 6)
                    {
                        discomfort += 0.5;
                    }
                }

                int diff = Math.Abs(start - appliance.PreferredStartHour);
                int circularDiff = Math.Min(diff, 24 - diff);
                discomfort += circularDiff;
            }

            if (hourlyLoad.Any(load => load > 9.0))
            {
                cost += 10000;
                discomfort += 1000;
            }

            if (washerIndex != -1 && dryerIndex != -1)
            {
                var washerEnd = individual.StartTimes[washerIndex] + _scenario.Appliances[washerIndex].DurationHours;

                if (individual.StartTimes[dryerIndex] < washerEnd)
                {
                    discomfort += 50;
                }
            }

            individual.Cost = Math.Round(cost, 3);
            individual.Discomfort = Math.Round(discomfort, 3);
        }

        private List<List<Individual>> NonDominatedSort(List<Individual> population)
        {
            var fronts = new List<List<Individual>>();
            var dominationCounts = new Dictionary<Individual, int>();
            var dominatedSets = new Dictionary<Individual, List<Individual>>();

            var firstFront = new List<Individual>();
            foreach (var p in population)
            {
                dominatedSets[p] = new List<Individual>();
                dominationCounts[p] = 0;

                foreach (var q in population)
                {
                    if (p == q) continue;

                    if (Dominates(p, q))
                    {
                        dominatedSets[p].Add(q);
                    }
                    else if (Dominates(q, p))
                    {
                        dominationCounts[p]++;
                    }
                }

                if (dominationCounts[p] == 0)
                {
                    p.Rank = 1;
                    firstFront.Add(p);
                }
            }

            fronts.Add(firstFront);
            var current = 0;
            while (fronts.ElementAtOrDefault(current)?.Count > 0)
            {
                var next = new List<Individual>();
                foreach (var p in fronts[current])
                {
                    foreach (var q in dominatedSets[p])
                    {
                        dominationCounts[q]--;
                        if (dominationCounts[q] == 0)
                        {
                            q.Rank = current + 2;
                            next.Add(q);
                        }
                    }
                }

                if (next.Count > 0)
                {
                    fronts.Add(next);
                }

                current++;
            }

            return fronts;
        }

        private static bool Dominates(Individual a, Individual b)
        {
            return a.Cost <= b.Cost && a.Discomfort <= b.Discomfort && (a.Cost < b.Cost || a.Discomfort < b.Discomfort);
        }

        private void CalculateCrowdingDistance(IReadOnlyList<Individual> front)
        {
            if (front.Count == 0)
            {
                return;
            }

            foreach (var individual in front)
            {
                individual.CrowdingDistance = 0;
            }

            void AssignDistance(Func<Individual, double> selector)
            {
                var ordered = front.OrderBy(selector).ToList();
                ordered.First().CrowdingDistance = double.PositiveInfinity;
                ordered.Last().CrowdingDistance = double.PositiveInfinity;

                var min = selector(ordered.First());
                var max = selector(ordered.Last());
                if (Math.Abs(max - min) < 1e-9)
                {
                    return;
                }

                for (var i = 1; i < ordered.Count - 1; i++)
                {
                    var distance = (selector(ordered[i + 1]) - selector(ordered[i - 1])) / (max - min);
                    ordered[i].CrowdingDistance += distance;
                }
            }

            AssignDistance(i => i.Cost);
            AssignDistance(i => i.Discomfort);
        }

        private List<Individual> CreateOffspring(IReadOnlyList<Individual> population)
        {
            var offspring = new List<Individual>(_parameters.PopulationSize);
            while (offspring.Count < _parameters.PopulationSize)
            {
                var parentA = BinaryTournament(population);
                var parentB = BinaryTournament(population);

                var child1 = parentA.Clone();
                var child2 = parentB.Clone();

                if (_random.NextDouble() < _parameters.CrossoverRate)
                {
                    SinglePointCrossover(child1, child2);
                }

                Mutate(child1);
                Mutate(child2);

                offspring.Add(child1);
                if (offspring.Count < _parameters.PopulationSize)
                {
                    offspring.Add(child2);
                }
            }

            return offspring;
        }

        private Individual BinaryTournament(IReadOnlyList<Individual> population)
        {
            var a = population[_random.Next(population.Count)];
            var b = population[_random.Next(population.Count)];

            if (a.Rank != b.Rank)
            {
                return a.Rank < b.Rank ? a : b;
            }

            return a.CrowdingDistance >= b.CrowdingDistance ? a : b;
        }

        private void SinglePointCrossover(Individual child1, Individual child2)
        {
            var point = _random.Next(1, child1.StartTimes.Length);
            for (var i = point; i < child1.StartTimes.Length; i++)
            {
                (child1.StartTimes[i], child2.StartTimes[i]) = (child2.StartTimes[i], child1.StartTimes[i]);
            }
        }

        private void Mutate(Individual individual)
        {
            for (var i = 0; i < individual.StartTimes.Length; i++)
            {
                if (_random.NextDouble() < _parameters.MutationRate)
                {
                    individual.StartTimes[i] = _random.Next(0, 24);
                }
            }
        }

        private void ReportProgress(IProgress<GenerationSnapshot>? progress, IReadOnlyList<Individual> population, int generation)
        {
            if (progress == null)
            {
                return;
            }

            var fronts = NonDominatedSort(population.ToList());
            if (fronts.Count > 0)
            {
                CalculateCrowdingDistance(fronts[0]);
                progress.Report(new GenerationSnapshot(generation, fronts[0], population));
            }
        }
    }
}