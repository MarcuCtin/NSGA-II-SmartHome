using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using NSGA_II_SmartHome.Core;

namespace NSGA_II_SmartHome.UI
{
    public class CustomScenarioForm : Form
    {
        private readonly BindingList<ApplianceRow> _appliances = new();

        private TextBox _nameBox = null!;
        private NumericUpDown _durationBox = null!;
        private NumericUpDown _powerBox = null!;
        private NumericUpDown _preferredBox = null!;
        private DataGridView _grid = null!;
        private TextBox _tariffsBox = null!;

        public Scenario? Result { get; private set; }

        public CustomScenarioForm(Scenario? seed = null)
        {
            Text = "Scenariu personalizat";
            Width = 720;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;

            InitializeInputs();
            InitializeGrid();
            InitializeTariffsArea();
            InitializeButtons();

            PrefillFromSeed(seed);
        }

        private void InitializeInputs()
        {
            var nameLabel = new Label { Text = "Aparat", Left = 10, Top = 12, AutoSize = true };
            _nameBox = new TextBox { Left = 10, Top = 30, Width = 140 };

            var durationLabel = new Label { Text = "Durata (h)", Left = 170, Top = 12, AutoSize = true };
            _durationBox = new NumericUpDown { Left = 170, Top = 30, Width = 70, Minimum = 1, Maximum = 24, Value = 1 };

            var powerLabel = new Label { Text = "Putere (kW)", Left = 260, Top = 12, AutoSize = true };
            _powerBox = new NumericUpDown { Left = 260, Top = 30, Width = 80, Minimum = 0.1M, Maximum = 20M, DecimalPlaces = 2, Increment = 0.1M, Value = 1 };

            var preferredLabel = new Label { Text = "Ora preferata", Left = 360, Top = 12, AutoSize = true };
            _preferredBox = new NumericUpDown { Left = 360, Top = 30, Width = 80, Minimum = 0, Maximum = 23, Value = 18 };

            var addButton = new Button { Text = "Adauga aparatul", Left = 470, Top = 25, Width = 140 };
            addButton.Click += OnAddAppliance;

            Controls.AddRange(new Control[] { nameLabel, _nameBox, durationLabel, _durationBox, powerLabel, _powerBox, preferredLabel, _preferredBox, addButton });
        }

        private void InitializeGrid()
        {
            _grid = new DataGridView
            {
                Left = 10,
                Top = 70,
                Width = 680,
                Height = 220,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false
            };

            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Aparat", DataPropertyName = nameof(ApplianceRow.Name), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Durata (h)", DataPropertyName = nameof(ApplianceRow.DurationHours), Width = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Putere (kW)", DataPropertyName = nameof(ApplianceRow.PowerKw), Width = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ora pref.", DataPropertyName = nameof(ApplianceRow.PreferredStartHour), Width = 90 });

            _grid.DataSource = _appliances;

            var removeButton = new Button { Text = "Sterge selectia", Left = 10, Top = 300, Width = 140 };
            removeButton.Click += OnRemoveSelected;

            Controls.AddRange(new Control[] { _grid, removeButton });
        }

        private void InitializeTariffsArea()
        {
            var label = new Label
            {
                Text = "Tarife 24h (separate prin virgula sau spatiu)",
                Left = 10,
                Top = 330,
                AutoSize = true
            };

            _tariffsBox = new TextBox
            {
                Left = 10,
                Top = 350,
                Width = 680,
                Height = 90,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            Controls.AddRange(new Control[] { label, _tariffsBox });
        }

        private void InitializeButtons()
        {
            var sampleButton = new Button { Text = "Tarife noapte/zi", Left = 10, Top = 450, Width = 140 };
            sampleButton.Click += (_, _) => _tariffsBox.Text = BuildSampleTariffs();

            var saveButton = new Button { Text = "Salveaza scenariu", Left = 440, Top = 450, Width = 250 };
            saveButton.Click += OnSave;

            Controls.AddRange(new Control[] { sampleButton, saveButton });
        }

        private void PrefillFromSeed(Scenario? seed)
        {
            if (seed != null)
            {
                foreach (var app in seed.Appliances)
                {
                    _appliances.Add(new ApplianceRow(app.Name, app.DurationHours, app.PowerKw, app.PreferredStartHour));
                }

                _tariffsBox.Text = FormatRates(seed.Tariffs.Rates);
                return;
            }

            _tariffsBox.Text = BuildSampleTariffs();
        }

        private void OnAddAppliance(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_nameBox.Text))
            {
                MessageBox.Show("Completeaza numele aparatului.", "Camp lipsa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _appliances.Add(new ApplianceRow(_nameBox.Text.Trim(), (int)_durationBox.Value, (double)_powerBox.Value, (int)_preferredBox.Value));
            _nameBox.Clear();
        }

        private void OnRemoveSelected(object? sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0)
            {
                return;
            }

            foreach (DataGridViewRow row in _grid.SelectedRows)
            {
                if (row.DataBoundItem is ApplianceRow item)
                {
                    _appliances.Remove(item);
                }
            }
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (_appliances.Count == 0)
            {
                MessageBox.Show("Adauga cel putin un aparat.", "Scenariu incomplet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var rates = ParseTariffs();
            if (rates == null)
            {
                return;
            }

            var appliances = _appliances
                .Select(a => new Appliance(a.Name, a.DurationHours, a.PowerKw, a.PreferredStartHour))
                .ToList();

            try
            {
                Result = new Scenario(appliances, new TariffSchedule(rates));
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Eroare scenariu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private double[]? ParseTariffs()
        {
            var tokens = _tariffsBox.Text
                .Split(new[] { ',', ';', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length != 24)
            {
                MessageBox.Show("Trebuie sa existe exact 24 valori de tarif.", "Format invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            var rates = new double[24];
            for (int i = 0; i < 24; i++)
            {
                if (!double.TryParse(tokens[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var rate))
                {
                    MessageBox.Show($"Tarif invalid la pozitia {i}. Foloseste punct pentru zecimale.", "Format invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }
                rates[i] = rate;
            }

            return rates;
        }

        private static string FormatRates(IReadOnlyList<double> rates)
        {
            return string.Join(", ", rates.Select(r => r.ToString("0.###", CultureInfo.InvariantCulture)));
        }

        private static string BuildSampleTariffs()
        {
            var values = new double[24];
            for (var h = 0; h < 24; h++)
            {
                values[h] = h <= 5 ? 0.3 : 0.9;
            }
            return FormatRates(values);
        }

        private record ApplianceRow(string Name, int DurationHours, double PowerKw, int PreferredStartHour);
    }
}
