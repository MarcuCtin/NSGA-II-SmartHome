using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NSGA_II_SmartHome.Algorithm;
using NSGA_II_SmartHome.Core;

namespace NSGA_II_SmartHome.UI
{
    public class MainForm : Form
    {
        private Panel _canvas = null!;
        private DataGridView _frontGrid = null!;
        private TextBox _scenarioBox = null!;

        private Button _btnStart = null!;
        private Button _btnPause = null!;
        private Button _btnStop = null!;

        private Button _btnExportPng = null!;
        private Button _btnExportCsv = null!;

        private StatusStrip _statusStrip = null!;
        private ToolStripProgressBar _progressBar = null!;
        private ToolStripStatusLabel _lblGen = null!;
        private ToolStripStatusLabel _lblFrontSize = null!;
        private ToolStripStatusLabel _lblBestCost = null!;
        private ToolStripStatusLabel _lblBestDisc = null!;
        private ToolStripStatusLabel _lblState = null!;

        private readonly Scenario _scenario;
        private readonly NSGAIIEngine _engine;
        private readonly NSGAIIParameters _parameters;

        private IReadOnlyList<Individual> _latestFront = Array.Empty<Individual>();
        private IReadOnlyList<Individual> _latestPopulation = Array.Empty<Individual>();
        private Individual? _selectedIndividual = null;

        private CancellationTokenSource? _cts;
        private bool _isPaused = false;

        public MainForm()
        {
            Text = "NSGA-II Smart Home Scheduler";
            Width = 1100;
            Height = 820;
            StartPosition = FormStartPosition.CenterScreen;

            _scenario = BuildDefaultScenario();
            _parameters = new NSGAIIParameters();
            _engine = new NSGAIIEngine(_scenario, _parameters);

            InitializeCustomControls();
        }

        private void InitializeCustomControls()
        {
            _canvas = CreateCanvas();
            _frontGrid = CreateFrontGrid();
            _scenarioBox = CreateScenarioBox();

            _btnStart = CreateButton("Start", 740, 20, OnStartClick);
            _btnPause = CreateButton("Pause", 850, 20, OnPauseClick);
            _btnStop = CreateButton("Stop", 960, 20, OnStopClick);

            _btnExportPng = CreateButton("Export Graph (PNG)", 740, 630, OnExportPngClick);
            _btnExportPng.Width = 160;

            _btnExportCsv = CreateButton("Export Selection (CSV)", 910, 630, OnExportCsvClick);
            _btnExportCsv.Width = 160;

            _btnPause.Enabled = false;
            _btnStop.Enabled = false;
            _btnExportCsv.Enabled = false;

            CreateStatusStrip();

            Controls.AddRange(new Control[] {
                _canvas, _frontGrid, _scenarioBox,
                _btnStart, _btnPause, _btnStop,
                _btnExportPng, _btnExportCsv,
                _statusStrip
            });
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

        private Button CreateButton(string text, int x, int y, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(100, 40)
            };
            btn.Click += onClick;
            return btn;
        }

        private void CreateStatusStrip()
        {
            _statusStrip = new StatusStrip();

            _progressBar = new ToolStripProgressBar { Size = new Size(200, 16) };
            _lblState = new ToolStripStatusLabel { Text = "Ready", Width = 60, BorderSides = ToolStripStatusLabelBorderSides.Right };
            _lblGen = new ToolStripStatusLabel { Text = "Gen: 0", Width = 80 };
            _lblFrontSize = new ToolStripStatusLabel { Text = "Pareto: 0", Width = 80 };
            _lblBestCost = new ToolStripStatusLabel { Text = "Min Cost: -", ForeColor = Color.DarkGreen, Width = 100 };
            _lblBestDisc = new ToolStripStatusLabel { Text = "Min Disc: -", ForeColor = Color.DarkBlue, Width = 100 };

            _statusStrip.Items.AddRange(new ToolStripItem[] {
                _lblState, _progressBar, _lblGen, _lblFrontSize, _lblBestCost, _lblBestDisc
            });
        }

        private DataGridView CreateFrontGrid()
        {
            var grid = new DataGridView
            {
                Left = 740,
                Top = 80,
                Width = 330,
                Height = 330,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.Columns.Add("Cost", "Cost");
            grid.Columns.Add("Discomfort", "Disc.");
            grid.Columns.Add("Starts", "Starts");
            grid.CellClick += FrontGrid_CellClick;
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
                ScrollBars = ScrollBars.Vertical,
                Text = DescribeScenario(_scenario)
            };
            return box;
        }

        private async void OnStartClick(object? sender, EventArgs e)
        {
            _btnStart.Enabled = false;
            _btnStop.Enabled = true;
            _btnPause.Enabled = true;
            _btnPause.Text = "Pause";
            _isPaused = false;

            _cts = new CancellationTokenSource();
            _progressBar.Maximum = _parameters.Generations;
            _progressBar.Value = 0;
            _lblState.Text = "Running";

            var progress = new Progress<GenerationSnapshot>(UpdateUi);

            try
            {
                await Task.Run(() => _engine.Run(progress, _cts.Token), _cts.Token);
                _lblState.Text = "Done";
                _progressBar.Value = _parameters.Generations;
            }
            catch (OperationCanceledException)
            {
                _lblState.Text = "Stopped";
            }
            finally
            {
                ResetControlsAfterRun();
            }
        }

        private void OnPauseClick(object? sender, EventArgs e)
        {
            if (_isPaused)
            {
                _engine.Resume();
                _btnPause.Text = "Pause";
                _lblState.Text = "Running";
                _isPaused = false;
            }
            else
            {
                _engine.Pause();
                _btnPause.Text = "Resume";
                _lblState.Text = "Paused";
                _isPaused = true;
            }
        }

        private void OnStopClick(object? sender, EventArgs e)
        {
            _cts?.Cancel();
        }

        private void OnExportPngClick(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = "ParetoFront.png" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                using var bmp = new Bitmap(_canvas.Width, _canvas.Height);
                using var g = Graphics.FromImage(bmp);

                g.Clear(Color.White);
                DrawPlot(g, _canvas.Width, _canvas.Height);

                bmp.Save(sfd.FileName, ImageFormat.Png);
                MessageBox.Show("Imagine salvată cu succes!", "Export PNG", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OnExportCsvClick(object? sender, EventArgs e)
        {
            if (_selectedIndividual == null)
            {
                MessageBox.Show("Selectați o soluție din tabel sau grafic mai întâi.", "Eroare", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog { Filter = "CSV File|*.csv", FileName = "Solutie_Aleasa.csv" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var csvContent = GenerateCsvContent(_selectedIndividual);
                File.WriteAllText(sfd.FileName, csvContent, Encoding.UTF8);
                MessageBox.Show("Fișier CSV salvat!", "Export CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string GenerateCsvContent(Individual individual)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Raport Solutie Optimizata Smart Home");
            sb.AppendLine($"Data generarii,{DateTime.Now}");
            sb.AppendLine($"Cost Total (RON),{individual.Cost}");
            sb.AppendLine($"Disconfort Total (ore),{individual.Discomfort}");
            sb.AppendLine();

            sb.AppendLine("DETALII APARATE");
            sb.AppendLine("Aparat,Durata (h),Putere (kW),Ora Preferata,Ora Start,Ora Stop,Cost Aparat (RON)");

            double totalCheck = 0;

            for (int i = 0; i < _scenario.Appliances.Count; i++)
            {
                var app = _scenario.Appliances[i];
                var start = individual.StartTimes[i];
                var end = (start + app.DurationHours) % 24;

                double appCost = 0;
                for (int h = 0; h < app.DurationHours; h++)
                {
                    int hour = (start + h) % 24;
                    appCost += app.PowerKw * _scenario.Tariffs.RateForHour(hour);
                }
                totalCheck += appCost;

                sb.AppendLine($"{app.Name},{app.DurationHours},{app.PowerKw},{app.PreferredStartHour},{start}:00,{end}:00,{appCost:0.00}");
            }

            sb.AppendLine();
            sb.AppendLine("TARIFE ORARE FOLOSITE");
            sb.AppendLine("Ora,Tarif (RON/kWh)");
            for (int h = 0; h < 24; h++)
            {
                sb.AppendLine($"{h}:00,{_scenario.Tariffs.RateForHour(h)}");
            }

            return sb.ToString();
        }

        private void ResetControlsAfterRun()
        {
            _cts?.Dispose();
            _cts = null;
            _btnStart.Enabled = true;
            _btnPause.Enabled = false;
            _btnStop.Enabled = false;
            _btnPause.Text = "Pause";
            _isPaused = false;
        }

        private void UpdateUi(GenerationSnapshot snapshot)
        {
            _latestFront = snapshot.ParetoFront;
            _latestPopulation = snapshot.Population;

            _canvas.Invalidate();
            PopulateGrid(snapshot.ParetoFront);

            _progressBar.Value = Math.Min(snapshot.Generation, _progressBar.Maximum);
            _lblGen.Text = $"Gen: {snapshot.Generation}";
            _lblFrontSize.Text = $"Pareto: {snapshot.ParetoFront.Count}";

            if (snapshot.ParetoFront.Count > 0)
            {
                _lblBestCost.Text = $"Min Cost: {snapshot.ParetoFront.Min(x => x.Cost):0.0}";
                _lblBestDisc.Text = $"Min Disc: {snapshot.ParetoFront.Min(x => x.Discomfort):0.0}";
            }
        }

        private Scenario BuildDefaultScenario()
        {
            var appliances = new List<Appliance> {
                new("Washer", 2, 1.2, 18), new("Dryer", 1, 1.0, 18),
                new("EV Charger", 4, 7.0, 18), new("Dishwasher", 2, 1.4, 20),
                new("Boiler", 3, 2.0, 7)
            };
            var rates = Enumerable.Repeat(0.9, 24).ToArray();
            for (var h = 0; h <= 5; h++) rates[h] = 0.3;
            return new Scenario(appliances, new TariffSchedule(rates));
        }

        private string DescribeScenario(Scenario s) =>
            "Scenario: prioritize 18:00 finishes, cheap night rates (0.3 RON 00-05, 0.9 otherwise)\r\n" +
            "Appliances:\r\n" + string.Join("\r\n", s.Appliances.Select(a => $"- {a.Name}: {a.DurationHours}h, {a.PowerKw}kW, pref {a.PreferredStartHour}:00"));

        private void PopulateGrid(IReadOnlyList<Individual> front)
        {
            int? selectedIndex = null;
            if (_frontGrid.SelectedRows.Count > 0) selectedIndex = _frontGrid.SelectedRows[0].Index;

            _frontGrid.Rows.Clear();
            foreach (var individual in front.Take(20))
            {
                var starts = string.Join(", ", individual.StartTimes.Select(h => $"{h:00}:00"));
                int idx = _frontGrid.Rows.Add(individual.Cost, individual.Discomfort, starts);
                _frontGrid.Rows[idx].Tag = individual;

                if (_selectedIndividual == individual) _frontGrid.Rows[idx].Selected = true;
            }
        }

        private void FrontGrid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && _frontGrid.Rows[e.RowIndex].Tag is Individual ind)
            {
                _selectedIndividual = ind;
                _btnExportCsv.Enabled = true;
                _canvas.Invalidate();
                ShowReportDialog("Detalii Soluție", GenerateAsciiSchedule(ind));
            }
        }

        private void CanvasOnPaint(object? sender, PaintEventArgs e)
        {
            DrawPlot(e.Graphics, _canvas.Width, _canvas.Height);
        }

        private void DrawPlot(Graphics g, int width, int height)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var margin = 50;
            var w = width - 2 * margin;
            var h = height - 2 * margin;

            using var pen = new Pen(Color.Black, 1.2f);
            g.DrawLine(pen, margin, margin, margin, margin + h);
            g.DrawLine(pen, margin, margin + h, margin + w, margin + h);

            var (minC, maxC, minD, maxD) = ComputeRanges();

            DrawTicks(g, margin, w, h, minC, maxC, minD, maxD);

            if (_latestPopulation.Count == 0) return;

            foreach (var ind in _latestPopulation)
                DrawPoint(g, ind, margin, w, h, minC, maxC, minD, maxD, Brushes.LightGray, 5, false);

            foreach (var ind in _latestFront)
            {
                bool isSelected = (ind == _selectedIndividual);
                var brush = isSelected ? Brushes.Blue : Brushes.Crimson;
                var size = isSelected ? 10 : 7;

                DrawPoint(g, ind, margin, w, h, minC, maxC, minD, maxD, brush, size, isSelected);
            }

            g.DrawString("Gray: Population", Font, Brushes.Gray, margin + 10, margin + 5);
            g.DrawString("Red: Pareto Front", Font, Brushes.Crimson, margin + 10, margin + 20);
            g.DrawString("Blue: Selected", Font, Brushes.Blue, margin + 10, margin + 35);
        }

        private void DrawTicks(Graphics g, int margin, int w, int h, double minC, double maxC, double minD, double maxD)
        {
            using var p = new Pen(Color.DimGray, 1); using var b = new SolidBrush(Color.Black);
            for (int i = 0; i <= 4; i++)
            {
                var x = margin + i * (w / 4f);
                g.DrawLine(p, x, margin + h, x, margin + h + 4);
                g.DrawString((minC + (maxC - minC) * i / 4).ToString("0.0"), Font, b, x - 10, margin + h + 6);

                var y = margin + h - i * (h / 4f);
                g.DrawLine(p, margin - 4, y, margin, y);
                g.DrawString((minD + (maxD - minD) * i / 4).ToString("0.0"), Font, b, 5, y - 8);
            }
            g.DrawString("Cost Total (RON)", Font, b, margin + w / 2 - 40, margin + h + 25);
            g.DrawString("Discomfort (h)", Font, b, 5, margin - 20);
        }

        private void DrawPoint(Graphics g, Individual ind, int m, int w, int h, double minC, double maxC, double minD, double maxD, Brush b, int s, bool highlight)
        {
            var x = m + (int)((ind.Cost - minC) / Math.Max(1e-6, maxC - minC) * w);
            var y = m + h - (int)((ind.Discomfort - minD) / Math.Max(1e-6, maxD - minD) * h);
            g.FillEllipse(b, x - s / 2, y - s / 2, s, s);
            if (highlight) g.DrawEllipse(Pens.Black, x - s / 2, y - s / 2, s, s);
        }

        private (double, double, double, double) ComputeRanges()
        {
            if (!_latestPopulation.Any()) return (0, 1, 0, 1);
            var costs = _latestPopulation.Select(p => p.Cost).ToList();
            var discs = _latestPopulation.Select(p => p.Discomfort).ToList();

            return (costs.Min(), costs.Max() + 0.1, discs.Min(), discs.Max() + 0.1);
        }

        private void CanvasOnMouseClick(object? sender, MouseEventArgs e)
        {
            if (!_latestFront.Any()) return;

            var (minC, maxC, minD, maxD) = ComputeRanges();
            var m = 50; var w = _canvas.Width - 2 * m; var h = _canvas.Height - 2 * m;

            Individual? closest = null;
            double bestDist = double.MaxValue;

            foreach (var ind in _latestFront)
            {
                var x = m + (ind.Cost - minC) / Math.Max(1e-6, maxC - minC) * w;
                var y = m + h - (ind.Discomfort - minD) / Math.Max(1e-6, maxD - minD) * h;
                var d = Math.Pow(e.X - x, 2) + Math.Pow(e.Y - y, 2);
                if (d < bestDist) { bestDist = d; closest = ind; }
            }

            if (closest != null && bestDist <= 225)
            {
                _selectedIndividual = closest;
                _btnExportCsv.Enabled = true;
                _canvas.Invalidate();
                ShowReportDialog("Detalii Soluție", GenerateAsciiSchedule(closest));
            }
        }

        private string GenerateAsciiSchedule(Individual individual)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Cost: {individual.Cost} RON");
            sb.AppendLine($"Disconfort: {individual.Discomfort} ore");
            sb.AppendLine(new string('-', 30));

            for (int i = 0; i < _scenario.Appliances.Count; i++)
            {
                var app = _scenario.Appliances[i];
                sb.AppendLine($"{app.Name,-12} : Start {individual.StartTimes[i]:00}:00 -> {(individual.StartTimes[i] + app.DurationHours) % 24:00}:00");
            }
            return sb.ToString();
        }

        private void ShowReportDialog(string t, string c)
        {
            using var f = new Form { Text = t, Size = new Size(400, 350), StartPosition = FormStartPosition.CenterParent };
            var txt = new TextBox { Multiline = true, Dock = DockStyle.Fill, Text = c, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 10), ReadOnly = true, BackColor = Color.White };
            f.Controls.Add(txt);
            f.ShowDialog(this);
        }
    }
}