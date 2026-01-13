using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NSGA_II_SmartHome.Algorithm;
using NSGA_II_SmartHome.Core;

namespace NSGA_II_SmartHome.UI
{
    public class MainForm : Form
    {
        private readonly Panel _canvas;
        private readonly Button _runButton;
        private readonly Label _statusLabel;
        private readonly DataGridView _frontGrid;
        private readonly TextBox _scenarioBox;

        private readonly Scenario _scenario;
        private readonly NSGAIIEngine _engine;
        private readonly NSGAIIParameters _parameters;

        private IReadOnlyList<Individual> _latestFront = Array.Empty<Individual>();
        private IReadOnlyList<Individual> _latestPopulation = Array.Empty<Individual>();
        private CancellationTokenSource? _cts;

        public MainForm()
        {
            Text = "NSGA-II Smart Home Scheduler";
            Width = 1100;
            Height = 750;
            StartPosition = FormStartPosition.CenterScreen;

            _scenario = BuildDefaultScenario();
            _parameters = new NSGAIIParameters();
            _engine = new NSGAIIEngine(_scenario, _parameters);

            _canvas = CreateCanvas();
            _runButton = CreateRunButton();
            _statusLabel = CreateStatusLabel();
            _frontGrid = CreateFrontGrid();
            _scenarioBox = CreateScenarioBox();

            Controls.AddRange(new Control[] { _canvas, _runButton, _statusLabel, _frontGrid, _scenarioBox });
        }

        private Panel CreateCanvas()
        {
            var canvas = new Panel
            {
                Left = 10,
                Top = 10,
                Width = 700,
                Height = 600,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            canvas.Paint += CanvasOnPaint;
            canvas.MouseClick += CanvasOnMouseClick;
            return canvas;
        }

        private Button CreateRunButton()
        {
            var button = new Button
            {
                Text = "Start Simulation",
                Width = 160,
                Height = 40,
                Left = 740,
                Top = 20
            };

            button.Click += async (_, _) => await ToggleRunAsync();
            return button;
        }

        private Label CreateStatusLabel()
        {
            return new Label
            {
                Text = "Ready",
                Left = 740,
                Top = 70,
                Width = 320
            };
        }

        private DataGridView CreateFrontGrid()
        {
            var grid = new DataGridView
            {
                Left = 740,
                Top = 110,
                Width = 330,
                Height = 300,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            grid.Columns.Add("Cost", "Cost");
            grid.Columns.Add("Discomfort", "Discomfort");
            grid.Columns.Add("Starts", "Start Hours");
            return grid;
        }

        private TextBox CreateScenarioBox()
        {
            var box = new TextBox
            {
                Left = 740,
                Top = 430,
                Width = 330,
                Height = 180,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            box.Text = DescribeScenario(_scenario);
            return box;
        }

        private Scenario BuildDefaultScenario()
        {
            var appliances = new List<Appliance>
            {
                new("Washer", 2, 1.2, 18),
                new("Dryer", 1, 1.0, 18),
                new("EV Charger", 4, 7.0, 18),
                new("Dishwasher", 2, 1.4, 20),
                new("Boiler", 3, 2.0, 7)
            };

            var rates = Enumerable.Repeat(0.9, 24).ToArray();
            for (var h = 0; h <= 5; h++) rates[h] = 0.3;
            var tariff = new TariffSchedule(rates);

            return new Scenario(appliances, tariff);
        }

        private string DescribeScenario(Scenario scenario)
        {
            var lines = new List<string>
            {
                "Scenario: prioritize 18:00 finishes, cheap night rates (0.3 RON 00-05, 0.9 otherwise)",
                "Appliances (Duration h, Power kW, Preferred h):"
            };

            lines.AddRange(scenario.Appliances.Select(a => $"- {a.Name}: {a.DurationHours}h, {a.PowerKw} kW, prefers {a.PreferredStartHour}:00"));
            lines.Add("Parameters: population 50, generations 100, crossover 0.9, mutation 0.05");
            return string.Join(Environment.NewLine, lines);
        }

        private async Task ToggleRunAsync()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                return;
            }

            _cts = new CancellationTokenSource();
            _runButton.Text = "Stop";
            _statusLabel.Text = "Running...";

            var progress = new Progress<GenerationSnapshot>(UpdateUi);

            try
            {
                await Task.Run(() => _engine.Run(progress, _cts.Token), _cts.Token);
                _statusLabel.Text = "Completed";
            }
            catch (OperationCanceledException)
            {
                _statusLabel.Text = "Cancelled";
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                _runButton.Text = "Start Simulation";
            }
        }

        private void UpdateUi(GenerationSnapshot snapshot)
        {
            _latestFront = snapshot.ParetoFront;
            _latestPopulation = snapshot.Population;
            _canvas.Invalidate();
            PopulateGrid(snapshot.ParetoFront);
            _statusLabel.Text = $"Generation {snapshot.Generation}/{_parameters.Generations} | Pareto size {snapshot.ParetoFront.Count}";
        }

        private void PopulateGrid(IReadOnlyList<Individual> front)
        {
            _frontGrid.Rows.Clear();
            foreach (var individual in front.Take(20))
            {
                var starts = string.Join(", ", individual.StartTimes.Select(h => $"{h:00}:00"));
                _frontGrid.Rows.Add(individual.Cost, individual.Discomfort, starts);
            }
        }

        private void CanvasOnPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var margin = 50;
            var plotWidth = _canvas.Width - 2 * margin;
            var plotHeight = _canvas.Height - 2 * margin;

            using var axisPen = new Pen(Color.Black, 1.2f);
            g.DrawLine(axisPen, margin, margin, margin, margin + plotHeight);
            g.DrawLine(axisPen, margin, margin + plotHeight, margin + plotWidth, margin + plotHeight);

            var (minCost, maxCost, minDisc, maxDisc) = ComputeRanges();
            if (_latestPopulation.Count == 0)
            {
                return;
            }

            DrawTicks(g, margin, plotWidth, plotHeight, minCost, maxCost, minDisc, maxDisc);

            foreach (var individual in _latestPopulation)
            {
                DrawPoint(g, individual, margin, plotWidth, plotHeight, minCost, maxCost, minDisc, maxDisc, Brushes.LightGray, 5);
            }

            foreach (var individual in _latestFront)
            {
                DrawPoint(g, individual, margin, plotWidth, plotHeight, minCost, maxCost, minDisc, maxDisc, Brushes.Crimson, 7);
            }

            using var legendBrush = new SolidBrush(Color.Black);
            g.DrawString("Gray: population", Font, legendBrush, margin + 10, margin + 10);
            g.DrawString("Red: Pareto front", Font, legendBrush, margin + 10, margin + 30);
        }

        private void DrawTicks(Graphics g, int margin, int plotWidth, int plotHeight, double minCost, double maxCost, double minDisc, double maxDisc)
        {
            using var tickPen = new Pen(Color.DimGray, 1);
            using var labelBrush = new SolidBrush(Color.Black);

            for (var i = 0; i <= 4; i++)
            {
                var x = margin + i * (plotWidth / 4f);
                g.DrawLine(tickPen, x, margin + plotHeight, x, margin + plotHeight + 4);
                var value = minCost + (maxCost - minCost) * i / 4;
                g.DrawString(Math.Round(value, 1).ToString(), Font, labelBrush, x - 10, margin + plotHeight + 6);

                var y = margin + plotHeight - i * (plotHeight / 4f);
                g.DrawLine(tickPen, margin - 4, y, margin, y);
                var discValue = minDisc + (maxDisc - minDisc) * i / 4;
                g.DrawString(Math.Round(discValue, 1).ToString(), Font, labelBrush, 5, y - 8);
            }

            g.DrawString("Cost (RON)", Font, labelBrush, margin + plotWidth / 2 - 30, margin + plotHeight + 24);
            g.DrawString("Discomfort (hours)", Font, labelBrush, 5, margin - 20);
        }

        private void DrawPoint(Graphics g, Individual individual, int margin, int plotWidth, int plotHeight, double minCost, double maxCost, double minDisc, double maxDisc, Brush brush, int size)
        {
            var x = margin + (int)((individual.Cost - minCost) / Math.Max(1e-6, maxCost - minCost) * plotWidth);
            var y = margin + plotHeight - (int)((individual.Discomfort - minDisc) / Math.Max(1e-6, maxDisc - minDisc) * plotHeight);

            g.FillEllipse(brush, x - size / 2, y - size / 2, size, size);
        }

        private (double minCost, double maxCost, double minDisc, double maxDisc) ComputeRanges()
        {
            if (_latestPopulation.Count == 0)
            {
                return (0, 1, 0, 1);
            }

            var costs = _latestPopulation.Select(p => p.Cost).ToList();
            var dis = _latestPopulation.Select(p => p.Discomfort).ToList();

            double minCost = costs.Min();
            double maxCost = costs.Max();
            double minDisc = dis.Min();
            double maxDisc = dis.Max();

            if (Math.Abs(maxCost - minCost) < 1e-6)
            {
                maxCost = minCost + 1;
            }

            if (Math.Abs(maxDisc - minDisc) < 1e-6)
            {
                maxDisc = minDisc + 1;
            }

            return (minCost, maxCost, minDisc, maxDisc);
        }

        private void CanvasOnMouseClick(object? sender, MouseEventArgs e)
        {
            if (_latestFront.Count == 0)
            {
                return;
            }

            var (minCost, maxCost, minDisc, maxDisc) = ComputeRanges();
            var margin = 50;
            var plotWidth = _canvas.Width - 2 * margin;
            var plotHeight = _canvas.Height - 2 * margin;

            Individual? closest = null;
            double bestDistance = double.MaxValue;

            foreach (var individual in _latestFront)
            {
                var x = margin + (individual.Cost - minCost) / Math.Max(1e-6, maxCost - minCost) * plotWidth;
                var y = margin + plotHeight - (individual.Discomfort - minDisc) / Math.Max(1e-6, maxDisc - minDisc) * plotHeight;

                var dx = e.X - x;
                var dy = e.Y - y;
                var dist = dx * dx + dy * dy;
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    closest = individual;
                }
            }

            if (closest != null && bestDistance <= 225)
            {
                var starts = string.Join(", ", closest.StartTimes.Select(h => $"{h:00}:00"));
                MessageBox.Show(
                    this,
                    $"Cost: {closest.Cost}\nDiscomfort: {closest.Discomfort}\nStart hours: {starts}",
                    "Solution details",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
    }
}
