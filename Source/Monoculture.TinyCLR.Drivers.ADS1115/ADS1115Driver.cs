using System;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace Monoculture.TinyCLR.Drivers.ADS1115
{
    public class ADS1115Driver
    {
        private readonly I2cDevice _device;
        private const int Ads1X15PointerConfig = 0x01;
        private const int Ads1X15PointerConversion = 0x00;
        private const int Ads1X15PointerLowThreshold = 0x02;
        private const int Ads1X15PointerHighThreshold = 0x03;

        public ADS1115Driver(I2cDevice device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }

        public static I2cConnectionSettings GetI2CConnectionSettings(ADS1115Address address)
        {
            var settings = new I2cConnectionSettings((int)address)
            {
                BusSpeed = I2cBusSpeed.FastMode,
                AddressFormat = I2cAddressFormat.SevenBit
            };

            return settings;
        }

        public float Read(
            ADS1115MultiplexerMode mux,
            ADS1115PgaScaling gain = ADS1115PgaScaling.TwoThirds,
            ADS1115SampleRate rate = ADS1115SampleRate.X128Sps)
        {
            StartAdcInternal(
                mux,
                gain,
                rate,
                ADS1115DeviceMode.SingleShot);

            Thread.Sleep(10);

            var adc = GetLastConversion();

            return ConvertMillivolts(adc, gain);
        }

        public void StartContinuousConversion(
            ADS1115MultiplexerMode mux,
            ADS1115PgaScaling gain = ADS1115PgaScaling.One,
            ADS1115SampleRate sampleRate = ADS1115SampleRate.X250Sps)
        {
            StartAdcInternal(
                mux, 
                gain, 
                sampleRate, 
                ADS1115DeviceMode.Continuous);
        }

        public void StartComparator(
            ADS1115MultiplexerMode mux,
            ushort lowThreshold = 32768,
            ushort highThreshHold = 32767,
            ADS1115PgaScaling gain = ADS1115PgaScaling.TwoThirds,
            ADS1115SampleRate sampleRate = ADS1115SampleRate.X128Sps,
            ADS1115ComparatorMode comparatorMode = ADS1115ComparatorMode.Traditional,
            ADS1115ComparatorPolarity comparatorPolarity = ADS1115ComparatorPolarity.ActiveLow,
            ADS1115ComparatorLatching comparatorLatching = ADS1115ComparatorLatching.Latching,
            ADS1115ComparatorQueueMode comparatorQueueMode = ADS1115ComparatorQueueMode.TwoConversions)
        {
            StartComparatorInternal(
                mux,
                gain,
                sampleRate,
                ADS1115DeviceMode.Continuous,
                lowThreshold,
                highThreshHold,
                comparatorPolarity,
                comparatorMode,
                comparatorLatching,
                comparatorQueueMode);
        }

        public void Stop()
        {
            _device.Write(new byte[] {Ads1X15PointerConfig, 0x85, 0x83 });
        }

        public int GetLastConversion()
        {
            var buffer = new byte[2];

            _device.WriteRead(new byte[] {Ads1X15PointerConversion}, buffer);

            return BitConverter.ToInt16(BitConverter.IsLittleEndian ? new[] {buffer[1], buffer[0]} : buffer, 0);
        }
    
        private void StartAdcInternal(
            ADS1115MultiplexerMode mux, 
            ADS1115PgaScaling gain, 
            ADS1115SampleRate rate,
            ADS1115DeviceMode mode)
        {
            var command = BuildConfigCommand(
                mux,
                gain,
                rate,
                mode,
                ADS1115ComparatorPolarity.ActiveLow,
                ADS1115ComparatorMode.Traditional,     
                ADS1115ComparatorLatching.NonLatching,
                ADS1115ComparatorQueueMode.None);

            _device.Write(command);
        }
 
        private void StartComparatorInternal(
            ADS1115MultiplexerMode mux,
            ADS1115PgaScaling gain,
            ADS1115SampleRate rate,
            ADS1115DeviceMode mode,
            ushort lowThreshold,
            ushort highThreshold,
            ADS1115ComparatorPolarity comparatorPolarity,
            ADS1115ComparatorMode comparatorMode,
            ADS1115ComparatorLatching comparatorLatching,
            ADS1115ComparatorQueueMode comparatorQueueMode)
        {
            WriteThreshold(lowThreshold,highThreshold);

            var command = BuildConfigCommand(
                mux,
                gain,
                rate,
                mode,
                comparatorPolarity,
                comparatorMode,
                comparatorLatching,
                comparatorQueueMode);

            _device.Write(command);
        }

        private static byte[] BuildConfigCommand(
            ADS1115MultiplexerMode mux,
            ADS1115PgaScaling gain,
            ADS1115SampleRate rate,
            ADS1115DeviceMode mode,
            ADS1115ComparatorPolarity comparatorPolarity,
            ADS1115ComparatorMode comparatorMode,
            ADS1115ComparatorLatching comparatorLatching,
            ADS1115ComparatorQueueMode comparatorQueueMode)
        {
            var command = new byte[3];

            command[0] = Ads1X15PointerConfig;

            command[1] = (byte)((byte) mode << 7
                                 | (byte) mux << 4
                                 | (byte) gain << 1
                                 | (byte) mode);

            command[2] = (byte)((byte) rate << 5
                                 | (byte)comparatorMode << 4
                                 | (byte)comparatorPolarity << 3
                                 | (byte)comparatorLatching << 2
                                 | (byte)comparatorQueueMode);

            return command;
        }

        private void WriteThreshold(ushort lowThreshold, ushort highThreshold)
        {
            if (lowThreshold > highThreshold)
                throw new Exception("lowThreshold cannot be greater than the highThreshold.");

            _device.Write(new byte[] { Ads1X15PointerHighThreshold, (byte)highThreshold, (byte)(highThreshold >> 8) });

            Thread.Sleep(10);

            _device.Write(new byte[] { Ads1X15PointerLowThreshold, (byte)lowThreshold, (byte)(lowThreshold >> 8) });

            Thread.Sleep(10);
        }

        public float ConvertMillivolts(int value, ADS1115PgaScaling scaling)
        {
            return value * GetMvPerBit(scaling);
        }

        public float GetMvPerBit(ADS1115PgaScaling gain)
        {
            switch (gain)
            {
                case ADS1115PgaScaling.TwoThirds:
                    return 0.187F;
                case ADS1115PgaScaling.One:
                    return 0.125F;
                case ADS1115PgaScaling.Two:
                    return 0.0625F;
                case ADS1115PgaScaling.Four:
                    return 0.03125F;
                case ADS1115PgaScaling.Eight:
                    return 0.015625F;
                case ADS1115PgaScaling.Sixteen:
                    return 0.0078125F;
                default:
                    throw new Exception("Unrecognized PgaScaling option");
            }
        }
    }
}
