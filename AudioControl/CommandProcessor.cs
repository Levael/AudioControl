using NAudio.CoreAudioApi;
using System.Text.Json;

namespace AudioControl
{
    public class AudioCommandProcessor
    {
        private MMDeviceEnumerator enumerator;
        public MMDeviceCollection inputDevices;
        public MMDeviceCollection outputDevices;

        public IntercomStream incomingStream;
        public IntercomStream outgoingStream;

        public int intercomStreamLatency = 10; // size of buffer in ms (less -- faster, but bigger chance of artifacts)
        private JsonSerializerOptions jsonDeserializeOptions;
        private string audioDevicesJsonAnswer;

        public AudioCommandProcessor()
        {
            enumerator = new();

            UpdateAudioDevices();
            audioDevicesJsonAnswer = GetAudioDevices();
            jsonDeserializeOptions = new JsonSerializerOptions { IncludeFields = true };

            incomingStream = new(direction: IntercomStreamDirection.Incoming, audioCommandProcessor: this);
            outgoingStream = new(direction: IntercomStreamDirection.Outgoing, audioCommandProcessor: this);
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
                    return PlayAudioFile(jsonCommand);

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
                //Console.WriteLine($"deviceName: {deviceName}, DeviceFriendlyName: {devices[i].DeviceFriendlyName}");

                if (devices[i].DeviceFriendlyName == deviceName)
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
                InputDevices = inputDevices.Select(device => device.DeviceFriendlyName).ToList(),
                OutputDevices = outputDevices.Select(device => device.DeviceFriendlyName).ToList()
            });
        }

        private string PlayAudioFile(string jsonCommand)
        {
            try
            {
                var obj = JsonSerializer.Deserialize<PlayAudioFile_Command>(jsonCommand, jsonDeserializeOptions);
                // stamp
                Console.Beep(500, 200);

                return JsonSerializer.Serialize(new { CommandName = "PlayAudioFile_Command", HasError = false });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return JsonSerializer.Serialize(new { CommandName = "PlayAudioFile_Command", HasError = true });
            }
        }
    }
}
