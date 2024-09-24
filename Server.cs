using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using LumiSoft.Net.STUN.Client;

namespace MVP_2_0_Server
{
    class Server
    {
        TcpListener tcpListener = new TcpListener(IPAddress.Any, 8888);
        List<ClientConnection> clients = new List<ClientConnection>();

        public List<Session> sessions = new List<Session>();
        public List<string> sessionsIDs = new List<string>();

        IPEndPoint publicEP = new IPEndPoint(0, 0);
        public Server()
        {
            CreateNewSession();
            CreateNewSession();
            CreateNewSession();


            /*
            // Create new socket for STUN client.
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, 0));

            // Query STUN server 
            // stun1.l.google.com:19305
            // stun.3deluxe.de:3478
            // stun.fitauto.ru
            STUN_Result result = STUN_Client.Query("stun.fitauto.ru", 3478, socket);
            if (result.NetType == STUN_NetType.UdpBlocked)
            {
                // UDP blocked or !!!! bad STUN server
                WriteToConsole($"Blocked or bad STUN server!");
            }
            else
            {
                publicEP = result.PublicEndPoint;
                // Do your stuff
                WriteToConsole($"Server Public IP: {publicEP.Address}:{publicEP.Port}");
            }
            */

            // Запускаем сервер
            Task.Run(() => WaitingForConnectionsAsync());
            //new Thread(() => WaitingForConnectionsAsync()).Start(); 
        }

        public async Task WaitingForConnectionsAsync()
        {
            try
            {
                tcpListener.Start();
                WriteToConsole($"Waiting for connections...");

                while (true)
                {
                    TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                    WriteToConsole($"TCP: {tcpClient.Client.RemoteEndPoint} connected!");

                    ClientConnection clientObject = new ClientConnection(tcpClient, this);
                    clients.Add(clientObject);
                    clientObject.StartBasicFunctions();
                }
            }
            catch (Exception ex)
            {
                WriteToConsole(ex.Message);
            }
            finally
            {
                Disconnect();
            }
        }
        public void CreateNewSession()
        {
            Session s = new Session();
            sessions.Add(s);
            sessionsIDs.Add(s.Id);
            WriteToConsole("Session " + s.Id + " was created!");
        }

        public void ConnectClientToTheSession(string sessionID, ClientConnection clientToConnect)
        {
            Session selectedSession = sessions.FirstOrDefault(s => s.Id == sessionID);
            if (selectedSession != null)
            {
                selectedSession.Connections.Add(clientToConnect);
                clientToConnect.session = selectedSession;
                WriteToConsole("Client " + clientToConnect.Id + " connected to session: " + selectedSession.Id);
            }
        }


        public void RemoveConnection(ClientConnection client)
        {
            if (!clients.Contains(client)) return;
            // Удалить клиента из списка на сервере
            clients.Remove(client);
            // Удалить клиента из сессии
            sessions.FirstOrDefault(client.session).Connections.Remove(client);
            // Закрыть подключение
            client.CloseConnection();

            WriteToConsole("Client removed: " + client.Id);
        }

        public void Disconnect()
        {
            foreach (var client in clients)
            {
                client.CloseConnection(); //отключение клиента
            }
            tcpListener.Stop(); //остановка сервера

            WriteToConsole("Disconnect: Server Stoped");
        }

        ~Server()
        {
            WriteToConsole("Server destructor");
        }

        void WriteToConsole(string msg)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss tt ") + msg);
        }
    }
}
