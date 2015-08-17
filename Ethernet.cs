using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Threading;

namespace Redecode.Archimede
{
    public delegate void OnConnectHandler();
    public delegate void OnDisconnectHandler();

    public class Ethernet
    {
        private static NetworkInterface networkInterface;

        public static string static_IP;
        public static string static_Mask;
        public static string static_Gateway;

        public static string IPAddress;

        public static event OnConnectHandler OnConnect;
        public static event OnDisconnectHandler OnDisconnect;

        //--- this is for Default
        public static void UseDHCP()
        {
            static_IP = "";
            static_Mask = "";
            static_Gateway = "";
        }

        public static void UseStaticIp(string ip, string mask, string gateway)
        {
            static_IP = ip;
            static_Mask = mask;
            static_Gateway = gateway;
        }

        

        public static void Connect()
        {
            if (IPAddress != null)
            {
                return; //-- Already connected
            }

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            networkInterface = interfaces[0];

            while (IPAddress == null)
            {
                try
                {
                    if (static_IP != "")
                    {
                        networkInterface.EnableStaticIP(static_IP, static_Mask, static_Gateway);
                    }
                    else
                    {
                        networkInterface.EnableDynamicDns();
                        networkInterface.EnableDhcp();
                    }

                    Thread.Sleep(500);

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

            if (OnConnect != null)
            {
                OnConnect();
            }

        }

        public static void Disconnect()
        {
            if (IPAddress == null)
            {
                return; //-- Already disconnected
            }

            IPAddress = null;
            if (OnDisconnect != null)
            {
                OnDisconnect();
            }
        }
    }
}
