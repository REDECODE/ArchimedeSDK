using System.Net;
using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Threading;
using System;
using System.Text;
using System.IO;
using System.Collections;
using Microsoft.SPOT.Hardware;

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
        public static bool IsRunning;

        public static void Start() {
            Stop();

            Thread webServerThread = new Thread(WebServerThread);
            webServerThread.Start();            
        }

        public static void Stop()
        {
            IsRunning = false;
            if (httpListener != null)
            {
                httpListener.Stop();
                httpListener = null;
            }
        }

        static void WebServerThread()
        {
            IsRunning = true;

            while (IsRunning)
            {
                try
                {
                    //---- CONNECTING 
                    Ethernet.Connect();

                    Log.Debug("Create HttpListener");
                    //----- MANAGE REQUEST
                    httpListener = new HttpListener("http", 80);
                    try
                    {
                        Log.Debug("Start HttpListener");
                        httpListener.Start();
                    }
                    catch (Exception ex)
                    {
                        //-- SOFT RESET when Start() fail because it fail forever even if Connect() is ok
                        PowerState.RebootDevice(true);
                    }

                    Log.Debug("Start HttpListener Listening");
                    while (httpListener.IsListening)
                    {
                        Log.Debug("Getting Context HttpListener");
                        var ctx = httpListener.GetContext();
                        Log.Debug("Got Context HttpListener");
                        //var webServerRequest = new WebServerRequest() { HttpRequest = ctx.Request };
                        //var webServerResponse = new WebServerResponse() { HttpResponse = ctx.Response };
                        try
                        {
                            ctx.Response.OutputStream.WriteTimeout = 2000;

                            if (OnWebRequest != null)
                            {
                                OnWebRequest(ctx.Request, ctx.Response);
                            }

                            foreach (UrlAction webApi in ListUrlAction)
                            {
                                if (webApi.pattern == ctx.Request.RawUrl)
                                {   
                                    if (webApi.required_auth)
                                    {
                                        if (OnAuth(ctx.Request))
                                        {
                                            webApi.handler(ctx.Request, ctx.Response);
                                            break;
                                        }
                                        else
                                        {
                                            ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                            break;
                                        }
                                        
                                    } else {
                                        webApi.handler(ctx.Request, ctx.Response);
                                        break;
                                    }
                                }

                            }
                        }
                        catch (Exception ex) {
                            Log.Error("WebRequest Error: " + ex.Message);                            
                        } 
                        finally
                        {
                            ctx.Response.OutputStream.Close();
                            //ctx.Response.Close();                            
                            //ctx.Close();                            
                            ctx = null;                            
                            Debug.GC(true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("WebServer Disconnect: " + ex.Message);
                    httpListener.Stop();
                    httpListener = null;
                    Debug.GC(true);
                    Ethernet.Disconnect();
                    
                }
            }
        }

        public static void UrlAction(string pattern, OnWebRequestHandler handler, bool required_auth = false)
        {
            if (ListUrlAction == null)
            {
                ListUrlAction = new ArrayList();
            }

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
            /*using (StreamWriter stream = new StreamWriter(response.OutputStream))
            {
                //response.ContentLength64 = bytes.Length;
                stream.Write(bytes);
            }
            */
            //response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
        }

        public static void String(this HttpListenerResponse response, string str)
        {
            /*
            using (StreamWriter stream = new StreamWriter(response.OutputStream))
            {
                //response.ContentLength64 = stream.BaseStream.Length;
                stream.Write(str);
            }
             */
                byte[] bytes = Encoding.UTF8.GetBytes(str);
                //response.ContentLength64 = bytes.Length;
                response.OutputStream.Write(bytes, 0, bytes.Length);
        }

        public static void File(this HttpListenerResponse response, string path, int buffer_size = 1000)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                int bytesRead;
                byte[] bytes = new byte[buffer_size];
                while ((bytesRead = fs.Read(bytes, 0, buffer_size)) > 0)
                {
                    response.OutputStream.Write(bytes, 0, bytesRead);
                }
                fs.Close();
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