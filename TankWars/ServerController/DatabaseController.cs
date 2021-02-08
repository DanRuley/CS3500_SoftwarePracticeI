//Authors: Dan Ruley, Gavin Gray
//Dec. 2019
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using TankWars;

namespace TankWarsServer
{
    /// <summary>
    /// This class interfaces with our SQL database to upload or retrieve game statistics.
    /// </summary>
    class DatabaseController
    {
        private const string connection_string = "server=atr.eng.utah.edu;" + "database=cs3500_u1040250;" + "uid=cs3500_u1040250;" + "password=peachpie;";

        /// <summary>
        /// Uploads the recently completed game to the SQL database.
        /// </summary>
        public static void UploadGameToSQLDatabase(int duration_in_sec, Dictionary<int, ServerGameWorld.GameStat> player_stats, Dictionary<int, string> player_names)
        {
            HashSet<string> SeenNames = new HashSet<string>();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connection_string))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conn;
                    cmd.CommandText = "INSERT INTO Games(Duration) VALUES(" + duration_in_sec + ");";
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();

                    int gid = (int)cmd.LastInsertedId;

                    foreach (int id in player_names.Keys)
                    {
                        string name = player_names[id];

                        //If there is a duplicate player name we just ignore it (Kopta said they will only test w/ unique names anyways)
                        if (SeenNames.Contains(name))
                            continue;
                        SeenNames.Add(name);

                        int pid = -1;

                        cmd.CommandText = "INSERT INTO Players(Name) VALUES('" + name + "');";
                        cmd.Prepare();

                        //Name not in DB - get id from LastInsertedId
                        try
                        {
                            cmd.ExecuteNonQuery();
                            pid = (int)cmd.LastInsertedId;
                        }
                        //Name was already in DB - get id by DB query
                        catch (Exception)
                        {
                            cmd.CommandText = "select pID from Players where Name = '" + name + "';";
                            // Execute the command and cycle through the DataReader object
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                reader.Read();
                                pid = (int)reader["pID"];
                            }
                        }

                        int score = player_stats[id].score;

                        int accuracy = (int)((double)player_stats[id].hits / ((player_stats[id].shots == 0) ? 1 : player_stats[id].shots) * 100);

                        cmd.CommandText = "INSERT INTO GamesPlayed(gID,pID,Score,Accuracy) VALUES(" + gid + "," + pid + "," + score + "," + accuracy + ");";
                        cmd.Prepare();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Parses information about all the Tank Wars games stored in the database.  Returns a dictionary of uints and GameModels for the WebView class so it can serve web clients this information in HTML format.
        /// </summary>
        public static Dictionary<uint, GameModel> ParseAllGamesSQL()
        {
            Dictionary<uint, GameModel> AllGames = new Dictionary<uint, GameModel>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connection_string))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conn;

                    cmd.CommandText = "select gID, Duration, Name, Score, Accuracy from Games natural join Players natural join GamesPlayed";

                    // Execute the command and cycle through the DataReader object
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            uint gID = uint.Parse(reader["gID"].ToString());
                            uint duration = uint.Parse(reader["Duration"].ToString());
                            string name = reader["Name"].ToString();
                            uint score = uint.Parse(reader["Score"].ToString());
                            uint accuracy = uint.Parse(reader["Accuracy"].ToString());
                            if (!AllGames.ContainsKey(gID))
                                AllGames.Add(gID, new GameModel(gID, duration));
                            AllGames[gID].AddPlayer(name, score, accuracy);
                        }
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Connection to Network Interupted.");
                return new Dictionary<uint, GameModel>();
            }
            return AllGames;
        }

        /// <summary>
        /// Retrieves the total number of players who have ever played a TankWars game, used for the GetHomePage method in WebView class.
        /// </summary>
        public static int GetNumberOfPlayers()
        {
            using (MySqlConnection conn = new MySqlConnection(connection_string))
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = "SELECT COUNT(*) FROM Players;";
                cmd.Prepare();

                try
                {
                    // Execute the command and cycle through the DataReader object
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        return Convert.ToInt32(reader["COUNT(*)"]);
                    }
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Parses information about all games the given player has participated in.  Returns a list of Session Models so the WebView can servec the web client the information in HTML format.  Note: the SQL requests data in such a way that only the relevant information is queried, thus avoiding inefficient SQL queries/parsing.
        /// </summary>
        /// <param name="name">player name</param>
        public static List<SessionModel> ParsePlayerGamesSQL(string name)
        {
            List<SessionModel> PlayerSessions = new List<SessionModel>();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connection_string))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = conn;

                    cmd.CommandText = "select gID, Duration, Score, Accuracy from GamesPlayed natural join Players natural join Games where Name='" + name + "';";

                    // Execute the command and cycle through the DataReader object
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            uint gID = uint.Parse(reader["gID"].ToString());
                            uint duration = uint.Parse(reader["Duration"].ToString());
                            uint score = uint.Parse(reader["Score"].ToString());
                            uint accuracy = uint.Parse(reader["Accuracy"].ToString());
                            PlayerSessions.Add(new SessionModel(gID, duration, score, accuracy));
                        }
                    }
                }

            }
            catch (Exception)
            {
                Console.WriteLine("Interuption over the network, cannot read database.");
                return new List<SessionModel>();
            }
            return PlayerSessions;
        }
    }
}
