// dotnet publish -c Release    -- for compilation (insert into "developer command prompt")
// c# version must be 10
// .net must be core

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;

class NamedPipeServer
{
    private const string PipeName = "AudioPipe";
    private NamedPipeServerStream pipeServer;
    private StreamReader streamReader;
    private StreamWriter streamWriter;
    private CancellationTokenSource cts = new CancellationTokenSource();
    private AudioCommandProcessor resultProcessor;

    public NamedPipeServer()
    {
        pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        streamReader = new StreamReader(pipeServer);
        streamWriter = new StreamWriter(pipeServer);

        resultProcessor = new();
    }

    public async Task StartAsync(int parentProcessId)
    {
        var parentProcess = Process.GetProcessById(parentProcessId);
        var monitorThread = new Thread(() =>
        {
            while (!parentProcess.HasExited) { Thread.Sleep(500); }
            Environment.Exit(0); // if parent process died -> close the program
        });
        monitorThread.Start();


        Console.WriteLine("Ожидание подключения клиента...");
        await pipeServer.WaitForConnectionAsync(cts.Token);

        Console.WriteLine("Клиент подключен. Начинаем мониторинг входящих сообщений.");
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                string message = await streamReader.ReadLineAsync();
                if (message != null)
                {
                    Console.WriteLine($"Получено сообщение: {message}");
                    string response = ProcessMessage(message);
                    await streamWriter.WriteLineAsync(response);
                    await streamWriter.FlushAsync();
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Ошибка ввода/вывода: {ex.Message}");
                break;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Операция отменена.");
                break;
            }
        }
    }

    public void Stop()
    {
        cts.Cancel();
    }

    private string ProcessMessage(string message)
    {
        return resultProcessor.ProcessCommand(message);
    }

    public void Dispose()
    {
        streamReader?.Dispose();
        streamWriter?.Dispose();
        pipeServer?.Dispose();
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Parent process ID is required.");
            return;
        }

        if (!int.TryParse(args[0], out int parentProcessId))
        {
            Console.WriteLine("Invalid parent process ID.");
            return;
        }

        var resultProcessor = new AudioCommandProcessor();
        var server = new NamedPipeServer();
        await server.StartAsync(parentProcessId);

        /*var debugThread = new Thread(() =>
        {
            while (true)
            {
                Console.Beep(800, 100);
                Thread.Sleep(250);
            }
        });
        debugThread.Start();*/
    }
}


class AudioCommandProcessor
{
    private MMDeviceEnumerator enumerator = new MMDeviceEnumerator();


    public string ProcessCommand(string command)
    {
        switch (command)
        {
            case "GetAudioDevices":
                return GetAudioDevices();
            case "StartStreaming":
                return StartStreaming();
            case "StopStreaming":
                return StopStreaming();
            case "PlayAudioFile":
                return PlayAudioFile();
            default:
                return "Unknown Command";
        }
    }

    private string GetAudioDevices()
    {
        string response = "";

        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active))
        {
            response += $"Device: {device.FriendlyName}\n";
        }

        return response;
    }

    private string StartStreaming()
    {
        return "Streaming Started";
    }

    private string StopStreaming()
    {
        return "Streaming Stopped";
    }

    private string PlayAudioFile()
    {
        return "Audio File Playing";
    }
}





/*using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using NAudio.CoreAudioApi;

class AudioControlApp
{
    static void Main(string[] args)
    {
        if (args.Length < 1) {
            Console.WriteLine("Parent process ID is required.");
            return;
        }

        if (!int.TryParse(args[0], out int parentProcessId)) {
            Console.WriteLine("Invalid parent process ID.");
            return;
        }

        var parentProcess = Process.GetProcessById(parentProcessId);
        var monitorThread = new Thread(() =>
        {
            while (!parentProcess.HasExited) { Thread.Sleep(500); }
            Environment.Exit(0);    // if parent process died -> close the program
        });
        monitorThread.Start();


        var debugThread = new Thread(() =>
        {
            while (true) {
                Console.Beep(800, 100);
                Thread.Sleep(250);
            }
        });
        debugThread.Start();


        var resultProcessor = new AudioCommandProcessor();
        var pipeServer = new NamedPipeServerStream("AudioPipe", PipeDirection.InOut);
        pipeServer.WaitForConnection();

        var streamReader = new StreamReader(pipeServer);
        var streamWriter = new StreamWriter(pipeServer);

        Task.Run(() => { Console.Beep(1800, 700); });

        
        var readThread = new Thread(async () =>
        {
            while (true)
            {
                try
                {
                    string command = await streamReader.ReadLineAsync();
                    if (command != null)
                    {
                        var response = resultProcessor.ProcessCommand(command);

                        *//*streamWriter.WriteLine($"Response from server: {response}");
                        streamWriter.Flush();*//*
                        //Task.Run(async  () => { 
                        Console.Beep(1500, 100);

                        await streamWriter.WriteLineAsync($"Response from server: {response}");
                        await streamWriter.FlushAsync();
                        //});
                    }
                }
                catch (Exception e)
                {
                    //Task.Run(() => { Console.Beep(200, 1000); });
                    break;
                }
            }
        });

        readThread.Start();
    }

}


class AudioCommandProcessor
{
    private MMDeviceEnumerator enumerator = new MMDeviceEnumerator();


    public string ProcessCommand(string command)
    {
        switch (command)
        {
            case "GetAudioDevices":
                return GetAudioDevices();
            case "StartStreaming":
                return StartStreaming();
            case "StopStreaming":
                return StopStreaming();
            case "PlayAudioFile":
                return PlayAudioFile();
            default:
                return "Unknown Command";
        }
    }

    private string GetAudioDevices()
    {
        string response = "";

        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active))
        {
            response += $"Device: {device.FriendlyName}\n";
        }

        return response;
    }

    private string StartStreaming()
    {
        return "Streaming Started";
    }

    private string StopStreaming()
    {
        return "Streaming Stopped";
    }

    private string PlayAudioFile()
    {
        return "Audio File Playing";
    }
}
*/