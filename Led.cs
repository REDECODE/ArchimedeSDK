using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace Redecode.Archimede
{
    public class Led : OutputPort
    {
        private static Led _Com;
        private static Led _Run;

        public static Led Com {
            get {
                if (_Com == null)
                {
                    _Com = new Led(48);
                }
                return _Com;
            }
        }
        public static Led Run
        {
            get
            {
                if (_Run == null)
                {
                    _Run = new Led(49);
                }
                return _Run;
            }
        }
        

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
