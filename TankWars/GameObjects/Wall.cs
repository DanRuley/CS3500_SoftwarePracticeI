//Authors: Dan Ruley, Gavin Gray
//November, 2019
using Newtonsoft.Json;
using System.Collections.Generic;

namespace TankWars
{
    /// <summary>
    /// This class represents a Wall in the game world.  This object is JSON serializable.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Wall
    {
        /// <summary>
        /// The ID of this wall.
        /// </summary>
        [JsonProperty(PropertyName = "wall")]
        public readonly int ID;



        /// <summary>
        /// Vector2D representing the origin of this wall.
        /// </summary>
        [JsonProperty(PropertyName = "p1")]
        public Vector2D p1 { private set; get; }

        /// <summary>
        /// Vector2D representing the end point of this wall.
        /// </summary>
        [JsonProperty(PropertyName = "p2")]
        public Vector2D p2 { private set; get; }

        public Wall(Vector2D start, Vector2D end, int id)
        {
            p1 = start;
            p2 = end;
            this.ID = id;
        }

        /// <summary>
        /// Returns an IEnumerable<Vector2D> that represents all the coordinates at which a wall needs to be draw given a specific width.
        ///            
        /// </summary>
        /// <param name="width">The width at which each wall object should be draw.</param>
        /// <returns></returns>
        public IEnumerable<Vector2D> GetAllCoordinates(double width)
        {
            List<Vector2D> all_coords = new List<Vector2D>();
            // Going Horizontal
            if (p1.GetY() == p2.GetY())
            {
                Vector2D start = (p1.GetX() < p2.GetX()) ? p1 : p2;
                Vector2D end = (p1.GetX() > p2.GetX()) ? p1 : p2;
                for (double i = start.GetX(); i <= end.GetX(); i += width)
                {
                    all_coords.Add(new Vector2D(i, start.GetY()));
                }
            }
            // Going Vertical
            else if (p1.GetX() == p2.GetX())
            {
                Vector2D start = (p1.GetY() < p2.GetY()) ? p1 : p2;
                Vector2D end = (p1.GetY() > p2.GetY()) ? p1 : p2;
                for (double i = start.GetY(); i <= end.GetY(); i += width)
                {
                    all_coords.Add(new Vector2D(start.GetX(), i));
                }
            }
            // Going Diagonal
            else
            {
                Vector2D start = (p1 < p2) ? p1 : p2;
                Vector2D end = (p1 > p2) ? p1 : p2;
                for (double i = start.GetX(), k = start.GetY(); i <= end.GetX() && k <= end.GetY(); i += width, k += width)
                {
                    all_coords.Add(new Vector2D(i, k));
                }
            }
            return all_coords;
        }

    }
}
