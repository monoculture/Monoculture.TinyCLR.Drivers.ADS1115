namespace Monoculture.TinyCLR.Drivers.ADS1115
{
    public enum ADS1115MultiplexerMode : byte
    {
        Differential01 = 0x00,
        Differential03 = 0x01,
        Differential13 = 0x02,
        Differential23 = 0x03,
        SingleEnded0 = 0x04,
        SingleEnded1 = 0x05,
        SingleEnded2 = 0x06,
        SingleEnded3 = 0x07
    }
}
