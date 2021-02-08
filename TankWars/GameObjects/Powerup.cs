//Authors: Gavin Gray, Dan Ruley
//November, 2019
using Newtonsoft.Json;

namespace TankWars
{
    /// <summary>
    /// Class that represents the Powerup object in the game.  This object is JSON Serializable.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Powerup
    {
        static int NextID = 0;

        /// <summary>
        /// The ID of this powerup.
        /// </summary>
        [JsonProperty(PropertyName = "power")]
        public readonly int ID;

        /// <summary>
        /// The location of this powerup.
        /// </summary>
        [JsonProperty(PropertyName = "loc")]
        public Vector2D location { private set; get; }

        /// <summary>
        /// Flag that indicates if this powerup is "dead" or not.
        /// </summary>
        [JsonProperty(PropertyName = "died")]
        public bool died
        { private set; get; }

        /// <summary>
        /// Kills this powerup.
        /// </summary>
        public void Kill()
        {
            died = true;
        }

        /// <summary>
        /// Default constructor so Powerups can be constructed with JSON deserialization.
        /// </summary>
        public Powerup(Vector2D loc)
        {
            ID = NextID++;
            died = false;
            location = loc;
        }
    }
}
