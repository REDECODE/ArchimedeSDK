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

        private static HttpListener httpListener;

        public static event OnWebRequestHandler OnWebRequest;
        public static event OnAuth OnAuth;

        private static ArrayList ListUrlAction;

        public static void Start() {
            httpListener = new HttpListener("http", 80);
            ListUrlAction = new ArrayList();
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
                //response.ContentLength64 = bytes.Length;
                stream.Write(bytes);
            }
        }

        public static void String(this HttpListenerResponse response, string str)
        {
            using (StreamWriter stream = new StreamWriter(response.OutputStream))
            {
                //response.ContentLength64 = stream.BaseStream.Length;
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