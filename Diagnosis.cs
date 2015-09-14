using System;
using Microsoft.SPOT;
using GHI.OSHW.Hardware;
using Microsoft.SPOT.IO;

namespace Redecode.Archimede
{
    public class Diagnosis
    {
        public static long Ticks;

        public static void Start()
        {
            Ticks = RTC.GetTime().Ticks;
        }

        public static DateTime DateLastReboot {
            get
            {
                return new DateTime(Ticks);
            }
        }

        public static uint RAM_FreeSpace
        {
            get
            {
                return Debug.GC(false);
            }
        }

        public static long SD_FreeSpace
        {
            get
            {
                try
                {
                    VolumeInfo SD = VolumeInfo.GetVolumes()[0];
                    return SD.TotalFreeSpace;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static long SD_TotalSize
        {
            get
            {
                try
                {
                    VolumeInfo SD = VolumeInfo.GetVolumes()[0];
                    return SD.TotalSize;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static bool SD_Connected
        {
            get
            {
                return VolumeInfo.GetVolumes().Length > 0;                    
            }
        }

    }
}
