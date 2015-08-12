using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace Redecode.Archimede
{
    public class Led : OutputPort
    {
        public static Led Com = new Led(48);
        public static Led Run = new Led(49);
        

        public Led(int pin) : base((Cpu.Pin)pin, false)
        {            
        }

        public void On()
        {
            Write(true);
        }

        public void Off()
        {
            Write(false);
        }

        public void Toggle()
        {
            Write(!Read());
        }
    }
}
