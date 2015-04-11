using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using HttpServer;
using HttpServer.Sessions;
using System.Net;
using System.Text;
using System.Collections.Specialized;

namespace ArchBench.PlugIns.Broker
{
	public class Broker : IArchServerModulePlugIn
	{
		readonly TcpListener mListener;
		Thread mRegisterThread;
		IList<Service> mRegisteredServices = new List<Service>();
		DateTime mCookieExpireDate = new DateTime ();

		const String COOKIE_NAME = "gluta_broker";
		const String DEFAULT_SERVICE = "default";

		public Broker()
		{
			mListener = new TcpListener(  IPAddress.Any, 9000 );
			mRegisteredServices.Add (new Service (DEFAULT_SERVICE));
		}

		private void ReceiveThreadFunction()
		{
			try
			{
				// Start listening for client requests.
				mListener.Start();

				// Buffer for reading data
				Byte[] bytes = new Byte[256];

				// Enter the listening loop.
				while (true)
				{
					// Perform a blocking call to accept requests.
					// You could also user server.AcceptSocket() here.
					TcpClient client = mListener.AcceptTcpClient();

					// Get a stream object for reading and writing
					NetworkStream stream = client.GetStream();

					int count = stream.Read(bytes, 0, bytes.Length);
					if (count != 0)
					{
						// Translate data bytes to a ASCII string.
						String data = Encoding.ASCII.GetString( bytes, 0, count );

						String server = data.Substring( 0, data.IndexOf('@') );
						String port   = data.Substring( data.IndexOf(':') + 1 );
						String ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
						Host.Logger.WriteLine( String.Format( "Server {0} available on {1}:{2}", server, ip, port ) );
						server = server != "" ? server : "default";
						Regist( server, ip, int.Parse( port ) );
					}

					client.Close();
				}
			}
			catch ( SocketException e )
			{
				Host.Logger.WriteLine( String.Format( "SocketException: {0}", e ) );
			}
			finally
			{
				mListener.Stop();
			}
		}

		void Regist( string aServiceName, string aIPAdress, int aPort )
		{
			//try to register server on some service
			foreach (Service registeredService in mRegisteredServices){ 
				if (registeredService.Add (aServiceName, aIPAdress, aPort) == true) {
					Host.Logger.WriteLine( String.Format( "Service {0} registered", aServiceName) );
					return;
				}
			}
		    Service newService = new Service (aServiceName, aIPAdress, aPort);
		    mRegisteredServices.Add (newService);
		    newService.Expired += new EventHandler (onServiceExpired);
		    Host.Logger.WriteLine(string.Format("Service {0} registered", aServiceName));
		}  

		void onServiceExpired(object sender, EventArgs e)
		{
			Service expiredService = sender as Service;
			if(expiredService.Name != DEFAULT_SERVICE)
				mRegisteredServices.Remove (expiredService);
		}

		void SetCookieExpire (DateTime date)
		{
			if(date.CompareTo(mCookieExpireDate)>0)
			{
				mCookieExpireDate = date;
			}
		}

		String EncodeSetCookie (string serviceName, int serverId, string originalSetCookie)
		{
			if (originalSetCookie == null) 
				return "";
			String encodedSetCookie = String.Format ("{0}@{1}=__broker__={2}", serviceName, COOKIE_NAME, serverId);
			String[] splitCookie = originalSetCookie.Split (';');
			// Set-Cookie contains Expires attr
			foreach(string cookieOpt in splitCookie){
				if (cookieOpt != "" && cookieOpt.Substring (0, cookieOpt.IndexOf ('=')) == "Expires") {
					SetCookieExpire (DateTime.Parse (cookieOpt.Substring(cookieOpt.IndexOf ('=') + 1)));
					encodedSetCookie += String.Format (";Expires={0}", mCookieExpireDate.ToString ("R"));
				}
			}
			encodedSetCookie += String.Format ("&{0}",originalSetCookie);
			return encodedSetCookie;
		}

		IList<string> DecodeCookie (string encoded)
		{
			IList<string> cookies = new List<string> ();
			foreach (string cookie in encoded.Split('&')) 
			{
				if(cookie.Length>0)
					cookies.Add (cookie);
			}
			return cookies;
		}

		String ProcessPath (IHttpRequest aRequest)
		{
		    string[] uriParts;
		    if (aRequest.Headers["Referer"] != null)
		    {
		        string uriPath = aRequest.Headers["Referer"].Remove(0, aRequest.Headers["Origin"].Length +1);
		        uriParts = uriPath.Split('/');
		    }
		    else
		    {
		        uriParts = aRequest.UriParts;
		    }
		    if (uriParts.Length == 0)
				return "/";

			string path = "";
            for (int i = 1; i < uriParts.Length; i++)
            {
                path += "/" + uriParts[i];
			}
			return path;
		}

		NameValueCollection GetNameValueCollection(HttpForm form){
			NameValueCollection col = new NameValueCollection (); 
			foreach (HttpInputItem item in form) {
				col.Add (item.Name, item.Value);
			}
			return col;

		}

		void HandleDownloadDataCompleted (object sender, DownloadDataCompletedEventArgs e)
		{
			byte[] result = e.Result;
			Host.Logger.WriteLine ("{0} bytes received!", result.Length);

		}

		void FinishWebRequest(IAsyncResult result) {
			Host.Logger.WriteLine (result.ToString ());
		}

		#region IArchServerModulePlugIn Members

		public bool Process( IHttpRequest aRequest, IHttpResponse aResponse, IHttpSession aSession )
		{
			WebClient webClient;
			byte[] result;
			String serviceName;
			Service service;
			Server server;
			String cookies;
			String cookieServerId;
			int index; 

			webClient = new WebClient ();
			result = null;
		    serviceName = getServiceName(aRequest);
			cookieServerId = "";
			cookies = "";
			index = mRegisteredServices.IndexOf (new Service (serviceName)); 
			service = index > -1 ? mRegisteredServices [index] : mRegisteredServices [0];
		    //check for cookie
		    foreach (RequestCookie requestCookie in aRequest.Cookies)
		    {
		        if (requestCookie.Name.Contains(COOKIE_NAME) && requestCookie.Name.IndexOf('@') > -1 &&
		            requestCookie.Name.Substring(0, requestCookie.Name.IndexOf('@')) == service.Name)
		        {
		            foreach (string subCookie in DecodeCookie(requestCookie.Value))
		            {
		                try
		                {
		                    if (subCookie.Contains("__broker__"))
		                    {
		                        cookieServerId = subCookie.Substring(subCookie.IndexOf('=') + 1);
		                    }
		                }
		                catch (Exception e)
		                {
		                    Host.Logger.WriteLine(e.ToString());
		                    if (subCookie != "" && cookies != "" && !subCookie.Contains("__broker__"))
		                        cookies += ';' + subCookie;
		                    else if (subCookie != "" && !subCookie.Contains("__broker__"))
		                        cookies = subCookie;
		                }
		                if (subCookie != "" && cookies != "" && !subCookie.Contains("__broker__"))
		                    cookies += ';' + subCookie;
		                else if (subCookie != "" && !subCookie.Contains("__broker__"))
		                    cookies = subCookie;
		            }
		        }
		    }
		    if (cookies != "")
		        webClient.Headers.Add("Cookie", cookies);
		    server = cookieServerId != "" ? service.getServer(int.Parse(cookieServerId)) : service.getServer();
		    if (server == null)
		    {
		        Host.Logger.WriteLine(String.Format("service provider not found!"));
		        return false;
		    }
		    Host.Logger.WriteLine(String.Format("service provider found on: {0}", server.getUrl()));
		    string url = String.Format("http://{0}{1}", server.getUrl(), ProcessPath(aRequest));
		    try
		    {
		        result = aRequest.Method == Method.Post
		            ? webClient.UploadValues(url, GetNameValueCollection(aRequest.Form))
		            : webClient.DownloadData(url);
		    }
		    catch (WebException e)
		    {
		        Host.Logger.WriteLine("This program is expected to throw WebException on successful run." +
		                              "\n\nException Message :" + e.Message);
		        if (e.Status == WebExceptionStatus.ProtocolError)
		        {
		            Console.WriteLine("Status Code : {0}", ((HttpWebResponse) e.Response).StatusCode);
		            Console.WriteLine("Status Description : {0}", ((HttpWebResponse) e.Response).StatusDescription);
		        }
		    }
		    if (result != null && result.Length > 0)
		    {
		        // Send data to corresponding service
		        String data = Encoding.ASCII.GetString(result, 0, result.Length);
		        Host.Logger.WriteLine(data);

		        // Forward cookie
		        foreach (string key in webClient.ResponseHeaders.AllKeys)
		        {
		            aResponse.AddHeader(key, webClient.ResponseHeaders[key]);
		        }
		        aResponse.AddHeader("Set-Cookie",
		            EncodeSetCookie(service.Name, server.Id, webClient.ResponseHeaders["Set-Cookie"]));

		        aResponse.Body.Write(result, 0, result.Length);
		        aResponse.Send();
		    } 

			//			webClient.DownloadDataCompleted += HandleDownloadDataCompleted;
			//			webClient.DownloadDataAsync (aRequest.Uri);

			return true;
		}

	    private string getServiceName(IHttpRequest aRequest)
	    {
	        string serviceName = "";
	        if (aRequest.Headers["Referer"] != null)
	        {
	            string uriPath = aRequest.Headers["Referer"].Remove(0, aRequest.Headers["Origin"].Length + 1);
	            return uriPath.Split('/')[0];
	        }

            serviceName = aRequest.UriParts.Length > 0 ? aRequest.UriParts[0] : DEFAULT_SERVICE;
	        return serviceName;
	    }

	    #endregion

		#region IArchServerPlugIn Members

		public string Name
		{
			get { return "ArchServer Broker Plugin"; }
		}

		public string Description
		{
			get { return "Dispatch clients to the proper server"; }
		}

		public string Author
		{
			get { return "Ruben Costa"; }
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
			mRegisterThread = new Thread( ReceiveThreadFunction );
			mRegisterThread.IsBackground = true;
			mRegisterThread.Start();
		}

		public void Dispose()
		{
			mRegisterThread.Abort ();
			mListener.Stop();
		}

		#endregion
	}
}

