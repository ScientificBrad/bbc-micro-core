namespace BbcMicro.Core
{
    public class Cpu65C02 : Cpu6502
    {
        public Cpu65C02(IMemoryBus bus) : base(bus)
        {
            Is65C02 = true;
        }
    }
}
