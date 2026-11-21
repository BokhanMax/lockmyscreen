using System.Runtime.InteropServices;

namespace LockMyScreen;

/// <summary>Мини-виджет на рабочем столе: минуты + «Старт», после старта — обратный отсчёт.</summary>
internal sealed class WidgetForm : Form
{
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 2;
    private const int WS_EX_TOOLWINDOW = 0x80;
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    private static readonly Color Background = Color.FromArgb(0x1F, 0x29, 0x37);
    private static readonly Color Accent = Color.FromArgb(0x25, 0x63, 0xEB);
    private static readonly Color Dim = Color.FromArgb(0x9C, 0xA3, 0xAF);

    private readonly NumericUpDown _minutes = new()
    {
        Minimum = 1,
        Maximum = 1439, // 23 ч 59 мин
        Value = 30,
        Width = 64,
        TextAlign = HorizontalAlignment.Center,
        Margin = new Padding(0, 3, 4, 0),
    };
    private readonly FlowLayoutPanel _input = new() { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
    private readonly Label _countdown = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleCenter,
        Font = new Font("Segoe UI", 15f, FontStyle.Bold),
        Visible = false,
    };
    private readonly Panel _slot = new() { Margin = new Padding(0, 0, 8, 0) };
    private readonly Button _button = new()
    {
        AutoSize = true,
        MinimumSize = new Size(76, 30),
        FlatStyle = FlatStyle.Flat,
        BackColor = Accent,
        ForeColor = Color.White,
        Cursor = Cursors.Hand,
    };

    private readonly Label _unit = new() { AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
    private bool _idle = true;
    private bool _loaded;

    // Прозрачность: в покое — заданная, при наведении мыши или вводе — непрозрачный.
    private readonly System.Windows.Forms.Timer _hoverTimer = new() { Interval = 100 };
    private double _restingOpacity = 1.0;

    public event Action<int>? StartRequested;
    public event Action? StopRequested;

    public WidgetForm(Icon icon, ContextMenuStrip menu)
    {
        Text = MainForm.AppName;
        Icon = icon;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10f);
        BackColor = Background;
        ForeColor = Color.White;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(10, 8, 10, 8);
        ContextMenuStrip = menu;

        _button.FlatAppearance.BorderSize = 0;

        var picture = new PictureBox
        {
            Image = new Icon(icon, 64, 64).ToBitmap(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(24, 24),
            Margin = new Padding(0, 3, 10, 0),
        };
        _input.Controls.AddRange([_minutes, _unit]);
        _slot.Controls.Add(_input);
        _slot.Controls.Add(_countdown);

        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Dock = DockStyle.Fill };
        row.Controls.AddRange([picture, _slot, _button]);
        Controls.Add(row);

        AcceptButton = _button;
        _button.Click += (_, _) =>
        {
            if (_idle) StartRequested?.Invoke((int)_minutes.Value);
            else StopRequested?.Invoke();
        };

        // Перетаскивание за любое место, кроме поля и кнопки.
        foreach (var c in new Control[] { this, row, picture, _unit, _input, _slot, _countdown })
            c.MouseDown += OnDragMouseDown;

        _minutes.Value = Settings.GetInt(Settings.WidgetMinutesKey, (int)_minutes.Minimum, (int)_minutes.Maximum) ?? 30;
        _minutes.ValueChanged += (_, _) => Settings.Set(Settings.WidgetMinutesKey, ((int)_minutes.Value).ToString());
        _hoverTimer.Tick += (_, _) => UpdateOpacity();

        ApplyTexts();
    }

    /// <summary>Прозрачность в процентах: 0 — непрозрачный.</summary>
    public void SetTransparency(int percent)
    {
        _restingOpacity = 1.0 - percent / 100.0;
        _hoverTimer.Enabled = percent > 0;
        UpdateOpacity();
    }

    private void UpdateOpacity()
    {
        bool active = Bounds.Contains(Cursor.Position) || ContainsFocus || (ContextMenuStrip?.Visible ?? false);
        var target = active ? 1.0 : _restingOpacity;
        if (Math.Abs(Opacity - target) > 0.001) Opacity = target;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _hoverTimer.Dispose();
        base.OnFormClosed(e);
    }

    public void ApplyTexts()
    {
        var s = Strings.Current;
        _unit.Text = s.UnitMinutes;
        _button.Text = _idle ? s.Start : s.Stop;

        if (!_loaded) return;
        // Ширина могла измениться — пересчитываем, сохраняя правый край на месте.
        var right = Right;
        LayoutSlot();
        Left = right - Width;
        Settings.Set(Settings.WidgetPositionKey, $"{Left},{Top}");
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW; // не показывать в Alt+Tab
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int pref = DWMWCP_ROUND;
        DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        LayoutSlot();
        Location = LoadPosition() ?? DefaultPosition();
        _loaded = true;
    }

    /// <summary>Место под поле ввода и под отсчёт одинаковое, чтобы виджет не «прыгал».</summary>
    private void LayoutSlot()
    {
        var input = _input.PreferredSize;
        var time = TextRenderer.MeasureText("00:00:00", _countdown.Font);
        _slot.Size = new Size(Math.Max(input.Width, time.Width + 8), Math.Max(input.Height, _button.PreferredSize.Height));
        _input.Location = new Point((_slot.Width - input.Width) / 2, (_slot.Height - input.Height) / 2);
        PerformLayout();
    }

    private Point? LoadPosition()
    {
        var parts = Settings.Get(Settings.WidgetPositionKey)?.Split(',');
        if (parts is null || parts.Length != 2
            || !int.TryParse(parts[0], out var x) || !int.TryParse(parts[1], out var y))
            return null;

        // Если монитор отключили и виджет оказался за пределами экранов — игнорируем.
        var center = new Point(x + Width / 2, y + Height / 2);
        return Screen.AllScreens.Any(s => s.WorkingArea.Contains(center)) ? new Point(x, y) : null;
    }

    private Point DefaultPosition()
    {
        var area = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        return new Point(area.Right - Width - 16, area.Bottom - Height - 16);
    }

    private void OnDragMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero); // возвращается, когда отпустили мышь
        Settings.Set(Settings.WidgetPositionKey, $"{Left},{Top}");
    }

    public void ShowIdle()
    {
        _idle = true;
        _countdown.Visible = false;
        _input.Visible = true;
        _button.Text = Strings.Current.Start;
        _button.BackColor = Accent;
    }

    public void ShowCountdown(string text, bool dimmed)
    {
        _idle = false;
        _input.Visible = false;
        _countdown.Visible = true;
        if (_countdown.Text != text) _countdown.Text = text;
        _countdown.ForeColor = dimmed ? Dim : Color.White;
        _button.Text = Strings.Current.Stop;
        _button.BackColor = Color.FromArgb(0x37, 0x41, 0x51);
    }
}
