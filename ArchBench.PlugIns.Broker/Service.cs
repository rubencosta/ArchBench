using System;
using System.Collections.Generic;
using System.Threading;

namespace ArchBench.PlugIns.Broker
{
	public class Service
	{
		public string Name { get; set; }
		public EventHandler Expired;
        public CookieHandler CookieHandler { get; private set; }
		IList<Server> _servers = new List<Server>();
		Timer _heartbeatTimer;
		int _nextServer = -1;
		int _idCounter = -1;

		public Service (string aServiceName)
		{
			Name = aServiceName;
            CookieHandler = new CookieHandler(Name);
		}

		public Service (string aServiceName, string aIpAdress, int aPort)
		{
			//TODO checks
			Name = aServiceName;
            CookieHandler = new CookieHandler(Name);
			_idCounter++;
			_servers.Add (new Server (aIpAdress, aPort, _idCounter));
			_heartbeatTimer = new Timer (HeartBeatTimerFunction, null, 5000, 5000);
		}
			
		public bool Add (string aServiceName, string aIpAdress, int aPort)
		{
			if (aServiceName != Name)
				return false;
			_idCounter++;
			_servers.Add (new Server (aIpAdress, aPort, _idCounter));
			if(_servers.Count == 1) 
				_heartbeatTimer = new Timer (HeartBeatTimerFunction, null, 5000, 5000);
			return true;

		}

		public Server GetServer (){
			if (_servers.Count == 0)
				return null;
			_nextServer = ++_nextServer % _servers.Count;
			return _servers [_nextServer];
		}

		public Server GetServer (int id)
		{
		    var index = _servers.IndexOf (new Server("",0,id));
		    return index > -1 ? _servers [index] : null;
		}

	    void HeartBeatTimerFunction(object state)
		{
			var auxServerList = new Server[_servers.Count];
			_servers.CopyTo(auxServerList,0);
			foreach (var server in auxServerList) 
			{
				if (!server.IsAlive ()) 
				{
					_servers.Remove (server);
				}
			}
			if (_servers.Count == 0) 
			{
				SetExpired ();
			}
		}

		void SetExpired () {
			_heartbeatTimer.Dispose ();
			if (Expired != null)
				Expired (this, null); 
		}
			
		public override bool Equals (object obj)
		{
			if (obj == null || GetType () != obj.GetType ())
				return false;
			return Name == ((Service)obj).Name;
		}

	    public override int GetHashCode()
	    {
	        return base.GetHashCode();
	    }

		public void Dispose () {
			_heartbeatTimer.Dispose ();
		}
	}
}

