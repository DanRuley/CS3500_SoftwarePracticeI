//Authors: Gavin Gray, Dan Ruley
//November, 2019
using System;
using System.Collections.Generic;

namespace TankWars
{
    /// <summary>
    /// Class that represents the Game World and contains objects that exist in the world.  Like the server's ServerGameWorld, it is a child of the GameModel class.  This way the client and server worlds only share the things that they both need, and this client world only contains relevant data and functions for the client.
    /// </summary>
    public class GameWorld : GameModel
    {
        //Only used by client
        public readonly int USER_ID;
        private Dictionary<int, Explosion> Explosions;
        public int STARTINGHP { private set; get; }

        /// <summary>
        /// Constructs the GameWorld with a specified size.
        ///
        /// This type of constructor would be used by the client.
        /// </summary>
        /// <param name="size">Size of the world</param>
        public GameWorld(int size, int id) : base()
        {
            SIZE = size;
            USER_ID = id;
            Explosions = new Dictionary<int, Explosion>();
            STARTINGHP = 0;
        }

        /// <summary>
        /// Adds the given object to the game world using its ID as a key if it does not already exist in the world.  If it does exist, updates the Dictionary with the new version of the object.
        /// </summary>
        /// <param name="id">Game Object id</param>
        /// <param name="o">Game Object to add</param>
        public void AddOrUpdateGameObj(int id, Object o)
        {
            if (o is Tank)
            {
                lock (Tanks)
                {
                    if (Tanks.ContainsKey(id))
                        Tanks[id] = (Tank)o;
                    else
                        Tanks.Add(id, (Tank)o);
                }
                if (((Tank)o).hitPoints > STARTINGHP) STARTINGHP = ((Tank)o).hitPoints;
            }

            else if (o is Projectile)
            {
                lock (Prjs)
                {
                    if (Prjs.ContainsKey(id))
                        Prjs[id] = (Projectile)o;
                    else
                        Prjs.Add(id, (Projectile)o);
                }
            }

            else if (o is Powerup)
            {
                lock (Pows)
                {
                    if (Pows.ContainsKey(id))
                        Pows[id] = (Powerup)o;
                    else
                        Pows.Add(id, (Powerup)o);
                }
            }

            else if (o is Beam)
            {
                lock (Beams)
                {
                    if (Beams.ContainsKey(id))
                        Beams[id] = (Beam)o;
                    else
                        Beams.Add(id, (Beam)o);
                }
            }

            else if (o is Wall)
            {
                lock (Walls)
                {
                    if (Walls.ContainsKey(id))
                        Walls[id] = (Wall)o;
                    else
                        Walls.Add(id, (Wall)o);
                }
            }
            //mostly for debug purposes for now
            else if (o is Explosion)
            {
                lock (Explosions)
                {
                    if (!Explosions.ContainsKey(id)) Explosions.Add(id, (Explosion)o);
                }
            }
            else { }

        }

        /// <summary>
        /// Removes the object of the specified type and ID from the Game World.
        /// </summary>
        /// <param name="id">ID of game object to remove.</param>
        /// <param name="t">Type of game object to remove</param>
        public void RemoveGameObj(int id, Type t)
        {
            switch (t.Name)
            {
                case "Tank":
                    RemoveTank(id);
                    break;
                case "Projectile":
                    RemoveProj(id);
                    break;
                case "Powerup":
                    RemovePowerup(id);
                    break;
                case "Beam":
                    RemoveBeam(id);
                    break;
                case "Wall":
                    RemoveWall(id);
                    break;
                case "Explosion":
                    RemoveExplosion(id);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Removes the given explosion from the world.
        /// </summary>
        /// <param name="id">ID of the explosion to remove</param>
        private void RemoveExplosion(int id)
        {
            lock (Explosions)
            {
                Explosions.Remove(id);
            }
        }

        /// <summary>
        /// Removes the given tank from the world.
        /// </summary>
        /// <param name="id">ID of the tank to remove</param>
        private void RemoveTank(int id)
        {
            lock (Tanks)
            {
                if (Tanks.ContainsKey(id)) Tanks.Remove(id);
            }
        }

        /// <summary>
        /// Removes the given projectile from the world.
        /// </summary>
        /// <param name="id">ID of the projectile to remove</param>
        private void RemoveProj(int id)
        {
            lock (Prjs)
            {
                if (Prjs.ContainsKey(id)) Prjs.Remove(id);
            }
        }

        /// <summary>
        /// Removes the given beam from the world.
        /// </summary>
        /// <param name="id">ID of the beam to remove</param>
        private void RemoveBeam(int id)
        {
            lock (Beams)
            {
                if (Beams.ContainsKey(id)) Beams.Remove(id);
            }
        }

        /// <summary>
        /// Removes the given wall from the world.
        /// </summary>
        /// <param name="id">ID of the wall to remove</param>
        private void RemoveWall(int id)
        {
            lock (Walls)
            {
                if (Walls.ContainsKey(id)) Walls.Remove(id);
            }
        }

        /// <summary>
        /// Removes the given powerup from the world.
        /// </summary>
        /// <param name="id">ID of the powerup to remove</param>
        private void RemovePowerup(int id)
        {
            lock (Pows)
            {
                if (Pows.ContainsKey(id)) Pows.Remove(id);
            }
        }

        /// <summary>
        /// Returns a dictionary containing the Explosions currently in the world.
        /// </summary>
        public Dictionary<int, Explosion> GetExplosions()
        {
            return this.Explosions;
        }

    }
}
