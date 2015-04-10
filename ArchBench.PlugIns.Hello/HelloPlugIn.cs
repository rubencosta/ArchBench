using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HttpServer;
using System.IO;


namespace ArchBench.PlugIns.Hello
{
    public class HelloPlugIn : IArchServerModulePlugIn
    {
        public bool Process( IHttpRequest aRequest, IHttpResponse aResponse, HttpServer.Sessions.IHttpSession aSession)
        {
            if (aRequest.Uri.AbsolutePath.StartsWith("/hello"))
            {
                Host.Logger.WriteLine("Accept request for : {0}", aRequest.Uri.ToString());

                StreamWriter writer = new StreamWriter(aResponse.Body);
                writer.WriteLine("Hello dude..dude!");
                writer.Flush();

                aResponse.Send();

                return true;
            }
            return false;
        }

        public string Name
        {
            get { return "Hello PlugIn"; }
        }

        public string Description
        {
            get { return "Say Hello"; }
        }

        public string Author
        {
            get { return "Leonel Nobrega"; }
        }

        public string Version
        {
            get { return "0.1"; }
        }

        public IArchServerPlugInHost Host
        {
            get;
            set;
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
        }
    }
}
