using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using HttpServer;
using HttpServer.Sessions;

namespace ArchBench.PlugIns.Broker
{
    public class Broker : IArchServerModulePlugIn
    {
        private const string DefaultService = "default";
        private readonly TcpListener _mListener;
        private readonly IList<Service> _registeredServices = new List<Service>();
        private Thread _mRegisterThread;

        public Broker()
        {
            _mListener = new TcpListener(IPAddress.Any, 9000);
            _registeredServices.Add(new Service(DefaultService));
        }

        private void ReceiveThreadFunction()
        {
            try
            {
                // Start listening for client requests.
                _mListener.Start();

                // Buffer for reading data
                var bytes = new byte[256];

                // Enter the listening loop.
                while (true)
                {
                    // Perform a blocking call to accept requests.
                    // You could also user server.AcceptSocket() here.
                    var client = _mListener.AcceptTcpClient();

                    // Get a stream object for reading and writing
                    var stream = client.GetStream();

                    var count = stream.Read(bytes, 0, bytes.Length);
                    if (count != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        var data = Encoding.ASCII.GetString(bytes, 0, count);

                        var server = data.Substring(0, data.IndexOf('@'));
                        var port = data.Substring(data.IndexOf(':') + 1);
                        var ip = ((IPEndPoint) client.Client.RemoteEndPoint).Address.ToString();
                        Host.Logger.WriteLine(string.Format("Server {0} available on {1}:{2}", server, ip, port));
                        server = server != "" ? server : "default";
                        Regist(server, ip, int.Parse(port));
                    }

                    client.Close();
                }
            }
            catch (SocketException e)
            {
                Host.Logger.WriteLine(string.Format("SocketException: {0}", e));
            }
            finally
            {
                _mListener.Stop();
            }
        }

        private void Regist(string aServiceName, string aIpAdress, int aPort)
        {
            //try to register server on some service
            foreach (var registeredService in _registeredServices)
            {
                if (registeredService.Add(aServiceName, aIpAdress, aPort))
                {
                    Host.Logger.WriteLine(string.Format("Service {0} registered", aServiceName));
                    return;
                }
            }
            var newService = new Service(aServiceName, aIpAdress, aPort);
            _registeredServices.Add(newService);
            newService.Expired += OnServiceExpired;
            Host.Logger.WriteLine(string.Format("Service {0} registered", aServiceName));
        }

        private void OnServiceExpired(object sender, EventArgs e)
        {
            var expiredService = sender as Service;
            if (expiredService.Name != DefaultService)
                _registeredServices.Remove(expiredService);
        }

        private NameValueCollection GetNameValueCollection(IEnumerable form)
        {
            var col = new NameValueCollection();
            foreach (HttpInputItem item in form)
            {
                col.Add(item.Name, item.Value);
            }
            return col;
        }

        private string GetServiceName(IHttpRequest aRequest)
        {
            var serviceName = "";
            if (aRequest.Headers["Referer"] != null)
            {
                //if aRequest.Uri does not contain the start of the referer
                var uriParts = aRequest.Uri.ToString().Split('/');
                var refererParts = aRequest.Headers["Referer"].Split('/');
                if (uriParts[3] != refererParts[3])
                {
                    return refererParts[3];
                }
            }
            serviceName = aRequest.UriParts.Length > 0 ? aRequest.UriParts[0] : DefaultService;
            return serviceName;
        }

        private string ProcessPath(IHttpRequest aRequest)
        {
            string[] uriParts;
            if (aRequest.Headers["Referer"] != null)
            {
                uriParts = aRequest.Uri.ToString().Split('/');
                var refererParts = aRequest.Headers["Referer"].Split('/');
                if (uriParts[3] != refererParts[3])
                {
                    return aRequest.UriPath;
                }
            }

            uriParts = aRequest.UriParts;
            if (uriParts.Length == 0)
                return "/";

            var path = "";
            for (var i = 1; i < uriParts.Length; i++)
            {
                path += "/" + uriParts[i];
            }
            return path;
        }

        private void HandleDownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            var result = e.Result;
            Host.Logger.WriteLine("{0} bytes received!", result.Length);
        }

        private void FinishWebRequest(IAsyncResult result)
        {
            Host.Logger.WriteLine(result.ToString());
        }

        #region IArchServerModulePlugIn Members

        public bool Process(IHttpRequest aRequest, IHttpResponse aResponse, IHttpSession aSession)
        {
            var webClient = new WebClient();
            byte[] result = null;
            var serviceName = GetServiceName(aRequest);
            var index = _registeredServices.IndexOf(new Service(serviceName));
            var service = index > -1 ? _registeredServices[index] : _registeredServices[0];

            //check for cookie
            var parsedCookie = service.CookieHandler.ParseCookie(aRequest.Cookies);
            if (parsedCookie.OriginalCookie != null)
                webClient.Headers.Add("Cookie", parsedCookie.OriginalCookie);
            var server = parsedCookie.ServerId != null ? service.GetServer(int.Parse(parsedCookie.ServerId)) : service.GetServer();
            if (server == null)
            {
                Host.Logger.WriteLine("service provider not found!");
                return false;
            }
            Host.Logger.WriteLine(string.Format("service provider found on: {0}", server.GetUrl()));
            var url = string.Format("http://{0}{1}", server.GetUrl(),
                service.Name == DefaultService ? aRequest.UriPath : ProcessPath(aRequest));
            try
            {
                result = aRequest.Method == Method.Post
                    ? webClient.UploadValues(url, GetNameValueCollection(aRequest.Form))
                    : webClient.DownloadData(url);
            }
            catch (WebException e)
            {
                Host.Logger.WriteLine("Exception Message :" + e.Message);
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    Console.WriteLine("Status Code : {0}", ((HttpWebResponse) e.Response).StatusCode);
                    Console.WriteLine("Status Description : {0}", ((HttpWebResponse) e.Response).StatusDescription);
                }
            }
            if (result != null && result.Length > 0)
            {
                // Send data to corresponding service
                var data = Encoding.UTF8.GetString(result, 0, result.Length);
                var parse = false;
                foreach (var key in webClient.ResponseHeaders.AllKeys)
                {
                    aResponse.AddHeader(key, webClient.ResponseHeaders[key]);
                    if (key == "Content-Type")
                    {
                        if (webClient.ResponseHeaders[key].Contains("html") ||
                            webClient.ResponseHeaders[key].Contains("text"))
                        {
                            //|| webClient.ResponseHeaders [key].Contains ("javascript")
                            parse = true;
                        }
                    }
                }

                aResponse.AddHeader("Set-Cookie",
                    service.CookieHandler.EncodeSetCookie(server.Id, webClient.ResponseHeaders["Set-Cookie"]));

                if (parse && service.Name != DefaultService)
                {
                    data = data.Replace("href=\"/", "href=\"/" + service.Name + "/");
                    data = data.Replace("action=\"/", "action=\"/" + service.Name + "/");
                    data = data.Replace("src=\"/", "src=\"/" + service.Name + "/");
                    data = data.Replace("url(\"/", "url(\"/" + service.Name + "/");
                    result = Encoding.UTF8.GetBytes(data);
                    aResponse.AddHeader("Content-Length", result.Length.ToString());
                }

                aResponse.Body.Write(result, 0, result.Length);
                aResponse.Body.Flush();
                aResponse.Send();
            }
            else
            {
                aResponse.Status = HttpStatusCode.NotFound;
                aResponse.Send();
            }

            //			webClient.DownloadDataCompleted += HandleDownloadDataCompleted;
            //			webClient.DownloadDataAsync (aRequest.Uri);

            return true;
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

        public IArchServerPlugInHost Host { get; set; }

        public void Initialize()
        {
            _mRegisterThread = new Thread(ReceiveThreadFunction) {IsBackground = true};
            _mRegisterThread.Start();
        }

        public void Dispose()
        {
            _mRegisterThread.Abort();
            _mListener.Stop();
        }

        #endregion
    }
}