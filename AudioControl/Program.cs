// dotnet publish -c Release

using System.Diagnostics;
using AudioControl;


class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 1) { Console.Beep(500, 500); return; }                                    //Console.WriteLine("Parent process ID is required.");

        if (!int.TryParse(args[0], out int parentProcessId)) { Console.Beep(500, 500); return; }    //Console.WriteLine("Invalid parent process ID.");

        StartParentProcessMonitorinf(parentProcessId);
        //DebugFunc();


        var server = new NamedPipeServer();
        await server.StartAsync();
    }

    

    private static void StartParentProcessMonitorinf(int parentProcessId)
    {
        var monitorThread = new Thread(() =>
        {
            while (!Process.GetProcessById(parentProcessId).HasExited) { Thread.Sleep(500); }
            Environment.Exit(0); // if parent process died -> close the program
        });
        monitorThread.Start();
    }

    private static void DebugFunc()
    {
        var debugThread = new Thread(() =>
        {
            while (true)
            {
                Console.Beep(800, 100);
                Thread.Sleep(250);
            }
        });
        debugThread.Start();
    }
}