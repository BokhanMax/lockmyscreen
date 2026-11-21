namespace LockMyScreen;

/// <summary>Простые настройки «ключ=значение» в %AppData%\LockMyScreen\settings.txt.</summary>
internal static class Settings
{
    public const string LanguageKey = "Language";
    public const string WidgetPositionKey = "WidgetPosition";
    public const string WidgetMinutesKey = "WidgetMinutes";
    public const string WidgetVisibleKey = "WidgetVisible";
    public const string WidgetTransparencyKey = "WidgetTransparency"; // в процентах
    public const string WhenModeKey = "WhenMode";                     // After | At
    public const string ActionKey = "Action";                         // Lock | Remind
    public const string RepeatKey = "Repeat";                         // 1 | 0
    public const string DurationKey = "Duration";                     // секунды, режим «через»
    public const string AtTimeKey = "AtTime";                         // секунды от полуночи, режим «в указанное время»

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        MainForm.AppName, "settings.txt");

    private static readonly Dictionary<string, string> Values = Load();

    public static string? Get(string key) => Values.TryGetValue(key, out var value) ? value : null;

    /// <summary>Целое число из настроек, если оно есть и попадает в диапазон.</summary>
    public static int? GetInt(string key, int min, int max) =>
        int.TryParse(Get(key), out var v) && v >= min && v <= max ? v : null;

    public static void Set(string key, string value) => Set((key, value));

    /// <summary>Сохраняет несколько значений за одну запись файла.</summary>
    public static void Set(params (string Key, string Value)[] pairs)
    {
        bool changed = false;
        foreach (var (key, value) in pairs)
        {
            if (Get(key) == value) continue;
            Values[key] = value;
            changed = true;
        }
        if (!changed) return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllLines(FilePath, Values.Select(kv => $"{kv.Key}={kv.Value}"));
        }
        catch
        {
            // Не удалось сохранить — не страшно, программа работает и без этого.
        }
    }

    private static Dictionary<string, string> Load()
    {
        var result = new Dictionary<string, string>();
        try
        {
            foreach (var line in File.ReadAllLines(FilePath))
            {
                var i = line.IndexOf('=');
                if (i > 0) result[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
            }
        }
        catch
        {
            // Файла ещё нет — используем значения по умолчанию.
        }
        return result;
    }
}
