using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MVP_2_0_Server
{
    class Session
    {
        public string Id { get; } = Guid.NewGuid().ToString();

        public List<ClientConnection> Connections = new List<ClientConnection>();
        private List<IPEndPoint> ClientUDPEndPoints = new List<IPEndPoint>();
        private List<string> ClientUDPEndPoints_String = new List<string>();

        public void Add_ClientUDPEndPoints(IPEndPoint ep)
        {
            ClientUDPEndPoints.Add(ep);
            ClientUDPEndPoints_String.Add(ep.ToString());
        }

        public void Remove_ClientUDPEndPoints(IPEndPoint ep)
        {
            ClientUDPEndPoints.Remove(ep);
            ClientUDPEndPoints_String.Remove(ep.ToString());
        }

        public List<IPEndPoint> Get_EP_ClientUDPEndPoints()
        {
            return ClientUDPEndPoints;
        }

        public List<string> Get_String_ClientUDPEndPoints()
        {
            return ClientUDPEndPoints_String;
        }

        public void SessionBroadcastExceptYourself(ClientConnection fromClient, string message)
        {
            foreach(ClientConnection client in Connections)
            {
                if (client == fromClient) continue; // Dont send it to ypurself
                lock (fromClient.writerLocker)
                {
                    client.Writer.WriteLine(message);
                    WriteToConsole($"{message} send to {client.Id}");
                }
            }
        }

        public void SessionBroadcastToAll(ClientConnection fromClient, string message)
        {
            foreach (ClientConnection client in Connections)
            {
                lock (fromClient.writerLocker)
                {
                    client.Writer.WriteLine(message);
                    WriteToConsole($"{message} send to {client.Id}");
                }
            }
        }

        ~Session()
        {
            WriteToConsole($"Session {Id} destructor");
        }

        void WriteToConsole(string msg)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss tt ") + msg);
        }
    }
}
