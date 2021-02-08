using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ChatClient
{
    public partial class Form1 : Form
    {
        private ClientController controller;


        public Form1()
        {
            InitializeComponent();
            controller = new ClientController();
            controller.MessagesArrived += NewMessages;
            controller.OnError += ErrorOccurred;
            messageToSendBox.KeyDown += new KeyEventHandler(MessageEnterHandler);
            FormClosed += OnExit;
        }

        private void ErrorOccurred(string ErrMsg)
        {
            if (ErrMsg != "Not Connected")
            {
                this.Invoke(new MethodInvoker(() =>
              {
                  connectButton.Enabled = true;
                  serverAddress.Enabled = true;
              }));
            }

            this.Invoke(new MethodInvoker(() => MessageBox.Show(ErrMsg)));

        }
        private void OnExit(object sender, FormClosedEventArgs e)
        {
            controller.EndConnection();

        }

        /// <summary>
        /// Connect button event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void connectButton_Click(object sender, EventArgs e)
        {
            if (serverAddress.Text == "")
            {
                MessageBox.Show("Please enter a server address");
                return;
            }

            // Disable the controls and try to connect
            connectButton.Enabled = false;
            serverAddress.Enabled = false;

            controller.Connect(serverAddress.Text);
        }


        /// <summary>
        /// This is the event handler when the enter key is pressed in the messageToSend box
        /// </summary>
        /// <param name="sender">The Form control that fired the event</param>
        /// <param name="e">The key event arguments</param>
        private void MessageEnterHandler(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                // Append a newline, since that is our protocol's terminating character for a message.
                string message = messageToSendBox.Text;
                // Reset the textbox
                messageToSendBox.Text = "";
                // Send the message to the controller
                controller.SendMessage(message);
            }
        }

        private void NewMessages(IEnumerable<string> Messages)
        {
            foreach (string item in Messages)
            {
                this.Invoke(new MethodInvoker(() => messages.AppendText(item + System.Environment.NewLine)));
            }
        }
    }
}

