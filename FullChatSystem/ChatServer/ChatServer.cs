using NetworkUtil;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ChatServer
{

    /// <summary>
    /// A simple server for receiving simple text messages from multiple clients
    /// </summary>
    class ChatServer
    {

        // A set of clients that are connected.
        private static ServerController controller;

        static void Main(string[] args)
        {
            controller = new ServerController();
            
            // Sleep to prevent the program from closing,
            // since all the real work is done in separate threads.
            // StartServer is non-blocking.
            Console.Read();
        }

        public static void ServerPrint(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}

