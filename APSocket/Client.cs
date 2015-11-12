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
    public delegate void ClientRecivedData(string message);

    public class Client
    {
        ManualResetEvent connectDone = new ManualResetEvent(false);
        ManualResetEvent receiveDone = new ManualResetEvent(false);
        ManualResetEvent sendDone = new ManualResetEvent(false);

        public event ClientRecivedData ClientDataRecived;

        public Socket clientsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        /// <summary>
        /// EOF: End of Each Message. But Reciving Will Be Continue.
        /// FOF: End OF Message And End Of Reciving
        /// </summary>
        public string EndOfMessage = "<EOF>";
        public string BreakMessage = "<FOF>";

        /// <summary>
        /// Connect To The Specified Server (Async)
        /// </summary>
        /// <param name="ip">Destination IP</param>
        /// <param name="port">Destination Port</param>
        public void ConnectAsync(IPAddress ip, int port)
        {
            EndPoint ep = new IPEndPoint(ip, port);
            clientsocket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            clientsocket.BeginConnect(new IPEndPoint(ip, port), new AsyncCallback(ConnectCallback), clientsocket);
            connectDone.WaitOne();
        }

        /// <summary>
        ///  Connect To The Specified Server
        /// </summary>
        /// <param name="ip">Destination IP</param>
        /// <param name="port">Destination Port</param>
        public void Connect(IPAddress ip, int port)
        {
            EndPoint ep = new IPEndPoint(ip, port);
            clientsocket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            clientsocket.Connect(ep);
        }

        /// <summary>
        ///  Connect To The Specified Server
        /// </summary>
        /// <param name="ip">Destination IP</param>
        /// <param name="port">Destination Port</param>
        public void Connect(string ip, int port)
        {
            Connect(IPAddress.Parse(ip), port);
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket clienthandel = (Socket)ar.AsyncState;

                clienthandel.EndConnect(ar);

                connectDone.Set();

                // Receive(clienthandel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        #region R
        /// <summary>
        /// Start Reciving
        /// </summary>
        public void ReceiveAsync()
        {
            try
            {
                APSocket.DataStruct state = new APSocket.DataStruct();
                state.socket = clientsocket;

                clientsocket.BeginReceive(state.buffer, 0, APSocket.DataStruct.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveAsync(Socket client)
        {
            try
            {
                APSocket.DataStruct state = new APSocket.DataStruct();
                state.socket = client;

                client.BeginReceive(state.buffer, 0, APSocket.DataStruct.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                APSocket.DataStruct state = (APSocket.DataStruct)ar.AsyncState;
                Socket client = state.socket;

                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.Unicode.GetString(state.buffer, 0, bytesRead));

                    if (state.sb.ToString().Contains(EndOfMessage))
                    {
                        if (ClientDataRecived != null)
                            ClientDataRecived(state.sb.ToString().Replace(EndOfMessage, ""));
                        state.sb.Clear();
                    }

                    if (state.sb.ToString().Contains(BreakMessage))
                    {
                        if (ClientDataRecived != null)
                            ClientDataRecived(state.sb.ToString().Replace(BreakMessage, ""));
                        state.sb.Clear();
                        client.Close();
                        receiveDone.Set();
                        return;
                    }

                    client.BeginReceive(state.buffer, 0, APSocket.DataStruct.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    if (state.sb.Length > 1 && ClientDataRecived != null)
                    {
                        ClientDataRecived(state.sb.ToString());
                    }
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        #endregion

        #region S
        /// <summary>
        /// Send Message To The Server (Async)
        /// </summary>
        /// <param name="data"></param>
        public void SendAsync(String data)
        {
            byte[] byteData = Encoding.Unicode.GetBytes(data + EndOfMessage);

            clientsocket.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), clientsocket);

            sendDone.WaitOne();
        }

        /// <summary>
        /// Send Data To The Server (Async)
        /// </summary>
        /// <param name="data"></param>
        public void SendAsync(byte[] data)
        {
            clientsocket.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), clientsocket);

            sendDone.WaitOne();
        }

        /// <summary>
        /// Send Data To The Server 
        /// </summary>
        /// <param name="byteData"></param>
        public void Send(byte[] byteData)
        {
            clientsocket.Send(byteData, 0, byteData.Length, SocketFlags.None);
        }

        /// <summary>
        /// Send Message To The Server 
        /// </summary>
        /// <param name="byteData"></param>
        public void Send(string byteData)
        {
            byte[] buf = Encoding.Unicode.GetBytes(byteData + EndOfMessage);
            Send(buf);
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

        public void Close()
        {
            Send(Encoding.Unicode.GetBytes(BreakMessage));
            clientsocket.Close();
        }

        #endregion

    }
}
