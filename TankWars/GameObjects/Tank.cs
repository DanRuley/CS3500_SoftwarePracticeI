//Authors: Dan Ruley, Gavin Gray
//November, 2019
using Newtonsoft.Json;

namespace TankWars
{
    /// <summary>
    /// This class represents a Tank in the game world.  This object is JSON serializable.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Tank
    {
        /// <summary>
        /// The ID of this tank.
        /// </summary>
        [JsonProperty(PropertyName = "tank")]
        public readonly int ID;

        /// <summary>
        /// A Vector2D representing the location of this tank in the game world.
        /// </summary>
        [JsonProperty(PropertyName = "loc")]
        public Vector2D location { private set; get; }

        /// <summary>
        /// A Vector2D representing the direction of this tank in the game world.
        /// </summary>
        [JsonProperty(PropertyName = "bdir")]
        public Vector2D orientation { private set; get; }

        /// <summary>
        /// A Vector2D representing the direction the tank is aiming in.
        /// </summary>
        [JsonProperty(PropertyName = "tdir")]
        public Vector2D aiming { private set; get; }

        /// <summary>
        /// The name of this tank.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string name { private set; get; }

        /// <summary>
        /// The hit points of this tank.
        /// </summary>
        [JsonProperty(PropertyName = "hp")]
        public int hitPoints { private set; get; }

        /// <summary>
        /// The score of this tank.
        /// </summary>
        [JsonProperty(PropertyName = "score")]
        public int score { private set; get; }

        /// <summary>
        /// Flag that represents whether this tank is dead.
        /// </summary>
        [JsonProperty(PropertyName = "died")]
        public bool died { private set; get; }

        /// <summary>
        /// Flag representing whether this tank has disconnected from the server.
        /// </summary>
        [JsonProperty(PropertyName = "dc")]
        public bool disconnected { private set; get; }

        /// <summary>
        /// Flag representing whether this tank has joined the server.
        /// </summary>
        [JsonProperty(PropertyName = "join")]
        public bool joined { private set; get; }

        /// <summary>
        /// MAXHP for the tank - used by the client.
        /// </summary>
        private readonly int MAXHP;

        /// <summary>
        /// Tank constructor used by the server to create Beams in the world.  Client tanks are constructed via JSON.
        /// </summary>
        public Tank(int id, Vector2D loc, string n, int hp)
        {
            location = loc;
            ID = id;
            name = n;
            orientation = new Vector2D(0, 1);
            aiming = new Vector2D(0, 1);
            died = false;
            disconnected = false;
            hitPoints = hp;
            MAXHP = hp;
            score = 0;
            joined = true;
        }

        /// <summary>
        /// Set the location for this tank.  Used when respawning a dead tank.
        /// </summary>
        /// <param name="coords"></param>
        public void SetLocation(Vector2D coords)
        {
            location = coords;
        }

        /// <summary>
        /// Set the turret vector for the tank.
        /// </summary>
        public void SetTurret(Vector2D aim)
        {
            aiming = aim;
        }

        /// <summary>
        /// Set the direction vector for the tank.
        /// </summary>
        public void SetDirection(Vector2D dir)
        {
            orientation = dir;
        }

        /// <summary>
        /// Sets this tank to disconnected.
        /// </summary>
        /// <param name="flag"></param>
        public void DisconnectTank()
        {
            disconnected = true;
        }

        /// <summary>
        /// Decreases HP by 1.
        /// </summary>
        /// <returns>true if this caused the tank to die, false otherwise.</returns>
        public bool DecreaseHP()
        {
            hitPoints--;
            died = hitPoints == 0;
            return died;
        }

        /// <summary>
        /// Sets tank HP to 0 - used for beam attacks.
        /// </summary>
        public bool DepleteHP()
        {
            hitPoints = 0;
            died = true;
            return true;
        }

        /// <summary>
        /// Resurrects the tank.
        /// </summary>
        public void DiedToFalse()
        {
            died = false;
        }

        /// <summary>
        /// Resets tank HP to max.
        /// </summary>
        public void ResetHP()
        {
            hitPoints = MAXHP;
        }

        /// <summary>
        /// Increments this tank's score.
        /// </summary>
        public void IncreaseScore()
        {
            score++;
        }
    }
}
