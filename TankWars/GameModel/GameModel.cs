//Authors: Gavin Gray, Dan Ruley
//Dec. 2019
using System.Collections.Generic;

namespace TankWars
{
    /// <summary>
    /// The GameModel class represents the basic data that defines the TankWars game world and needs to be shared by both the client and server versions of the world.  The client and server world models inherit from this class.
    /// </summary>
    public class GameModel
    {
        public int TANKWIDTH { protected set; get; }
        public int WALLWIDTH { protected set; get; }
        public int PROJWIDTH { protected set; get; }
        public int TURRSIZE { protected set; get; }
        public int PROJSIZE { protected set; get; }
        public int POWSIZE { protected set; get; }
        public int SIZE { protected set; get; }

        protected Dictionary<int, Tank> Tanks;
        protected Dictionary<int, Projectile> Prjs;
        protected Dictionary<int, Powerup> Pows;
        protected Dictionary<int, Beam> Beams;
        protected Dictionary<int, Wall> Walls;

        /// <summary>
        /// Constructs the Game model with the default constant values.
        /// </summary>
        public GameModel()
        {
            TANKWIDTH = 60;
            WALLWIDTH = 50;
            PROJWIDTH = 0;
            PROJSIZE = 30;
            TURRSIZE = 50;
            POWSIZE = 30;

            Tanks = new Dictionary<int, Tank>();
            Prjs = new Dictionary<int, Projectile>();
            Pows = new Dictionary<int, Powerup>();
            Beams = new Dictionary<int, Beam>();
            Walls = new Dictionary<int, Wall>();
        }

        /// <summary>
        /// Returns a dictionary containing the tanks currently in the world.
        /// </summary>
        public Dictionary<int, Tank> GetTanks()
        {
            return Tanks;
        }

        /// <summary>
        /// Returns a dictionary containing the powerups currently in the world.
        /// </summary>
        public Dictionary<int, Powerup> GetPowerups()
        {
            return Pows;
        }

        /// <summary>
        /// Returns a dictionary containing the projectiles currently in the world.
        /// </summary>
        public Dictionary<int, Projectile> GetProjectiles()
        {
            return Prjs;
        }

        /// <summary>
        /// Returns a dictionary containing the beams currently in the world.
        /// </summary>
        public Dictionary<int, Beam> GetBeams()
        {
            return Beams;
        }

        /// <summary>
        /// Returns a dictionary containing the walls currently in the world.
        /// </summary>
        public Dictionary<int, Wall> GetWalls()
        {
            return Walls;
        }

    }
}
