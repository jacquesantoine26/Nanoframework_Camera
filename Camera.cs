using System;
using System.Collections;
using System.Device.Gpio;
using System.Device.Spi;
using System.IO;
using System.Threading;


namespace MEGA_CAM_03
{
    // Code translated from MicroPython https://github.com/CoreElectronics/CE-Arducam-MicroPython
    public class Camera
    {
        // Struct to hold resolution key-value pairs
        private struct ResolutionEntry
        {
            public string Key;
            public byte Value;

            public ResolutionEntry(string key, byte value)
            {
                Key = key;
                Value = value;
            }
        }

        // Camera register constants
        private const byte CAM_REG_SENSOR_RESET = 0x07;
        private const byte CAM_SENSOR_RESET_ENABLE = 0x40;
        private const byte CAM_REG_SENSOR_ID = 0x40;
        private const byte SENSOR_5MP_1 = 0x81;
        private const byte SENSOR_3MP_1 = 0x82;
        private const byte SENSOR_5MP_2 = 0x83;
        private const byte SENSOR_3MP_2 = 0x84;
        private const byte CAM_REG_COLOR_EFFECT_CONTROL = 0x27;
        private const byte SPECIAL_NORMAL = 0x00;
        private const byte SPECIAL_COOL = 0x01;
        private const byte SPECIAL_WARM = 0x02;
        private const byte SPECIAL_BW = 0x04;
        private const byte SPECIAL_YELLOWING = 0x04;
        private const byte SPECIAL_REVERSE = 0x05;
        private const byte SPECIAL_GREENISH = 0x06;
        private const byte SPECIAL_LIGHT_YELLOW = 0x09; // 3MP Only
        private const byte CAM_REG_BRIGHTNESS_CONTROL = 0x22;
        private const byte BRIGHTNESS_MINUS_4 = 0x08;
        private const byte BRIGHTNESS_MINUS_3 = 0x06;
        private const byte BRIGHTNESS_MINUS_2 = 0x04;
        private const byte BRIGHTNESS_MINUS_1 = 0x02;
        private const byte BRIGHTNESS_DEFAULT = 0x00;
        private const byte BRIGHTNESS_PLUS_1 = 0x01;
        private const byte BRIGHTNESS_PLUS_2 = 0x03;
        private const byte BRIGHTNESS_PLUS_3 = 0x05;
        public const byte BRIGHTNESS_PLUS_4 = 0x07;
        private const byte CAM_REG_CONTRAST_CONTROL = 0x23;
        public const byte CONTRAST_MINUS_3 = 0x06;
        private const byte CONTRAST_MINUS_2 = 0x04;
        private const byte CONTRAST_MINUS_1 = 0x02;
        private const byte CONTRAST_DEFAULT = 0x00;
        private const byte CONTRAST_PLUS_1 = 0x01;
        private const byte CONTRAST_PLUS_2 = 0x03;
        public const byte CONTRAST_PLUS_3 = 0x05;
        private const byte CAM_REG_SATURATION_CONTROL = 0x24;
        private const byte SATURATION_MINUS_3 = 0x06;
        private const byte SATURATION_MINUS_2 = 0x04;
        private const byte SATURATION_MINUS_1 = 0x02;
        private const byte SATURATION_DEFAULT = 0x00;
        private const byte SATURATION_PLUS_1 = 0x01;
        private const byte SATURATION_PLUS_2 = 0x03;
        private const byte SATURATION_PLUS_3 = 0x05;
        private const byte CAM_REG_EXPOSURE_CONTROL = 0x25;
        private const byte EXPOSURE_MINUS_3 = 0x06;
        private const byte EXPOSURE_MINUS_2 = 0x04;
        private const byte EXPOSURE_MINUS_1 = 0x02;
        private const byte EXPOSURE_DEFAULT = 0x00;
        private const byte EXPOSURE_PLUS_1 = 0x01;
        private const byte EXPOSURE_PLUS_2 = 0x03;
        private const byte EXPOSURE_PLUS_3 = 0x05;
        private const byte CAM_REG_WB_MODE_CONTROL = 0x26;
        private const byte WB_MODE_AUTO = 0x00;
        private const byte WB_MODE_SUNNY = 0x01;
        private const byte WB_MODE_OFFICE = 0x02;
        private const byte WB_MODE_CLOUDY = 0x03;
        private const byte WB_MODE_HOME = 0x04;
        private const byte CAM_REG_SHARPNESS_CONTROL = 0x28; // 3MP only
        private const byte SHARPNESS_NORMAL = 0x00;
        private const byte SHARPNESS_1 = 0x01;
        private const byte SHARPNESS_2 = 0x02;
        private const byte SHARPNESS_3 = 0x03;
        private const byte SHARPNESS_4 = 0x04;
        private const byte SHARPNESS_5 = 0x05;
        private const byte SHARPNESS_6 = 0x06;
        private const byte SHARPNESS_7 = 0x07;
        private const byte SHARPNESS_8 = 0x08;
        private const byte CAM_REG_AUTO_FOCUS_CONTROL = 0x29; // 5MP only
        private const byte CAM_REG_IMAGE_QUALITY = 0x2A;
        private const byte IMAGE_QUALITY_HIGH = 0x00;
        private const byte IMAGE_QUALITY_MEDI = 0x01;
        private const byte IMAGE_QUALITY_LOW = 0x02;
        private const byte CAM_REG_DEBUG_DEVICE_ADDRESS = 0x0A;
        private const byte deviceAddress = 0x78;
        private const byte CAM_REG_SENSOR_STATE = 0x44;
        private const byte CAM_REG_SENSOR_STATE_IDLE = 0x01;
        private const byte CAM_REG_FORMAT = 0x20;
        private const byte CAM_IMAGE_PIX_FMT_JPG = 0x01;
        private const byte CAM_IMAGE_PIX_FMT_RGB565 = 0x02;
        private const byte CAM_IMAGE_PIX_FMT_YUV = 0x03;
        private const byte CAM_REG_CAPTURE_RESOLUTION = 0x21;
        private const byte RESOLUTION_320X240 = 0x01;
        private const byte RESOLUTION_640X480 = 0x02;
        private const byte RESOLUTION_1280X720 = 0x04;
        private const byte RESOLUTION_1600X1200 = 0x06;
        private const byte RESOLUTION_1920X1080 = 0x07;
        private const byte RESOLUTION_2048X1536 = 0x08; // 3MP only
        private const byte RESOLUTION_2592X1944 = 0x09; // 5MP only
        private const byte RESOLUTION_96X96 = 0x0A;
        private const byte RESOLUTION_128X128 = 0x0B;
        private const byte RESOLUTION_320X320 = 0x0C;
        private const byte ARDUCHIP_FIFO = 0x04;
        private const byte FIFO_CLEAR_ID_MASK = 0x01;
        private const byte FIFO_START_MASK = 0x02;
        private const byte ARDUCHIP_TRIG = 0x44;
        private const byte CAP_DONE_MASK = 0x04;
        private const byte FIFO_SIZE1 = 0x45;
        private const byte FIFO_SIZE2 = 0x46;
        private const byte FIFO_SIZE3 = 0x47;
        private const byte SINGLE_FIFO_READ = 0x3D;
        private const byte BURST_FIFO_READ = 0x3C;
        private const int BUFFER_MAX_LENGTH = 255;
        private const int WHITE_BALANCE_WAIT_TIME_MS = 500;

        private readonly SpiDevice _spiDevice;
        private readonly GpioPin _csPin;
        private readonly bool _debugTextEnabled;
        private string _cameraIdx;
        private bool _runStartUpConfig;
        private byte _currentPixelFormat;
        private byte _oldPixelFormat;
        private byte _currentResolutionSetting;
        private byte _oldResolution;
        private int _receivedLength;
        private int _totalLength;
        private bool _firstBurstRun;
        private readonly byte[] _imageBuffer;
        private int _validImageBuffer;
        private readonly long _startTime;

        private readonly ResolutionEntry[] _valid3MpResolutions = new ResolutionEntry[]
        {
        new ResolutionEntry("320x240", RESOLUTION_320X240),
        new ResolutionEntry("640x480", RESOLUTION_640X480),
        new ResolutionEntry("1280x720", RESOLUTION_1280X720),
        new ResolutionEntry("1600x1200", RESOLUTION_1600X1200),
        new ResolutionEntry("1920x1080", RESOLUTION_1920X1080),
        new ResolutionEntry("2048x1536", RESOLUTION_2048X1536),
        new ResolutionEntry("96x96", RESOLUTION_96X96),
        new ResolutionEntry("128x128", RESOLUTION_128X128),
        new ResolutionEntry("320x320", RESOLUTION_320X320)
        };

        private readonly ResolutionEntry[] _valid5MpResolutions = new ResolutionEntry[]
        {
        new ResolutionEntry("320x240", RESOLUTION_320X240),
        new ResolutionEntry("640x480", RESOLUTION_640X480),
        new ResolutionEntry("1280x720", RESOLUTION_1280X720),
        new ResolutionEntry("1600x1200", RESOLUTION_1600X1200),
        new ResolutionEntry("1920x1080", RESOLUTION_1920X1080),
        new ResolutionEntry("2592x1944", RESOLUTION_2592X1944),
        new ResolutionEntry("96x96", RESOLUTION_96X96),
        new ResolutionEntry("128x128", RESOLUTION_128X128),
        new ResolutionEntry("320x320", RESOLUTION_320X320)
        };

        public Camera(SpiDevice spiDevice, GpioPin csPin, bool skipSleep = false, bool debugTextEnabled = false)
        {
            _spiDevice = spiDevice;
            _csPin = csPin;
            _debugTextEnabled = debugTextEnabled;
            _cameraIdx = "NOT DETECTED";
            _runStartUpConfig = true;
            _imageBuffer = new byte[BUFFER_MAX_LENGTH];
            _startTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

            WriteReg(CAM_REG_SENSOR_RESET, CAM_SENSOR_RESET_ENABLE); // Reset camera
            WaitIdle();
            GetSensorConfig(); // Get camera sensor information
            WaitIdle();
            WriteReg(CAM_REG_DEBUG_DEVICE_ADDRESS, deviceAddress);
            WaitIdle();

            _currentPixelFormat = CAM_IMAGE_PIX_FMT_JPG;
            _oldPixelFormat = _currentPixelFormat;
            _currentResolutionSetting = RESOLUTION_640X480;
            _oldResolution = _currentResolutionSetting;

            SetFilter(SPECIAL_NORMAL); 

            _receivedLength = 0;
            _totalLength = 0;
            _firstBurstRun = false;
            _validImageBuffer = 0;

            if (_debugTextEnabled)
            {
                Console.WriteLine($"Camera version = {_cameraIdx}");
            }

            if (_cameraIdx == "3MP")
            {
                Thread.Sleep(WHITE_BALANCE_WAIT_TIME_MS);
                WaitIdle();
                StartupRoutine3MP();
            }
            else if (_cameraIdx == "5MP" && !skipSleep)
            {
                Thread.Sleep(WHITE_BALANCE_WAIT_TIME_MS);
            }
        }

        private void StartupRoutine3MP()
        {
            if (_debugTextEnabled) Console.WriteLine("Running 3MP startup routine");
            Thread.Sleep(WHITE_BALANCE_WAIT_TIME_MS);
            if (_debugTextEnabled) Console.WriteLine("Finished 3MP startup routine");

            //string[] files = Directory.GetFiles(@"D:\");
            //foreach (string file in files)
            //{
            //    Console.WriteLine(file);
            //}
        }

        public void CaptureJpg()
        {
            long currentTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            if (_cameraIdx == "5MP" && (currentTime - _startTime) <= WHITE_BALANCE_WAIT_TIME_MS)
            {
                Console.WriteLine($"Please add a {WHITE_BALANCE_WAIT_TIME_MS}ms delay to allow for white balance to run");
                return;
            }

            if (_debugTextEnabled) Console.WriteLine("Entered capture_jpg");

            if (_oldPixelFormat != _currentPixelFormat || _runStartUpConfig)
            {
                _oldPixelFormat = _currentPixelFormat;
                WriteReg(CAM_REG_FORMAT, _currentPixelFormat);
                WaitIdle();
            }

            if (_oldResolution != _currentResolutionSetting || _runStartUpConfig)
            {
                _oldResolution = _currentResolutionSetting;
                WriteReg(CAM_REG_CAPTURE_RESOLUTION, _currentResolutionSetting);
                if (_debugTextEnabled) Console.WriteLine($"Setting resolution: {_currentResolutionSetting}");
                WaitIdle();
            }

            _runStartUpConfig = false;
            SetCapture();
            if (_debugTextEnabled) Console.WriteLine("Finished capture_jpg");
        }

        private void UpdateProgress(float progress, int barLength = 20)
        {
            int filledLength = (int)(barLength * progress);
            string bar = new string('#', filledLength) + new string('-', barLength - filledLength);
            Console.Write($"\rProgress: |{bar}| {(int)(progress * 100)}%");
        }

        public void SaveJpg(string filename = @"I:\image.jpg", bool progressBar = true)
        {
            using (var jpgStream = File.Open(filename, FileMode.Append))
            {
                int recvLen = _receivedLength;
                int startingLen = recvLen;

                _csPin.Write(PinValue.Low);
                _spiDevice.Write(new byte[] { BURST_FIFO_READ });
                _spiDevice.Read(new byte[1]); // Dummy read

                int inx = -1;
                while (recvLen > 0)
                {
                    float progress = (float)(startingLen - recvLen) / startingLen;
                    if (progressBar) UpdateProgress(progress);

                    byte lastByte = _imageBuffer[BUFFER_MAX_LENGTH - 1];
                    _spiDevice.Read(_imageBuffer);
                    recvLen -= BUFFER_MAX_LENGTH;

                    // Check for JPEG end marker (0xFF 0xD9)
                    for (int i = 0; i < BUFFER_MAX_LENGTH - 1; i++)
                    {
                        if (_imageBuffer[i] == 0xFF && _imageBuffer[i + 1] == 0xD9)
                        {
                            inx = i + 1;
                            break;
                        }
                    }

                    if (inx >= 0)
                    {
                        jpgStream.Write(_imageBuffer, 0, inx + 1);
                        jpgStream.Close();
                        if (progressBar) UpdateProgress(1);
                        Console.WriteLine("\nImage saved");
                        break;
                    }
                    else if (lastByte == 0xFF && _imageBuffer[0] == 0xD9)
                    {
                        byte[] endMarker = new byte[] { 0xD9 };
                        jpgStream.Write(endMarker, 0, 1);
                        jpgStream.Close();
                        if (progressBar) UpdateProgress(1);
                        Console.WriteLine("\nImage saved");
                        break;
                    }
                    else
                    {
                        jpgStream.Write(_imageBuffer, 0, BUFFER_MAX_LENGTH);
                    }
                }

                _csPin.Write(PinValue.High);
            }
        }

        public string Resolution
        {
            get => _currentResolutionSetting.ToString();
            set
            {
                string inputStringLower = value.ToLower();
                bool found = false;
                if (_cameraIdx == "3MP")
                {
                    foreach (var entry in _valid3MpResolutions)
                    {
                        if (entry.Key == inputStringLower)
                        {
                            _currentResolutionSetting = entry.Value;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        string validKeys = "";
                        for (int i = 0; i < _valid3MpResolutions.Length; i++)
                        {
                            validKeys += _valid3MpResolutions[i].Key;
                            if (i < _valid3MpResolutions.Length - 1)
                            {
                                validKeys += ", ";
                            }
                        }
                        throw new ArgumentException($"Invalid resolution provided for {_cameraIdx}, please select from {validKeys}");
                    }
                }
                else if (_cameraIdx == "5MP")
                {
                    foreach (var entry in _valid5MpResolutions)
                    {
                        if (entry.Key == inputStringLower)
                        {
                            _currentResolutionSetting = entry.Value;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        string validKeys = "";
                        for (int i = 0; i < _valid5MpResolutions.Length; i++)
                        {
                            validKeys += _valid5MpResolutions[i].Key;
                            if (i < _valid5MpResolutions.Length - 1)
                            {
                                validKeys += ", ";
                            }
                        }
                        new ArgumentException($"Invalid resolution provided for {_cameraIdx}, please select from {validKeys}");
                    }
                }
            }
        }

        public void SetPixelFormat(byte newPixelFormat)
        {
            _currentPixelFormat = newPixelFormat;
        }


        //////////////////////////////////////////////////////////////////////////
        //Routines missing from the Micropython code

        byte[] ov3640GainValue = new byte[]
            {
                0x00, 0x10, 0x18, 0x30, 0x34, 0x38, 0x3b, 0x3f,
                0x72, 0x74, 0x76, 0x78, 0x7a, 0x7c, 0x7e, 0xf0,
                0xf1, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8,
                0xf9, 0xfa, 0xfb, 0xfc, 0xfd, 0xfe, 0xff
            };
                //0 to 63: Moderate steps(0, 16, 24, 48, 52, 56, 59, 63).
                //63 to 114: A large jump from 63 to 114.
                //114 to 126: Small steps(114, 116, 118, 120, 122, 124, 126).
                //126 to 240: Another large jump from 126 to 240.
                //240 to 255: Very fine steps(240, 241, 242, ..., 255).

        private const byte CAM_REG_EXPOSURE_GAIN_WHILEBALANCE_CONTROL = 0x30;
        private const byte CAM_REG_MANUAL_GAIN_BIT_9_8 = 0x31;
        private const byte CAM_REG_MANUAL_GAIN_BIT_7_0 = 0x32;
        private const byte SET_EXPOSURE = 0x01;
        private const byte SET_GAIN = 0x00;
        private const byte SET_WHILEBALANCE = 0x02;

        public void CameraSetAutoWhiteBalance(bool val)
        {
            // 0: Turn off automatic
            // 1: Turn on Automatic
            byte symbol = 0;
            if (val == true)
            {
                symbol |= 0x80;
            }
            symbol |= SET_GAIN; //0x00
            WriteReg(CAM_REG_EXPOSURE_GAIN_WHILEBALANCE_CONTROL, symbol); 
            WaitIdle();
        }

        public void cameraSetAutoExposure(bool val)
        {
            // 0: Turn off automatic
            // 1: Turn on Automatic
            byte symbol = 0;
            if (val == true)
            {
                symbol |= 0x80;
            }
            symbol |= SET_EXPOSURE; // 0x01
            WriteReg(CAM_REG_EXPOSURE_GAIN_WHILEBALANCE_CONTROL, symbol); 
            WaitIdle();
        }

        public void cameraSetAutoISOSensitive(bool val)
        {
            // 0: Turn off automatic
            // 1: Turn on Automatic
            byte symbol = 0;
            if (val == true)
            {
                symbol |= 0x80;
            }
            symbol |= SET_WHILEBALANCE; // 0x02
            WriteReg(CAM_REG_EXPOSURE_GAIN_WHILEBALANCE_CONTROL, symbol); 
            WaitIdle();
        }

        public void CameraSetISOSensitivity(int isoSense)
        {   
            //   1..isosense..32
            //Before calling this method, you need to use the setAutoISOSensitive(false) function to turn off the auto gain function
            if (_cameraIdx == "3MP")
            {
                isoSense = ov3640GainValue[isoSense - 1];
            }
            WriteReg(CAM_REG_MANUAL_GAIN_BIT_9_8, (byte)(isoSense >> 8)); // set AGC VALUE
            WaitIdle();
            WriteReg(CAM_REG_MANUAL_GAIN_BIT_7_0, (byte)(isoSense & 0xff));
            WaitIdle();
        }

        //////////////////////////////////////////////////////////////////////////

        public void SetBrightnessLevel(byte brightness)
        {
            WriteReg(CAM_REG_BRIGHTNESS_CONTROL, brightness);
            WaitIdle();
        }

        public void SetFilter(byte effect)
        {
            WriteReg(CAM_REG_COLOR_EFFECT_CONTROL, effect);
            WaitIdle();
        }

        public void SetExposure(byte exposure)
        {
            WriteReg(CAM_REG_EXPOSURE_CONTROL, exposure);
            WaitIdle();
        }

        public void SetSaturationControl(byte saturationValue)
        {
            WriteReg(CAM_REG_SATURATION_CONTROL, saturationValue);
            WaitIdle();
        }

        public void SetContrast(byte contrast)
        {
            WriteReg(CAM_REG_CONTRAST_CONTROL, contrast);
            WaitIdle();
        }

        public void SetWhiteBalance(string environment)
        {
            byte registerValue = WB_MODE_AUTO;

            switch (environment.ToLower())
            {
                case "sunny":
                    registerValue = WB_MODE_SUNNY;
                    break;
                case "office":
                    registerValue = WB_MODE_OFFICE;
                    break;
                case "cloudy":
                    registerValue = WB_MODE_CLOUDY;
                    break;
                case "home":
                    registerValue = WB_MODE_HOME;
                    break;
                default:
                    if (_cameraIdx == "3MP")
                    {
                        Console.WriteLine("For best results set a White Balance setting");
                    }
                    break;
            }

            WriteReg(CAM_REG_WB_MODE_CONTROL, registerValue);
            WaitIdle();
        }

        private void ClearFifoFlag()
        {
            WriteReg(ARDUCHIP_FIFO, FIFO_CLEAR_ID_MASK);
        }

        private void StartCapture()
        {
            WriteReg(ARDUCHIP_FIFO, FIFO_START_MASK);
        }

        private void SetCapture()
        {
            if (_debugTextEnabled) Console.WriteLine("Entered _set_capture");
            ClearFifoFlag();
            WaitIdle();
            StartCapture();
            if (_debugTextEnabled) Console.WriteLine("FIFO flag cleared, started _start_capture, waiting for CAP_DONE_MASK");

            while ((GetBit(ARDUCHIP_TRIG, CAP_DONE_MASK) & CAP_DONE_MASK) == 0)
            {
                if (_debugTextEnabled) Console.WriteLine($"ARDUCHIP_TRIG register, CAP_DONE_MASK: {GetBit(ARDUCHIP_TRIG, CAP_DONE_MASK)}");
                Thread.Sleep(200);
            }

            WaitIdle();
            _receivedLength = ReadFifoLength();
            _totalLength = _receivedLength;
            _firstBurstRun = false;
            if (_debugTextEnabled) Console.WriteLine("FIFO length has been read");
        }

        private int ReadFifoLength()
        {
            if (_debugTextEnabled) Console.WriteLine("Entered _read_fifo_length");
            byte len1 = ReadReg(FIFO_SIZE1);
            byte len2 = ReadReg(FIFO_SIZE2);
            byte len3 = ReadReg(FIFO_SIZE3);

            if (_debugTextEnabled)
            {
                Console.WriteLine($"FIFO length bytes: {len1}, {len2}, {len3}");
            }

            int length = ((len3 << 16) | (len2 << 8) | len1) & 0xFFFFFF;
            if (length > 5000000)
            {
                Console.WriteLine($"Error fifo length is too long >5MB Size: {length}");
                Console.WriteLine("Arducam possibly did not take a picture and is returning garbage data");
            }

            return length;
        }

        private void GetSensorConfig()
        {
            byte cameraId = ReadReg(CAM_REG_SENSOR_ID);
            WaitIdle();
            if (cameraId == SENSOR_3MP_1 || cameraId == SENSOR_3MP_2)
            {
                _cameraIdx = "3MP";
            }
            else if (cameraId == SENSOR_5MP_1 || cameraId == SENSOR_5MP_2)
            {
                _cameraIdx = "5MP";
            }
        }

        private void WriteReg(byte addr, byte val)
        {
            BusWrite((byte)(addr | 0x80), val);
        }

        private byte ReadReg(byte addr)
        {
            return BusRead((byte)(addr & 0x7F));
        }

        private byte BusWrite(byte addr, byte val)
        {
            _csPin.Write(PinValue.Low);
            _spiDevice.Write(new byte[] { addr, val });
            _csPin.Write(PinValue.High);
            Thread.Sleep(1); // From Arducam Library
            return 1;
        }

        private byte BusRead(byte addr)
        {
            _csPin.Write(PinValue.Low);
            _spiDevice.Write(new byte[] { addr });
            byte[] data = new byte[1];
            _spiDevice.Read(data); // Dummy read
            _spiDevice.Read(data);
            _csPin.Write(PinValue.High);
            return data[0];
        }

        private byte ReadByte()
        {
            _csPin.Write(PinValue.Low);
            _spiDevice.Write(new byte[] { SINGLE_FIFO_READ });
            byte[] data = new byte[1];
            _spiDevice.Read(data); // Dummy read
            _spiDevice.Read(data);
            _csPin.Write(PinValue.High);
            _receivedLength--;
            return data[0];
        }

        private void WaitIdle()
        {
            byte data = ReadReg(CAM_REG_SENSOR_STATE);
            while ((data & 0x03) == CAM_REG_SENSOR_STATE_IDLE)
            {
                data = ReadReg(CAM_REG_SENSOR_STATE);
                Thread.Sleep(2);
            }
        }

        private byte GetBit(byte addr, byte bit)
        {
            byte data = ReadReg(addr);
            return (byte)(data & bit);
        }
    }
}