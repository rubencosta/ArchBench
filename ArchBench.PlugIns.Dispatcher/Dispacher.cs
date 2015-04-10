using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using HttpServer;
using HttpServer.Sessions;
using System.Net.Sockets;

namespace ArchBench.PlugIns.Dispatcher
{
    public class PlugIn : IArchServerModulePlugIn
    {
        private TcpListener mListener;
        private Thread      mRegisterThread;
		private readonly IList<Service> mRegisteredServices = new List<Service>();

        public PlugIn()
        {
            mListener = new TcpListener(  IPAddress.Any, 9000 );
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

                        String server = data.Substring( 0, data.IndexOf('-') );
                        String port   = data.Substring( data.IndexOf('-') + 1 );
						String ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
						Host.Logger.WriteLine( String.Format( "Server {0} available on {1}:{2}", server, ip, port ) );
       
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

		private void Regist( string aServiceName, string aIPAdress, int aPort )
		{
			bool registered = false;
			//try to register server on some service
			foreach (Service registeredService in mRegisteredServices){ 
				if (registeredService.Add (aServiceName, aIPAdress, aPort)) {
					Host.Logger.WriteLine( String.Format( "Service {0} registered", aServiceName) );
					registered = true;
				}
			}
			if (!registered) {
				Service newService = new Service (aServiceName, aIPAdress, aPort);
				mRegisteredServices.Add (newService);
				newService.Expired += new EventHandler (onServiceExpired);
				Host.Logger.WriteLine( String.Format( "Service {0} registered", aServiceName) );
			}
		}  

		void onServiceExpired(object sender, EventArgs e)
		{
			Service expiredService = sender as Service;
			mRegisteredServices.Remove (expiredService);
		}

		String ProcessPath (string [] UriParts)
		{
			if (UriParts.Length == 0)
				return "/";

			string path = "";
			for (int i = 1; i < UriParts.Length; i++) {
				path += "/" + UriParts [i];
			}
			return path;
		}

        #region IArchServerModulePlugIn Members

        public bool Process( IHttpRequest aRequest, IHttpResponse aResponse, IHttpSession aSession )
        {
			Server server;
			string serverName = aRequest.UriParts[0];
			Host.Logger.WriteLine (String.Format("request for: {0} ",serverName));
			int index = mRegisteredServices.IndexOf (new Service (serverName, "", 0)); 
			if (index > -1) {
				server = mRegisteredServices [index].getServer ();
				var redirection = new StringBuilder();
				redirection.AppendFormat( "http://{0}:{1}", server.IPAdress, server.Port );
				redirection.Append( ProcessPath (aRequest.UriParts) );

				aResponse.Redirect( redirection.ToString() );
			} else {
				//no service found
				//TODO send 404 page
				Host.Logger.WriteLine ("no service provider found");
			}


            return true;
        }

        #endregion

        #region IArchServerPlugIn Members

        public string Name
        {
            get { return "ArchServer Dispatcher Plugin"; }
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
			if (mRegisterThread != null)
				mRegisterThread.Abort ();
        }

        #endregion
    }
}
