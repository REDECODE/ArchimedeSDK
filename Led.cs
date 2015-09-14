using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;

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

        Thread tBlink;

        public Led(int pin) : base((Cpu.Pin)pin, false)
        {            
        }

        public void On()
        {
            StopBlink();
            Write(true);
        }

        public void Off()
        {
            StopBlink();
            Write(false);
        }

        public void Toggle()
        {
            Write(!Read());
        }        

        public void Blink(int time = 1000)
        {
            StopBlink();

            tBlink = new Thread(() =>
            {
                while (true) {
                    Write(true);
                    Thread.Sleep(time);
                    Write(false);
                    Thread.Sleep(time);
                }
            });

            tBlink.Start();
        }

        public void BlinkFor(int n, int time = 500)
        {
            StopBlink();

            tBlink = new Thread(() =>
            {
                while (true) {
                    for (int i = 0; i < n; i++)
                    {
                        Write(true);
                        Thread.Sleep(time);
                        Write(false);
                        Thread.Sleep(time);
                    }

                    Thread.Sleep(time*2);
                }
            });

            tBlink.Start();
        }

        public void StopBlink()
        {
            if (tBlink != null)
            {
                tBlink.Abort();
                tBlink = null;
            }
        }

    }
}
