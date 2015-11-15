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
    public delegate void RecivedData(Socket socket, string message);

    public class APSocket
    {
        public class DataStruct
        {
            public Socket socket { get; set; }

            public const int BufferSize = 1024;
            public byte[] buffer = new byte[BufferSize];

            public StringBuilder sb = new StringBuilder();

        }

        public class DataStructLenghFirst
        {
            public Socket socket { get; set; }

            public const int BufferSize = 1024;
            public int? FileSize;
            public int? Index;
            public byte[] buffer = new byte[BufferSize];
            public byte[] RawData;
        }

        public class AcceptedClient
        {
            public int id { get; set; }
            public string IPAddress { get; set; }
            public Socket acceptedSocket { get; set; }
        }

       public class NetStream
        {
            NetworkStream myNetworkStream;
            List<byte> buf = new List<byte>();

            public event RecivedData RecivedDatastr;
            public NetStream(Socket mySocket, bool isOwner)
            {
                if (isOwner)
                {
                    myNetworkStream = new NetworkStream(mySocket, true);
                }
                else
                {
                    myNetworkStream = new NetworkStream(mySocket);
                }
            }
            public string ReadString()
            {
                if (myNetworkStream.CanRead)
                {
                    byte[] myReadBuffer = new byte[1024];
                    StringBuilder myCompleteMessage = new StringBuilder();
                    int numberOfBytesRead = 0;

                    // Incoming message may be larger than the buffer size.
                    do
                    {
                        numberOfBytesRead = myNetworkStream.Read(myReadBuffer, 0, myReadBuffer.Length);

                        myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));

                    }
                    while (myNetworkStream.DataAvailable);

                    return myCompleteMessage.ToString();
                }
                else
                {
                    return null;
                }
            }

            public byte[] ReadByte()
            {
                try {
                    if (myNetworkStream.CanRead)
                    {
                        byte[] myReadBuffer = new byte[1024 * 10];
                        StringBuilder myCompleteMessage = new StringBuilder();
                        int numberOfBytesRead = 0;

                        do
                        {
                            numberOfBytesRead = myNetworkStream.Read(myReadBuffer, 0, myReadBuffer.Length);
                            byte[] temp = new byte[numberOfBytesRead];
                            //Array.Copy(myReadBuffer, temp, numberOfBytesRead);

                            //buf.AddRange(temp);

                            for (int i = 0; i < numberOfBytesRead; i++)
                            {
                                buf.Add(myReadBuffer[i]);
                            }
                        }
                        while (myNetworkStream.DataAvailable);

                        myNetworkStream.Close();
                        return buf.ToArray();

                    }
                    else
                    {
                        return null;
                    }
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
