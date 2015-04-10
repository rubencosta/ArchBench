using HttpServer;
using HttpServer.HttpModules;
using HttpServer.Sessions;

namespace ArchBench.Server
{
    public class ArchServerPlugInsModule : HttpModule, IArchServerPlugInHost
    {
        public ArchServerPlugInsModule( IArchServerLogger aLogger )
        {
            this.Logger = aLogger;
            PlugInManager = new PlugInManager( this );
        }

        public PlugInManager PlugInManager { get; private set; }

        public override bool Process( IHttpRequest aRequest, IHttpResponse aResponse, IHttpSession aSession )
        {
            foreach ( var archServerPlugIn in PlugInManager.PlugIns )
            {
                var plugin = (IArchServerModulePlugIn) archServerPlugIn;
                if ( plugin == null ) continue;
                if ( plugin.Process( aRequest, aResponse, aSession ) ) return true;
            }
            return false;
        }

        #region IArchServerPlugInHost Members

        public IArchServerLogger Logger
        {
            get; set;
        }

        #endregion
    }
}
