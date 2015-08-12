using System.Net;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Threading;
using System;
using System.Text;
using System.IO;

namespace Redecode.Archimede
{
    
    public delegate void OnConnectHandler();
    public delegate void OnDisconnectHandler();
    public delegate void OnWebRequestHandler(HttpListenerRequest request, HttpListenerResponse response, HttpListenerOutput output);
    
    public static class WebServer
    {        
        private static readonly HttpListener httpListener = new HttpListener("http", 80);
        private static NetworkInterface networkInterface;
        
        public static event OnConnectHandler OnConnect;
        public static event OnDisconnectHandler OnDisconnect;
        public static event OnWebRequestHandler OnWebRequest;        

        public class HttpRequestEventArgs : EventArgs 
    {
        public HttpListenerContext Context { get; private set; }
        public byte[] ResponseBytes { get; set; }

        public HttpRequestEventArgs(HttpListenerContext ctx)
        {
            Context = ctx;
        }
    }

        public static string IPAddress;

        public static string static_IP;
        public static string static_Mask;
        public static string static_Gateway;

        public static void SetStaticIp(string ip, string mask, string gateway)
        {
            static_IP = ip;
            static_Mask = mask;
            static_Gateway = gateway;
        }

        public static void Start() {

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

                    Thread.Sleep(1000);

                    if (networkInterface.IPAddress != "0.0.0.0")
                    {
                        IPAddress = networkInterface.IPAddress;
                        httpListener.Start();
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

            Thread webServerThread = new Thread(WebServerThread);
            webServerThread.Start();
        }

        static void WebServerThread()
        {
            try
            {
                while (httpListener.IsListening)
                {
                    var ctx = httpListener.GetContext();
                    try
                    {
                        if (OnWebRequest != null)
                        {                            
                            OnWebRequest(ctx.Request, ctx.Response, new HttpListenerOutput(ctx));
                        }
                    }
                    catch { } // suppress any exceptions
                    finally
                    {
                        // always close the stream
                        ctx.Response.OutputStream.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                httpListener.Stop();
                
                IPAddress = null;
                if (OnDisconnect != null)
                {
                    OnDisconnect();
                }

                if (static_IP == "")
                {
                    networkInterface.RenewDhcpLease();
                }

                Start();
            } 
        }
        /*
        public void MainLoop()
        {
            Debug.Print("Webserver running...");

            while (_listener.IsListening)
            {
                {
                    HttpListenerContext ctx = _listener.GetContext();
                    try
                    {
                        if(ManageRequest != null)
                        {
                            HttpRequestEventArgs e = new HttpRequestEventArgs(ctx);

                            ManageRequest(this, e);
                            if(e.ResponseBytes != null && e.ResponseBytes.Length > 0)
                            {
                                ctx.Response.ContentLength64 = e.ResponseBytes.Length;
                                ctx.Response.OutputStream.Write(e.ResponseBytes, 0, e.ResponseBytes.Length);
                            }
                        }
                    }
                    catch { } // sopprimo ogni eventuale eccezione
                    finally
                    {
                        // Chiudo lo stream di output
                        ctx.Response.OutputStream.Close();
                    }
                }
            }
        }

        // suppress any exceptions
        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
         */
    }
    
    public class HttpListenerOutput
    {

        public HttpListenerContext Context { get; private set; }
        
        public HttpListenerOutput(HttpListenerContext ctx)
        {
            Context = ctx;
        }

        public void Bytes(byte[] bytes)
        {
            Context.Response.ContentLength64 = bytes.Length;
            Context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        }

        public void String(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            Context.Response.ContentLength64 = bytes.Length;
            Context.Response.OutputStream.Write(bytes, 0, bytes.Length);
        }
    }
     
}