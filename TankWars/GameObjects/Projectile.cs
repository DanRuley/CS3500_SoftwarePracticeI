//Authors: Gavin Gray, Dan Ruley
//November, 2019
using Newtonsoft.Json;

namespace TankWars
{
    /// <summary>
    /// Represents Projectile objects in the game world.  This object is JSON Serializable.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Projectile
    {
        static int next_id = 0;

        /// <summary>
        /// The ID of this projectile.
        /// </summary>
        [JsonProperty(PropertyName = "proj")]
        public readonly int ID;

        /// <summary>
        /// A Vector2D representing the location of this projectile.
        /// </summary>
        [JsonProperty(PropertyName = "loc")]
        public Vector2D location { private set; get; }

        /// <summary>
        /// A Vector2D representing the direction of this projectile.
        /// </summary>
        [JsonProperty(PropertyName = "dir")]
        public Vector2D orientation { private set; get; }

        /// <summary>
        /// Flag representing whether this projectile has "died".
        /// </summary>
        [JsonProperty(PropertyName = "died")]
        public bool died { private set; get; }

        /// <summary>
        /// The ID of the player who shot this projectile.
        /// </summary>
        [JsonProperty(PropertyName = "owner")]
        public readonly int owner;

        /// <summary>
        /// Default constructor so that this object can be created with JSON deserialization.
        /// </summary>
        public Projectile(Vector2D loc, Vector2D angle, int own)
        {
            ID = next_id++;
            location = loc;
            orientation = angle;
            owner = own;
            died = false;
        }

        /// <summary>
        /// Updates the location of this projectile
        /// </summary>
        /// <param name="new_loc">The new location of the projectile.</param>
        public void SetLocation(Vector2D new_loc)
        {
            location = new_loc;
        }

        /// <summary>
        /// Sets died to true so the projectile can be removed from the world.
        /// </summary>
        public void Died()
        {
            died = true;
        }
    }
}
