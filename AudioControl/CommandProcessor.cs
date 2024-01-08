using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
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
        public Dictionary<string, (WasapiOut player, MMDevice device, MixingSampleProvider mixer, BufferedWaveProvider bufferForSingleAudioPlay, BufferedWaveProvider bufferForIntercom)> audioOutputsDictionary;
        public Dictionary<string, (WasapiCapture receiver, MMDevice device, BufferedWaveProvider ?buffer)> audioInputsDictionary;
        private Dictionary<string, byte[]> preLoadedAudioFiles;
        private List<string> audioFileNames;
        public WaveFormat unifiedWaveFormat;

        public AudioCommandProcessor()
        {
            unifiedWaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2); // 32 bit IEEFloat: 44100Hz 2 channels
            pathToAudioFiles = @"C:\Users\Levael\GitHub\MOCU\Assets\Audio";     // todo: move it to config file later
            audioFileNames = new() { "test.mp3", "test2.mp3" };                 // todo: maybe read it from config or unity, idk

            enumerator = new();

            UpdateAudioDevices();
            //audioDevicesJsonAnswer = GetAudioDevices();
            jsonDeserializeOptions = new JsonSerializerOptions { IncludeFields = true };

            incomingStream = new(direction: IntercomStreamDirection.Incoming, audioCommandProcessor: this);
            outgoingStream = new(direction: IntercomStreamDirection.Outgoing, audioCommandProcessor: this);

            LoadAudioFiles();
            InitOutputDevicesDictionary();
            InitInputDevicesDictionary();
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

        private void InitOutputDevicesDictionary()
        {
            audioOutputsDictionary = new();

            foreach (var device in outputDevices)
            {
                audioOutputsDictionary.Add(
                    device.FriendlyName,
                    (
                        player: new WasapiOut(device, AudioClientShareMode.Shared, false, 0),
                        device: device,
                        mixer:  new MixingSampleProvider(unifiedWaveFormat),
                        bufferForSingleAudioPlay: new BufferedWaveProvider(unifiedWaveFormat),
                        bufferForIntercom: new BufferedWaveProvider(unifiedWaveFormat)
                    )
                );

                var currentItem = audioOutputsDictionary[device.FriendlyName];
                currentItem.mixer.AddMixerInput(currentItem.bufferForSingleAudioPlay);
                currentItem.mixer.AddMixerInput(currentItem.bufferForIntercom);

                currentItem.player.Init(currentItem.mixer);
                currentItem.player.Play();
            }
        }

        private void InitInputDevicesDictionary()
        {
            audioInputsDictionary = new();

            foreach (var device in inputDevices)
            {
                audioInputsDictionary.Add(
                    device.FriendlyName,
                    (
                        receiver: new WasapiCapture(device),
                        device: device,
                        buffer: null    // link to "bufferForIntercom" in "audioOutputsDictionary" object
                    )
                );

                var currentItem = audioInputsDictionary[device.FriendlyName];
                currentItem.receiver.WaveFormat = unifiedWaveFormat;
            }
        }

        private void LoadAudioFiles()
        {
            preLoadedAudioFiles = new();

            foreach (var audioFileName in audioFileNames)
            {
                using var reader = new AudioFileReader(Path.Combine(pathToAudioFiles, audioFileName));
                var resampler = new WdlResamplingSampleProvider(reader, unifiedWaveFormat.SampleRate);
                var sampleProvider = resampler.ToStereo();

                var wholeFile = new List<byte>();

                float[] readBuffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
                byte[] byteBuffer = new byte[readBuffer.Length * 4];
                int samplesRead;

                while ((samplesRead = sampleProvider.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    Buffer.BlockCopy(readBuffer, 0, byteBuffer, 0, samplesRead * 4);
                    wholeFile.AddRange(byteBuffer.Take(samplesRead * 4));
                }

                preLoadedAudioFiles.Add(audioFileName, wholeFile.ToArray());
            }
        }

        /*private void UnsubscribeDevice_TestMethod(ISampleProvider sampleProvider)
        {
            foreach (var test in audioOutputsDictionary.Keys.ToList())
            {
                audioOutputsDictionary[test].mixer.RemoveMixerInput(sampleProvider);
            }
        }*/


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

                /*outgoingStream.UpdateAudioOutputDevicesVolume(audioOutputDeviceVolume: obj.AudioOutputDeviceVolumeParticipant);
                incomingStream.UpdateAudioOutputDevicesVolume(audioOutputDeviceVolume: obj.AudioOutputDeviceVolumeResearcher);*/

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

        /// <summary>
        /// Copies pre-prepared and unified audio to a buffer subscribed to the mixer.
        /// If something is being played at the moment of calling the function, it purposely cuts it off and puts a newer one
        /// </summary>
        private void PlayAudioFile(string jsonCommand)
        {
            var commandData = JsonSerializer.Deserialize<PlayAudioFile_Command>(jsonCommand, jsonDeserializeOptions);

            var audioData = preLoadedAudioFiles[commandData.AudioFileName];
            var buffer = audioOutputsDictionary[commandData.AudioOutputDeviceName].bufferForSingleAudioPlay;

            buffer.ClearBuffer();
            buffer.AddSamples(audioData, 0, audioData.Length);
        }
    }
}
