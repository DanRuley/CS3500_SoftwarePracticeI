//Authors: Dan Ruley, Gavin Gray
//Dec. 2019
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace TankWars
{

    /// <summary>
    /// This class represents the server's model of the game world.  Like the client's GameWorld, it is a child of the GameModel class.  This way the client and server worlds only share the things that they both need, and this server world only contains relevant data and functions for the server.
    /// </summary>
    public class ServerGameWorld : GameModel
    {
        public int MSPERFRAME { private set; get; }
        public int RESPAWNRATE { private set; get; }
        public int FRAMESPERSHOT { private set; get; }
        public int STARTINGHP { private set; get; }
        public double PRJSPEED { private set; get; }
        public double TANKSPEED { private set; get; }
        public int MAXPOW { private set; get; }
        public int MAXPOWDELAY { private set; get; }
        public int WALLIDCOUNT { private set; get; }
        public int MAX_POWERUPS { private set; get; }

        private Dictionary<int, int> DiedTanks;
        private Dictionary<int, int> FiredTanks;
        private Dictionary<string, Vector2D> DIRECTIONS;
        private Dictionary<int, string> PlayerNames;
        private Dictionary<int, GameStat> PlayerStats;
        private HashSet<int> Tanks_with_powerup;
        private List<int> TANKS_TO_REMOVE;

        private Random rng;
        readonly public string DISC_CMD = "d";


        public ServerGameWorld() : base()
        {
            WALLIDCOUNT = 0;

            //initialize the constant properties; however, they may be overriden if provided in XML file.
            PRJSPEED = 25;
            TANKSPEED = 2.9;
            MAXPOW = 3;
            MAXPOWDELAY = 1650;
            STARTINGHP = 3;

            rng = new Random(Guid.NewGuid().GetHashCode());
            DiedTanks = new Dictionary<int, int>();
            FiredTanks = new Dictionary<int, int>();
            Tanks_with_powerup = new HashSet<int>();
            DIRECTIONS = new Dictionary<string, Vector2D>(){
                { "up", new Vector2D(0, -1) },
                { "down", new Vector2D(0, 1) },
                { "right", new Vector2D(1, 0) },
                { "left", new Vector2D(-1, 0) },
                { "none", new Vector2D(0, 0) }
            };
            TANKS_TO_REMOVE = new List<int>();
            PlayerNames = new Dictionary<int, string>();
            PlayerStats = new Dictionary<int, GameStat>();
            DISC_CMD = (DISC_CMD + rng.Next().ToString()).GetHashCode().ToString();
        }

        // *********************************************************
        // ****************** SERVER ONLY CODE *********************
        // *********************************************************

        /// <summary> 
        /// Returns an IEnumerable consisting of the JSON serialized representations of this Game World's walls.  
        /// </summary> 
        public IEnumerable<string> GetSerializedWalls()
        {
            HashSet<string> Serialized_Walls = new HashSet<string>();
            lock (Walls)
            {
                foreach (Wall w in Walls.Values)
                {
                    Serialized_Walls.Add(JsonConvert.SerializeObject(w));
                }
            }
            return Serialized_Walls;
        }


        // *********************************************************
        // ******************** UTILITY METHODS ********************
        // *********************************************************

        /// <summary>
        /// This method will reset the dictionary of Beams in the GameWorld to Zero.
        /// This needs to be called at the beginning of every frame so that no beam is broadcasted twice.
        /// </summary>
        private void RefreshBeams()
        {

            // Set Beams to a new empty dictionary.
            Beams = new Dictionary<int, Beam>();

        }

        /// <summary>
        /// Method used to make the tanks wraparound to the other side of the map if they go off the edge.
        /// </summary>
        /// <param name="loc"></param>
        /// <returns></returns>
        private Vector2D GetPointInsideWorld(Vector2D loc)
        {
            double x = loc.GetX();
            double y = loc.GetY();
            // If it's off on the left side set it equal to the right side.
            if (x < -SIZE / 2) x = SIZE / 2;
            // Else if it was off on the right side make it wrap to the left side.
            else if (x > SIZE / 2) x = -SIZE / 2;
            // If it's off on the top (bottom) of the map then we make it wrap to the other side.
            if (y < -SIZE / 2) y = SIZE / 2;
            // If it's off on the bottom (top) of the map then we make it wrap to the other side.
            else if (y > SIZE / 2) y = -SIZE / 2;

            return new Vector2D(x, y);
        }

        /// <summary>
        /// Generates a new pair of coordinates that does not conflict with any pre-existing objects.
        /// </summary>
        /// <returns></returns>
        private Vector2D GetNewCoords()
        {
            Vector2D respawn_coords;
            do
            {
                // Get a random pair of coordinates.
                respawn_coords = new Vector2D(rng.Next(-SIZE / 2, SIZE / 2), rng.Next(-SIZE / 2, SIZE / 2));
            }
            // While it is conflicting with any of the objects in the world then we will get a new pair of coordinates.
            while (!(CheckPointWithWalls(TANKWIDTH, respawn_coords) && CheckPointWithTanks(TANKWIDTH, respawn_coords) && CheckPointWithProjectiles(respawn_coords, 5)));
            return respawn_coords;
        }

        /// <summary>
        /// Spawns a new Powerup if there are less on the screen than the max number
        /// </summary>
        private void SpawnNewPowerup()
        {
            lock (Pows)
            {
                // If there is already the max amount of powerups on the screen then we won't generate a new powerup
                if (Pows.Count >= MAXPOW) return;
                // ELSE get a pair of randomly generated coordinates, and add the new powerup
                Powerup p = new Powerup(GetNewCoords());
                Pows.Add(p.ID, p);
            }
        }

        /// <summary>
        /// 1/500 chance that a new powerup will be spawned every frame.
        /// </summary>
        private void RandomPowerup()
        {
            int rand = rng.Next(MAXPOWDELAY);
            // The IF statement is supposed to add some randomness into when the powerups respawn.
            if (rand == 0) SpawnNewPowerup();
        }

        // *********************************************************
        // ****************** END UTILITY METHODS ******************
        // *********************************************************





        // *********************************************************
        // ****************** COLLISION DETECTION ******************
        // *********************************************************

        /// <summary>
        /// Determines if a ray interescts a circle
        /// </summary>
        /// <param name="rayOrig">The origin of the ray</param>
        /// <param name="rayDir">The direction of the ray</param>
        /// <param name="center">The center of the circle</param>
        /// <param name="r">The radius of the circle</param>
        /// <returns></returns>
        public static bool Intersects(Vector2D rayOrig, Vector2D rayDir, Vector2D center, double r)
        {
            // ray-circle intersection test
            // P: hit point
            // ray: P = O + tV
            // circle: (P-C)dot(P-C)-r^2 = 0
            // substitute to solve for t gives a quadratic equation:
            // a = VdotV
            // b = 2(O-C)dotV
            // c = (O-C)dot(O-C)-r^2
            // if the discriminant is negative, miss (no solution for P)
            // otherwise, if both roots are positive, hit

            double a = rayDir.Dot(rayDir);
            double b = ((rayOrig - center) * 2.0).Dot(rayDir);
            double c = (rayOrig - center).Dot(rayOrig - center) - r * r;

            // discriminant
            double disc = b * b - 4.0 * a * c;

            if (disc < 0.0)
                return false;

            // find the signs of the roots
            // technically we should also divide by 2a
            // but all we care about is the sign, not the magnitude
            double root1 = -b + Math.Sqrt(disc);
            double root2 = -b - Math.Sqrt(disc);

            return (root1 > 0.0 && root2 > 0.0);
        }

        /// <summary>
        /// Check all the tanks to see if the new beam intersects any of them.
        /// </summary>
        /// <param name="rayOrig">Origin of the new Beam</param>
        /// <param name="rayDir">Direction of the new Beam</param>
        /// <param name="owner">Owner of the Beam.</param>
        private void CheckBeamWithAllTanks(Vector2D rayOrig, Vector2D rayDir, int owner)
        {
            lock (Tanks)
            {
                foreach (Tank t in Tanks.Values)
                {
                    // Check if the ray intersects the tank.
                    if (Intersects(rayOrig, rayDir, t.location, TANKWIDTH / 2))
                    {
                        //Tanks[owner].IncrementHit();
                        lock (PlayerStats) PlayerStats[owner].Score();
                        // Destroy the tank.
                        t.DepleteHP();
                        // Increase the score of the owner.
                        Tanks[owner].IncreaseScore();
                        // Add the tank to the died tanks.
                        DiedTanks.Add(t.ID, 0);
                    }
                }
            }
        }

        /// <summary>
        /// Method to check the new tank location with all the powerups.
        /// 
        /// If the tank did collide then we want to remove the powerup and indicate that the tank has a powerup available for use.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="loc"></param>
        private void CheckTankWithPowerups(int id, Vector2D loc)
        {
            lock (Pows)
            {
                foreach (Powerup p in Pows.Values)
                {
                    // If the tank is within proximity to the powerup
                    if ((p.location - loc).Length() <= TANKWIDTH / 2)
                    {
                        // We set the powerup to died, so the clients remove it.
                        Pows[p.ID].Kill();
                        // and we indicate that the tank has an available powerup
                        Tanks_with_powerup.Add(id);
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the input Vector2D collides with any tanks.
        /// </summary>
        private bool CheckPointWithTanks(int obj_width, Vector2D coords, bool proj = false, int owner = -1)
        {
            lock (Tanks)
            {
                foreach (Tank t in Tanks.Values)
                {
                    if (t.died || t.hitPoints == 0 || t.disconnected) continue;
                    //Make sure respawning tank is not overlapping with an existing tank
                    Vector2D result = coords - t.location;
                    if (result.Length() < TANKWIDTH / 2 + obj_width)
                    {
                        // If the method caller indicated that it is a projectile that we are checking with the tank by setting PROJ=TRUE;
                        if (proj)
                        {
                            //Tanks[owner].IncrementHit();
                            lock (PlayerStats) PlayerStats[owner].Hit();
                            // If DecreaseHP returns true then the tank died.
                            if (t.DecreaseHP())
                            {
                                lock (PlayerStats) PlayerStats[owner].Score();
                                // Increase the score of the shooter.
                                Tanks[owner].IncreaseScore();
                                // Add the tank as dead
                                lock (DiedTanks) DiedTanks.Add(t.ID, 0);
                            }
                        }
                        // False indicates that the objects are considered to be touching.
                        return false;
                    }
                }
            }
            // We return true here indicating that the given point is not close to any of the tanks.
            return true;
        }

        /// <summary>
        /// Checks whether the input Vector2D collides with any game walls.
        /// </summary>
        private bool CheckPointWithWalls(int obj_width, Vector2D coords)
        {
            lock (Walls)
            {
                foreach (Wall w in Walls.Values)
                {
                    //Calculate the wall spawn in the y direction; return false if the respawn coords overlap in this direction
                    double ty1 = coords.GetY() - (obj_width / 2);
                    double ty2 = coords.GetY() + (obj_width / 2);
                    double highwy = ((w.p1.GetY() > w.p2.GetY()) ? w.p1.GetY() : w.p2.GetY()) + WALLWIDTH / 2;
                    double lowwy = ((w.p1.GetY() < w.p2.GetY()) ? w.p1.GetY() : w.p2.GetY()) - WALLWIDTH / 2;
                    // If the lowest edge is greater than the highest, or highest is greater than the lowest they cannot overlap
                    if (ty1 > highwy || ty2 < lowwy) continue;

                    //Calculate the wall spawn in the x direction; return false if the respawn coords overlap in this direction
                    double tx1 = coords.GetX() - (obj_width / 2);
                    double tx2 = coords.GetX() + (obj_width / 2);
                    double highwx = ((w.p1.GetX() > w.p2.GetX()) ? w.p1.GetX() : w.p2.GetX()) + WALLWIDTH / 2;
                    double lowwx = ((w.p1.GetX() < w.p2.GetX()) ? w.p1.GetX() : w.p2.GetX()) - WALLWIDTH / 2;
                    // Same logic here
                    if (tx1 > highwx || tx2 < lowwx) continue;
                    // If neither of the above statements is true then there must be an overlap.
                    return false;
                }
            }
            // If the function has not returned yet that indicates that the coordinate is not overlapping with any of the walls.
            return true;
        }

        /// <summary>
        /// Checks whether the input Vector2D collides with any game projectiles.
        /// </summary>
        /// <param name="coords"></param>
        /// <param name="frames_ahead"></param>
        /// <returns></returns>
        private bool CheckPointWithProjectiles(Vector2D coords, int frames_ahead = 0)
        {
            lock (Prjs)
            {
                foreach (Projectile p in Prjs.Values)
                {
                    Vector2D temp_prj = new Vector2D(p.location);

                    //If this method is called for a regular collision detection, frames ahead will be 0
                    //If it's called for a respawning tank, make sure the tank will not collide with the proj
                    //in the next several frames
                    while (frames_ahead-- >= 0)
                    {
                        Vector2D result = coords - temp_prj;
                        //if (Math.Abs(result.GetX()) < TANKWIDTH / 2 && Math.Abs(result.GetY()) < TANKWIDTH / 2)
                        // If this condition is true then the coordinates are too close to a projectile and we return false.
                        if (result.Length() <= TANKWIDTH / 2)
                            return false;
                        temp_prj = temp_prj + (p.orientation * PRJSPEED);
                    }
                }
            }
            return true;
        }

        // *********************************************************
        // **************** END COLLISION DETECTION ****************
        // *********************************************************



        // *********************************************************
        // ***************** OBJECT UPDATE METHODS *****************
        // *********************************************************

        /// <summary>
        /// We have a dictionary that keeps track of how many frames a tank has been died.
        /// 
        /// In this method if that dictionary indicates that the tank has waited long enough we will respawn the tank
        ///     and remove it from the dictionary.
        /// If a tank has not waited long enough then we increase its wait time to indicate that it has waited one additional frame.
        /// </summary>
        private void UpdateAllDeadTanksAndRespawnIfNecessary()
        {
            // List of tanks that aren't ready to respawn, but we need to increase their count.
            List<int> tanks_to_increase = new List<int>();
            // Tanks that have waited long enough and should be respawned.
            List<int> tanks_to_remove = new List<int>();
            foreach (int id in DiedTanks.Keys)
            {
                // If the tank has waited as long as the respawn rate.
                if (DiedTanks[id] == RESPAWNRATE)
                {
                    // Respawn the tank.
                    RespawnTank(id);
                    // Remove it from the waiting dictionary.
                    tanks_to_remove.Add(id);
                }
                // Otherwise we need to increase the time that it has waited.
                else tanks_to_increase.Add(id);
            }
            foreach (int id in tanks_to_remove)
            {
                // Remove tank from waiting dictionary.
                if (Tanks_with_powerup.Contains(id)) Tanks_with_powerup.Remove(id);
                DiedTanks.Remove(id);
            }
            foreach (int id in tanks_to_increase)
            {
                // Set died to false, so that it is only broadcasted on one frame only.
                lock (Tanks) Tanks[id].DiedToFalse();
                // Increase the time that the tank has been waiting.
                DiedTanks[id]++;
            }

        }

        /// <summary>
        /// Update the position of all projectiles.
        /// 
        /// Check to see whether they have collided with any other game object, a.k.a. if they should die or not.
        /// </summary>
        private void UpdateProjectiles()
        {
            // To hold the projectiles that died on the previous frame and need to be removed on this frame.
            HashSet<int> ProjsToRemove = new HashSet<int>();

            lock (Prjs)
            {
                foreach (int id in Prjs.Keys)
                {
                    //add previously dead projectiles
                    if (Prjs[id].died)
                    {
                        ProjsToRemove.Add(id);
                        continue;
                    }
                    // Get the projectiles new location.
                    Vector2D new_loc = Prjs[id].location + (Prjs[id].orientation * PRJSPEED);
                    // Check the new location with the walls, tanks, and game world edges.
                    if (!CheckPointWithWalls(PROJWIDTH, new_loc) || Math.Abs(new_loc.GetX()) > SIZE / 2 || Math.Abs(new_loc.GetY()) > SIZE / 2 || !CheckPointWithTanks(PROJWIDTH, new_loc, true, Prjs[id].owner))
                    {
                        // If the projectile hit any of the above we need to set it to died.
                        Prjs[id].Died();
                    }
                    // Lastly we set the new location. Regardless of if it hits anything or not, because it still needs to travel forward and hit the tank/wall/GameEdge
                    Prjs[id].SetLocation(new_loc);
                }

                //Remove projectiles that are dead and have been previously broadcasted to the clients.
                foreach (int id in ProjsToRemove)
                {
                    Prjs.Remove(id);
                }
            }
        }

        /// <summary>
        ///Creates a random pair of coordinates and checks them for collisions with walls, tanks, and projectiles.  If the coordinates do not collide with any of these, we respawn the tank with these coordinates.
        /// </summary>
        /// <param name="id"></param>
        private void RespawnTank(int id)
        {
            // Get a new pair of randomly generated coordinates.
            Vector2D respawn_coords = GetNewCoords();
            lock (Tanks)
            {
                if (Tanks.ContainsKey(id))
                {
                    Tanks[id].SetLocation(respawn_coords);
                    Tanks[id].ResetHP();
                }
            }
        }

        /// <summary>
        /// Removes a tank that was previously disconnected and broadcasted to the remaining clients.
        /// </summary>
        private void RemovePreviouslyDisconnected()
        {
            lock (Tanks)
            {
                foreach (int k in TANKS_TO_REMOVE)
                {
                    // If the tank was previously broadcasted as disconnected, we want to remove it. 
                    if (Tanks.ContainsKey(k)) Tanks.Remove(k);
                }
            }
            // If the disconnected tank was dead when it was disconnected then we want to remove it from the diedtanks dicitonary as well.
            //      if we didn't remove it then it would respawn after the allotted time even though it was disconnected.

            foreach (int k in TANKS_TO_REMOVE)
            {
                if (DiedTanks.ContainsKey(k)) DiedTanks.Remove(k);
            }

            TANKS_TO_REMOVE = new List<int>();
        }

        /// <summary>
        /// Removes all the previously died projectiles
        /// </summary>
        private void RemoveDeadPowerups()
        {
            lock (Pows)
            {
                // Keep list of id's that are to be removed.
                List<int> pows_to_remove = new List<int>();
                foreach (Powerup p in Pows.Values)
                {
                    if (p.died) pows_to_remove.Add(p.ID);
                }
                // Remove those stashed id's
                foreach (int i in pows_to_remove)
                {
                    Pows.Remove(i);
                }
            }
        }

        /// <summary>
        /// Updates the fired count when a tank successfully fires a beam or projectile.
        /// </summary>
        /// <param name="id"></param>
        private void UpdateTankAccuracy(int id)
        {
            lock (PlayerStats) PlayerStats[id].Shot();
        }
        // *********************************************************
        // ************** END OBJECT UPDATE METHODS ****************
        // *********************************************************






        // *********************************************************
        // **************** CMD PROCESSING METHODS *****************
        // *********************************************************

        /// <summary>
        /// This method parses all the commands and checks whether or not a tank needs to be respawned, disconnected, or a normal command.
        /// </summary>
        /// <param name="cmds"></param>
        private void ParseAllCommands(Dictionary<int, string> cmds)
        {
            try
            {
                foreach (int id in cmds.Keys)
                {
                    // If the tank has disconnected, then we need to disconnect the tank, and store it's ID so that we can remove it next frame.
                    if (cmds[id] == DISC_CMD)
                    {
                        lock (Tanks) Tanks[id].DisconnectTank();
                        TANKS_TO_REMOVE.Add(id);
                    }
                    // If it's not either of the first two then it is a regular command and we shall treat it like so.
                    // if the protocal is broken or another command is found this will throw an error and the Exception will be ignored.
                    else
                    {
                        // Deserialize the command.
                        Command c = JsonConvert.DeserializeObject<Command>(cmds[id]);
                        // Update the game objects from the command.
                        UpdateGameObjectsFromCommand(id, c);
                    }
                }
            }
            // If the command is not recognized, or if there is an error parsing it we will ignore it.
            catch (Exception) { }
        }

        /// <summary>
        /// This method takes in a regular Game command and updates the objects accordingly.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="c"></param>
        private void UpdateGameObjectsFromCommand(int id, Command c)
        {
            Vector2D new_loc;
            lock (Tanks)
            {
                // Don't parse commands for a dead tank.
                if (!Tanks.ContainsKey(id) || Tanks[id].hitPoints == 0) return;
                // Get the new location for the tank.
                new_loc = GetPointInsideWorld(Tanks[id].location + (DIRECTIONS[c.move] * TANKSPEED));
                // If it DOESN'T collide with a wall then the new location will be set.
                if (CheckPointWithWalls(TANKWIDTH, new_loc)) Tanks[id].SetLocation(new_loc);
                // Else it will not be set to the new location.
                else new_loc = Tanks[id].location;

                // If the move command is "none" then we don't want to update its orientation (orientation of the tank body).
                if (c.move != "none") Tanks[id].SetDirection(DIRECTIONS[c.move]);
                // We will always set the turret direction.
                Tanks[id].SetTurret(c.dir);
            }
            // Check the tankn location with the powerups to see if the tank got a powerup.
            CheckTankWithPowerups(id, new_loc);


            // Get how many frames have passed since the tank last fired.
            int last_fire = 0;
            if (FiredTanks.ContainsKey(id)) last_fire = FiredTanks[id];
            else FiredTanks.Add(id, 0);
            // If the fired was main and it's been long enough we can go ahead and spawn a new projectile.
            if (c.fire == "main" && last_fire >= FRAMESPERSHOT)
            {
                UpdateTankAccuracy(id);
                // Indicate that it has been ZERO frames since the tank last fired.
                FiredTanks[id] = 0;
                lock (Prjs)
                {
                    // Create a new projectile and add it to the game.
                    Projectile p = new Projectile(new_loc + (Tanks[id].aiming * (TANKWIDTH / 2)), c.dir, id);
                    Prjs.Add(p.ID, p);
                }
            }
            // If the fire was the alternative
            else if (c.fire == "alt")
            {
                // If the tank has an available powerup.
                if (Tanks_with_powerup.Contains(id))
                {
                    UpdateTankAccuracy(id);
                    // create a new beam.
                    Beam b = new Beam(id, c.dir, new_loc);
                    // Add the beam.
                    lock (Beams) Beams.Add(b.ID, b);
                    // Indicate that the tank has used its powerup by removing it from the list.
                    Tanks_with_powerup.Remove(id);
                    // Check the Beam with all the tanks to see if any of them were hit.
                    CheckBeamWithAllTanks(new_loc, c.dir, id);
                }
            }
            // We always want to increase the frames that the tank has been waiting.
            FiredTanks[id]++;
        }

        // *********************************************************
        // ************ END CMD PROCESSING METHODS *****************
        // *********************************************************





        // *********************************************************
        // ****************** SERVER USED METHODS ******************
        // *********************************************************

        /// <summary>
        /// MAIN METHOD THAT NEEDS TO BE CALLED IN ORDER TO COMPLETELY UPDATE THE GAME WORLD.
        /// 
        /// A dictionary of commands is passed in from the server controller indicating what the clients desire to do.
        /// First we need to remove all the beams from the previous Frame.
        /// Then we remove all the Tanks that were previously disconnected.
        /// Then Remove all the dead projectiles.
        /// Then update the condition of the dead tanks.
        /// Parse the commands given by the server controller.
        /// Update the projectiles in flight and check for collisions with tanks.
        /// Lastly we check to see if we can spawn a new powerup in the world.
        /// </summary>
        /// <param name="cmds"></param>
        public void UpdateGameWorld(Dictionary<int, string> cmds)
        {
            // Remove all the beams that were fired on the previous frame.
            RefreshBeams();

            // Remove all Previously disconnected 
            RemovePreviouslyDisconnected();

            // Remove all the died objects from the previous frame.
            RemoveDeadPowerups();

            // Respawn all dead tanks that have waited long enough & remove from waiting Dictionary
            UpdateAllDeadTanksAndRespawnIfNecessary();

            // Parse all valid commands
            ParseAllCommands(cmds);

            // Update all Projectiles while Checking for collision with Walls/Tanks
            UpdateProjectiles();

            // See if we can spawn a new powerup
            RandomPowerup();
        }

        public void AddNewPlayer(int id, string name)
        {
            lock (PlayerNames)
            {
                PlayerNames.Add(id, name);
            }
            lock (PlayerStats)
            {
                PlayerStats.Add(id, new GameStat());
            }
            lock (Tanks)
            {
                Tanks.Add(id, new Tank(id, GetNewCoords(), name, STARTINGHP));
            }
        }

        /// <summary>
        /// Method returns the entire world with all of it's objects as a string that is in accordance with our protocol
        /// </summary>
        /// <returns></returns>
        public string GetEntireWorldAsJsonMsg()
        {
            StringBuilder msg = new StringBuilder();

            // Add all the tanks to the string.
            lock (Tanks)
            {
                foreach (Tank t in Tanks.Values)
                {
                    msg.Append(JsonConvert.SerializeObject(t) + '\n');
                }
            }
            // Add all the projectiles to the string.
            lock (Prjs)
            {
                foreach (Projectile p in Prjs.Values)
                {
                    msg.Append(JsonConvert.SerializeObject(p) + '\n');
                }
            }
            // Add all the powerups to the string.
            lock (Pows)
            {
                foreach (Powerup p in Pows.Values)
                {
                    msg.Append(JsonConvert.SerializeObject(p) + '\n');
                }
            }
            // Add all the beams to the string.
            lock (Beams)
            {
                foreach (Beam b in Beams.Values)
                {
                    msg.Append(JsonConvert.SerializeObject(b) + '\n');
                }
            }
            // Return final message.
            return msg.ToString();
        }

        /// <summary>
        /// Parses the XML settings file for the game to determine the constant values and wall positions for the game world.  For customizable properties, please consult the README for expected XML tag format.
        /// </summary>
        /// <param name="filename">Filepath for the xml settings</param>
        public void ReadWorldSettings(string filename)
        {
            XmlReader reader = null;
            try
            {
                reader = XmlReader.Create(filename);
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (reader.Name.ToLower())
                        {
                            case "wall":
                                AddWallToWorld(reader);
                                break;
                            case "universesize":
                                reader.Read();
                                SIZE = int.Parse(reader.Value);
                                break;
                            case "msperframe":
                                reader.Read();
                                MSPERFRAME = int.Parse(reader.Value);
                                break;
                            case "framespershot":
                                reader.Read();
                                FRAMESPERSHOT = int.Parse(reader.Value);
                                break;
                            case "respawnrate":
                                reader.Read();
                                RESPAWNRATE = int.Parse(reader.Value);
                                break;
                            case "tankspeed":
                                reader.Read();
                                TANKSPEED = double.Parse(reader.Value);
                                break;
                            case "projectilespeed":
                                reader.Read();
                                PRJSPEED = double.Parse(reader.Value);
                                break;
                            case "maxpowerupcount":
                                reader.Read();
                                MAXPOW = int.Parse(reader.Value);
                                break;
                            case "maxpowerupdelay":
                                reader.Read();
                                MAXPOWDELAY = int.Parse(reader.Value);
                                break;
                            case "startinghp":
                                reader.Read();
                                STARTINGHP = int.Parse(reader.Value);
                                break;
                            case "wallwidth":
                                reader.Read();
                                WALLWIDTH = int.Parse(reader.Value);
                                break;
                            case "tankwidth":
                                reader.Read();
                                TANKWIDTH = int.Parse(reader.Value);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            finally
            {
                if (reader != null)
                    reader.Dispose();
            }
        }

        // This method is not public to the server, but it technically is part of the ReadWorldSettings method.
        /// <summary>
        /// Helper function that parses and constructs the walls for the game world.
        /// </summary>
        private void AddWallToWorld(XmlReader reader)
        {
            Vector2D start = null;
            Vector2D end = null;
            try
            {
                double x = Double.NaN;
                double y = Double.NaN;
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        //Read the start/end coordinates for this wall
                        switch (reader.Name.ToLower())
                        {
                            case "p1":
                                break;
                            case "p2":
                                break;
                            case "x":
                                reader.Read();
                                x = Double.Parse(reader.Value);
                                break;
                            case "y":
                                reader.Read();
                                y = Double.Parse(reader.Value);
                                break;
                            default:
                                break;
                        }
                    }

                    else
                    {
                        //end element for wall
                        if (reader.Name.ToLower() == "wall")
                        {
                            if (start == null || end == null)
                                throw new Exception("Invalid wall format in XML file.");
                            Wall w = new Wall(start, end, WALLIDCOUNT++);
                            Walls.Add(w.ID, w);
                            return;
                        }
                        //end element for p1
                        if (reader.Name.ToLower() == "p1")
                        {
                            if (x == Double.NaN || y == Double.NaN)
                                throw new Exception("Not enough information to create a point");
                            start = new Vector2D(x, y);
                            x = Double.NaN;
                            y = Double.NaN;
                        }
                        //end element for p2
                        if (reader.Name.ToLower() == "p2")
                        {
                            if (x == Double.NaN || y == Double.NaN)
                                throw new Exception("Not enough information to create a point.");
                            end = new Vector2D(x, y);
                            x = Double.NaN;
                            y = Double.NaN;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Getter for the GameStat dictionary so we can upload them to the SQL database.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, GameStat> GetStats()
        {
            return PlayerStats;
        }

        /// <summary>
        /// Getter for the PlayerNames dictionary so we can upload them to the SQL database.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, string> GetNames()
        {
            return PlayerNames;
        }

        // *********************************************************
        // ******************* END SERVER METHODS ******************
        // *********************************************************

        /// <summary>
        /// Small class that defines the game stats for a player.
        /// These stats persist throughout the game even if the player is disconnected. 
        /// </summary>
        public class GameStat
        {
            public int hits { private set; get; }
            public int shots { private set; get; }
            public int score { private set; get; }
            public GameStat() { hits = shots = score = 0; }
            public void Hit() { hits++; }
            public void Shot() { shots++; }
            public void Score() { score++; }
        }
    }

}

