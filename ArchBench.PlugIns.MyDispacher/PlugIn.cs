using System;
using HttpServer;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Net;

namespace ArchBench.PlugIns.MyDispacher
{
	public class PlugIn : IArchServerModulePlugIn
	{
		private TcpListener mListener;
		private Thread mRegisterThread;

		public PlugIn ()
		{
			mListener = new TcpListener (IPAddress.Any, 9000);
		}

		private void ReceiveThreadFunction () {
			try{
				// Start listening for clients requests.
				mListener.Start ();

				// Buffer for reading data
				Byte[] bytes = new Byte[256];

				// Enter the listening loop
				while (true){
					// Perform a blocking call to accept requests.
					// You could also user server.AcceptSocket() here.
					TcpClient client = mListener.AcceptTcpClient();

					// Get a stream object for reading and writing
					NetworkStream stream = client.GetStream();

					int count = stream.Read( bytes, 0, bytes.Length );
					if(count !=0)
					{
						//Translate bytes to ASCII
						String data = Encoding.ASCII.GetString(bytes, 0, count);

						Host.Logger.WriteLine(data);

					}
					client.Close();
				}
			
			}catch(SocketException e) {
				Host.Logger.WriteLine( String.Format( "SocketException: {0}", e ) );
			}
			finally {
				mListener.Stop ();
			}
		



		}
		#region IArchServerModulePlugIn implementation

		public bool Process (IHttpRequest aRequest, IHttpResponse aResponse, HttpServer.Sessions.IHttpSession aSession)
		{
			return true;
		}


		public string Name {
			get {
				return "My Dispatcher!";
			}
		}

		public string Description {
			get {
				return "Dispacher that hides servers ports, i.e. always shows dispacher port";
			}
		}

		public string Author {
			get {
				return "Ruben Costa";
			}
		}

		public string Version {
			get {
				return "0.0.1";
			}
		}

		public IArchServerPlugInHost Host 
		{
			get; set;
		}

		public void Initialize()
		{
			mRegisterThread = new Thread (ReceiveThreadFunction);
			mRegisterThread.IsBackground = true;
			mRegisterThread.Start ();
		}

		public void Dispose ()
		{
		}


		#endregion


	}
}

