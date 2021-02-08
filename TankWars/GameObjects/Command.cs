//Authors: Gavin Gray, Dan Ruley
//November, 2019
using Newtonsoft.Json;

namespace TankWars
{
    /// <summary>
    /// Class that contains the commands to be sent to the game server. This object is JSON Serializable.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Command
    {
        /// <summary>
        /// The direction to move.
        /// </summary>
        [JsonProperty(PropertyName = "moving")]
        public string move
        {
            private set;
            get;
        }

        /// <summary>
        /// The firing command.
        /// </summary>
        [JsonProperty(PropertyName = "fire")]
        public string fire
        {
            private set;
            get;
        }

        /// <summary>
        /// A Vector 2D indicating a direction
        /// </summary>
        [JsonProperty(PropertyName = "tdir")]
        public Vector2D dir
        {
            private set;
            get;
        }

        /// <summary>
        /// Constructs a Command based on the input moving, firing, and direction values.
        /// </summary>
        public Command(string mo, string fi, Vector2D d)
        {
            move = mo;
            fire = fi;
            dir = d;
        }
    }
}
