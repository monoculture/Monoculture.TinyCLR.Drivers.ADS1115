namespace Monoculture.TinyCLR.Drivers.ADS1115
{
    public enum ADS1115ComparatorQueueMode : byte
    {
        OneConversions = 0x01,
        TwoConversions = 0x02,
        FourConversions = 0x04,
        None = 0x03
    }
}
