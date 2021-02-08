//Authors: Dan Ruley, Gavin Gray
//November 2019
using System;
using System.Windows.Forms;

namespace TankWarsView
{
    /// <summary>
    /// Static class that executes the application.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ClientView());
        }
    }
}
