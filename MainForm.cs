using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace LockMyScreen;

public sealed class MainForm : Form
{
    internal const string AppName = "LockMyScreen";

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LockWorkStation();

    private enum State { Idle, Running, Paused, Waiting }

    // Настройки
    // Тексты всех элементов задаются в ApplyTexts() — на текущем языке.
    private readonly Label _whenCaption = CreateCaption();
    private readonly Label _actionCaption = CreateCaption();
    private readonly RadioButton _afterRadio = new() { AutoSize = true, Checked = true };
    private readonly RadioButton _atRadio = new() { AutoSize = true };
    private readonly RadioButton _lockRadio = new() { AutoSize = true, Checked = true };
    private readonly RadioButton _remindRadio = new() { AutoSize = true };
    private readonly CheckBox _repeatCheck = new() { AutoSize = true };
    private readonly NumericUpDown _hours = CreateSpin(23);
    private readonly NumericUpDown _minutes = CreateSpin(59);
    private readonly NumericUpDown _seconds = CreateSpin(59);
    private readonly Label _unitHours = CreateUnitLabel();
    private readonly Label _unitMinutes = CreateUnitLabel();
    private readonly Label _unitSeconds = CreateUnitLabel();
    private readonly FlowLayoutPanel _presets = CreateRow(centered: true);
    private readonly List<(Button Button, int Minutes)> _presetButtons = [];

    // Меню трея и виджета
    private readonly ContextMenuStrip _menu = new();
    private readonly ToolStripMenuItem _menuOpen = new();
    private readonly ToolStripMenuItem _menuLanguage = new();
    private readonly ToolStripMenuItem _menuShowWidget = new() { CheckOnClick = true };
    private readonly ToolStripMenuItem _menuTransparency = new();
    private readonly ToolStripMenuItem _menuExit = new();
    private static readonly int[] TransparencyLevels = [0, 15, 30, 50];

    // Отображение и управление
    private readonly Label _display = new()
    {
        AutoSize = true,
        Anchor = AnchorStyles.None,
        Font = new Font("Segoe UI", 32f, FontStyle.Bold),
        Margin = new Padding(0, 8, 0, 0),
    };
    private readonly Label _status = new()
    {
        AutoSize = true,
        Anchor = AnchorStyles.None,
        ForeColor = SystemColors.GrayText,
        Margin = new Padding(0, 0, 0, 8),
    };
    private readonly Button _startPause = new() { AutoSize = true, MinimumSize = new Size(110, 0) };
    private readonly Button _reset = new() { AutoSize = true, MinimumSize = new Size(110, 0) };

    private readonly System.Windows.Forms.Timer _tick = new() { Interval = 200 };
    private readonly NotifyIcon _tray = new();
    private readonly ToolTip _tips = new();
    private readonly Icon _icon = LoadAppIcon();

    private State _state = State.Idle;
    private DateTime _endUtc;
    private TimeSpan _remaining;
    private bool _trayHintShown;
    private bool _allowShow; // при запуске главное окно скрыто, пока его не откроют из трея
    private bool _exitWithoutQuestions;
    private bool _suspendSave = true; // не сохранять, пока загружаем настройки
    private ReminderForm? _reminder;
    private readonly WidgetForm _widget;

    private bool AtTime => _atRadio.Checked;
    private bool LockAction => _lockRadio.Checked;
    private bool Repeat => _repeatCheck.Checked;

    public MainForm()
    {
        Text = AppName;
        Font = new Font("Segoe UI", 10f);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);
        Icon = _icon;

        var whenRow = CreateRow();
        whenRow.Controls.AddRange([_whenCaption, _afterRadio, _atRadio]);

        var spinRow = CreateRow(centered: true);
        spinRow.Controls.AddRange([
            _hours, _unitHours,
            _minutes, _unitMinutes,
            _seconds, _unitSeconds,
        ]);

        foreach (var minutes in new[] { 5, 15, 30, 60 })
        {
            var b = new Button { AutoSize = true, MinimumSize = new Size(70, 0) };
            b.Click += (_, _) => SetSpins(TimeSpan.FromMinutes(minutes));
            _presets.Controls.Add(b);
            _presetButtons.Add((b, minutes));
        }

        var actionRow = CreateRow();
        actionRow.Controls.AddRange([_actionCaption, _lockRadio, _remindRadio]);

        var repeatRow = CreateRow();
        repeatRow.Controls.Add(_repeatCheck);

        var buttonRow = CreateRow(centered: true);
        buttonRow.Controls.AddRange([_startPause, _reset]);

        var root = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };
        root.Controls.AddRange([whenRow, spinRow, _presets, actionRow, repeatRow, _display, _status, buttonRow]);
        Controls.Add(root);

        // Меню (общее для трея и виджета)
        _menuOpen.Click += (_, _) => ShowFromTray();
        _menuExit.Click += (_, _) => Close();
        foreach (Language language in Enum.GetValues(typeof(Language)))
        {
            var item = new ToolStripMenuItem(Strings.For(language).NativeName) { Tag = language };
            item.Click += (_, _) => ChangeLanguage(language);
            _menuLanguage.DropDownItems.Add(item);
        }
        _menuShowWidget.CheckedChanged += (_, _) => OnShowWidgetChanged();
        foreach (var level in TransparencyLevels)
        {
            var item = new ToolStripMenuItem { Tag = level };
            item.Click += (_, _) => SetWidgetTransparency(level);
            _menuTransparency.DropDownItems.Add(item);
        }
        _menu.Items.AddRange([
            _menuOpen, _menuShowWidget, _menuTransparency, _menuLanguage,
            new ToolStripSeparator(), _menuExit,
        ]);

        // Трей
        _tray.Icon = new Icon(_icon, SystemInformation.SmallIconSize); // чёткий маленький размер для трея
        _tray.Text = AppName;
        _tray.ContextMenuStrip = _menu;
        _tray.Visible = true;
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) ShowFromTray(); };

        // Виджет на рабочем столе
        _widget = new WidgetForm(_icon, _menu);
        _widget.StartRequested += OnWidgetStart;
        _widget.StopRequested += () => ApplyState(State.Idle);

        // События
        foreach (var spin in new[] { _hours, _minutes, _seconds })
            spin.ValueChanged += (_, _) => { UpdateIdleDisplay(); SaveTimerSettings(); };
        _atRadio.CheckedChanged += (_, _) => OnWhenModeChanged();
        _lockRadio.CheckedChanged += (_, _) => SaveTimerSettings();
        _repeatCheck.CheckedChanged += (_, _) => SaveTimerSettings();
        _startPause.Click += (_, _) => OnStartPause();
        _reset.Click += (_, _) => ApplyState(State.Idle);
        _tick.Tick += (_, _) => OnTick();
        SystemEvents.SessionSwitch += OnSessionSwitch;
        Strings.Changed += ApplyTexts;

        AcceptButton = _startPause;
        LoadSettings();
        ApplyTexts();
    }

    // ---------- Настройки ----------

    private void LoadSettings()
    {
        _suspendSave = true;
        _atRadio.Checked = Settings.Get(Settings.WhenModeKey) == "At";
        _remindRadio.Checked = Settings.Get(Settings.ActionKey) == "Remind";
        _repeatCheck.Checked = Settings.Get(Settings.RepeatKey) == "1";
        LoadSpinsForMode();
        _suspendSave = false;

        SetWidgetTransparency(Settings.GetInt(Settings.WidgetTransparencyKey, 0, 90) ?? 0);
        // Последним: включение галочки покажет виджет.
        _menuShowWidget.Checked = Settings.Get(Settings.WidgetVisibleKey) != "0";
    }

    /// <summary>Для каждого режима своё запомненное время: длительность для «через», время суток для «в».</summary>
    private void LoadSpinsForMode()
    {
        if (AtTime)
        {
            var saved = Settings.GetInt(Settings.AtTimeKey, 0, 86399);
            if (saved is int seconds)
            {
                SetSpins(TimeSpan.FromSeconds(seconds));
            }
            else
            {
                // Впервые: «сейчас + 30 минут», округлённое до минуты.
                var t = DateTime.Now.AddMinutes(30);
                SetSpins(new TimeSpan(t.Hour, t.Minute, 0));
            }
        }
        else
        {
            SetSpins(TimeSpan.FromSeconds(Settings.GetInt(Settings.DurationKey, 1, 86399) ?? 30 * 60));
        }
    }

    private void SaveTimerSettings()
    {
        if (_suspendSave || _state != State.Idle) return;
        Settings.Set(
            (Settings.WhenModeKey, AtTime ? "At" : "After"),
            (Settings.ActionKey, LockAction ? "Lock" : "Remind"),
            (Settings.RepeatKey, Repeat ? "1" : "0"),
            (AtTime ? Settings.AtTimeKey : Settings.DurationKey, ((int)SelectedTime.TotalSeconds).ToString()));
    }

    // ---------- Виджет: показ и прозрачность ----------

    private void OnShowWidgetChanged()
    {
        _widget.Visible = _menuShowWidget.Checked;
        Settings.Set(Settings.WidgetVisibleKey, _menuShowWidget.Checked ? "1" : "0");
    }

    private void SetWidgetTransparency(int percent)
    {
        _widget.SetTransparency(percent);
        foreach (ToolStripMenuItem item in _menuTransparency.DropDownItems)
            item.Checked = (int)item.Tag! == percent;
        Settings.Set(Settings.WidgetTransparencyKey, percent.ToString());
    }

    // ---------- Язык ----------

    private static void ChangeLanguage(Language language)
    {
        Settings.Set(Settings.LanguageKey, language.ToString());
        Strings.Set(language); // вызовет ApplyTexts через Strings.Changed
    }

    private void ApplyTexts()
    {
        var s = Strings.Current;

        _whenCaption.Text = s.WhenCaption;
        _afterRadio.Text = s.After;
        _atRadio.Text = s.AtTime;
        _actionCaption.Text = s.ActionCaption;
        _lockRadio.Text = s.Lock;
        _remindRadio.Text = s.Remind;
        _repeatCheck.Text = s.Repeat;
        _tips.SetToolTip(_repeatCheck, s.RepeatTip);
        _unitHours.Text = s.UnitHours;
        _unitMinutes.Text = s.UnitMinutes;
        _unitSeconds.Text = s.UnitSeconds;
        _reset.Text = s.Reset;
        foreach (var (button, minutes) in _presetButtons)
        {
            button.Text = minutes < 60
                ? string.Format(s.PresetMinutes, minutes)
                : string.Format(s.PresetHours, minutes / 60);
        }

        _menuOpen.Text = s.MenuOpen;
        _menuLanguage.Text = s.MenuLanguage;
        _menuShowWidget.Text = s.MenuShowWidget;
        _menuTransparency.Text = s.MenuTransparency;
        foreach (ToolStripMenuItem item in _menuTransparency.DropDownItems)
            item.Text = (int)item.Tag! == 0 ? s.TransparencyNone : $"{item.Tag}%";
        _menuExit.Text = s.MenuExit;
        foreach (ToolStripMenuItem item in _menuLanguage.DropDownItems)
            item.Checked = (Language)item.Tag! == Strings.CurrentLanguage;

        _widget.ApplyTexts();
        _reminder?.ApplyTexts();
        ApplyState(_state); // обновит кнопки, статус, заголовок и виджет
    }

    protected override void SetVisibleCore(bool value)
    {
        if (!_allowShow)
        {
            value = false;
            if (!IsHandleCreated) CreateHandle(); // нужен для BeginInvoke и таймера
        }
        base.SetVisibleCore(value);
    }

    private void OnWidgetStart(int minutes)
    {
        if (_state != State.Idle) return;
        // Запуск с виджета не трогает запомненные настройки большого окна.
        _suspendSave = true;
        _afterRadio.Checked = true;
        SetSpins(TimeSpan.FromMinutes(minutes));
        _suspendSave = false;
        StartCycle();
        if (Visible) WindowState = FormWindowState.Minimized;
    }

    // ---------- Создание элементов ----------

    private static FlowLayoutPanel CreateRow(bool centered = false) => new()
    {
        AutoSize = true,
        WrapContents = false,
        Anchor = centered ? AnchorStyles.None : AnchorStyles.Left,
    };

    private static Label CreateCaption() => new()
    {
        AutoSize = true,
        Width = 90,
        Margin = new Padding(3, 5, 6, 0),
    };

    private static NumericUpDown CreateSpin(int max) => new()
    {
        Minimum = 0,
        Maximum = max,
        Width = 56,
        TextAlign = HorizontalAlignment.Center,
    };

    private static Label CreateUnitLabel() => new()
    {
        AutoSize = true,
        Margin = new Padding(0, 6, 10, 0),
    };

    // ---------- Логика таймера ----------

    private TimeSpan SelectedTime =>
        new((int)_hours.Value, (int)_minutes.Value, (int)_seconds.Value);

    private void SetSpins(TimeSpan t)
    {
        _hours.Value = Math.Min((int)t.TotalHours % 24, (int)_hours.Maximum);
        _minutes.Value = t.Minutes;
        _seconds.Value = t.Seconds;
    }

    private void OnWhenModeChanged()
    {
        // Пока подставляем запомненное время нового режима, промежуточные значения не сохраняем.
        var wasSuspended = _suspendSave;
        _suspendSave = true;
        LoadSpinsForMode();
        _suspendSave = wasSuspended;

        ApplyState(State.Idle);
        SaveTimerSettings();
    }

    private void OnStartPause()
    {
        switch (_state)
        {
            case State.Idle:
                if (!AtTime && SelectedTime <= TimeSpan.Zero) return;
                StartCycle();
                WindowState = FormWindowState.Minimized; // OnResize уберёт окно в трей
                break;
            case State.Running:
                _remaining = _endUtc - DateTime.UtcNow;
                ApplyState(State.Paused);
                break;
            case State.Paused:
                _endUtc = DateTime.UtcNow + _remaining;
                ApplyState(State.Running);
                break;
        }
    }

    private void StartCycle()
    {
        if (AtTime)
        {
            var target = DateTime.Today + SelectedTime;
            if (target <= DateTime.Now) target = target.AddDays(1);
            _endUtc = target.ToUniversalTime();
        }
        else
        {
            _endUtc = DateTime.UtcNow + SelectedTime;
        }
        ApplyState(State.Running);
    }

    private void OnTick()
    {
        if (_state == State.Running && _endUtc <= DateTime.UtcNow)
        {
            Fire();
            return;
        }
        UpdateRunningDisplay();
    }

    private void Fire()
    {
        // Сначала решаем, что будет дальше, потом выполняем действие.
        if (!Repeat) ApplyState(State.Idle);
        else if (AtTime) StartCycle();      // следующий раз — завтра в это же время
        else ApplyState(State.Waiting);     // ждём разблокировки / закрытия напоминания

        if (LockAction)
        {
            if (!LockWorkStation())
            {
                if (_state == State.Waiting) StartCycle();
                MessageBox.Show(this, Strings.Current.LockFailed, AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        else
        {
            ShowReminder();
        }
    }

    private void ShowReminder()
    {
        _reminder?.Close();
        _reminder = new ReminderForm(_icon);
        _reminder.FormClosed += (_, _) =>
        {
            _reminder = null;
            if (_state == State.Waiting && !LockAction) StartCycle();
        };
        _reminder.Show();
        _reminder.Activate();
    }

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason != SessionSwitchReason.SessionUnlock || IsDisposed) return;
        BeginInvoke(() =>
        {
            if (_state == State.Waiting && LockAction) StartCycle();
        });
    }

    // ---------- Отображение ----------

    private void ApplyState(State state)
    {
        _state = state;
        bool idle = state == State.Idle;

        foreach (var c in new Control[] { _afterRadio, _atRadio, _lockRadio, _remindRadio, _repeatCheck, _hours, _minutes, _seconds })
            c.Enabled = idle;
        _presets.Enabled = idle && !AtTime;
        _reset.Enabled = !idle;

        // Паузу для «в указанное время» не делаем — это будильник, а не секундомер.
        _startPause.Enabled = state switch
        {
            State.Idle => true,
            State.Waiting => false,
            _ => !AtTime,
        };
        var s = Strings.Current;
        _startPause.Text = state switch
        {
            State.Running when AtTime => s.RunningButton,
            State.Running => s.Pause,
            State.Paused => s.Resume,
            State.Waiting => s.WaitingButton,
            _ => s.Start,
        };

        if (state == State.Running) _tick.Start(); else _tick.Stop();

        if (idle) UpdateIdleDisplay();
        else UpdateRunningDisplay();
    }

    private void UpdateIdleDisplay()
    {
        if (_state != State.Idle) return;
        _display.Text = Format(SelectedTime);
        _status.Text = AtTime ? Strings.Current.IdleStatusAt : Strings.Current.IdleStatusAfter;
        SetTitle(null);
        _widget.ShowIdle();
    }

    private void UpdateRunningDisplay()
    {
        var s = Strings.Current;
        switch (_state)
        {
            case State.Waiting:
                _display.Text = "--:--:--";
                _status.Text = LockAction ? s.WaitUnlock : s.WaitReminder;
                SetTitle(s.WaitingTitle);
                _widget.ShowCountdown("--:--:--", dimmed: true);
                return;

            case State.Paused:
                _display.Text = Format(_remaining);
                _status.Text = s.PausedStatus;
                SetTitle($"{_display.Text} {s.PausedTitle}");
                _widget.ShowCountdown(_display.Text, dimmed: true);
                return;

            default:
                var shown = Format(_endUtc - DateTime.UtcNow);
                _display.Text = shown;
                var endLocal = _endUtc.ToLocalTime();
                var day = endLocal.Date == DateTime.Today ? "" : s.Tomorrow;
                _status.Text = string.Format(LockAction ? s.LockAt : s.RemindAt, endLocal.ToString("HH:mm:ss")) + day;
                SetTitle(shown);
                _widget.ShowCountdown(shown, dimmed: false);
                return;
        }
    }

    private void SetTitle(string? info)
    {
        var title = info is null ? AppName : $"{info} · {AppName}";
        if (Text != title) Text = title;
        var tip = info is null ? AppName : $"{AppName} — {info}";
        if (_tray.Text != tip) _tray.Text = tip;
    }

    private static string Format(TimeSpan t)
    {
        var secs = (long)Math.Ceiling(Math.Max(0, t.TotalSeconds));
        return $"{secs / 3600:D2}:{secs / 60 % 60:D2}:{secs % 60:D2}";
    }

    // ---------- Трей и закрытие ----------

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (WindowState != FormWindowState.Minimized) return;

        Hide();
        if (!_trayHintShown)
        {
            _trayHintShown = true;
            _tray.ShowBalloonTip(3000, AppName, Strings.Current.TrayHint, ToolTipIcon.Info);
        }
    }

    internal void ShowFromTray()
    {
        _allowShow = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    /// <summary>Закрыться без вопросов — по просьбе установщика (LockMyScreen.exe --exit).</summary>
    internal void ExitWithoutQuestions()
    {
        _exitWithoutQuestions = true;
        _reminder?.Close();
        Close();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && _state != State.Idle && !_exitWithoutQuestions)
        {
            if (!Visible) ShowFromTray();
            var answer = MessageBox.Show(this, Strings.Current.ConfirmExit, AppName,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        Strings.Changed -= ApplyTexts;
        _widget.Close();
        _tick.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _tips.Dispose();
        base.OnFormClosed(e);
    }

    private static Icon LoadAppIcon()
    {
        using var stream = typeof(MainForm).Assembly.GetManifestResourceStream("LockMyScreen.app.ico")!;
        return new Icon(stream);
    }
}
