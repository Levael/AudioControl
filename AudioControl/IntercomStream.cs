using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioControl
{
    public class IntercomStream
    {
        private IntercomStreamDirection _direction;
        private bool isStreamOn = false;
        private bool isStreamReady = false;

        public string  _audioInputDeviceName;
        public string  _audioOutputDeviceName;
        public float   _audioOutputDeviceVolume;

        private WasapiCapture _audioInputObject;
        private WasapiOut _audioOutputObject;        

        /*private MMDevice _audioInputDevice;
        private MMDevice _audioOutputDevice;*/

        /*private BufferedWaveProvider _streamDataBuffer;*/

        private AudioCommandProcessor _audioCommandProcessor;

        public IntercomStream(IntercomStreamDirection direction, AudioCommandProcessor audioCommandProcessor, string audioInputDeviceName = "", string audioOutputDeviceName = "", float audioOutputDeviceVolume = -1f)
        {
            _direction = direction;
            _audioCommandProcessor = audioCommandProcessor;

            if (string.IsNullOrEmpty(audioInputDeviceName) || string.IsNullOrEmpty(audioOutputDeviceName))
            {
                //Console.WriteLine($"IntercomStream constructor. inp: {string.IsNullOrEmpty(audioInputDeviceName)}, out: {string.IsNullOrEmpty(audioOutputDeviceName)}");
                isStreamReady = false;
                return;
            }

            UpdateAudioDevices(audioInputDeviceName: audioInputDeviceName, audioOutputDeviceName: audioOutputDeviceName, audioOutputDeviceVolume: audioOutputDeviceVolume);

            isStreamReady = true;
        }

        public void StartStream()
        {
            if (!isStreamReady || isStreamOn) return;

            _audioInputObject.StartRecording();
            //_audioOutputObject.Play();
            isStreamOn = true;
        }

        public void StopStream()
        {
            if (!isStreamOn) return;

            _audioInputObject.StopRecording();
            //_audioOutputObject.Stop();
            isStreamOn = false;
        }

        /// <summary>
        /// Sets or reloads the input device based on the provided device name
        /// </summary>
        private void SetInputDevice(string audioInputDeviceName)
        {
            // if function didn't get any parameter -- just reload the object (looks like this because of error CS1736)
            _audioInputDeviceName = audioInputDeviceName == "same" ? _audioInputDeviceName : audioInputDeviceName;

            if (string.IsNullOrEmpty(_audioInputDeviceName)) throw new InvalidOperationException("Device name can't be Null or Empty");
            if (isStreamOn) StopStream();

            _audioInputObject = _audioCommandProcessor.audioInputsDictionary[_audioInputDeviceName].receiver;

            /*_audioInputDevice = _audioCommandProcessor.GetDeviceByItsName(_audioInputDeviceName, _audioCommandProcessor.inputDevices);
            _audioInputObject?.Dispose();   // Disposing old object if there is one
            _audioInputObject = new WasapiCapture(_audioInputDevice);*/
        }

        /// <summary>
        /// Sets or reloads the output device based on the provided device name
        /// </summary>
        private void SetOutputDevice(string audioOutputDeviceName, float audioOutputDeviceVolume)
        {
            // if function didn't get any parameter -- just same the object (looks like this because of error CS1736)
            _audioOutputDeviceName = audioOutputDeviceName == "same" ? _audioOutputDeviceName : audioOutputDeviceName;
            _audioOutputDeviceVolume = audioOutputDeviceVolume == -1f ? _audioOutputDeviceVolume : audioOutputDeviceVolume;

            if (string.IsNullOrEmpty(audioOutputDeviceName)) throw new InvalidOperationException("Device name can't be Null or Empty");
            if (isStreamOn) StopStream();

            _audioOutputObject = _audioCommandProcessor.audioOutputsDictionary[_audioOutputDeviceName].player;
            _audioCommandProcessor.audioOutputsDictionary[_audioOutputDeviceName].device.AudioEndpointVolume.MasterVolumeLevelScalar = _audioOutputDeviceVolume / 100.0f;

            /*_audioOutputDeviceName = audioOutputDeviceName;
            _audioOutputDevice = _audioCommandProcessor.GetDeviceByItsName(audioOutputDeviceName, _audioCommandProcessor.outputDevices);
            _audioOutputDevice.AudioEndpointVolume.MasterVolumeLevelScalar = _audioOutputDeviceVolume / 100.0f;
            _audioOutputObject?.Dispose();   // Disposing old object if there is one
            _audioOutputObject = new WasapiOut(_audioOutputDevice, AudioClientShareMode.Shared, false, _audioCommandProcessor.intercomStreamLatency);*/
        }

        /*private void SetBuffer()
        {
            if (isStreamOn) StopStream();

            _streamDataBuffer = new BufferedWaveProvider(_audioInputObject.WaveFormat);
            _audioOutputObject.Init(_streamDataBuffer);

            _audioInputObject.DataAvailable += (sender, e) =>
            {
                _streamDataBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
            };
        }*/

        /// <summary>
        /// When passing a value to a function, it is necessary to indicate the specific type of device
        /// </summary>
        public void UpdateAudioDevices(string audioInputDeviceName, string audioOutputDeviceName, float audioOutputDeviceVolume)
        {
            if (audioOutputDeviceVolume != -1f) _audioOutputDeviceVolume = audioOutputDeviceVolume; // must be before "SetOutputDevice"

            SetInputDevice(audioInputDeviceName);
            SetOutputDevice(audioOutputDeviceName, audioOutputDeviceVolume);
            BindInputToOutput(audioInputDeviceName, audioOutputDeviceName);

            isStreamReady = true;
        }

        public void UpdateAudioDevices_test(string audioInputDeviceName, string audioOutputDeviceName, float audioOutputDeviceVolume)
        {
            if (string.IsNullOrEmpty(audioInputDeviceName) || string.IsNullOrEmpty(audioOutputDeviceName))
                throw new InvalidOperationException("Device name can't be Null or Empty");
            if (isStreamOn)
                StopStream();

            _audioOutputDeviceVolume    = audioOutputDeviceVolume   == -1f      ? _audioOutputDeviceVolume  : audioOutputDeviceVolume;
            _audioInputDeviceName       = audioInputDeviceName      == "same"   ? _audioInputDeviceName     : audioInputDeviceName;
            _audioOutputDeviceName      = audioOutputDeviceName     == "same"   ? _audioOutputDeviceName    : audioOutputDeviceName;

            BindInputToOutput(inputDeviceName: _audioInputDeviceName, outputDeviceName: _audioOutputDeviceName);    // temp (so dirty)

            isStreamReady = true;
        }

        /*public void UpdateAudioOutputDevicesVolume(float audioOutputDeviceVolume)
        {
            if (audioOutputDeviceVolume != -1f) _audioOutputDeviceVolume = audioOutputDeviceVolume;

            _audioOutputDevice.AudioEndpointVolume.MasterVolumeLevelScalar = _audioOutputDeviceVolume / 100.0f;
        }*/





        private void BindInputToOutput(string inputDeviceName, string outputDeviceName)
        {
            // The data in the tuple cannot be changed one at a time,
            // you need to copy the entire tuple, change something and overwrite the entire tuple 

            var inputDeviceTuple = _audioCommandProcessor.audioInputsDictionary[inputDeviceName];
            var outputDeviceTuple = _audioCommandProcessor.audioOutputsDictionary[outputDeviceName];

            inputDeviceTuple.buffer = outputDeviceTuple.bufferForIntercom;
            _audioCommandProcessor.audioInputsDictionary[inputDeviceName] = inputDeviceTuple;


            // Due to the fact that these are now new elements, you need to overwrite the event handler and specify the data format

            

            inputDeviceTuple.receiver.DataAvailable += (sender, e) =>
            {
                inputDeviceTuple.buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
            };
        }
    }




    public enum IntercomStreamDirection
    {
        Incoming,   // from Participant to Researcher
        Outgoing    // from Researcher to Participant
    }
}
