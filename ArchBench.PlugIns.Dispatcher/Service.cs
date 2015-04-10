using System;
using System.Collections.Generic;
using System.Threading;

namespace ArchBench.PlugIns.Dispatcher
{
	public class Service
	{
		public String Name { get; set; }
		public EventHandler Expired;
		IList<Server> mServers = new List<Server>();
		Timer mHeartbeatTimer;
		int mNextServer = -1;
		int mId = -1;

		public Service (string aServiceName, string aIPAdress, int aPort)
		{
			//TODO checks
			Name = aServiceName;
			mServers.Add (new Server (aIPAdress, aPort, mId++));
			mHeartbeatTimer = new Timer (HeartBeatTimerFunction, null, 5000, 5000);
		}

		public bool Add (string aServiceName, string aIPAdress, int aPort)
		{
			if (aServiceName != Name)
				return false;

			mServers.Add (new Server (aIPAdress, aPort, mId++));
			return true;

		}

		public Server getServer (){
			mNextServer = ++mNextServer % mServers.Count;
			return mServers [mNextServer];
		}

		void HeartBeatTimerFunction(object state)
		{
			Server[] auxServerList = new Server[mServers.Count];
			mServers.CopyTo(auxServerList,0);
			foreach (Server server in auxServerList) 
			{
				if (!server.isAlive ()) 
				{
					mServers.Remove (server);
				}
			}
			if (mServers.Count == 0) 
			{
				setExpired ();
			}
		}

		void setExpired () {
			if (Expired != null)
				Expired (this, null); 
		}
			
		public override bool Equals (object obj)
		{
			if (obj == null || GetType () != obj.GetType ())
				return false;
			return Name == ((Service)obj).Name;
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}

		public void Dispose () {
			mHeartbeatTimer.Dispose ();
		}
	}
}

