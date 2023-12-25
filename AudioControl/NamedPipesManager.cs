using AudioControl;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// IDK, some sh*t i did...

namespace CrossProcessCommunication
{
    /*public class NamedPipe
    {
        public DeviceConnectionStatus connectionStatus;

        private StreamReader streamReader;
        private StreamWriter streamWriter;
        private CancellationTokenSource cts;
        private ConcurrentQueue<string> inputMessageQueue;
        private ConcurrentQueue<string> outputMessageQueue;
        private bool isProcessingWriting = false;

        public async void StartMonitoringIncomingMessages()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                string jsonCommand = await streamReader.ReadLineAsync();

                if (jsonCommand != null)   {
                    inputMessageQueue.Enqueue(jsonCommand);
                    ProcessCommandAsync();
                }
                else { CloseConnection(); }
            }
        }

        private async Task ProcessCommandAsync()
        {
            while (!inputMessageQueue.IsEmpty)
            {
                if (inputMessageQueue.TryDequeue(out string jsonCommand))
                {
                    try
                    {
                        var response = commandProcessor.ProcessCommand(jsonCommand);
                        await streamWriter.WriteLineAsync(response);
                        await streamWriter.FlushAsync();
                    }
                    catch (InvalidOperationException ex) { }
                }
            }
        }

        public async void SendCommandAsync(string jsonCommand)
        {
            outputMessageQueue.Enqueue(jsonCommand);

            if (!isProcessingWriting)
            {
                isProcessingWriting = true;
                ProcessResponseAsync(); // this method is without "await" in order not to block the current thread
            }
        }

        private async Task ProcessResponseAsync()
        {
            while (!outputMessageQueue.IsEmpty)
            {
                if (outputMessageQueue.TryDequeue(out string jsonResponse))
                {
                    try
                    {
                        await streamWriter.WriteLineAsync(jsonResponse);
                        await streamWriter.FlushAsync();
                    }
                    catch (InvalidOperationException ex) { }
                }
            }

            isProcessingWriting = false;
        }

        private void CloseConnection() {
            connectionStatus = DeviceConnectionStatus.Disconnected;
        }
    }*/





    public class NamedPipeServer
    {
        private NamedPipeServerStream   pipeServer;
        private StreamReader            streamReader;
        private StreamWriter            streamWriter;

        public DeviceConnectionStatus   connectionStatus;
        private ConcurrentQueue<string> inputMessageQueue;
        private ConcurrentQueue<string> outputMessageQueue;
        private bool                    isWritingNow;
        private CancellationTokenSource cts;
        private AudioCommandProcessor   commandProcessor;


        public NamedPipeServer(string pipeName, AudioCommandProcessor commandProcessor)
        {
            connectionStatus = DeviceConnectionStatus.Disconnected;
            isWritingNow = false;
            inputMessageQueue = new();
            outputMessageQueue = new();
            cts = new();
            this.commandProcessor = commandProcessor;

            pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            streamReader = new StreamReader(pipeServer);
            streamWriter = new StreamWriter(pipeServer);

            StartAsync();
        }

        private async Task StartAsync()
        {
            await pipeServer.WaitForConnectionAsync(cts.Token);

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    string message = await streamReader.ReadLineAsync();
                    if (message != null)
                    {
                        var response = commandProcessor.ProcessCommand(message);
                        await streamWriter.WriteLineAsync(response);
                        await streamWriter.FlushAsync();
                    }
                    else
                    {
                        Console.Beep(1500, 500); //Console.WriteLine($"NamedPipeServer / StartAsync / message == null");
                        break;
                    }
                }
                catch (IOException ex)
                {
                    Console.Beep(1500, 500); //Console.WriteLine($"NamedPipeServer / StartAsync / IO exception: {ex.Message}");
                    break;
                }
                catch (OperationCanceledException)
                {
                    Console.Beep(1500, 500); //Console.WriteLine($"NamedPipeServer / StartAsync / Operation canceled");
                    break;
                }
            }
            Dispose();
        }

        private void Dispose()
        {
            try { streamReader?.Dispose();  } catch { }
            try { streamWriter?.Dispose();  } catch { }
            try { pipeServer?.Dispose();    } catch { }

            connectionStatus = DeviceConnectionStatus.Disconnected;
        }
    }





    public class NamedPipesClient
    {
    }

    public enum DeviceConnectionStatus
    {
        Connected,
        Disconnected,
        InProgress,
        NotRelevant
    }
}
