using APSocket.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace APSocket.Net
{
    public delegate void RecivedDataByte(Socket socket, byte[] data);

    public delegate void Accept(int id, string ipAddress);
    public delegate void DisConnect(int id, string ipAddress);

    public class Server
    {
        /// <summary>
        /// EOF: End of Each Message. But Reciving Will Be Continue.
        /// FOF: End OF Message And End Of Reciving
        /// </summary>
        public string EndOfMessage = "<EOF>";
        public string BreakMessage = "<FOF>";

        public int BackLog { get; set; }

        public enum CommunicationMode { Messaging, StreamFile }

        public event RecivedData ReciveIntterupt;
        public event RecivedDataByte ReciveByteIntterupt;
        public event Accept AcceptNewConnection;
        public event DisConnect DisConnectConnection;

        public Queue<APSocket.AcceptedClient> ConnectedClients = new Queue<APSocket.AcceptedClient>();
        public ManualResetEvent DoWork = new ManualResetEvent(false);

        private CommunicationMode selectedMode;

        public Server()
        {
            BackLog = 10;
        }

        public static IPAddress[] CurrentServerIPs()
        {
            return Dns.Resolve(Dns.GetHostName()).AddressList;
        }

        public void RefreshConnectionState()
        {
            int len = ConnectedClients.Count;

            for (int i = 0; i < len; i++)
            {
                var temp = ConnectedClients.Dequeue();
                if (!temp.acceptedSocket.Connected)
                {
                    if (DisConnectConnection != null)
                        DisConnectConnection(temp.id, temp.IPAddress);
                    return;
                }

                try
                {
                    bool part1 = temp.acceptedSocket.Poll(1000, SelectMode.SelectRead);
                    bool part2 = (temp.acceptedSocket.Available == 0);
                    if ((part1 && part2) || !temp.acceptedSocket.Connected)
                    {
                        if (DisConnectConnection != null)
                            DisConnectConnection(temp.id, temp.IPAddress);
                    }
                    else
                        ConnectedClients.Enqueue(temp);

                    //if (!(temp.acceptedSocket.Poll(1, SelectMode.SelectRead) && temp.acceptedSocket.Available == 0))
                    //{
                    //    ConnectedClients.Enqueue(temp);
                    //}
                }
                catch (SocketException)
                {
                    if (DisConnectConnection != null)
                        DisConnectConnection(temp.id, temp.IPAddress);
                }
            }
        }

        /// <summary>
        /// Start Listening On Specified IP And Port For Accespting Connections
        /// </summary>
        /// <param name="serverIP">Current Host IP (Listener)</param>
        /// <param name="port">Current Host Port (Listener)</param>
        /// <param name="communicationMode">Type Of Ccommunicatio
        ///Messaging: For Transfer Message. End Of Each Message Is 'EOF'/'FOF'.
        ///StreamFile: For Transfer File (In This Case Your BufferSize Must Be Larger Than File Size) </param>
        public async void StartListeninig(string serverIP, int port, CommunicationMode communicationMode)
        {
            await Task.Factory.StartNew(() =>
            {
                selectedMode = communicationMode;

                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(serverIP), port);
                Socket server = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(ep);
                server.Listen(BackLog);

                while (true)
                {
                    DoWork.Reset();
                    server.BeginAccept(new AsyncCallback(Accepting), server);
                    DoWork.WaitOne();
                }
            });
        }

        private void Accepting(IAsyncResult res)
        {
            DoWork.Set();

            Socket Listener = (Socket)res.AsyncState;
            Socket Handel = Listener.EndAccept(res);

            if (AcceptNewConnection != null)
                AcceptNewConnection((ConnectedClients.Count + 1), Handel.RemoteEndPoint.ToString());


            switch (selectedMode)
            {
                case CommunicationMode.Messaging:

                    APSocket.DataStruct state = new APSocket.DataStruct();
                    state.socket = Handel;

                    APSocket.AcceptedClient temp = new APSocket.AcceptedClient { id = (ConnectedClients.Count + 1), IPAddress = Handel.RemoteEndPoint.ToString(), acceptedSocket = Handel };
                    ConnectedClients.Enqueue(temp);

                    Handel.BeginReceive(
                        state.buffer
                        , 0
                        , APSocket.DataStruct.BufferSize
                        , 0
                        ,
                        new AsyncCallback(ReadCallback), state);
                    break;

                case CommunicationMode.StreamFile:
                    APSocket.NetStream currentStream = new APSocket.NetStream(Handel, false);

                    if (ReciveByteIntterupt != null)
                        ReciveByteIntterupt(Handel, currentStream.ReadByte());

                    Handel.Close();
                    break;
            }

        }

        private void ReadCallback(IAsyncResult ar)
        {

            Task.Factory.StartNew(() =>
            {
                try
                {
                    String content = String.Empty;

                    APSocket.DataStruct state = (APSocket.DataStruct)ar.AsyncState;
                    Socket handler = state.socket;

                    int bytesRead = handler.EndReceive(ar);

                    if (bytesRead > 0)
                    {
                        state.sb.Append(Encoding.Unicode.GetString(state.buffer, 0, bytesRead));

                        content = state.sb.ToString();

                        if (ReciveIntterupt != null && content.IndexOf(BreakMessage) > -1)
                        {
                            ReciveIntterupt(handler, content.Replace(BreakMessage, ""));
                            state.sb.Clear();
                            handler.Close();

                            return;
                        }

                        if (ReciveIntterupt != null && content.IndexOf(EndOfMessage) > -1)
                        {
                            ReciveIntterupt(handler, content.Replace(EndOfMessage, ""));
                            state.sb.Clear();
                            //handler.Close();
                        }

                        //ReciveIntterupt(handler, content.Replace("<EOF>", ""));
                        state.sb.Clear();
                        handler.BeginReceive(state.buffer, 0, APSocket.DataStruct.BufferSize, 0,
                            new AsyncCallback(ReadCallback), state);

                    }

                }
                catch (SocketException e)
                {

                    RefreshConnectionState();

                }
            });

        }


        //..............................................................................

            /// <summary>
            /// Get Count Of Connected Clients
            /// </summary>
            /// <returns></returns>
        public int countOfAcceptedConnection()
        {
            return ConnectedClients.Count;
        }

        /// <summary>
        /// Assiagn Specified Connected Client
        /// </summary>
        /// <param name="id">Client Id</param>
        /// <returns></returns>
        public Socket GetAcceptedConnection(int id)
        {
            var temp = ConnectedClients.Where(p => p.id == id).FirstOrDefault();
            if (temp != null)
                return temp.acceptedSocket;

            return null;
        }

        //..............................................................................
        ManualResetEvent sendDone = new ManualResetEvent(false);

        #region Send
        /// <summary>
        /// Send Data Async
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data">Message</param>
        public void SendAsync(Socket socket, String data)
        {
            byte[] byteData = Encoding.Unicode.GetBytes(data + EndOfMessage);

            socket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), socket);

            sendDone.WaitOne();
        }

        /// <summary>
        /// Send Data Async
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        public void SendAsync(Socket socket, byte[] data)
        {
            socket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), socket);

            sendDone.WaitOne();
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handel = (Socket)ar.AsyncState;

                int bytesSent = handel.EndSend(ar);

                sendDone.Set();
            }
            catch (Exception e)
            {

            }
        }

        /// <summary>
        /// Send Data To Specified Client
        /// </summary>
        /// <param name="ClientId">Client Id</param>
        /// <param name="data">Data</param>
        public void SendAsyncTo(int ClientId, byte[] data)
        {
            Socket socket = GetAcceptedConnection(ClientId);
            SendAsync(socket, data);
        }

        /// <summary>
        /// Send Data To Specified Client
        /// </summary>
        /// <param name="ClientId">Client Id</param>
        /// <param name="message">Data</param>
        public void SendAsyncTo(int ClientId, string message)
        {
            byte[] data = Encoding.Unicode.GetBytes(message + EndOfMessage);
            Socket socket = GetAcceptedConnection(ClientId);
            SendAsync(socket, data);
        }

        #endregion
    }
}
