using System.Diagnostics;
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
            while (!parentProcess.HasExited)    Thread.Sleep(1000);
            Environment.Exit(0);
        });
        monitorThread.Start();
        /*Thread.Sleep(1000);
        //Console.WriteLine("started");
        Console.Beep(800, 500);
        Thread.Sleep(3000);
        //Console.WriteLine("continued");
        Console.Beep(800, 500);
        Thread.Sleep(1000);*/

        while(true)
        {
            Console.Beep(800, 200);
            Thread.Sleep(500);
        }




        /*var processor = new AudioCommandProcessor();
        using (var pipeServer = new NamedPipeServerStream("AudioPipe", PipeDirection.InOut))
        {
            pipeServer.WaitForConnection();

            using (var streamReader = new StreamReader(pipeServer))
            using (var streamWriter = new StreamWriter(pipeServer))
            {
                *//*while (true)
                {
                    var command = streamReader.ReadLine();
                    if (command == null || command == "Exit") break; // "null" means closing of named pipe

                    var response = processor.ProcessCommand(command);
                    streamWriter.WriteLine(response);
                    streamWriter.Flush();
                }*//*
            }
        }*/
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
                StartStreaming();
                return "Streaming Started";
            case "StopStreaming":
                StopStreaming();
                return "Streaming Stopped";
            case "PlayAudioFile":
                PlayAudioFile();
                return "Audio File Playing";
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

    private void StartStreaming()
    {
        
    }

    private void StopStreaming()
    {
        
    }

    private void PlayAudioFile()
    {
        
    }
}
