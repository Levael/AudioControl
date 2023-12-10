using System.IO.Pipes;


namespace AudioControl
{
    class NamedPipeServer
    {
        private const string PipeName = "AudioPipe";    // todo: read from config
        private NamedPipeServerStream pipeServer;
        private StreamReader streamReader;
        private StreamWriter streamWriter;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private AudioCommandProcessor commandProcessor;

        public NamedPipeServer()
        {
            pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            streamReader = new StreamReader(pipeServer);
            streamWriter = new StreamWriter(pipeServer);

            commandProcessor = new();
        }

        public async Task StartAsync()
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
                    } else
                    {
                        Console.Beep(1000, 500); //Console.WriteLine($"NamedPipeServer / StartAsync / message == null");
                        break;
                    }
                }
                catch (IOException ex)
                {
                    Console.Beep(1000, 500); //Console.WriteLine($"NamedPipeServer / StartAsync / IO exception: {ex.Message}");
                    break;
                }
                catch (OperationCanceledException)
                {
                    Console.Beep(1000, 500); //Console.WriteLine($"NamedPipeServer / StartAsync / Operation canceled");
                    break;
                }
            }
        }

        public void Stop()
        {
            cts.Cancel();
        }

        public void Dispose()
        {
            streamReader?.Dispose();
            streamWriter?.Dispose();
            pipeServer?.Dispose();
        }
    }
}
