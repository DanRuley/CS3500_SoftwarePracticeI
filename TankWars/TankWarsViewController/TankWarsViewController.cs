//Authors Gavin Gray, Dan Ruley
//November 2019
using NetworkUtil;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using TankWars;

namespace ViewController
{
    /// <summary>
    /// This class represents the TankWars Game and View controller.  It interfaces with the TankWars view as well as the game server.  It receives data from the server and constructs the game model and notifies the view when it needs to redraw the world or display other visual information to the client.
    /// </summary>
    public class TankWarsViewController
    {
        private GameWorld TheWorld;
        private SocketState theServer;
        private readonly int _PORT_NUMBER = 11000;
        private string UserName;
        private List<string> MOVING;
        private string FIRE;
        private Vector2D PLAYER_POS;
        private Vector2D MOUSE_COORD;

        // Informs the subscriber that an error was found
        public delegate void ErrorHandler(string ErrMsg);
        public event ErrorHandler OnError;

        // Informs the subsciber that the messages were processed. 
        // This means that a command should be sent to the server and world re-drawn
        public delegate void OnFrameReDraw();
        public event OnFrameReDraw MessagesProcessed;

        // Informs the subscriber that the world is ready and references to it should be updated
        public delegate void WorldCreated(GameWorld gw);
        public event WorldCreated WorldReady;

        // Informs the subscriber that a tank died and an explosion should be started
        public delegate void StartNewExplosion(int id, double x, double y);
        public event StartNewExplosion TankDied;

        /// <summary>
        /// Constructs the view controller.  Takes the player's (x,y) coordinates as a parameter.
        /// </summary>
        /// <param name="x">Player's x position</param>
        /// <param name="y">Player's y position</param>
        public TankWarsViewController(int x, int y)
        {
            MOUSE_COORD = new Vector2D(0, 0);
            PLAYER_POS = new Vector2D(x, y);
            MOVING = new List<string>();
            FIRE = "none";
        }

        /// <summary>
        /// Ends the connection to the Server.
        /// </summary>
        public void EndConnection()
        {
            if (theServer != null && theServer.TheSocket != null)
            {
                theServer.TheSocket.Shutdown(SocketShutdown.Both);
            }
        }

        /// <summary>
        /// Attempts to connect to a server using the Networking API, passing OnConnect in as the delegate.
        /// </summary>
        /// <param name="ServerAddress">Address of the server</param>
        /// <param name="name">User name</param>
        public void Connect(string ServerAddress, string name)
        {
            // If the Username is too long then we want to trim it to be the first 16 chars.
            if (name.Length > 16) name = name.Substring(0, 16);
            else if (name.Length == 0)
            {   // If the username is invalid, send the error and don't try to connect.
                OnError("Invalid Username");
                return;
            }
            else UserName = name;

            // Finally connect to the server.
            Networking.ConnectToServer(OnConnect, ServerAddress, _PORT_NUMBER);
        }

        /// <summary>
        /// Invoked by the SocketState that is created via the Connect method.  Triggers the OnError event if something goes wrong, otherwise it initializes the server and calls SendName with the SocketState.
        /// </summary>
        /// <param name="state">Socket State object</param>
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
            SendName(state);
        }

        /// <summary>
        /// Sends the user name to the server in order to complete the handshake.  Triggers the OnError event if something goes wrong, otherwise it sets the SocketStates network action delegate to InitializeWorld and triggers the event loop with a call to GetData on the SocketState.
        /// </summary>
        private void SendName(SocketState state)
        {
            if (state.ErrorOccured)
            {
                OnError(state.ErrorMessage);
                return;
            }

            state.OnNetworkAction = InitializeWorld;

            // Send Username
            SendMessage(UserName);
            Networking.GetData(state);
        }

        /// <summary>
        /// Processes the message from the server and ensures the client receives enough information to initialize the game world.  Sets the Socket State's network action to ReceiveGameData and triggers the WorldReady event to notify the view that the drawing panel should initialize its reference to the world.
        /// </summary>
        private void InitializeWorld(SocketState state)
        {
            string id = null, size = null;
            GetIDandSize(state, ref id, ref size);

            // We got enough info to start receiveing full messages.
            if (id != null && size != null)
            {
                // Create the world and add our player.
                TheWorld = new GameWorld(int.Parse(size), int.Parse(id));
                //TheWorld.AddPlayer(int.Parse(id));
                state.OnNetworkAction = ReceiveGameData;
                WorldReady(TheWorld);
            }

            Networking.GetData(state);
        }

        /// <summary>
        /// Helper method for parsing the first message sent from the server, retrieving the player id and the world size.
        /// </summary>
        private void GetIDandSize(SocketState state, ref string id, ref string size)
        {
            string[] msg = Regex.Split(state.GetData(), @"(?<=[\n])");

            foreach (string s in msg)
            {
                if (s.Length == 0) continue;
                else if (id == null)
                {
                    id = s;
                    state.RemoveData(0, s.Length);
                }
                else if (size == null)
                {
                    size = s;
                    state.RemoveData(0, s.Length);
                }
                else break;
            }
        }

        /// <summary>
        /// Main driver method for the server <--> client messaging loop.  It triggers the OnError event if something went wrong with the connection.  Otherwise, it sends player commands to the server, processes this frames server messages, and informs the view to redraw the world with the MessagesProcessed event.  Lastly, it continues the event loop with a call to GetData.
        /// </summary>
        private void ReceiveGameData(SocketState state)
        {
            if (state.ErrorOccured)
            {
                OnError(state.ErrorMessage);
                return;
            }
            // Inform the server of the new position of the player.
            SendCommandToServer();
            // Tell the view that the game data was processed, so the world can be redrawn.
            MessagesProcessed();
            // We need to process the game data.
            ProcessGameData(state);
            // Continue the event loop
            Networking.GetData(state);
        }

        /// <summary>
        /// Parses the latest batch of data sent by the server.  Deserializes the JSON messages, creating or removing Game World Objects as needed.   
        /// </summary>
        private void ProcessGameData(SocketState state)
        {
            string totalData = state.GetData();

            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            foreach (string p in parts)
            {
                // Ignore empty strings added by the regex splitter
                if (p.Length == 0)
                    continue;
                // The regex splitter will include the last string even if it doesn't end with a '\n',
                // So we need to ignore it if this happens. 
                if (p[p.Length - 1] != '\n')
                    break;

                if (p.Contains("tank"))
                {
                    Tank tank = JsonConvert.DeserializeObject<Tank>(p);
                    if (tank.died || tank.disconnected || tank.hitPoints == 0)
                    {
                        TheWorld.RemoveGameObj(tank.ID, tank.GetType());
                        if (tank.died || tank.disconnected) TankDied(tank.ID, tank.location.GetX(), tank.location.GetY());
                    }
                    else TheWorld.AddOrUpdateGameObj(tank.ID, tank);
                }

                else if (p.Contains("proj"))
                {
                    Projectile proj = JsonConvert.DeserializeObject<Projectile>(p);
                    if (proj.died) TheWorld.RemoveGameObj(proj.ID, proj.GetType());
                    else TheWorld.AddOrUpdateGameObj(proj.ID, proj);
                }

                else if (p.Contains("beam"))
                {
                    Beam beam = JsonConvert.DeserializeObject<Beam>(p);
                    TheWorld.AddOrUpdateGameObj(beam.ID, beam);
                }

                else if (p.Contains("power"))
                {
                    Powerup powe = JsonConvert.DeserializeObject<Powerup>(p);
                    if (powe.died) TheWorld.RemoveGameObj(powe.ID, powe.GetType());
                    else TheWorld.AddOrUpdateGameObj(powe.ID, powe);
                }

                else if (p.Contains("wall"))
                {
                    Wall wall = JsonConvert.DeserializeObject<Wall>(p);
                    TheWorld.AddOrUpdateGameObj(wall.ID, wall);
                }
                else { } // If this Else is reached that means an object was received that is not recognized. Therefore I will IGNORE it.
                state.RemoveData(0, p.Length);
            }
        }

        /// <summary>
        /// Send the tank command to the server
        /// </summary>
        /// <param name="fire">String indicating whether the tank is firing or not, and which type of fire it is.</param>
        /// <param name="cur_x">Mouse X coord</param>
        /// <param name="cur_y">Mouse Y coord</param>
        /// <param name="win_x">Middle of the Client view X coord</param>
        /// <param name="win_y">Middle of the Client view Y coord</param>
        public void SendCommandToServer()
        {
            // If the tank hasn't respawned yet we don't want to try sending data about it.
            if (!TheWorld.GetTanks().ContainsKey(TheWorld.USER_ID))
                return;
            string moving;
            // If there is nothing in the list then we stay still
            if (MOVING.Count == 0) moving = "none";
            else if (MOVING[MOVING.Count - 1] == "W") moving = "up";
            else if (MOVING[MOVING.Count - 1] == "S") moving = "down";
            else if (MOVING[MOVING.Count - 1] == "A") moving = "left";
            else if (MOVING[MOVING.Count - 1] == "D") moving = "right";
            else
            {
                MOVING.RemoveAt(MOVING.Count - 1); // Remove the problematic letter. This code should NEVER run.
                moving = "none"; // just stay where we were.
            }

            // Get the new direction of the turret based off of the cursor position
            // **NOTE: the orig, is the center of the drawing panel. We use this point because the
            //      player tank is always centered in the drawing panel. Therefore, we can derive the 
            //      the new direction assuming that the tank is in fact at the center position.
            Vector2D turr = MOUSE_COORD - PLAYER_POS;
            turr.Normalize();
            string msg = JsonConvert.SerializeObject(new Command(moving, FIRE, turr));
            SendMessage(msg);
        }

        /// <summary>
        /// Sends the input string to theServer, triggers the OnError event if something goes wrong.
        /// </summary>
        /// <param name="message">Message to send</param>
        internal void SendMessage(string message)
        {
            if (theServer != null)
                Networking.Send(theServer.TheSocket, message += "\n");
            else
                OnError("Not Connected");
        }

        /// <summary>
        /// Triggered by the view when it detects a keypress, calls AddKeyToPressed.
        /// </summary>
        /// <param name="k">Key that was pressed.</param>
        public void KeyPressed(string k)
        {
            switch (k)
            {
                case "W":
                    goto case "D";
                case "A":
                    goto case "D";
                case "S":
                    goto case "D";
                case "D":
                    AddKeyToPressed(k);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Triggered by the view when it detects a key release.  Calls RemoveKeyFromPressed so that it is removed from the list of pressed keys.
        /// </summary>
        public void KeyReleased(string k)
        {
            switch (k)
            {
                case "W":
                    goto case "D";
                case "A":
                    goto case "D";
                case "S":
                    goto case "D";
                case "D":
                    RemoveKeyFromPressed(k);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Adds a key to the list of pressed keys
        /// </summary>
        /// <param name="key">Key to add</param>
        private void AddKeyToPressed(string key)
        {
            lock (this.MOVING)
            {
                if (!MOVING.Contains(key)) MOVING.Add(key);

            }
        }

        /// <summary>
        /// Removes a key from the list of pressed keys.
        /// </summary>
        /// <param name="key">Key to remove</param>
        private void RemoveKeyFromPressed(string key)
        {
            lock (this.MOVING)
            {
                if (MOVING.Contains(key)) MOVING.Remove(key);
            }
        }

        /// <summary>
        /// Triggered by the view when it detects a mouse click from the user.  Sets the FIRE command based on whether it was a left or right mouse click.
        /// </summary>
        /// <param name="side">The mouse button which was pressed</param>
        public void MouseDown(string side)
        {
            lock (FIRE)
            {
                if (side == "left") FIRE = "main";
                else if (side == "right") FIRE = "alt";
                else FIRE = "none";
            }
        }

        /// <summary>
        /// Triggered by the view when the user releases the mouse button.  Sets FIRE to none.
        /// </summary>
        public void MouseUp()
        {
            lock (FIRE)
            {
                FIRE = "none";
            }
        }

        /// <summary>
        /// Sets MOUSE_COORD to the input x,y pair.
        /// </summary>
        public void MouseCoordChange(int x, int y)
        {
            MOUSE_COORD = new Vector2D(x, y);
        }
    }
}
