namespace biblioteket.ChromeImporter;

public class MainForm : Form
{
    private TextBox     _apiAdressTextBox   = null!;
    private Button      _sökButton          = null!;
    private Button      _importeraButton    = null!;
    private Button      _markeraAllaButton  = null!;
    private Button      _avmarkeraButton    = null!;
    private DataGridView _grid              = null!;
    private Label       _statusLabel        = null!;
    private ProgressBar _progressBar        = null!;

    private const int KolumnMarkera     = 0;
    private const int KolumnUrl         = 1;
    private const int KolumnTitel       = 2;
    private const int KolumnSenastBesökt = 3;

    public MainForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text            = "Biblioteket – Chrome-importerare";
        Size            = new Size(960, 620);
        MinimumSize     = new Size(720, 480);
        StartPosition   = FormStartPosition.CenterScreen;

        var ytterPanel = new TableLayoutPanel
        {
            Dock     = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding  = new Padding(10)
        };
        ytterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // Rad 0: API-adress + Sök
        ytterPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // Rad 1: Grid
        ytterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // Rad 2: Progressbar
        ytterPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // Rad 3: Status + knappar

        // ── Rad 0: API-adress + sök ──────────────────────────────────────────
        var toppPanel = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            AutoSize      = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin        = new Padding(0, 0, 0, 6)
        };

        toppPanel.Controls.Add(new Label
        {
            Text    = "API-adress:",
            AutoSize = true,
            Padding = new Padding(0, 6, 4, 0)
        });

        _apiAdressTextBox = new TextBox
        {
            Text   = Program.ApiAdress,
            Width  = 260,
            Margin = new Padding(0, 4, 10, 4)
        };
        toppPanel.Controls.Add(_apiAdressTextBox);

        _sökButton = new Button
        {
            Text     = "Sök i Chrome-historik",
            AutoSize = true,
            Margin   = new Padding(0, 4, 0, 4)
        };
        _sökButton.Click += SökButton_Click;
        toppPanel.Controls.Add(_sökButton);

        ytterPanel.Controls.Add(toppPanel, 0, 0);

        // ── Rad 1: DataGridView ───────────────────────────────────────────────
        _grid = new DataGridView
        {
            Dock                    = DockStyle.Fill,
            AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode           = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect             = true,
            ReadOnly                = false,
            AllowUserToAddRows      = false,
            AllowUserToDeleteRows   = false,
            RowHeadersVisible       = false,
            BackgroundColor         = SystemColors.Window
        };

        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText       = "Importera",
            Width            = 75,
            AutoSizeMode     = DataGridViewAutoSizeColumnMode.None,
            ThreeState       = false
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText   = "URL",
            ReadOnly     = true,
            FillWeight   = 50
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText   = "Titel",
            ReadOnly     = true,
            FillWeight   = 35
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText       = "Senast besökt",
            ReadOnly         = true,
            Width            = 140,
            AutoSizeMode     = DataGridViewAutoSizeColumnMode.None
        });

        ytterPanel.Controls.Add(_grid, 0, 1);

        // ── Rad 2: Progressbar ───────────────────────────────────────────────
        _progressBar = new ProgressBar
        {
            Dock    = DockStyle.Fill,
            Visible = false,
            Margin  = new Padding(0, 4, 0, 4)
        };
        ytterPanel.Controls.Add(_progressBar, 0, 2);

        // ── Rad 3: Urval + status + importera ────────────────────────────────
        var bottenPanel = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 1,
            AutoSize    = true,
            Margin      = new Padding(0, 6, 0, 0)
        };
        bottenPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottenPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var vänsterBotten = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            AutoSize      = true,
            FlowDirection = FlowDirection.LeftToRight
        };

        _markeraAllaButton = new Button
        {
            Text     = "Markera alla",
            AutoSize = true,
            Enabled  = false,
            Margin   = new Padding(0, 0, 6, 0)
        };
        _markeraAllaButton.Click += (_, _) => MarkAllaRader(markera: true);
        vänsterBotten.Controls.Add(_markeraAllaButton);

        _avmarkeraButton = new Button
        {
            Text     = "Avmarkera alla",
            AutoSize = true,
            Enabled  = false
        };
        _avmarkeraButton.Click += (_, _) => MarkAllaRader(markera: false);
        vänsterBotten.Controls.Add(_avmarkeraButton);

        _statusLabel = new Label
        {
            Text      = "Redo.",
            AutoSize  = true,
            Padding   = new Padding(10, 4, 0, 0)
        };
        vänsterBotten.Controls.Add(_statusLabel);

        bottenPanel.Controls.Add(vänsterBotten, 0, 0);

        _importeraButton = new Button
        {
            Text     = "Importera markerade",
            AutoSize = true,
            Enabled  = false
        };
        _importeraButton.Click += ImporteraButton_Click;
        bottenPanel.Controls.Add(_importeraButton, 1, 0);

        ytterPanel.Controls.Add(bottenPanel, 0, 3);

        Controls.Add(ytterPanel);
    }

    // ── Händelsehanterare ─────────────────────────────────────────────────────

    private void SökButton_Click(object? sender, EventArgs e)
    {
        _grid.Rows.Clear();
        _importeraButton.Enabled  = false;
        _markeraAllaButton.Enabled = false;
        _avmarkeraButton.Enabled  = false;
        _sökButton.Enabled        = false;
        _statusLabel.Text         = "Söker i Chrome-historik…";

        try
        {
            var tjänst  = new ChromeHistoryService();
            var poster  = tjänst.HämtaLegimusPoster();

            if (poster.Count == 0)
            {
                _statusLabel.Text = "Inga Legimus-böcker hittades i Chrome-historiken.";
                return;
            }

            foreach (var post in poster)
            {
                _grid.Rows.Add(
                    true,
                    post.Url,
                    post.Titel,
                    post.SenastBesökt.ToString("g"));
            }

            _statusLabel.Text         = $"{poster.Count} Legimus-bok(böcker) hittades.";
            _importeraButton.Enabled  = true;
            _markeraAllaButton.Enabled = true;
            _avmarkeraButton.Enabled  = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Fel vid läsning av Chrome-historik:\n\n{ex.Message}",
                "Fel",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _statusLabel.Text = "Sökning misslyckades.";
        }
        finally
        {
            _sökButton.Enabled = true;
        }
    }

    private async void ImporteraButton_Click(object? sender, EventArgs e)
    {
        var valda = _grid.Rows
            .Cast<DataGridViewRow>()
            .Where(r => r.Cells[KolumnMarkera].Value is true)
            .Select(r => r.Cells[KolumnUrl].Value?.ToString() ?? string.Empty)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .ToList();

        if (valda.Count == 0)
        {
            MessageBox.Show(
                "Markera minst en bok att importera.",
                "Ingen markerad",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        _importeraButton.Enabled  = false;
        _sökButton.Enabled        = false;
        _markeraAllaButton.Enabled = false;
        _avmarkeraButton.Enabled  = false;
        _progressBar.Visible      = true;
        _progressBar.Maximum      = valda.Count;
        _progressBar.Value        = 0;

        var klient  = new ApiClient(_apiAdressTextBox.Text);
        var lyckade = 0;
        var misslyckade = 0;

        foreach (var url in valda)
        {
            _statusLabel.Text = $"Importerar: {url}";
            var ok = await klient.LäggTillLegimusUrlAsync(url);
            if (ok) lyckade++; else misslyckade++;
            _progressBar.Value++;
        }

        _progressBar.Visible      = false;
        _importeraButton.Enabled  = true;
        _sökButton.Enabled        = true;
        _markeraAllaButton.Enabled = true;
        _avmarkeraButton.Enabled  = true;

        var meddelande = $"Klart! {lyckade} bok(böcker) importerades.";
        if (misslyckade > 0)
            meddelande += $"\n{misslyckade} misslyckades – kontrollera att API:et körs.";

        _statusLabel.Text = meddelande.Replace("\n", " ");
        MessageBox.Show(meddelande, "Import klar", MessageBoxButtons.OK,
            misslyckade > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    private void MarkAllaRader(bool markera)
    {
        foreach (DataGridViewRow rad in _grid.Rows)
            rad.Cells[KolumnMarkera].Value = markera;
    }
}