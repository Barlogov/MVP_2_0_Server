using System.Net;
using System.Net.Sockets;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

namespace MVP_2_0_Server
{
    class ClientConnection
    {
        // ClientConnection NET part
        public string Id { get; } = Guid.NewGuid().ToString();
        public StreamWriter Writer { get; }
        public StreamReader Reader { get; }

        TcpClient client;
        Server server;

        bool activeConnection = true;

        public Session session;
        IPEndPoint clientUDPEndPoint;

        public DateTime previousPing;

        // ClientConnection Data part
        public List<string> modelIDs = new List<string>();

        // Protocol 
        string MSGSplitter = "///";
        enum MsgType_TCP
        {
            Error, // idk
            Ping, // Alive msg to avoid disconnect
            TextMessage, // Send text msg for debugging

            Request_ClientToServer_GetSessionsListIDs, // Client Request to get List from Server
            Response_ServerToClient_GetSessionsListIDs, // Server Response with List<Session>

            Request_ClientToServer_AddPublicUDP_EP, // Client send its "UDP listen port" to Server

            Request_ClientToSession_ConnectToSession, // Client trying to conect to Session
            Response_SessionToClient_ConnectToSession, // Connection to Session state (true or false)

            Request_ClientToSession_AddModel, // Client has Created a local Model and notifies the Session about it
            Request_ClientToSession_DeleteModel, // Client has Deleted a local Model and notifies the Session about it

            Request_ClientToServer_ClientEndPointUDP, // Client asks for its UDP address
            Response_ServerToClient_ClientEndPointUDP, // Server sends the Client's UDP address


            Broadcast_SessionToClients_ClientUDPEndPoints, // Session broadcast msg about Clients UDPEndPoint on the session

            Broadcast_SessionToClients_AddModel, // Session broadcast msg about new model was added on another Client
            Broadcast_SessionToClients_DeleteModel, // Session broadcast msg about deleted model from Client

            Request_ClientToServer_Disconect, // Сlient asks to be disconnected from the server
            Response_ServerToClient_Disconect // Nothing atm
        }
        public ClientConnection(TcpClient tcpClient, Server serverObj)
        {
            client = tcpClient;
            server = serverObj;
            // получаем NetworkStream для взаимодействия с сервером
            var stream = client.GetStream();
            // создаем StreamReader для чтения данных
            Reader = new StreamReader(stream);
            // создаем StreamWriter для отправки данных
            Writer = new StreamWriter(stream);
            Writer.AutoFlush = true;
            WriteToConsole("Client created: " + Id);
        }

        public void StartBasicFunctions()
        {
            //Task.Run(ProcessAsync);
            Task.Run(() => ProcessAsync());
            //new Thread(() => ProcessAsync()).Start();
        }

        public async Task CheckForPing()
        {
            while (activeConnection)
            {
                if ((DateTime.Now - previousPing).TotalSeconds > 5)
                {
                    server.RemoveConnection(this);
                    break;
                }
            }
        }

        // Reader
        // Hex: [00]      [00]
        // TCP: MsgType   Data
        public async Task ProcessAsync()
        {
            previousPing = DateTime.Now;
            Task.Run(CheckForPing);

            while (activeConnection)
            {
                try
                {
                    string? receivedMessage = await Reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(receivedMessage)) continue;
                    string[] messageCollection = receivedMessage.Split(MSGSplitter);

                    // Ping
                    if (messageCollection[0] == MsgType_TCP.Ping.ToString())
                    {
                        //WriteToConsole("Ping received!");
                        previousPing = DateTime.Now;
                        continue;
                    }

                    // TextMessage
                    if (messageCollection[0] == MsgType_TCP.TextMessage.ToString())
                    {
                        WriteToConsole("TextMessage received!");
                        WriteToConsole(client.Client.RemoteEndPoint + " : " + messageCollection[1]);
                        continue;
                    }

                    // Disconect
                    if (messageCollection[0] == MsgType_TCP.Request_ClientToServer_Disconect.ToString())
                    {
                        server.RemoveConnection(this);
                        continue;
                    }

                    // Session List Request
                    if (messageCollection[0] == MsgType_TCP.Request_ClientToServer_GetSessionsListIDs.ToString())
                    {
                        string sessionsIDs_json = JsonConvert.SerializeObject(server.sessionsIDs);
                        WriteToConsole($"Json: {sessionsIDs_json} send to {client.Client.RemoteEndPoint}");
                        SendMessageToClient(MsgType_TCP.Response_ServerToClient_GetSessionsListIDs, sessionsIDs_json);

                        continue;
                    }

                    // Received -Connect To The Session- Request
                    if (messageCollection[0] == MsgType_TCP.Request_ClientToSession_ConnectToSession.ToString())
                    {
                        server.ConnectClientToTheSession(messageCollection[1], this);
                        session.Add_ClientUDPEndPoints(clientUDPEndPoint);
                        SendMessageToClient(MsgType_TCP.Response_SessionToClient_ConnectToSession, "true");
                        BroadcastClientUDPEndPoints();
                        SendAlreadyCreatedModelsToClient();

                        //TODO Connected/Fail condition

                        continue;
                    }

                    // Received -Add Model To Sessions- Request
                    if (messageCollection[0] == MsgType_TCP.Request_ClientToSession_AddModel.ToString())
                    {
                        WriteToConsole("Model ID received!");
                        modelIDs.Add(messageCollection[1]);

                        string msg = $"{MsgType_TCP.Broadcast_SessionToClients_AddModel + MSGSplitter + messageCollection[1]}";
                        session.SessionBroadcastExceptYourself(this, msg);

                        continue;
                    }

                    if(messageCollection[0] == MsgType_TCP.Request_ClientToSession_DeleteModel.ToString())
                    {
                        modelIDs.Remove(messageCollection[1]);
                        string msg = $"{MsgType_TCP.Broadcast_SessionToClients_DeleteModel + MSGSplitter + messageCollection[1]}";
                        session.SessionBroadcastExceptYourself(this, msg);

                        continue;
                    }

                    if (messageCollection[0] == MsgType_TCP.Request_ClientToServer_AddPublicUDP_EP.ToString())
                    {
                        //msg[1] - ep "ip:port"
                        string[] ep_string = messageCollection[1].Split(':');
                        clientUDPEndPoint = new IPEndPoint(IPAddress.Parse(ep_string[0]), Int32.Parse(ep_string[1]));
                        WriteToConsole($"UDP: {clientUDPEndPoint}");

                        SendMessageToClient(MsgType_TCP.Response_ServerToClient_ClientEndPointUDP, clientUDPEndPoint.ToString());

                        continue;
                    }

                    if(messageCollection[0] == MsgType_TCP.Request_ClientToServer_ClientEndPointUDP.ToString())
                    {
                        SendMessageToClient(MsgType_TCP.Response_ServerToClient_ClientEndPointUDP, clientUDPEndPoint.ToString());

                        continue;
                    }

                    // If its unknown message
                    //WriteToConsole($"Write continue; or Unknown Client message: {receivedMessage}");
                    WriteToConsole($"Unknown {client.Client.RemoteEndPoint} message: {receivedMessage}");
                }
                catch (Exception ex)
                {
                    WriteToConsole(ex.ToString());
                    server.RemoveConnection(this);
                    break;
                }
            }
        }

        // Writer
        public object writerLocker = new object();
        private void SendMessageToClient(MsgType_TCP type, string msg = "")
        {
            lock (writerLocker)
            {
                Writer.WriteLine(type + MSGSplitter + msg);
            }
        }

        void BroadcastClientUDPEndPoints()
        {
            string clientUDPEndPoints_json = JsonConvert.SerializeObject(session.Get_String_ClientUDPEndPoints());
            string msg = $"{MsgType_TCP.Broadcast_SessionToClients_ClientUDPEndPoints + MSGSplitter + clientUDPEndPoints_json}";
            session.SessionBroadcastToAll(this, msg);
        }

        void SendAlreadyCreatedModelsToClient()
        {
            foreach(ClientConnection client in session.Connections)
            {
                foreach(string modelID in client.modelIDs)
                {
                    SendMessageToClient(MsgType_TCP.Broadcast_SessionToClients_AddModel, modelID);
                }
            }
        }

        void BroadcastDeleteAllMyModels()
        {
            foreach (string modelID in modelIDs)
            {
                string msg = $"{MsgType_TCP.Broadcast_SessionToClients_DeleteModel + MSGSplitter + modelID}";
                session.SessionBroadcastExceptYourself(this, msg);
            }
            modelIDs.Clear();
        }


        public void CloseConnection()
        {
            activeConnection = false;

            BroadcastDeleteAllMyModels();

            // Disconect from session
            if(session != null && clientUDPEndPoint != null)
            {
                session.Remove_ClientUDPEndPoints(clientUDPEndPoint);
                BroadcastClientUDPEndPoints();
                WriteToConsole("Disconnected from the Session: " + session.Id);
            }

            WriteToConsole("Connection Closed: " + client.Client.RemoteEndPoint);
            Writer.Close();
            Reader.Close();
            client.Close();
        }

        ~ClientConnection()
        {
            WriteToConsole($"Client {Id} destructor");
        }

        void WriteToConsole(string msg)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss tt ") + msg);
        }
    }
}
