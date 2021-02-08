//Authors: Gavin Gray, Dan Ruley
//November, 2019
using Newtonsoft.Json;

namespace TankWars
{
    /// <summary>
    /// Represents the Beam object in the game world. This object is JSON Serializable.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Beam
    {
        static int NextID = 0;

        /// <summary>
        /// The ID of this beam.
        /// </summary>
        [JsonProperty(PropertyName = "beam")]
        public readonly int ID;

        /// <summary>
        /// A Vector2D representing the origin of this beam.
        /// </summary>
        [JsonProperty(PropertyName = "org")]
        public Vector2D origin { private set; get; }

        /// <summary>
        /// A direction vector representing the direction of this beam.
        /// </summary>
        [JsonProperty(PropertyName = "dir")]
        public Vector2D direction { private set; get; }

        /// <summary>
        /// ID of the player who fired this beam.
        /// </summary>
        [JsonProperty(PropertyName = "owner")]
        public int owner { private set; get; }

        /// <summary>
        /// The number of frames the beam will be displayed.
        /// </summary>
        public int frame_count { set; get; }

        /// <summary>
        /// Beam constructor used by the server to create Beams in the world.  Client beams are constructed via JSON.
        /// </summary>
        /// <param name="own"></param>
        /// <param name="ang"></param>
        /// <param name="orig"></param>
        public Beam(int own, Vector2D ang, Vector2D orig)
        {
            ID = NextID++;
            owner = own;
            direction = ang;
            origin = orig;
            frame_count = 35;
        }

        /// <summary>
        /// Constructs a beam with a frame count of 35.
        /// </summary>
        public Beam()
        {
            ID = NextID++;
            frame_count = 35;
        }

    }
}
