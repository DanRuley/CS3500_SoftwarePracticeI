//Authors: Gavin Gray, Dan Ruley
//November, 2019
using System;
using System.Drawing;
using System.Windows.Forms;
using TankWars;
using ViewController;

namespace TankWarsView
{
    /// <summary>
    /// The TankWars client view.  It interfaces with the controller which informs the view when to update the drawing panel or display error messages to the user.
    /// </summary>
    public partial class ClientView : Form
    {
        private TankWarsViewController controller;
        private DrawingPanel GamePanel;
        private readonly int OFFSET = 40;

        /// <summary>
        ///Constructs the ClientView.  Initializes the TankWars controller as well as the DrawingPanel.  Also subscribes various methods to relevant events contained in the controller class. 
        ///</summary>
        public ClientView()
        {
            InitializeComponent();
            this.ClientSize = new Size(800, 800);

            controller = new TankWarsViewController(this.ClientSize.Width / 2, this.ClientSize.Height / 2 + OFFSET);

            controller.WorldReady += CreateWorld;
            // Event that indicates some sort of error was thrown.
            controller.OnError += ErrorOccurred;
            // Event that all the messages were processed and we need to redraw the frame.
            controller.MessagesProcessed += OnFrame;
            // Inform the DrawingPanel to start a new explosion at (x,y)
            controller.TankDied += InitiateExplosion;
            // Event that signals the Form was closed.
            FormClosed += OnExit;
            // Set up the DrawingPanel
            GamePanel = new DrawingPanel();
            GamePanel.Location = new Point(0, OFFSET);
            // Make the DrawingPanel the same size as the window.
            //** Note that it does not resize to the window.
            GamePanel.Size = this.ClientSize;
            // Background of the DrawingPanel looks better when it's black.
            GamePanel.BackColor = Color.Black;
            // Add the DrawingPanel to the Form.
            this.Controls.Add(GamePanel);
            // Add a subscriber to the MouseDown event, this is fired when the mouse button is clicked DOWN
            GamePanel.MouseDown += MouseDownClick;
            // Add subscriber to the MouseUp event, which is fired when the mouse button is released
            GamePanel.MouseUp += MouseUpClick;
        }

        /// <summary>
        /// Called when the controller triggers the WorldCreated event.  This notifies the DrawingPanel that the world is ready to be drawn.
        /// </summary>
        /// <param name="gw">Reference to the GameWorld</param>
        private void CreateWorld(GameWorld gw)
        {
            GamePanel.SetWorldReference(gw);
        }

        /// <summary>
        /// Triggered by the controller when the current frame's data has been processed.  Informs the DrawingPanel that it should draw the next frame.
        /// </summary>
        private void OnFrame()
        {
            try
            {
                this.Invoke(new MethodInvoker(() =>
                {
                    Point mp = PointToClient(Control.MousePosition);
                    controller.MouseCoordChange(mp.X, mp.Y);
                    this.Invalidate(true);
                }));
            }

            catch (Exception)
            {
                return;
            }
        }


        /// <summary>
        /// Initiates the adding of an explosion in the DrawingPanel class.
        /// 
        /// This method is subscribed to an event that is fired from the Controller class.
        /// Here is the reason as to why this needs to be invoked here and not in the controller:
        ///     The controller does not have a reference to the DrawingPanel because it is 
        ///     strickly a concern of the view. 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void InitiateExplosion(int id, double x, double y)
        {
            GamePanel.StartNewExplosion(id, x, y);
        }

        /// <summary>
        /// Triggered by the controller when it detects an error and displays the error message to the user.
        /// </summary>
        /// <param name="ErrMsg"></param>
        private void ErrorOccurred(string ErrMsg)
        {
            try
            {
                // Show error in a message box to the user.
                this.Invoke(new MethodInvoker(() => MessageBox.Show(ErrMsg)));

                // Re-enable the inputs to allow the user to fix the problem
                this.Invoke(new MethodInvoker(() =>
                {
                    connectButton.Enabled = true;
                    serverAddress.Enabled = true;
                    userName.Enabled = true;
                }));
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Method ends the connection with the controller in the case that the form was closed for an unexpected reason.
        /// </summary>
        private void OnExit(object sender, FormClosedEventArgs e)
        {
            controller.EndConnection();
        }

        /// <summary>
        /// Connect button event handler
        /// </summary>
        private void connectButton_Click(object sender, EventArgs e)
        {
            // Disable the TextBoxes and ConnectButton and try to connect
            connectButton.Enabled = false;
            serverAddress.Enabled = false;
            userName.Enabled = false;

            controller.Connect(serverAddress.Text, userName.Text);
        }

        /// <summary>
        /// Triggered on a key down event - informs the controller.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            controller.KeyPressed(e.KeyCode.ToString());
            base.OnKeyDown(e);
            e.SuppressKeyPress = true;
        }

        /// <summary>
        /// Triggered by a key up event - informs the controller.
        /// </summary>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            controller.KeyReleased(e.KeyCode.ToString());
            base.OnKeyUp(e);
        }

        /// <summary>
        /// Triggered by a mouse click - informs the controller.
        /// </summary>
        protected void MouseDownClick(Object sender, MouseEventArgs e)
        {
            string side;
            if (e.Button == MouseButtons.Left) side = "left";
            else if (e.Button == MouseButtons.Right) side = "right";
            else side = "unknown";
            controller.MouseDown(side);
            base.OnMouseDown(e);
        }

        /// <summary>
        /// Triggered by a mouse button release - informs the controller.
        /// </summary>
        protected void MouseUpClick(Object sender, MouseEventArgs e)
        {
            controller.MouseUp();
            base.OnMouseUp(e);
        }
    }
}
