using System.Globalization;

namespace LockMyScreen;

internal enum Language { Ru, En, Uk }

/// <summary>Все тексты интерфейса. Чтобы добавить язык — добавьте значение в Language и экземпляр ниже.</summary>
internal sealed class Strings
{
    public required string NativeName { get; init; }

    // Главное окно
    public required string WhenCaption { get; init; }
    public required string After { get; init; }
    public required string AtTime { get; init; }
    public required string ActionCaption { get; init; }
    public required string Lock { get; init; }
    public required string Remind { get; init; }
    public required string Repeat { get; init; }
    public required string RepeatTip { get; init; }
    public required string UnitHours { get; init; }
    public required string UnitMinutes { get; init; }
    public required string UnitSeconds { get; init; }
    public required string PresetMinutes { get; init; } // {0} — число
    public required string PresetHours { get; init; }
    public required string Start { get; init; }
    public required string Pause { get; init; }
    public required string Resume { get; init; }
    public required string Reset { get; init; }
    public required string RunningButton { get; init; }
    public required string WaitingButton { get; init; }
    public required string IdleStatusAt { get; init; }
    public required string IdleStatusAfter { get; init; }
    public required string WaitUnlock { get; init; }
    public required string WaitReminder { get; init; }
    public required string PausedStatus { get; init; }
    public required string PausedTitle { get; init; }
    public required string WaitingTitle { get; init; }
    public required string LockAt { get; init; }     // {0} — время
    public required string RemindAt { get; init; }   // {0} — время
    public required string Tomorrow { get; init; }
    public required string LockFailed { get; init; }
    public required string ConfirmExit { get; init; }

    // Трей и меню
    public required string MenuOpen { get; init; }
    public required string MenuLanguage { get; init; }
    public required string MenuShowWidget { get; init; }
    public required string MenuTransparency { get; init; }
    public required string TransparencyNone { get; init; }
    public required string MenuExit { get; init; }
    public required string TrayHint { get; init; }

    // Напоминание
    public required string TimeIsUp { get; init; }
    public required string FiredAt { get; init; }    // {0} — время
    public required string GotIt { get; init; }

    // Виджет
    public required string Stop { get; init; }

    private static readonly Strings Ru = new()
    {
        NativeName = "Русский",
        WhenCaption = "Сработать:",
        After = "через",
        AtTime = "в указанное время",
        ActionCaption = "Действие:",
        Lock = "заблокировать",
        Remind = "только напомнить",
        Repeat = "Повторять",
        RepeatTip = "«Через»: новый отсчёт начнётся после разблокировки\nили после закрытия напоминания.\n«В указанное время»: срабатывать каждый день.",
        UnitHours = "ч",
        UnitMinutes = "мин",
        UnitSeconds = "сек",
        PresetMinutes = "{0} мин",
        PresetHours = "{0} ч",
        Start = "Старт",
        Pause = "Пауза",
        Resume = "Продолжить",
        Reset = "Сброс",
        RunningButton = "Идёт…",
        WaitingButton = "Ожидание…",
        IdleStatusAt = "Время срабатывания (часы:минуты:секунды)",
        IdleStatusAfter = "Длительность таймера",
        WaitUnlock = "Новый отсчёт начнётся после разблокировки",
        WaitReminder = "Новый отсчёт начнётся после закрытия напоминания",
        PausedStatus = "Пауза",
        PausedTitle = "(пауза)",
        WaitingTitle = "ожидание",
        LockAt = "Блокировка в {0}",
        RemindAt = "Напоминание в {0}",
        Tomorrow = " (завтра)",
        LockFailed = "Не удалось заблокировать компьютер.",
        ConfirmExit = "Таймер ещё работает. Закрыть программу?",
        MenuOpen = "Открыть",
        MenuLanguage = "Язык",
        MenuShowWidget = "Показывать виджет",
        MenuTransparency = "Прозрачность виджета",
        TransparencyNone = "Нет",
        MenuExit = "Выход",
        TrayHint = "Я свернулся в трей. Щёлкните по значку, чтобы открыть.",
        TimeIsUp = "Время вышло!",
        FiredAt = "Таймер сработал в {0}",
        GotIt = "Понятно",
        Stop = "Стоп",
    };

    private static readonly Strings En = new()
    {
        NativeName = "English",
        WhenCaption = "Trigger:",
        After = "after",
        AtTime = "at a set time",
        ActionCaption = "Action:",
        Lock = "lock",
        Remind = "remind only",
        Repeat = "Repeat",
        RepeatTip = "“After”: the next countdown starts once you unlock\nor close the reminder.\n“At a set time”: fire every day.",
        UnitHours = "h",
        UnitMinutes = "min",
        UnitSeconds = "sec",
        PresetMinutes = "{0} min",
        PresetHours = "{0} h",
        Start = "Start",
        Pause = "Pause",
        Resume = "Resume",
        Reset = "Reset",
        RunningButton = "Running…",
        WaitingButton = "Waiting…",
        IdleStatusAt = "Trigger time (hours:minutes:seconds)",
        IdleStatusAfter = "Timer duration",
        WaitUnlock = "Next countdown starts after you unlock",
        WaitReminder = "Next countdown starts after you close the reminder",
        PausedStatus = "Paused",
        PausedTitle = "(paused)",
        WaitingTitle = "waiting",
        LockAt = "Lock at {0}",
        RemindAt = "Reminder at {0}",
        Tomorrow = " (tomorrow)",
        LockFailed = "Could not lock the computer.",
        ConfirmExit = "The timer is still running. Exit the program?",
        MenuOpen = "Open",
        MenuLanguage = "Language",
        MenuShowWidget = "Show widget",
        MenuTransparency = "Widget transparency",
        TransparencyNone = "None",
        MenuExit = "Exit",
        TrayHint = "I'm in the tray now. Click the icon to open.",
        TimeIsUp = "Time's up!",
        FiredAt = "Timer fired at {0}",
        GotIt = "Got it",
        Stop = "Stop",
    };

    private static readonly Strings Uk = new()
    {
        NativeName = "Українська",
        WhenCaption = "Спрацювати:",
        After = "через",
        AtTime = "у вказаний час",
        ActionCaption = "Дія:",
        Lock = "заблокувати",
        Remind = "лише нагадати",
        Repeat = "Повторювати",
        RepeatTip = "«Через»: новий відлік почнеться після розблокування\nабо після закриття нагадування.\n«У вказаний час»: спрацьовувати щодня.",
        UnitHours = "год",
        UnitMinutes = "хв",
        UnitSeconds = "сек",
        PresetMinutes = "{0} хв",
        PresetHours = "{0} год",
        Start = "Старт",
        Pause = "Пауза",
        Resume = "Продовжити",
        Reset = "Скинути",
        RunningButton = "Працює…",
        WaitingButton = "Очікування…",
        IdleStatusAt = "Час спрацювання (години:хвилини:секунди)",
        IdleStatusAfter = "Тривалість таймера",
        WaitUnlock = "Новий відлік почнеться після розблокування",
        WaitReminder = "Новий відлік почнеться після закриття нагадування",
        PausedStatus = "Пауза",
        PausedTitle = "(пауза)",
        WaitingTitle = "очікування",
        LockAt = "Блокування о {0}",
        RemindAt = "Нагадування о {0}",
        Tomorrow = " (завтра)",
        LockFailed = "Не вдалося заблокувати комп'ютер.",
        ConfirmExit = "Таймер ще працює. Закрити програму?",
        MenuOpen = "Відкрити",
        MenuLanguage = "Мова",
        MenuShowWidget = "Показувати віджет",
        MenuTransparency = "Прозорість віджета",
        TransparencyNone = "Немає",
        MenuExit = "Вихід",
        TrayHint = "Я згорнувся в трей. Клацніть по значку, щоб відкрити.",
        TimeIsUp = "Час вийшов!",
        FiredAt = "Таймер спрацював о {0}",
        GotIt = "Зрозуміло",
        Stop = "Стоп",
    };

    public static Strings For(Language language) => language switch
    {
        Language.En => En,
        Language.Uk => Uk,
        _ => Ru,
    };

    public static Language CurrentLanguage { get; private set; } = Language.Ru;
    public static Strings Current { get; private set; } = Ru;

    /// <summary>Срабатывает при смене языка — окна обновляют свои тексты.</summary>
    public static event Action? Changed;

    public static void Set(Language language)
    {
        CurrentLanguage = language;
        Current = For(language);
        Changed?.Invoke();
    }

    /// <summary>Язык из настроек, а если его там нет — по языку Windows.</summary>
    public static Language Initial()
    {
        if (Enum.TryParse<Language>(Settings.Get(Settings.LanguageKey), out var saved)
            && Enum.IsDefined(typeof(Language), saved))
            return saved;

        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
        {
            "ru" => Language.Ru,
            "uk" => Language.Uk,
            _ => Language.En,
        };
    }
}
