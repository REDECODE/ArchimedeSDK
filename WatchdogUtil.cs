using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace Redecode.Archimede
{
    public static class WatchdogUtil
    {
        static OutputPort port;

        public static void Reset()
        {
            if (port == null)
            {
                port = new OutputPort((Cpu.Pin)60, false);
            }

            port.Write(true);
            Thread.Sleep(10);
            port.Write(false);
        }
    }
}
