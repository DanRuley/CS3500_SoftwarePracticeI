using NetworkUtil;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace ChatClient
{
    class ClientController
    {
        private SocketState theServer;

        public delegate void MessagesArrivedHander(IEnumerable<string> Messages);
        public event MessagesArrivedHander MessagesArrived;

        public delegate void ErrorHandler(string ErrMsg);
        public event ErrorHandler OnError;

        public ClientController()
        {
        }

        internal void EndConnection()
        {
            if (theServer != null && theServer.TheSocket != null)
            {
                theServer.TheSocket.Shutdown(SocketShutdown.Both);
            }
        }

        internal void Connect(string ServerAddress)
        {
            Networking.ConnectToServer(OnConnect, ServerAddress, 11000);
        }

        private void OnConnect(SocketState state)
        {
            if (state.ErrorOccured)
            {
                OnError(state.ErrorMessage);
                return;
            }

            // Save the SocketState so we can use it to send messages
            theServer = state;

            // Start an event loop to receive messages from the server
            state.OnNetworkAction = ReceiveMessage;
            Networking.GetData(state);

        }

        private void ReceiveMessage(SocketState state)
        {
            if (state.ErrorOccured)
            {
                OnError(state.ErrorMessage);
                return;
            }

            ProcessMessages(state);
            // Continue the event loop
            Networking.GetData(state);
        }

        private void ProcessMessages(SocketState state)
        {
            string totalData = state.GetData();
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");


            // Loop until we have processed all messages.
            // We may have received more than one.
            List<string> Messages = new List<string>();

            foreach (string p in parts)
            {
                // Ignore empty strings added by the regex splitter
                if (p.Length == 0)
                    continue;
                // The regex splitter will include the last string even if it doesn't end with a '\n',
                // So we need to ignore it if this happens. 
                if (p[p.Length - 1] != '\n')
                    break;

                Messages.Add(p + "\n");

                // Then remove it from the SocketState's growable buffer
                state.RemoveData(0, p.Length);
            }

            //Fire MessagesArrived event so view can display
            MessagesArrived(Messages);

        }

        internal void SendMessage(string message)
        {
            if (theServer != null)
                Networking.Send(theServer.TheSocket, message += "\n");
            else
                OnError("Not Connected");
        }
    }
}
