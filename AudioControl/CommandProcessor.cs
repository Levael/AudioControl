using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Numerics;
using System.Text.Json;

// deserialize func todo (upd speed instead of using JSON)

namespace AudioControl
{
    public class AudioCommandProcessor
    {
        private MMDeviceEnumerator enumerator;
        public MMDeviceCollection inputDevices;
        public MMDeviceCollection outputDevices;

        public IntercomStream incomingStream;
        public IntercomStream outgoingStream;

        public int intercomStreamLatency = 10;  // size of buffer in ms (less -- faster, but bigger chance of artifacts)
        private JsonSerializerOptions jsonDeserializeOptions;
        //private string audioDevicesJsonAnswer;
        private string pathToAudioFiles;
        private Dictionary<string, (WasapiOut player, MMDevice device, MixingSampleProvider mixer, BufferedWaveProvider bufferForSingleAudioPlay)> audioOutputsDictionary;
        private Dictionary<string, byte[]> audioFilesDictionary;
        private WaveFormat unifiedWaveFormat;

        public AudioCommandProcessor()
        {
            unifiedWaveFormat = new(rate: 44100, bits: 16, channels: 2);    // 2 channels = stereo
            pathToAudioFiles = @"C:\Users\Levael\GitHub\MOCU\Assets\Audio"; // todo: move it to config file later

            enumerator = new();

            UpdateAudioDevices();
            //audioDevicesJsonAnswer = GetAudioDevices();
            jsonDeserializeOptions = new JsonSerializerOptions { IncludeFields = true };

            incomingStream = new(direction: IntercomStreamDirection.Incoming, audioCommandProcessor: this);
            outgoingStream = new(direction: IntercomStreamDirection.Outgoing, audioCommandProcessor: this);

            LoadAudioFiles();
            InitOutputDevicesObjectsForSingleAudioPlay();
        }


        public string ProcessCommand(string jsonCommand)
        {
            var commandName = JsonDocument.Parse(jsonCommand).RootElement.GetProperty("Command").GetString();
            

            switch (commandName)
            {
                // COMMON COMMANDS
                case "SetDevicesParameters_Command":
                    return SetDevicesParameters(jsonCommand);

                case "ChangeOutputDeviceVolume_Command":
                    return ChangeOutputDeviceVolume(jsonCommand);

                case "PlayAudioFile_Command":
                    try {
                        Task.Run(() => PlayAudioFile(jsonCommand));
                        //PlayAudioFile(jsonCommand);
                        return JsonSerializer.Serialize(new { CommandName = "Bell" });
                    } catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        return JsonSerializer.Serialize(new { CommandName = "Error" });
                    }
                    

                case "GetAudioDevices_Command":
                    return GetAudioDevices();

                // INTERCOM COMMANDS
                case "StartIntercomStream_ResearcherToParticipant_Command":
                    outgoingStream.StartStream();
                    return JsonSerializer.Serialize(new { CommandName = "Temp" });
                //return StartIntercomStreaming_ResearcherToParticipant();

                case "StartIntercomStream_ParticipantToResearcher_Command":
                    incomingStream.StartStream();
                    return JsonSerializer.Serialize(new { CommandName = "Temp" });
                    //return StartIntercomStreaming_ParticipantToResearcher();

                case "StopIntercomStream_ResearcherToParticipant_Command":
                    outgoingStream.StopStream();
                    return JsonSerializer.Serialize(new { CommandName = "Temp" });
                    //return StopIntercomStreaming_ResearcherToParticipant();

                case "StopIntercomStream_ParticipantToResearcher_Command":
                    incomingStream.StopStream();
                    return JsonSerializer.Serialize(new { CommandName = "Temp" });
                    //return StopIntercomStreaming_ParticipantToResearcher();

                // CHANGE INPUT DEVICE COMMANDS
                case "SetResearcherAudioInputDevice_Command":
                    return $"SetResearcherAudioInputDevice_Command";
                case "SetParticipantAudioInputDevice_Command":
                    return $"SetParticipantAudioInputDevice_Command";
                case "DisconnectResearcherAudioInputDevice_Command":
                    return $"DisconnectResearcherAudioInputDevice_Command";
                case "DisconnectParticipantAudioInputDevice_Command":
                    return $"DisconnectParticipantAudioInputDevice_Command";

                // CHANGE OUTPUT DEVICE COMMANDS
                case "SetResearcherAudioOutputDevice_Command":
                    return $"SetResearcherAudioOutputDevice_Command";
                case "SetParticipantAudioOutputDevice_Command":
                    return $"SetParticipantAudioOutputDevice_Command";
                case "DisconnectResearcherAudioOutputDevice_Command":
                    return $"DisconnectResearcherAudioOutputDevice_Command";
                case "DisconnectParticipantAudioOutputDevice_Command":
                    return $"DisconnectParticipantAudioOutputDevice_Command";


                default:
                    return JsonSerializer.Serialize(new { CommandName = "Unknown command"});
            }
        }

        private void InitOutputDevicesObjectsForSingleAudioPlay()
        {
            audioOutputsDictionary = new();

            foreach (var device in outputDevices)
            {

                audioOutputsDictionary.Add(
                    device.FriendlyName,
                    (
                        player: new WasapiOut(device, AudioClientShareMode.Shared, false, 0),
                        device: device,
                        mixer:  new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2)),
                        bufferForSingleAudioPlay: new BufferedWaveProvider(unifiedWaveFormat)
                    )
                );

                var currentItem = audioOutputsDictionary[device.FriendlyName];
                currentItem.mixer.AddMixerInput(currentItem.bufferForSingleAudioPlay);

                currentItem.player.Init(currentItem.mixer);
                currentItem.player.Play();
            }
        }

        private void LoadAudioFiles()
        {
            audioFilesDictionary = new() {
                { "test.mp3", null },
                { "test2.mp3", null },
            };

            foreach (var audioFile in audioFilesDictionary.Keys.ToList())
            {
                using var reader = new AudioFileReader(Path.Combine(pathToAudioFiles, audioFile));
                var resampler = new WdlResamplingSampleProvider(reader, unifiedWaveFormat.SampleRate);
                var sampleProvider = new SampleToWaveProvider16(resampler);
                var memoryStream = new MemoryStream();
                var waveFileWriter = new WaveFileWriter(memoryStream, sampleProvider.WaveFormat);

                byte[] buffer = new byte[reader.WaveFormat.AverageBytesPerSecond * 4];
                int bytesRead;

                while ((bytesRead = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                {
                    waveFileWriter.Write(buffer, 0, bytesRead);
                }

                audioFilesDictionary[audioFile] = memoryStream.ToArray();
            }
        }


        private string SetDevicesParameters(string jsonCommand)
        {
            try
            {
                var obj = JsonSerializer.Deserialize<SetDevicesParameters_Command>(jsonCommand, jsonDeserializeOptions);

                outgoingStream.UpdateAudioDevices(
                    audioInputDeviceName: obj.AudioInputDeviceNameResearcher,
                    audioOutputDeviceName: obj.AudioOutputDeviceNameParticipant,
                    audioOutputDeviceVolume: obj.AudioOutputDeviceVolumeParticipant
                );

                incomingStream.UpdateAudioDevices(
                    audioInputDeviceName: obj.AudioInputDeviceNameParticipant,
                    audioOutputDeviceName: obj.AudioOutputDeviceNameResearcher,
                    audioOutputDeviceVolume: obj.AudioOutputDeviceVolumeResearcher
                );

                return JsonSerializer.Serialize(new {
                    CommandName = "SetDevicesParameters_Command",
                    HasError = false,
                    AudioInputDeviceNameResearcher = outgoingStream._audioInputDeviceName,
                    AudioOutputDeviceNameResearcher = incomingStream._audioOutputDeviceName,
                    AudioInputDeviceNameParticipant = incomingStream._audioInputDeviceName,
                    AudioOutputDeviceNameParticipant = outgoingStream._audioOutputDeviceName,
                    AudioOutputDeviceVolumeParticipant = outgoingStream._audioOutputDeviceVolume,
                    AudioOutputDeviceVolumeResearcher = incomingStream._audioOutputDeviceVolume
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return JsonSerializer.Serialize(new { CommandName = "SetDevicesParameters_Command", HasError = true });
            }
        }

        private string ChangeOutputDeviceVolume(string jsonCommand)
        {
            try
            {
                var obj = JsonSerializer.Deserialize<ChangeOutputDeviceVolume_Command>(jsonCommand, jsonDeserializeOptions);

                outgoingStream.UpdateAudioOutputDevicesVolume(audioOutputDeviceVolume: obj.AudioOutputDeviceVolumeParticipant);
                incomingStream.UpdateAudioOutputDevicesVolume(audioOutputDeviceVolume: obj.AudioOutputDeviceVolumeResearcher);

                return JsonSerializer.Serialize(new { CommandName = "ChangeOutputDeviceVolume_Command", HasError = false });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return JsonSerializer.Serialize(new { CommandName = "ChangeOutputDeviceVolume_Command", HasError = true });
            }
        }

        private void UpdateAudioDevices()
        {
            inputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            outputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        }

        public MMDevice GetDeviceByItsName(string deviceName, MMDeviceCollection devices)
        {
            int index = -1;

            for (int i = 0; i < devices.Count; i++)
            {
                //Console.WriteLine($"deviceName: {deviceName}, FriendlyName: {devices[i].FriendlyName}");

                if (devices[i].FriendlyName == deviceName)
                {
                    index = i;
                    break;
                }
            }

            return devices[index];
        }

        private string GetAudioDevices()
        {
            return JsonSerializer.Serialize(new {
                Command = "GetAudioDevices_Command",
                InputDevices = inputDevices.Select(device => device.FriendlyName).ToList(),
                OutputDevices = outputDevices.Select(device => device.FriendlyName).ToList()
            });
        }

        private async void PlayAudioFile(string jsonCommand)
        {
            var commandData = JsonSerializer.Deserialize<PlayAudioFile_Command>(jsonCommand, jsonDeserializeOptions);

            var audioData = audioFilesDictionary[commandData.AudioFileName];                                    // unified audio (array of bytes of audio data)
            var buffer = audioOutputsDictionary[commandData.AudioOutputDeviceName].bufferForSingleAudioPlay;    // sub-buffer of mixer that's responsible for single audios

            buffer.ClearBuffer();
            buffer.AddSamples(audioData, 0, audioData.Length);
        }

        private async void PlayAudioFile_Legacy_2(string jsonCommand)
        {
            var commandData = JsonSerializer.Deserialize<PlayAudioFile_Command>(jsonCommand, jsonDeserializeOptions);
            var player = new WasapiOut(GetDeviceByItsName(commandData.AudioOutputDeviceName, outputDevices), AudioClientShareMode.Shared, false, 0);
            //var player = outputDevicesObjectsForSingleAudioPlay[commandData.AudioOutputDeviceName];
            //var audio = audioFiles[commandData.AudioFileName];
            var audio = new AudioFileReader(Path.Combine(pathToAudioFiles, "test.mp3"));
            audio.Position = 0;

            /*player.Stop();
            while (player.PlaybackState != PlaybackState.Stopped) await Task.Delay(1);
            audio.Position = 0;*/

            player.Init(audio);
            player.Play();

            while (player.PlaybackState == PlaybackState.Playing) await Task.Delay(50);
        }

        private async void PlayAudioFile_Legacy_1(string jsonCommand)
        {
            var obj = JsonSerializer.Deserialize<PlayAudioFile_Command>(jsonCommand, jsonDeserializeOptions);
            var fullAudioFilePath = Path.Combine(pathToAudioFiles, obj.AudioFileName);

            Console.WriteLine(
                $"PlayAudioFile: AudioFileName = {obj.AudioFileName}, " +
                $"AudioOutputDeviceName = {obj.AudioOutputDeviceName}, " +
                $"FullAudioFilePath = {fullAudioFilePath}"
            );

            using (var audioFileReader = new AudioFileReader(fullAudioFilePath))
            using (var audioOutputDevice = GetDeviceByItsName(obj.AudioOutputDeviceName, outputDevices))
            using (var wasapiOut = new WasapiOut(audioOutputDevice, AudioClientShareMode.Shared, false, 0))
            {
                wasapiOut.Init(audioFileReader);
                wasapiOut.Play();

                while (wasapiOut.PlaybackState == PlaybackState.Playing) await Task.Delay(50);
            }
        }
    }
}
