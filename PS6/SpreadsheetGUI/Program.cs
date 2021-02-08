//authors: Daniel Kopta, Dan Ruley, and Gavin Grey.  Sept/Oct. 2019
using System;
using System.Windows.Forms;

namespace SpreadsheetGUI
{
    /// <summary>
    /// Keeps track of how many top-level forms are running
    /// </summary>
    class PS6ApplicationContext : ApplicationContext
    {
        // Number of open forms
        private int formCount = 0;

        // Singleton ApplicationContext
        private static PS6ApplicationContext appContext;

        /// <summary>
        /// Private constructor for singleton pattern
        /// </summary>
        private PS6ApplicationContext()
        {
        }

        /// <summary>
        /// Returns the one DemoApplicationContext.
        /// </summary>
        public static PS6ApplicationContext GetAppContext()
        {
            if (appContext == null)
            {
                appContext = new PS6ApplicationContext();
            }
            return appContext;
        }

        /// <summary>
        /// Runs the form
        /// </summary>
        public void RunForm(System.Windows.Forms.Form form)
        {
            // One more form is running
            formCount++;

            // When this form closes, we want to find out
            form.FormClosed += (o, e) => { if (--formCount <= 0) ExitThread(); };

            // Run the form
            form.Show();
        }
    }

    /// <summary>
    /// Contains the main method to run this program.
    /// </summary>
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            foreach(string s in args)
            {
                Application.Run(new SpreadsheetForm(s));
            }
            if (args.Length == 0) Application.Run(new SpreadsheetForm());
        }
    }
}
