using System;
using System.Collections.Generic;
using System.Linq;

namespace NSGA_II_SmartHome.Core
{
    public class Appliance
    {
        public string Name { get; }
        public int DurationHours { get; }
        public double PowerKw { get; }
        public int PreferredStartHour { get; }

        public Appliance(string name, int durationHours, double powerKw, int preferredStartHour)
        {
            Name = name;
            DurationHours = durationHours;
            PowerKw = powerKw;
            PreferredStartHour = preferredStartHour;
        }
    }

    public class TariffSchedule
    {
        private readonly double[] _rates;

        public TariffSchedule(IReadOnlyCollection<double> hourlyRates)
        {
            if (hourlyRates.Count != 24)
            {
                throw new ArgumentException("Tariff schedule must cover 24 hours.", nameof(hourlyRates));
            }

            _rates = hourlyRates.ToArray();
        }

        public IReadOnlyList<double> Rates => _rates;

        public double RateForHour(int hour)
        {
            var h = (hour % 24 + 24) % 24;
            return _rates[h];
        }
    }

    public class Scenario
    {
        public IReadOnlyList<Appliance> Appliances { get; }
        public TariffSchedule Tariffs { get; }

        public Scenario(IReadOnlyList<Appliance> appliances, TariffSchedule tariffs)
        {
            Appliances = appliances;
            Tariffs = tariffs;
        }
    }

    public class Individual
    {
        public int[] StartTimes { get; }
        public double Cost { get; set; }
        public double Discomfort { get; set; }
        public int Rank { get; set; }
        public double CrowdingDistance { get; set; }

        public Individual(int geneCount)
        {
            StartTimes = new int[geneCount];
        }

        public Individual Clone()
        {
            var clone = new Individual(StartTimes.Length)
            {
                Cost = Cost,
                Discomfort = Discomfort,
                Rank = Rank,
                CrowdingDistance = CrowdingDistance
            };

            Array.Copy(StartTimes, clone.StartTimes, StartTimes.Length);
            return clone;
        }
    }
}
