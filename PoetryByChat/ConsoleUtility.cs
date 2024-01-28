using System.Runtime.InteropServices;
// based on https://stackoverflow.com/a/4647168
public static class ConsoleUtility
{
    public static event Action? OnClose = null;

    static ConsoleUtility()
    {
        handler = new ConsoleEventDelegate(ConsoleEventCallback);
        SetConsoleCtrlHandler(handler, true);
    }

    static bool ConsoleEventCallback(int eventType)
    {
        if (eventType == 2) // closing
        {
            Console.WriteLine("Console window closing, death imminent");
            try
            {
                OnClose?.Invoke();
            }
            catch (Exception e)
            {

                Console.WriteLine(e);
            }
        }
        return false;
    }

    static ConsoleEventDelegate handler;   // Keeps it from getting garbage collected
                                           // Pinvoke
    private delegate bool ConsoleEventDelegate(int eventType);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);
}