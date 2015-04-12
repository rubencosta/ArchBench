using System;
using System.Net.Sockets;

namespace ArchBench.PlugIns.Broker
{
	public class Server
	{
		public string IpAdress { get; set; }
		public int Port { get; set; }
		public int Id { get; private set;}

		public Server (string aIpAdress, int aPort, int id)
		{
			//TODO verificar por strings vazias e portas fora do range
			IpAdress = aIpAdress;
			Port = aPort;
			Id = id;
		}

		public string GetUrl() {
			return IpAdress + ':' + Port;
		}

		public bool IsAlive () 
		{
			try
			{
				var client = new TcpClient();
				client.Connect(IpAdress, Port);
				client.Close();
				return true;
			}
			catch (Exception e)
			{
				// delete server

				return false;
			}
		}

		public override bool Equals (object obj)
		{
			if(obj == null || GetType() != obj.GetType()) 
				return false;
			return ((Server)obj).Id == Id;
		}
		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}
	}



}

