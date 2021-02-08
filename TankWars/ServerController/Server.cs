//Authors: Gavin Gray, Dan Ruley
//December 2019
using NetworkUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using TankWars;

namespace TankWarsServer
{
    /// <summary>
    /// Main class for the TankWars server.  It creates an event loop to listen for client connections on port 11000 for TankWars players and on port 80 for Web clients requesting game statistics via HTTP requests.
    /// </summary>
    class Server
    {
        /// <summary>
        /// The model for the server's game world.
        /// </summary>
        ServerGameWorld TheWorld;
        /// <summary>
        /// Stores the socket states of connected players.
        /// </summary>
        Dictionary<long, SocketState> PlayerClients;
        /// <summary>
        /// Stores the (valid) incoming player commands.
        /// </summary>
        Dictionary<int, string> Commands;
        /// <summary>
        /// TCP listener for the game server.
        /// </summary>
        TcpListener GameListener;
        /// <summary>
        /// TCP listener for the web server.
        /// </summary>
        TcpListener WebListener;
        /// <summary>
        /// Game server listens on port 11000.
        /// </summary>
        readonly int GAMEPORT = 11000;
        /// <summary>
        /// Web server listens on port 80.
        /// </summary>
        readonly int WEBPORT = 80;

        /// <summary>
        /// Constructs the Server object.
        /// </summary>
        public Server()
        {
            TheWorld = new ServerGameWorld();
            PlayerClients = new Dictionary<long, SocketState>();
            Commands = new Dictionary<int, string>();
        }

        /// <summary>
        /// The main driver method for the game server.  Sets up the server and client connections.  Then, as long as the server remains open it checks for commands for clients and updates the game model once per frame.
        /// </summary>
        private void StartServerLoop()
        {
            try
            {
                // Populates the GameWorld with setting from the settings file.
                TheWorld.ReadWorldSettings("..\\..\\..\\Resources\\Resources\\settings.xml");
            }
            catch (Exception e)
            {
                // If we can't accuratly parse the settings file then we don't want to try and run the game.
                //      so just return.
                Console.WriteLine(e.Message + "\n\nProblem occurred opening settings file.\nPress any key to continue...");
                return;
            }

            // Start the Game Client listener
            GameListener = Networking.StartServer(OnPlayerConnect, GAMEPORT);
            // Start the Web client listener
            WebListener = Networking.StartServer(OnWebConnect, WEBPORT);

            Console.WriteLine("Server ready, waiting for clients.");

            Stopwatch game_timer = new Stopwatch();

            game_timer.Start();
            while (true)
            {
                while (game_timer.ElapsedMilliseconds < TheWorld.MSPERFRAME) { }

                game_timer.Restart();

                // pass the received messages in order to update the world.
                Dictionary<int, string> cmds;
                lock (Commands)
                {
                    cmds = new Dictionary<int, string>(Commands);
                    // Reset the Commands Dictionary in order to be able to receive commands in async
                    Commands = new Dictionary<int, string>();
                }
                // Update the world with current commands
                TheWorld.UpdateGameWorld(cmds);

                BroadcastMessageAndRemoveDisconnected();
            }
        }

        /// <summary>
        /// Sent server messages to connected clients, remove any disconnected clients and add the disconnect command to the command dictionary so we can broadcast to remaining clients that they have disconnected on the next frame.
        /// </summary>
        private void BroadcastMessageAndRemoveDisconnected()
        {
            // Send the game results to all the made connections.    
            string msg = TheWorld.GetEntireWorldAsJsonMsg();
            lock (PlayerClients)
            {
                List<long> DisconnectedClients = new List<long>();
                foreach (SocketState ss in PlayerClients.Values)
                {
                    if (ss != null)
                    {
                        if (!Networking.Send(ss.TheSocket, msg))
                        {
                            DisconnectedClients.Add(ss.ID);
                            //handling disconnects => if send returns false either socket is not connected, or an error occurred so we consider
                            //this client to be disconnected, add Disc_cmd to dictionary so we can broadcast to the rest of clients on next frame
                            lock (Commands)
                            {
                                Commands.Add((int)ss.ID, TheWorld.DISC_CMD);
                            }
                        }
                    }
                }

                lock (PlayerClients)
                {
                    foreach (long id in DisconnectedClients)
                    {
                        PlayerClients.Remove(id);
                        Console.WriteLine("Player (" + id.ToString() + ") diconnected.");
                    }
                }
            }
        }

        /// <summary>
        /// Callback for initial web web connections.  Sets the SS delegate to OnWebReceive and calls GetData to continue loop.
        /// </summary>
        /// <param name="ss"></param>
        private void OnWebConnect(SocketState ss)
        {
            if (ss.ErrorOccured) return;
            ss.OnNetworkAction = OnWebReceive;
            Networking.GetData(ss);
        }

        /// <summary>
        /// Receives data from web client SS.  Parses the request via ParseHTTPRequest in the WebController, sends the result to client, and closes the socket.
        /// </summary>
        /// <param name="ss"></param>
        private void OnWebReceive(SocketState ss)
        {
            if (ss.ErrorOccured) return;
            string request = ss.GetData();
            Console.WriteLine(request);
            ss.ClearData();
            Networking.SendAndClose(ss.TheSocket, WebController.ParseHTTPRequest(request) + "\n");
        }

        /// <summary>
        /// Callback for StartSever.
        /// 
        /// Requests the player name.
        /// </summary>
        /// <param name="ss"></param>
        private void OnPlayerConnect(SocketState ss)
        {
            if (ss.ErrorOccured) return;
            // Request the player for her UserName;
            ss.OnNetworkAction = OnReceiveName;
            Console.WriteLine("Accepted New Connection");
            Networking.GetData(ss);
        }

        /// <summary>
        /// Completes the handshake between client and server, sending the initial message to the client so they can build their version of the game world. Continues the event loop with a call to GetData on the connected socket state.
        /// </summary>
        /// <param name="ss"></param>
        private void OnReceiveName(SocketState ss)
        {
            if (ss.ErrorOccured) return;

            // Get User Name - If user entered an invalid or empty name we return
            string PlayerName = AddPlayerName(ss);
            if (PlayerName == ";")
            {
                ss.TheSocket.Close();
                return;
            }

            // Send the User ID and World Size
            StringBuilder msg = new StringBuilder();
            msg.Append(ss.ID.ToString() + '\n');
            msg.Append(TheWorld.SIZE.ToString() + '\n');
            // Send all the walls
            foreach (string w in TheWorld.GetSerializedWalls())
            {
                msg.Append(w + '\n');
            }
            bool sent = Networking.Send(ss.TheSocket, msg.ToString());
            // Continue Handshake with Receiveing data and changing the callback
            if (sent)
            {
                Console.WriteLine("Player (" + ss.ID.ToString() + ") has joined.");
                // Save the socket state 
                ss.OnNetworkAction = ReceiveGameCommand;
                AddClientSocket(ss);
                // Add Tank To GameWorld
                TheWorld.AddNewPlayer((int)ss.ID, PlayerName);
                Networking.GetData(ss);
            }
        }

        /// <summary>
        /// Delegate for processing Game Commands from a connected client.
        /// </summary>
        /// <param name="ss">SocketState of the client.</param>
        private void ReceiveGameCommand(SocketState ss)
        {
            if (ss.ErrorOccured) return;
            // Process Game Command Request
            AddOrModifyCommand((int)ss.ID, GetSingleCommand(ss));
            lock (PlayerClients)
            {
                if (PlayerClients.ContainsKey(ss.ID)) PlayerClients[ss.ID].ClearData();
            }
            Networking.GetData(ss);
        }

        /// <summary>
        /// Adds the the client SocketState to the PlayerClients dictionary.
        /// </summary>
        /// <param name="ss">SocketState of the connected client</param>
        private void AddClientSocket(SocketState ss)
        {
            lock (PlayerClients)
            {
                PlayerClients.Add(ss.ID, ss);
            }
        }

        /// <summary>
        /// Adds the name the player has provided to the server to the PlayerNames dictionary.  The key is the automatically generated from the SocketState's ID property.
        /// </summary>
        private string AddPlayerName(SocketState ss)
        {
            string name = ss.GetData();

            //Make sure connecting players cannot inject SQL commands - we just replace any possible SQL syntax characters with a space.
            name = name.Replace(';', ' ');
            name = name.Replace('(', ' ');
            name = name.Replace(')', ' ');

            //Trim the new line from name.
            name = name.Trim();

            if (name.Length == 0)
                return ";";

            lock (ss)
            {
                ss.RemoveData(0, name.Length - 1);
            }
            if (name.Length > 16) name = name.Substring(0, 16);
            return name;
        }

        /// <summary>
        /// Adds the given command string to the dictionary of commands waiting to be processed for this frame.
        /// </summary>
        /// <param name="k">The Client ID</param>
        /// <param name="c">The Command string</param>
        private void AddOrModifyCommand(int k, string c)
        {
            lock (Commands)
            {
                if (Commands.ContainsKey(k))
                {
                    // First make sure that command isn't one of our special ones.
                    if (Commands[k] == TheWorld.DISC_CMD) return;
                    Commands[k] = c;
                }
                else Commands.Add(k, c);
            }
        }

        /// <summary>
        /// Gets and returns the most recent command from a client SocketState.  This ensures that only one command is accepted per client per frame, and that it is always the most recent command.
        /// </summary>
        private string GetSingleCommand(SocketState ss)
        {
            string cmd = ss.GetData();
            string[] parts = Regex.Split(cmd, @"(?<=[\n])");
            for (int i = parts.Length - 1; i >= 0; i--)
            {
                if (parts[i] == "") continue;
                if (parts[i][parts[i].Length - 1] == '\n')
                {
                    cmd = parts[i];
                    break;
                }
            }
            //ss.ClearData();
            return cmd;
        }

        private void StopServer()
        {
            Networking.StopServer(WebListener);
            Networking.StopServer(GameListener);
        }

        /// <summary>
        /// Main entry point for the TankWars server application.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // Create a new server.
            Server s = new Server();
            // Start the server on a new thread.
            Thread t = new Thread(s.StartServerLoop);
            Stopwatch global_timer = new Stopwatch();
            // Start timing the game.
            global_timer.Start();
            // Start the thread.
            t.Start();
            // Hold the console open.
            Console.ReadLine();
            // Close the TcpListeners appropriatly
            s.StopServer();
            // Stop Timing the World
            global_timer.Stop();
            // Abort the thread.
            // NOTE TO GRADERS: We know that in class Professor Kopta said that by terminating the main thread the other threads would automatically close.
            //      However, for some reason the program does not close for us in less we abort the thread that is running StartServerLoop.
            t.Abort();
            // Upload the game to the database
            DatabaseController.UploadGameToSQLDatabase((int)global_timer.ElapsedMilliseconds / 1000, s.TheWorld.GetStats(), s.TheWorld.GetNames());
            return;
        }

    }
}
