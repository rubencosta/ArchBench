using System;
using System.Text;
using HttpServer;
using HttpServer.Sessions;
using System.Collections.Generic;


namespace ArchBench.PlugIns.CookieBaker
{
	public class CookieBaker : IArchServerModulePlugIn
	{
		private IList<String> Cookies = new List<String>();
		private int mNextCookie = -1;
		public CookieBaker ()
		{
			Cookies.Add (String.Format ("{0}={1}", "flavor", "chocolat"));
			Cookies.Add (String.Format ("{0}={1};Expires={2}", "chocolat_chips", "true", DateTime.Now.AddMinutes(5).ToString("R")));
			Cookies.Add (String.Format ("{0}={1}; path={2}", "flour", "wheat", "/cookie"));
		}

		public bool Process (IHttpRequest aRequest, IHttpResponse aResponse, IHttpSession aSession)
		{
			mNextCookie = ++mNextCookie % Cookies.Count;
			string cookie = Cookies [mNextCookie];
			Host.Logger.WriteLine (String.Format ("Set-Cookie={0}",cookie));
			aResponse.AddHeader ("Set-Cookie", cookie); 
			
			byte[] body = Encoding.ASCII.GetBytes("<h1>Cookies set!<h1/>"); 
			aResponse.Body.Write (body, 0, body.Length);
			aResponse.Send ();
			return true;
		}

		#region IArchServerPlugIn implementation

		public void Initialize ()
		{
		}

		public void Dispose ()
		{
		}

		public string Name {
			get {
				return "ArchBench Cookie Baker Plugin";
			}
		}

		public string Description {
			get {
				return "Creates diferent cookies for testing purposes";
			}
		}

		public string Author {
			get {
				return "Ruben Costa";
			}
		}

		public string Version {
			get {
				return "1.0.0";
			}
		}

		public IArchServerPlugInHost Host {get; set;}

		#endregion
	}
}

