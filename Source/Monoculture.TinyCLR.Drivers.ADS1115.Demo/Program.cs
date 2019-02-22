using System;
using System.Diagnostics;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.I2c;
using GHIElectronics.TinyCLR.Pins;

namespace Monoculture.TinyCLR.Drivers.ADS1115.Demo
{
    class Program
    {
        static void Main()
        {
            ReadDifferential();
        }

        private static void ReadSingleEnded()
        {
            var device = GetDevice();

            var ch1Millivolts = device.Read(ADS1115MultiplexerMode.SingleEnded0);
            var ch2Millivolts = device.Read(ADS1115MultiplexerMode.SingleEnded1);
            var ch3Millivolts = device.Read(ADS1115MultiplexerMode.SingleEnded2);
            var ch4Millivolts = device.Read(ADS1115MultiplexerMode.SingleEnded3);

            Debug.WriteLine($"Channel #0: {ch1Millivolts}");
            Debug.WriteLine($"Channel #1: {ch2Millivolts}");
            Debug.WriteLine($"Channel #2: {ch3Millivolts}");
            Debug.WriteLine($"Channel #3: {ch4Millivolts}");
        }

        private static void ReadDifferential()
        {
            var device = GetDevice();

            var ch01Millivolts = device.Read(ADS1115MultiplexerMode.Differential01, ADS1115PgaScaling.One);

            Debug.WriteLine($"Channel #0: {ch01Millivolts}");
        }

        private static void ContinuousConversion()
        {
            var device = GetDevice();

            device.StartContinuousConversion(
                ADS1115MultiplexerMode.SingleEnded0,
                ADS1115PgaScaling.TwoThirds,
                ADS1115SampleRate.X128Sps);

            for (var i = 0; i < 100; i++)
            {
                Thread.Sleep(10);

                var adc = device.GetLastConversion();

                var mv = device.ConvertMillivolts(adc, ADS1115PgaScaling.TwoThirds);

                Debug.WriteLine(mv.ToString());
            }

            device.Stop();
        }

        public static void Comparator()
        {
            var device = GetDevice();

            const ushort lowThreshold = 0;

            const ushort highThreshold = 1000;

            device.StartComparator(
                ADS1115MultiplexerMode.SingleEnded0,
                lowThreshold,
                highThreshold,
                ADS1115PgaScaling.TwoThirds,
                ADS1115SampleRate.X128Sps,
                ADS1115ComparatorMode.Traditional,
                ADS1115ComparatorPolarity.ActiveHi,
                ADS1115ComparatorLatching.NonLatching,
                ADS1115ComparatorQueueMode.OneConversions);
        }

        private static ADS1115Driver GetDevice()
        {
            var settings = ADS1115Driver.GetI2CConnectionSettings(ADS1115Address.Gnd);

            var controller = I2cController.FromName(G120E.I2cBus.I2c0);

            var device = controller.GetDevice(settings);

            return new ADS1115Driver(device);
        }
    }
}
