namespace LockMyScreen;

internal static class Program
{
    // Имена общие для всех запусков программы у одного пользователя.
    // MutexName также указан в установщике (installer\LockMyScreen.iss) — менять вместе.
    internal const string MutexName = "LockMyScreen-SingleInstance-5E1C7A2B";
    private const string ShowEventName = "LockMyScreen-Show-5E1C7A2B";
    private const string ExitEventName = "LockMyScreen-Exit-5E1C7A2B";

    [STAThread]
    private static void Main(string[] args)
    {
        // --exit: попросить уже запущенную программу закрыться (используется установщиком).
        bool exitRequest = args.Any(a => a.Equals("--exit", StringComparison.OrdinalIgnoreCase));

        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool firstInstance);
        if (!firstInstance)
        {
            // Программа уже работает — передаём ей просьбу и выходим.
            Signal(exitRequest ? ExitEventName : ShowEventName);
            return;
        }
        if (exitRequest) return; // закрывать нечего

        using var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        using var exitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ExitEventName);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Strings.Set(Strings.Initial());

        var form = new MainForm();
        var showWait = ThreadPool.RegisterWaitForSingleObject(showEvent,
            (_, _) => form.BeginInvoke(new Action(form.ShowFromTray)), null, Timeout.Infinite, executeOnlyOnce: false);
        var exitWait = ThreadPool.RegisterWaitForSingleObject(exitEvent,
            (_, _) => form.BeginInvoke(new Action(form.ExitWithoutQuestions)), null, Timeout.Infinite, executeOnlyOnce: false);

        Application.Run(form);

        showWait.Unregister(null);
        exitWait.Unregister(null);
    }

    private static void Signal(string eventName)
    {
        if (EventWaitHandle.TryOpenExisting(eventName, out var handle))
        {
            using (handle) handle.Set();
        }
    }
}
