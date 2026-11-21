using System.Media;

namespace LockMyScreen;

/// <summary>Окно-напоминание поверх всех окон. Пищит, пока его не закроют.</summary>
internal sealed class ReminderForm : Form
{
    private readonly System.Windows.Forms.Timer _beep = new() { Interval = 5000 };
    private readonly DateTime _firedAt = DateTime.Now;

    private readonly Label _title = new()
    {
        AutoSize = true,
        Anchor = AnchorStyles.None,
        Font = new Font("Segoe UI", 32f, FontStyle.Bold),
    };
    private readonly Label _when = new()
    {
        AutoSize = true,
        Anchor = AnchorStyles.None,
        ForeColor = SystemColors.GrayText,
        Margin = new Padding(0, 0, 0, 16),
    };
    private readonly Button _ok = new()
    {
        AutoSize = true,
        Anchor = AnchorStyles.None,
        MinimumSize = new Size(140, 0),
    };

    public ReminderForm(Icon icon)
    {
        Text = MainForm.AppName;
        Icon = icon;
        Font = new Font("Segoe UI", 12f);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(32, 24, 32, 24);

        _ok.Click += (_, _) => Close();

        var root = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, Dock = DockStyle.Fill };
        root.Controls.AddRange([_title, _when, _ok]);
        Controls.Add(root);

        AcceptButton = _ok;
        CancelButton = _ok;
        _beep.Tick += (_, _) => SystemSounds.Exclamation.Play();
        ApplyTexts();
    }

    public void ApplyTexts()
    {
        var s = Strings.Current;
        _title.Text = s.TimeIsUp;
        _when.Text = string.Format(s.FiredAt, _firedAt.ToString("HH:mm"));
        _ok.Text = s.GotIt;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        SystemSounds.Exclamation.Play();
        _beep.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _beep.Dispose();
        base.OnFormClosed(e);
    }
}
