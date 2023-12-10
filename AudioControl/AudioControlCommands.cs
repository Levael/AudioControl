namespace AudioControl
{

    public class StartIntercomStreamCommand
    {
        public string Command = "StartIntercomStream";
        public string Microphone;
        public string Speaker;

        public StartIntercomStreamCommand(string microphone, string speaker)
        {
            Microphone = microphone;
            Speaker = speaker;
        }
    }

    public class StopIntercomStreamCommand
    {
        public string Command = "StopIntercomStream";
    }

    public class PlayAudioFileCommand
    {
        public string Command = "PlayAudioFile";
        public string FileName;

        public PlayAudioFileCommand(string fileName)
        {
            FileName = fileName;
        }
    }

    public class GetAudioDevicesCommand
    {
        public string Command = "GetAudioDevices";
        public bool DoUpdate;

        public GetAudioDevicesCommand(bool doUpdate)
        {
            DoUpdate = doUpdate;
        }
    }

}

