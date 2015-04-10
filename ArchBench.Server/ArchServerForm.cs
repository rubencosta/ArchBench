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

                Byte[] data = Encoding.ASCII.GetBytes( String.Format( "{0}-{1}", mServerName.Text, mPort.Text ) );         

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
    }
}
