using System;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace ArchBench.Server
{
    public partial class ArchServerForm : Form
    {
        #region Fields
        private HttpServer.HttpServer mServer;
        private readonly IArchServerLogger mLogger;
        private ArchServerPlugInsModule    mPlugInsModule;
        #endregion

        public ArchServerForm()
        {
            InitializeComponent();
            mLogger = new TextBoxLogger( mOutput );
            mPlugInsModule = new ArchServerPlugInsModule( mLogger );
        }

        private void OnExit(object sender, EventArgs e)
        {
            if ( mServer != null ) mServer.Stop();
            mPlugInsModule.PlugInManager.ClosePlugIns();
            Application.Exit();
        }

		private void OnClosing(object sender, FormClosingEventArgs e)
		{
			if ( mServer != null ) mServer.Stop();
			mPlugInsModule.PlugInManager.ClosePlugIns();
		}

        private void OnConnect(object sender, EventArgs e)
        {
            mConnectTool.Checked = ! mConnectTool.Checked;
            if (mConnectTool.Checked)
            {
                mServer = new HttpServer.HttpServer();

                mServer.Add( new ArchServerModule( mLogger ) );
                mServer.Add( mPlugInsModule );

                mServer.Start( IPAddress.Any, int.Parse( mPort.Text ) );
                mLogger.WriteLine( String.Format( "Server online on port {0}", mPort.Text ) );
            }
            else
            {
                mServer.Stop();
                mServer = null;
            }
        }

        private void OnPlugIn( object sender, EventArgs e )
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Multiselect = false;
            dialog.Filter = @"Arch.Bench PlugIn File (*.dll)|*.dll";

            if ( dialog.ShowDialog() == DialogResult.OK )
            {
                mPlugInsModule.PlugInManager.AddPlugIn( dialog.FileName );
                mLogger.WriteLine( String.Format( "Added PlugIn from {0}", 
                    System.IO.Path.GetFileName( dialog.FileName ) ) );
            }
        }

        private void OnRegistServer( object sender, EventArgs evt )
        {
            try 
            {
                TcpClient client = new TcpClient( "127.0.0.1", 9000 );

                Byte[] data = Encoding.ASCII.GetBytes( String.Format( "{0}@{1}:{2}", mServerName.Text, GetLocalIP(), mPort.Text ) );         

                NetworkStream stream = client.GetStream();
                stream.Write( data, 0, data.Length );
                stream.Close();         
                client.Close();         
            } 
            catch ( SocketException e ) 
            {
               mLogger.WriteLine( String.Format( "SocketException: {0}", e ) );
            }

        }

        public static string GetLocalIP()
        {
            // Resolves a host name or IP address to an IPHostEntry instance.
            // IPHostEntry - Provides a container class for Internet host address information. 
            IPHostEntry IPHostEntry = Dns.GetHostEntry(Dns.GetHostName());

            // IPAddress class contains the address of a computer on an IP network. 
            foreach (IPAddress IPAddress in IPHostEntry.AddressList)
            {
                // InterNetwork indicates that an IP version 4 address is expected 
                // when a Socket connects to an endpoint
                if (IPAddress.AddressFamily.ToString() != "InterNetwork") continue;
                return IPAddress.ToString();
            }
            return @"127.0.0.1";
        }
    }
}
