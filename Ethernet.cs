using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Threading;
using GHI.OSHW.Hardware;
using Microsoft.SPOT.Hardware;

namespace Redecode.Archimede
{
    public delegate void OnConnectHandler();
    public delegate void OnDisconnectHandler();

    public class Ethernet
    {
        public static string static_IP;
        public static string static_Mask;
        public static string static_Gateway;

        public static byte[] MACAddress;
        public static string IPAddress;

        public static event OnConnectHandler OnConnect;
        public static event OnDisconnectHandler OnDisconnect;

        //--- this is for Default
        public static void UseDHCP()
        {
            static_IP = null;
            static_Mask = null;
            static_Gateway = null;
        }

        public static void UseStaticIp(string ip, string mask, string gateway)
        {
            static_IP = ip;
            static_Mask = mask;
            static_Gateway = gateway;
        }

        public static void UseMacAddress(byte[] mac)
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var networkInterface = interfaces[0];

            MACAddress = mac;

            bool different = false;

            for (int i = 0; i < MACAddress.Length; i++)
            {
                if (different = (MACAddress[i] != networkInterface.PhysicalAddress[i]))
                {
                    break;
                }
            }

            if (different)
            {
                Log.Debug("SETTING MAC Address");
                networkInterface.PhysicalAddress = MACAddress;                             
                PowerState.RebootDevice(true);
            }
        }

        public static void UseMacAddress(string mac)
        {
            string[] nums = mac.Split(':');
            byte[] bytes = new byte[nums.Length];
            for (int i=0; i<nums.Length; i++) {
               bytes[i] = (byte)Convert.ToInt32(nums[i], 16);
            }

            UseMacAddress(bytes);
        }

        

        public static void Connect()
        {
            Log.Debug("IPAddress "+IPAddress);
            if (IPAddress != null)
            {
                return; //-- Already connected
            }

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var networkInterface = interfaces[0];

            while (IPAddress == null)
            {
                try
                {
                    if (static_IP != null)
                    {
                        //networkInterface.ReleaseDhcpLease();
                        networkInterface.EnableStaticIP(static_IP, static_Mask, static_Gateway);
                        //networkInterface.EnableStaticDns(new string[] { "8.8.8.8" });

                        //if (MACAddress != null)
                        //{
                        //    networkInterface.PhysicalAddress = MACAddress;
                        //}
                    }
                    else
                    {

                        //Microsoft.SPOT.Hardware.PowerState.RebootDevice();
                        //networkInterface.EnableDynamicDns();
                        /*if (MACAddress != null)
                        {
                            bool different = false;
                            for (int i=0; i<MACAddress.Length; i++) {
                                if (different = (MACAddress[i] != networkInterface.PhysicalAddress[i])) {
                                    break;
                                }
                            }

                            if (different)
                            {
                                Log.Debug("SETTING MAC Address");
                                networkInterface.PhysicalAddress = MACAddress;
                                //networkInterface.RenewDhcpLease();                                
                                PowerState.RebootDevice(true);
                                Thread.Sleep(1500);                                
                            }
                        }*/

                        //networkInterface.EnableStaticIP("0.0.0.0", "0.0.0.0", "0.0.0.0");
                        //networkInterface.EnableDynamicDns();  
                        /*if (networkInterface.IPAddress != "0.0.0.0")
                        {
                            networkInterface.RenewDhcpLease();
                            Thread.Sleep(1000);
                        }*/

                        networkInterface.EnableDhcp();

                        int t=0;
                        while ((networkInterface.IPAddress == "0.0.0.0")  && t++ < 30)
                        {
                            Thread.Sleep(100);
                            /*
                            try
                            {                                

                                int k = 0;
                                while (networkInterface.IPAddress == "0.0.0.0" && k++ < 20)
                                {
                                    k++;
                                    Thread.Sleep(100);
                                } 
                                
                            }
                            catch
                            {
                            }*/                            
                        }
                        /*
                        if (networkInterface.IPAddress == "0.0.0.0")
                        {
                            networkInterface.RenewDhcpLease();
                            Thread.Sleep(200);
                        }*/
                    }

                    if (networkInterface.IPAddress != "0.0.0.0")
                    {
                        IPAddress = networkInterface.IPAddress;
                    }
                    
                }
                catch
                {
                    IPAddress = null;
                }
            }

            Log.Debug("NEW IPAddress " + IPAddress);

            if (OnConnect != null)
            {
                OnConnect();
            } 
        }

        public static void Disconnect()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var networkInterface = interfaces[0];

            if (IPAddress == null)
            {
                return; //-- Already disconnected
            }

            IPAddress = null;
            if (OnDisconnect != null)
            {
                OnDisconnect();
            }

            if (static_IP == null)
            {
                networkInterface.RenewDhcpLease();
                Thread.Sleep(1000);
            }
        }
    }
}
