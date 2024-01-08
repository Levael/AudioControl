using NAudio.CoreAudioApi;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;

namespace AudioControl
{

    public class InputDevice
    {
        public string name                      { get; private set; }
        public WaveFormat waveFormat            { get; private set; }

        private MMDevice device                 { get; set; }
        private BufferedWaveProvider? buffer    { get; set; }
        private WasapiCapture receiver          { get; set; }


        public InputDevice(MMDevice mmDevice, WaveFormat deviceWaveFormat)
        {
            device = mmDevice;
            name = device.FriendlyName;
            waveFormat = deviceWaveFormat;
            buffer = null;

            receiver = new WasapiCapture(device);
            receiver.DataAvailable += OnDataAvailable;
            receiver.WaveFormat = waveFormat;
        }



        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
        }

        public void BindToBuffer(BufferedWaveProvider outputBuffer)
        {
            buffer = outputBuffer;
        }
    }


    public class OutputDevice
    {
        public WasapiOut player;
        public MMDevice device;

        public MixingSampleProvider mixer;
        public BufferedWaveProvider bufferForSingleAudioPlay;
        public BufferedWaveProvider bufferForIntercom;
    }
}
