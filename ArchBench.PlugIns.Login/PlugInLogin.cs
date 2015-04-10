using System;
using System.IO;

using HttpServer;
using HttpServer.Sessions;

namespace ArchBench.PlugIns.Login
{
    public class PlugInLogin : IArchServerModulePlugIn
    {
        #region IArchServerModulePlugIn Members

        public bool Process( IHttpRequest aRequest, IHttpResponse aResponse, IHttpSession aSession )
        {
            if ( ! aRequest.Uri.AbsolutePath.StartsWith( "/login" ) ) return false;

            foreach ( RequestCookie cookie in aRequest.Cookies )
            {
                Host.Logger.WriteLine( "Cookie: {0} = {1}", cookie.Name, cookie.Value );                
            }

            Host.Logger.WriteLine( String.Format( "Accept request for : {0}", aRequest.Uri ) );
            if ( aRequest.Method == Method.Get )
            {
				if (aSession ["Username"] != null) {
					StreamWriter writer = new StreamWriter (aResponse.Body);
					writer.WriteLine ("Already logged in as {0}!", aSession["Username"]);
					writer.WriteLine("<a href=\"/logout \">Logout<a/>");
					writer.Flush ();
					aResponse.Send ();

				} else {

					StreamWriter writer = new StreamWriter (aResponse.Body);
					writer.Write (Resource.login);
					writer.Flush ();
					aResponse.Send ();
				}
            }
            else if (aRequest.Method == Method.Post)
            {
                foreach ( HttpInputItem item in aRequest.Form )
                {
                    Host.Logger.WriteLine( "==> [{0}] := {1}", item.Name, item.Value );
                }

				if (aRequest.Form.Contains ("Username")) {
					String username = aRequest.Form ["Username"].Value;
					aSession[ "Username" ] = username;
					Host.Logger.WriteLine(String.Format("User {0} logged on.", aSession["Username"]));
					StreamWriter writer = new StreamWriter (aResponse.Body);
					writer.WriteLine("<h1>Welcome {0}<h1/>", username);
					writer.WriteLine("<a href=\"/logout \">Logout<a/>");
					writer.Flush ();
				}
                else
                    Host.Logger.WriteLine("Error: invalid login data.");
            }

            return true;
        }

        #endregion

        #region IArchServerPlugIn Members

        public string Name
        {
            get { return "ArchServer Login Plugin"; }
        }

        public string Description
        {
            get { return "Process /user/login/ requests"; }
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
            get; set;
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
        }

        #endregion
    }
}
