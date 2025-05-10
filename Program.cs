using Iot.Device.Button;
using nanoFramework.Hardware.Esp32;
using nanoFramework.System.IO.FileSystem;
using System;
using System.Device.Gpio;
using System.Device.Spi;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace MEGA_CAM_03
{
    
    public class Program
    {
        private static SpiDevice _spiCamera;
        private static GpioPin _csCamera;
        private static SDCard _sdCard;
        private static Camera _camera;

        public static void Main()
        {
            //SD card: SPI bus 2 configuration
            const uint csSD = 15; // SD card CS pin
            Configuration.SetPinFunction(12, DeviceFunction.SPI2_MISO);
            Configuration.SetPinFunction(13, DeviceFunction.SPI2_MOSI);
            Configuration.SetPinFunction(14, DeviceFunction.SPI2_CLOCK);

            // Camera: SPI bus 1 configuration
            int sckPin = 18;
            int misoPin = 19;
            int mosiPin = 23;
            int csCameraPin = 5; // Camera CS pin
            Configuration.SetPinFunction(misoPin, DeviceFunction.SPI1_MISO);
            Configuration.SetPinFunction(mosiPin, DeviceFunction.SPI1_MOSI);
            Configuration.SetPinFunction(sckPin, DeviceFunction.SPI1_CLOCK);

            // Initialize GPIO controller for camera CS
            GpioController gpio = new GpioController();
            _csCamera = gpio.OpenPin(csCameraPin, PinMode.Output);
            _csCamera.Write(PinValue.High); // Deselect camera

            // Initialize SPI for camera (bus 1)
            var cameraSpiSettings = new SpiConnectionSettings(1)
            {
                ClockFrequency = 1_000_000, // 2 MHz
                Mode = SpiMode.Mode0
            };
            _spiCamera = SpiDevice.Create(cameraSpiSettings);

            // Initialize SD card (SPI bus 2, no CS toggling)
            _sdCard = new SDCard(new SDCardSpiParameters { spiBus = 2, chipSelectPin = csSD });
            MountSDCard();

            // Initialize camera
            _camera = new Camera(_spiCamera, _csCamera);
            _camera.cameraSetAutoExposure(false);
            _camera.SetBrightnessLevel(0x07);
            _camera.SetExposure(0x05);
            _camera.CameraSetAutoWhiteBalance(false);
            _camera.SetWhiteBalance("home");
            _camera.CaptureJpg();
            Thread.Sleep(1000);
            _camera.SaveJpg(@"D:\test.jpg");

            Thread.Sleep(Timeout.Infinite);
        }

        private static void MountSDCard()
        {
            try
            {
                Debug.WriteLine("Mounting SD card...");
                Thread.Sleep(10000);
                _sdCard.Mount();
                Debug.WriteLine("SD card mounted.");
                string[] files = Directory.GetFiles(@"D:\");
                foreach (string file in files)
                {
                    Console.WriteLine(file);
                }
                
                Debug.WriteLine("SD card mounted.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SD mount failed: {ex.Message}");
            }
        }
    }
}