namespace AudioControl {

    public interface IAudioCommand
    {
        string Command { get; }
    }

    public class StartIntercomStreamCommand : IAudioCommand
    {
        public string Command => "StartIntercomStream";
        public string Microphone { get; set; }
        public string Speaker { get; set; }
    }

    public class StopIntercomStreamCommand : IAudioCommand
    {
        public string Command => "StopIntercomStream";
    }

    public class PlayAudioFileCommand : IAudioCommand
    {
        public string Command => "PlayAudioFile";
        public string FileName { get; set; }
    }

    public class GetAudioDevicesCommand : IAudioCommand
    {
        public string Command => "GetAudioDevices";
        public bool DoUpdate { get; set; }
    }

}

