using NetworkUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatServer
{
    class ServerController
    {

        // A set of clients that are connected.
        private Dictionary<long, SocketState> clients;

        public ServerController()
        {
            clients = new Dictionary<long, SocketState>();
            this.StartServer();
        }


        private void NewClientConnected(SocketState state)
        {
            if (state.ErrorOccured)
            {
                clients.Remove(state.ID);
                return;
            }

            // Save the client state
            // Need to lock here because clients can disconnect at any time
            lock (clients)
            {
                clients[state.ID] = state;
            }

            state.OnNetworkAction = ReceiveMessage;
            Networking.GetData(state);
        }


        private void ReceiveMessage(SocketState state)
        {
            if (state.ErrorOccured)
            {
                RemoveClient(state.ID);
                return;
            }

            ProcessMessage(state);
            // Continue the event loop that receives messages from this client
            Networking.GetData(state);
        }

        private void RemoveClient(long id)
        {
            ChatServer.ServerPrint("Client " + id + " disconnected");
            lock (clients)
            {
                clients.Remove(id);
            }
        }

        /// <summary>
        /// Given the data that has arrived so far, 
        /// potentially from multiple receive operations, 
        /// determine if we have enough to make a complete message,
        /// and process it (print it and broadcast it to other clients).
        /// </summary>
        /// <param name="sender">The SocketState that represents the client</param>
        private void ProcessMessage(SocketState state)
        {
            string totalData = state.GetData();

            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            // Loop until we have processed all messages.
            // We may have received more than one.
            foreach (string p in parts)
            {
                // Ignore empty strings added by the regex splitter
                if (p.Length == 0)
                    continue;
                // The regex splitter will include the last string even if it doesn't end with a '\n',
                // So we need to ignore it if this happens. 
                if (p[p.Length - 1] != '\n')
                    break;

                ChatServer.ServerPrint("received message from client " + state.ID + ": \"" + p + "\"");

                // Remove it from the SocketState's growable buffer
                state.RemoveData(0, p.Length);

                // Broadcast the message to all clients
                // Lock here beccause we can't have new connections 
                // adding while looping through the clients list.
                // We also need to remove any disconnected clients.
                HashSet<long> disconnectedClients = new HashSet<long>();
                lock (clients)
                {
                    foreach (SocketState client in clients.Values)
                    {
                        if (!Networking.Send(client.TheSocket, "Message from client " + state.ID + ": " + p))
                            disconnectedClients.Add(client.ID);
                    }
                }
                foreach (long id in disconnectedClients)
                    RemoveClient(id);
            }
        }

        /// <summary>
        /// Start accepting Tcp sockets connections from clients
        /// </summary>
        public void StartServer()
        {
            // This begins an "event loop"
            Networking.StartServer(NewClientConnected, 11000);

            ChatServer.ServerPrint("Server is running");
        }
    }
}
