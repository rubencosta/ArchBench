using System.IO;
using HttpServer;

namespace ArchBench.PlugIns.Logout
{
    public class Logout : IArchServerModulePlugIn
    {
        public bool Process( HttpServer.IHttpRequest aRequest, HttpServer.IHttpResponse aResponse, HttpServer.Sessions.IHttpSession aSession )
        {
			if (!aRequest.Uri.AbsolutePath.StartsWith ("/logout")) {
				return false;
			}
            foreach ( RequestCookie cookie in aRequest.Cookies )
            {
                Host.Logger.WriteLine( "Cookie({0}) = {1}", cookie.Name, cookie.Value );
            }
          
            Host.Logger.WriteLine( "Logout PlugIn: {0}", aRequest.Uri.AbsolutePath );
            StreamWriter writer = new StreamWriter( aResponse.Body );
            writer.WriteLine( "Goodbye {0}!", aSession["Username"] );
			writer.Flush();
			aResponse.Send();

			//clear the session
			aSession.Clear ();


            return true;
           
        }

        public string Name
        {
            get { return "Logout"; }
        }

        public string Description
        {
            get { return "Say goodbye..."; }
        }

        public string Author
        {
            get { return "Leonel Nobrega"; }
        }

        public string Version
        {
            get { return "1.0"; }
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
