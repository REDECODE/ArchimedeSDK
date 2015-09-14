using System;
using Microsoft.SPOT;
using System.Reflection;
using System.IO;
using System.Threading;

namespace Redecode.Archimede
{
    public class Loader
    {
        public static Assembly LoadAssembly(string file)
        {
            Assembly assembly = null;
            try
            {
                using (FileStream assmfile = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    byte[] assmbytes = new byte[assmfile.Length];
                    assmfile.Read(assmbytes, 0, (int)assmfile.Length);
                    //bool loaded = false;
                    Thread tConnect = new Thread(() =>
                    {
                        try
                        {
                            assembly = Assembly.Load(assmbytes);
                            //loaded = true;
                        }
                        catch
                        {
                        }
                    });
                    tConnect.Start();

                    int millsThread = 0;
                    while (tConnect.IsAlive && millsThread < 10)
                    {
                        Thread.Sleep(10);
                        millsThread += 10;
                    }

                    if (tConnect.IsAlive)
                    {
                        tConnect.Abort();
                    }
                    tConnect = null;
                    assmfile.Close();
                }
            }
            catch (Exception ex)
            {

            }
            return assembly;
        }

        public static void CallMain(string className)
        {
            Type magicType = Type.GetType(className);
            MethodInfo magicMethod = magicType.GetMethod("Main");
            magicMethod.Invoke(null, null);
        }
    }
}
