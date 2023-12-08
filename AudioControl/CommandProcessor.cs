using NAudio.CoreAudioApi;
using System.Text;
using System.Text.Json;

namespace AudioControl
{
    public class AudioCommandProcessor
    {
        private MMDeviceEnumerator enumerator;
        private string audioDevices;

        public AudioCommandProcessor()
        {
            enumerator = new();
            audioDevices = GetAudioDevices();
        }

        public IAudioCommand DeserializeCommand(string jsonCommand)
        {
            var jsonDocument = JsonDocument.Parse(jsonCommand);
            var root = jsonDocument.RootElement;
            var commandType = root.GetProperty("command").GetString();

            switch (commandType)
            {
                case "StartIntercomStream":
                    return JsonSerializer.Deserialize<StartIntercomStreamCommand>(jsonCommand);
                case "StopIntercomStream":
                    return JsonSerializer.Deserialize<StopIntercomStreamCommand>(jsonCommand);
                case "PlayAudioFile":
                    return JsonSerializer.Deserialize<PlayAudioFileCommand>(jsonCommand);
                case "GetAudioDevices":
                    return new GetAudioDevicesCommand();
                default:
                    throw new InvalidOperationException("Unknown command type.");
            }
        }

        public string ProcessCommand(IAudioCommand command)
        {
            switch (command)
            {
                case StartIntercomStreamCommand startCommand:
                    return $"Intercom started with Microphone: {startCommand.Microphone} and Speaker: {startCommand.Speaker}";

                case PlayAudioFileCommand playCommand:
                    return $"Played: {playCommand.FileName}";

                case GetAudioDevicesCommand getDevicesCommand:
                    return $"Requested audio devices with parameter: {getDevicesCommand.DoUpdate}";

                default:
                    return $"Unknown command";
            }
        }



        private string GetAudioDevices()
        {
            StringBuilder response = new StringBuilder();

            foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active))
            {
                response.AppendLine($"Device: {device.FriendlyName}");
            }

            return response.ToString();
        }

        private string StartMic2SpeakerStreaming()
        {
            return "Streaming Started";
        }

        private string StopMic2SpeakerStreaming()
        {
            return "Streaming Stopped";
        }

        private string PlayAudioFile()
        {
            return "Audio File Playing";
        }
    }
}
