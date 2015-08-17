using System.Net;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Threading;
using System;
using System.Text;
using System.IO;
using System.Collections;

namespace Redecode.Archimede
{
    
    public delegate void OnWebRequestHandler(HttpListenerRequest request, HttpListenerResponse response);
    public delegate bool OnAuth(HttpListenerRequest request);

    public static class WebServer
    {        

        private static readonly HttpListener httpListener = new HttpListener("http", 80);

        public static event OnWebRequestHandler OnWebRequest;
        public static event OnAuth OnAuth;

        private static ArrayList ListUrlAction = new ArrayList();
     

        public class HttpRequestEventArgs : EventArgs 
        {
            public HttpListenerContext Context { get; private set; }
            public byte[] ResponseBytes { get; set; }

            public HttpRequestEventArgs(HttpListenerContext ctx)
            {
                Context = ctx;
            }
        }

        public static void Start() {
            Thread webServerThread = new Thread(WebServerThread);
            webServerThread.Start();
        }

        static void WebServerThread()
        {
            //---- CONNECTING 
            Ethernet.Connect();

            httpListener.Start();    

            //----- MANAGE REQUEST
            try
            {
                while (httpListener.IsListening)
                {
                    var ctx = httpListener.GetContext();
                    //var webServerRequest = new WebServerRequest() { HttpRequest = ctx.Request };
                    //var webServerResponse = new WebServerResponse() { HttpResponse = ctx.Response };
                    try
                    {
                        if (OnWebRequest != null)
                        {
                            OnWebRequest(ctx.Request, ctx.Response);
                        }

                        foreach (UrlAction webApi in ListUrlAction)
                        {
                            if (webApi.pattern == ctx.Request.RawUrl) {
                                if (!webApi.required_auth || OnAuth(ctx.Request))
                                {
                                    webApi.handler(ctx.Request, ctx.Response);
                                    break;
                                }                                
                            }

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
                Ethernet.Disconnect();
            }
        }

        public static void UrlAction(string pattern, OnWebRequestHandler handler, bool required_auth = false)
        {

            ListUrlAction.Add(new UrlAction() {
                pattern = pattern,
                handler = handler,
                required_auth = required_auth
            });
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

    public static class HttpListenerRequestExtensions 
    {
        public static string Post(this HttpListenerRequest request,  string str)
        {
            using (StreamReader stream = new StreamReader(request.InputStream))
            {
                return stream.ReadToEnd();
            }
        }
    }

    public static class HttpListenerResponseExtensions
    {
        public static void Bytes(this HttpListenerResponse response, byte[] bytes)
        {
            using (StreamWriter stream = new StreamWriter(response.OutputStream))
            {
                response.ContentLength64 = stream.BaseStream.Length;
                stream.Write(bytes);
            }
        }

        public static void String(this HttpListenerResponse response, string str)
        {
            using (StreamWriter stream = new StreamWriter(response.OutputStream))
            {
                response.ContentLength64 = stream.BaseStream.Length;
                stream.Write(str);
            }
        }
    }

    struct UrlAction
    {
        public string pattern;
        public OnWebRequestHandler handler;
        public bool required_auth;
    }
     
}