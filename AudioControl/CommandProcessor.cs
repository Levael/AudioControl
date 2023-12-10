using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Text.Json;

namespace AudioControl
{
    public class AudioCommandProcessor
    {
        private MMDeviceEnumerator enumerator;
        private string audioDevicesJsonAnswer;
        private MMDeviceCollection inputDevices;
        private MMDeviceCollection outputDevices;
        private bool isIntercomOn = false;

        private WasapiCapture audioInput;
        private WasapiOut audioOutput;

        public AudioCommandProcessor()
        {
            enumerator = new();

            UpdateAudioDevices();
            audioDevicesJsonAnswer = GetAudioDevices();
        }


        public string ProcessCommand(string jsonCommand)
        {
            // Command and Answers templates are in Unity "AudioControlCommands.cs"

            var jsonDocument = JsonDocument.Parse(jsonCommand);
            var jsonElement = jsonDocument.RootElement;
            var commandName = jsonElement.GetProperty("Command").GetString();

            switch (commandName)
            {
                case "StartIntercomStream":
                    return StartMic2SpeakerStreaming(jsonElement.GetProperty("MicrophoneIndex").GetInt32(), jsonElement.GetProperty("SpeakerIndex").GetInt32());

                case "StopIntercomStream":
                    return StopMic2SpeakerStreaming();

                case "PlayAudioFile":
                    return $"Played: {jsonElement.GetProperty("FileName").GetString()}";

                case "GetAudioDevices":
                    if (jsonElement.GetProperty("DoUpdate").GetBoolean()) UpdateAudioDevices();
                    return GetAudioDevices();

                default:
                    return $"Unknown command";
            }
        }

        private void UpdateAudioDevices()
        {
            inputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            outputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        }

        private string GetAudioDevices()
        {
            var responseInfo = new
            {
                Command = "GetAudioDevices",
                InputDevices = inputDevices.Select(device => device.FriendlyName).ToList(),
                OutputDevices = outputDevices.Select(device => device.FriendlyName).ToList()
            };

            string json = JsonSerializer.Serialize(responseInfo);
            return json;
        }

        private string StartMic2SpeakerStreaming(int inputDeviceIndex, int outputDeviceIndex)
        {
            if (isIntercomOn) return JsonSerializer.Serialize(new { Command = "StartIntercomStream", IsOn = true });

            var status = "";

            try {
                MMDevice inputDevice = inputDevices[inputDeviceIndex];
                MMDevice outputDevice = outputDevices[outputDeviceIndex];

                audioInput = new WasapiCapture(inputDevice);
                audioOutput = new WasapiOut(outputDevice, AudioClientShareMode.Shared, false, 10);
                // number here is size of buffer in ms (less -- faster, but more chance of artifacts)

                var buffer = new BufferedWaveProvider(audioInput.WaveFormat);
                audioOutput.Init(buffer);

                audioInput.DataAvailable += (sender, e) =>
                {
                    buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
                };

                audioInput.StartRecording();
                audioOutput.Play();

                status = "Successful";
                isIntercomOn = true;
            } catch
            {
                status = "Unsuccessful";
                isIntercomOn = false;
            }


            var responseInfo = new
            {
                Command = "StartIntercomStream",
                Status = status,
                InputDeviceIndex = inputDeviceIndex,
                OutputDeviceIndex = outputDeviceIndex,
                InputDeviceName = inputDevices[inputDeviceIndex].FriendlyName,
                OutputDeviceName = outputDevices[outputDeviceIndex].FriendlyName
            };

            string json = JsonSerializer.Serialize(responseInfo);
            return json;
        }

        private string StopMic2SpeakerStreaming()
        {
            if (audioInput != null && audioOutput != null && isIntercomOn)
            {
                audioInput.StopRecording();
                audioOutput.Stop();

                audioInput.Dispose();
                audioOutput.Dispose();

                audioInput = null;
                audioOutput = null;

                isIntercomOn = false;
            }

            var responseInfo = new
            {
                Command = "StopIntercomStream"
            };

            string json = JsonSerializer.Serialize(responseInfo);
            return json;
        }

        private string PlayAudioFile()
        {
            return "Audio File Playing";
        }
    }
}
