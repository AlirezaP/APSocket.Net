# APSocket.Net
APSocket.Net Is A Good Tool For Socket Progarming.</br>
With APSocket.Net You Can Listen,Recive,Send,Manage Clients Very Easy.

</br>


Server Side Example:</br>

                  var all = APSocket.Net.Server.CurrentServerIPs(); //Get The Host IPs

                  server = new APSocket.Net.Server();
                  server.AcceptNewConnection += Server_AcceptNewConnection;
                  server.DisConnectConnection += Server_DisConnectConnection;
                  server.ReciveByteIntterupt += Server_ReciveByteIntterupt;
                  server.ReciveIntterupt += Server_ReciveIntterupt;
                  server.StartListeninig(APSocket.Net.Server.CurrentServerIPs()[0].ToString(), 159,                   APSocket.Net.Server.CommunicationMode.Messaging);


        private void Server_ReciveIntterupt(System.Net.Sockets.Socket socket, string message)
        {
            //When Recive Data From Client
        }

        private void Server_DisConnectConnection(int id, string ipAddress)
        {
                //When A Connection Was Disconnect
        }

        private void Server_AcceptNewConnection(int id, string ipAddress)
        {
              //When A Connection Was Accepted
        }


               //You Can Send Data To The Clients With Send Method:
                 server.SendAsyncTo(clientId, "Message");
                 server.SendAsync(socketObject, "Message");
                 server.SendAsync(socketObject, byte[]);


               //You Can Update Clients Status:
                server.RefreshConnectionState();

               //You Can Get The Specified Client By Id:
               Socket = server.GetAcceptedConnection(Client Id)
               
</br>
</br>

Client Side Example:</br>

            client.Connect("Server IP", Server Port);
            client.ClientDataRecived += Client_ClientDataRecived;
            client.ReceiveAsync();

        private void Client_ClientDataRecived(string message)
        {
            //When Recive Data From Server
        }

        // You Can Send Data To The Server With Send Method:
          client.Send("Hello");
          client.Send(byte[]);
