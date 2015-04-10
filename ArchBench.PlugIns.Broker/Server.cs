using System;
using System.Net.Sockets;

namespace ArchBench.PlugIns.Broker
{
	public class Server
	{
		public String IPAdress { get; set; }
		public int Port { get; set; }
		public int Id { get; private set;}

		public Server (String aIPAdress, int aPort, int id)
		{
			//TODO verificar por strings vazias e portas fora do range
			IPAdress = aIPAdress;
			Port = aPort;
			Id = id;
		}

		public string getUrl() {
			return IPAdress + ':' + Port;
		}

		public bool isAlive () 
		{
			try
			{
				TcpClient client = new TcpClient();
				client.Connect(IPAdress, Port);
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

