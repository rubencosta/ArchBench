using System;
using System.Text;
using HttpServer;
using HttpServer.Sessions;
using System.Collections.Generic;
using System.Resources;
using System.IO;


namespace ArchBench.PlugIns.ImageGallery
{
	public class ImageGallery : IArchServerModulePlugIn
	{
		private IList<String> Cookies = new List<String>();
		private int mNextCookie = -1;
		ResourceManager imagesResourceManager;

		public ImageGallery ()
		{ 
			imagesResourceManager = ResourceManager.CreateFileBasedResourceManager ("images", "/home/ruben/Documents/AS/ArchBench/ArchBench.PlugIns.ImageGallery/images", null);
			imagesResourceManager.IgnoreCase = true;
		}

		public bool Process (IHttpRequest aRequest, IHttpResponse aResponse, IHttpSession aSession)
		{
			Byte[] image = (byte[])ResourceManager.GetObject (ImageGallery.homenuance); 
			aResponse.Body = new MemoryStream (image);
			aResponse.AddHeader("Content-type", "image/jpg");
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
				return "ArchBench Image Gallery Plugin";
			}
		}

		public string Description {
			get {
				return "Sends a page with images";
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

