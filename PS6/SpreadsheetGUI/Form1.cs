//authors:Gavin Grey and Dan Ruley.  Sept/Oct. 2019
using Convert;
using SpreadsheetUtilities;
using SS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SpreadsheetGUI
{
    /// <summary>
    /// This class defines the Spreadsheet object.
    /// </summary>
    public partial class SpreadsheetForm : System.Windows.Forms.Form
    {
        /// <summary>
        /// Spreadsheet object providing the model for the GUI.
        /// </summary>
        private Spreadsheet form_spreadsheet;

        /// <summary>
        /// FileName for this SpreadsheetForm, "" denotes that a new SS has not been saved yet.
        /// </summary>
        private string FileName;

        /// <summary>
        /// Constructs a spreadsheet GUI corresponding to a blank spreadsheet.
        /// </summary>
        public SpreadsheetForm()
        {

            InitializeComponent();

            // Initialize our underlying Spreadsheet object.
            this.form_spreadsheet = new Spreadsheet(SpreadSheetValidator, s => s.ToUpper(), "ps6");

            AcceptButton = SetContentsButton;

            // Set subscribers of all events.
            SpreadsheetGrid.SelectionChanged += UpdateCoordTextBox;
            SpreadsheetGrid.SelectionChanged += UpdateDisplayBoxs;
            SpreadsheetGrid.SelectionChanged += UnfocusContentDisplay;
            SpreadsheetGrid.DoubleClickByMouse += FocusContentBox;
            SetContentsButton.Click += SetContentsOfCell;
            ErrorTimer.Tick += CloseError;

            FileName = "";

            // Other important initializations
            SpreadsheetGrid.SetSelection(0, 0);
            CoordDisplayBox.Text = "A1";
        }

        public SpreadsheetForm(string filename)
        {
            InitializeComponent();

            // Initialize our underlying Spreadsheet object.
            this.form_spreadsheet = new Spreadsheet(filename, SpreadSheetValidator, s => s.ToUpper(), "ps6");

            AcceptButton = SetContentsButton;

            // Set subscribers of all events.
            SpreadsheetGrid.SelectionChanged += UpdateCoordTextBox;
            SpreadsheetGrid.SelectionChanged += UpdateDisplayBoxs;
            SpreadsheetGrid.SelectionChanged += UnfocusContentDisplay;
            SpreadsheetGrid.DoubleClickByMouse += FocusContentBox;
            SetContentsButton.Click += SetContentsOfCell;
            ErrorTimer.Tick += CloseError;

            FileName = "";

            // Other important initializations
            SpreadsheetGrid.SetSelection(0, 0);
            CoordDisplayBox.Text = "A1";

            List<string> il = new List<string>(this.form_spreadsheet.GetNamesOfAllNonemptyCells());
            UpdateCellsAfterChange(il);
        }


        /// <summary>
        /// Overloaded constructor that takes in an already built spreadsheet (e.g. from a file)
        /// </summary>
        /// <param name="ss">The input spreadsheet</param>
        public SpreadsheetForm(Spreadsheet ss, string filename) : this()
        {
            FileName = filename;
            form_spreadsheet = ss;
            UpdateCellsAfterChange(new List<string>(ss.GetNamesOfAllNonemptyCells()));
        }

        /// <summary>
        /// Sets focus to input box on double click.
        /// </summary>
        private void FocusContentBox()
        {
            DisplayContentsBox.Focus();
        }

        /// <summary>
        /// Gets a string representation of the named cell's value.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetStringValue(string name)
        {
            object ret = form_spreadsheet.GetCellValue(name);
            if (ret is SpreadsheetUtilities.FormulaError) return "!ERROR";
            else return ret.ToString();
        }

        /// <summary>
        /// Gets a string representation of the named cell's contents.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetStringContents(string name)
        {
            object ret = form_spreadsheet.GetCellContents(name);
            if (ret is SpreadsheetUtilities.Formula) return "=" + ret.ToString();
            else return ret.ToString();
        }

        /// <summary>
        /// When a cell selection changes the value of the DisplayBoxes update their values to reflect
        ///     the contents and value of the selected cell.
        /// </summary>
        /// <param name="ssp"></param>
        private void UpdateDisplayBoxs(SpreadsheetPanel ssp)
        {
            int row, col;

            // Gets the current selected row, col values
            SpreadsheetGrid.GetSelection(out row, out col);

            // Get the string name for those values.
            UpdateDisplayBoxs(row, col);
        }

        /// <summary>
        /// Updates the displayed value of the TextBoxes that show the 
        ///     current value and contents of the selected cell.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        private void UpdateDisplayBoxs(int row, int col)
        {
            string name;
            name = GetNameFromCoord(row, col);
            DisplayValueBox.Text = GetStringValue(name);
            DisplayContentsBox.Text = GetStringContents(name);
        }

        /// <summary>
        /// This function is subscribed to the envent of SetCellContentsButton_Click()
        /// 
        /// updates the value of the cell if it does not create a circular exception
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetContentsOfCell(Object sender, EventArgs e)
        {

            int col, row;

            // Get the coordinates of the selection and the formal string name of the given coordinates.
            SpreadsheetGrid.GetSelection(out col, out row);
            string name = GetNameFromCoord(col, row);

            // Try to change the value knowing that a circular exception is possible
            try
            {
                // Try to change the contents of the cell.
                IList<string> recalculate_cells = form_spreadsheet.SetContentsOfCell(name, DisplayContentsBox.Text);
                UpdateCellsAfterChange(recalculate_cells);

                // Moves the selection down one cell just as it does in Excel.
                row += 1;
            }

            catch (CircularException)
            {
                // If a circularError is thrown then we want to inform the user.
                ErrorNotify.SetError(ErrorText, "Error: input formula cannot create a circular reference.");
                ErrorText.Text = "Error: Circular Reference Detected.";
                //DisplayContentsBox.Text = "";

                //Only display error for a short amount of time.
                ErrorTimer.Interval = 2000;
                ErrorTimer.Start();
            }
            catch (FormulaFormatException ex)
            {
                // If a FormulaFormatException is thrown then we want to inform the user.
                ErrorNotify.SetError(ErrorText, ex.Message);
                ErrorText.Text = ex.Message;

                //Only display error for a short amount of time.
                ErrorTimer.Interval = 2000;
                ErrorTimer.Start();
            }

            // Move the selection down one similar to the funcitonality in Excel.
            SpreadsheetGrid.SetSelection(col, row);
        }

        /// <summary>
        /// When the error timer goes off, remove the error.
        /// </summary>
        private void CloseError(object sender, EventArgs e)
        {
            ErrorTimer.Dispose();
            ErrorText.Text = "";
            ErrorNotify.SetError(ErrorText, null);
        }

        /// <summary>
        /// Overriden the following Keys to not force the user to shift to using the mouse all day long.
        /// 
        /// UpArrow | K : move selected cell up.
        /// DownArrow | J : move selected cell down.
        /// LeftArrow | H : move selected cell left.
        /// RightArrow | K : move selected cell right.
        /// 
        /// E : edit the current cell.
        /// 
        /// Ctrl + q : abort editing the contents of the cell and revert back to previous contents.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            // If the contents box is focused then we don't care about the arrow keys.
            if (DisplayContentsBox.Focused)
            {
                // If Ctrl+q is pressed while editing the contents we 
                //      want to abort the edit and move focus back to the grid.
                if (e.Control && e.KeyCode == Keys.Q) ;
                else return; // Else ignore the key event
            }

            int row, col;
            // First find out where the current selection is.
            SpreadsheetGrid.GetSelection(out row, out col);

            // Change the cell based off of arrow-key strokes.
            switch (e.KeyCode)
            {
                case Keys.E:
                    DisplayContentsBox.Focus();
                    // Avoid Selecting the cell so it focuses the DisplayContentsBox.
                    return;
                case Keys.K:
                    goto case Keys.Up;
                case Keys.Up:
                    col -= 1;
                    break;
                case Keys.J:
                    goto case Keys.Down;
                case Keys.Down:
                    col += 1;
                    break;
                case Keys.H:
                    goto case Keys.Left;
                case Keys.Left:
                    row -= 1;
                    break;
                case Keys.L:
                    goto case Keys.Right;
                case Keys.Right:
                    row += 1;
                    break;
            }

            SpreadsheetGrid.SetSelection(row, col);
        }

        /// <summary>
        /// Unfocus the DisplayContentBox,
        ///     this happens because we are going to give the arrow keys the 
        ///     functionality of changing the current selected cell. Therefore
        ///     when a cell is modified we want to go back to focusing on the 
        ///     SpreadsheetPanel so the user can use the arrow keys to navigate.
        /// </summary>
        /// <param name="ssp"></param>
        private void UnfocusContentDisplay(SpreadsheetPanel ssp)
        {
            // Give the Focus back to the spreadsheet.
            SpreadsheetGrid.Focus();
        }

        /// <summary>
        /// Given a list of cell names recalculate all the SpreadsheetGrid values.
        /// 
        /// Note that this does not recalculate anything inside the actual AbstractSpreadsheet object, 
        ///     rather updates the values shown inside the GUI.
        /// </summary>
        /// <param name="rec">List of cell names to be recalculated.</param>
        private void UpdateCellsAfterChange(IList<string> rec)
        {
            int row, col;
            foreach (string name in rec)
            {
                GetCoordFromName(name, out row, out col);
                SpreadsheetGrid.SetValue(row, col, GetStringValue(name));
            }
        }

        /// <summary>
        /// When a cell selection has changed the CoordDisplay box updates it's values acoordingly.
        /// </summary>
        /// <param name="ssp"></param>
        private void UpdateCoordTextBox(SpreadsheetPanel ssp)
        {
            int row, col;
            SpreadsheetGrid.GetSelection(out row, out col);
            CoordDisplayBox.Text = GetNameFromCoord(row, col);
        }

        /// <summary>
        /// Given a coordinate (int x, int y),
        ///     returns the string name of the cell.
        ///     
        /// example: (0, 0) => "A1"
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private string GetNameFromCoord(int row, int col)
        {
            return System.Convert.ToChar(((int)'A' + row)).ToString() + (col + 1);
        }

        /// <summary>
        /// Return the coordinates row, col given the string representation of the name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        private void GetCoordFromName(string name, out int row, out int col)
        {
            int start = name[0];
            row = start - (int)'A';
            col = int.Parse(name.Substring(1)) - 1;
        }

        /// <summary>
        /// This method should not invoke anything because the text inside this TextBox is un-editable.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CoordDisplayBox_TextChanged(object sender, EventArgs e)
        {
            // This text Box is UNEDITABLE.
        }

        /// <summary>
        /// Event that is triggered when the user selects New from File menu.
        /// </summary>
        private void NewMenuItem_Click(object sender, EventArgs e)
        {
            NewSpreadsheet();
        }

        /// <summary>
        /// Creates a new spreadsheet.
        /// </summary>
        private void NewSpreadsheet()
        {
            PS6ApplicationContext.GetAppContext().RunForm(new SpreadsheetForm());
        }

        /// <summary>
        /// Called when the user clicks Close under the Main Menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseMenuItem_Click(object sender, EventArgs e)
        {
            CloseSpreadsheet();
        }

        /// <summary>
        /// Closes the current SS window.
        /// </summary>
        private void CloseSpreadsheet()
        {
            Close();
        }

        /// <summary>
        /// Control for the OpenFile menu item.
        /// </summary>
        private void OpenFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileBrowser(false);
        }

        /// <summary>
        ///  Open the file browser to select file to open.
        /// </summary>
        /// <param name="image">true if the user is opening an image.</param>
        private void OpenFileBrowser(bool image)
        {
            OpenFileDialog = new OpenFileDialog();
            int width = 0;
            string widthstring = "";

            OpenFileDialog.Filter = "Spreadsheet Files|*.sprd|All Files|*.*";

            //Configure setting for image browser
            if (image)
            {
                widthstring = Prompt.ShowDialog("Enter the number of columns for image size (between 2 and 26):", "Column Prompt");
                OpenFileDialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.TIFF;*.EXIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.TIFF;*.EXIF;*.PNG";
            }

            try
            {
                //If user clicked open image from file but did not enter a width, don't show dialog
                if (image && widthstring == "") ;

                //User did not enter an integer for width
                else if (image && !int.TryParse(widthstring, out width))
                    throw new ArgumentException("Width must be an integer between 2 and 26");

                else
                    OpenFileDialog.ShowDialog();


                if (OpenFileDialog.FileName != "")
                {
                    FileName = OpenFileDialog.FileName;
                    //get width and throw exception if it is invalid
                    if (image)
                    {
                        if ((width < 2 || width > 26) && widthstring != "")
                            throw new ArgumentException("Invalid width");

                        CreateSpreadsheetImageFromFile(FileName, width * 11);
                    }

                    else
                        CreateSpreadsheetFromFile(FileName);
                }
            }
            //Catch any exceptions and notify user without crashing program.
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        /// <summary>
        /// Control for the Save As File menu item.
        /// </summary>
        private void SaveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileHandler(true);
        }

        /// <summary>
        /// Control for the Save menu item.
        /// </summary>
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileHandler(false);
        }

        /// <summary>
        /// Open the file browser to save the current file.
        /// </summary>
        private void SaveFileHandler(bool SaveAs)
        {
            SaveFileDialog = new SaveFileDialog();
            try
            {
                //User either clicked "SaveAs" or user clicked "Save" for the first time - need to create new FileName.
                if (SaveAs || FileName == "")
                {
                    SaveFileDialog.Filter = "Spreadsheet File|*.sprd|Text File|*.txt|XML File|*.xml";
                    SaveFileDialog.AddExtension = true;
                    SaveFileDialog.ShowDialog();

                    if (SaveFileDialog.FileName != "")
                    {
                        FileName = SaveFileDialog.FileName;
                        form_spreadsheet.Save(FileName);
                    }
                }

                //We already have a FileName and user clicked "Save"
                else
                    form_spreadsheet.Save(FileName);
            }

            catch (Exception)
            {
                MessageBox.Show("Error saving file.  Please try again.");
            }
        }

        /// <summary>
        /// Creates a new spreadsheet object from the given file, and redraws the open SS grid.
        /// </summary>
        private void CreateSpreadsheetFromFile(string filename)
        {
            Spreadsheet ss = new Spreadsheet(filename, SpreadSheetValidator, s => s.ToUpper(), "ps6");
            PS6ApplicationContext.GetAppContext().RunForm(new SpreadsheetForm(ss, filename));
        }

        /// <summary>
        /// File that converts a jpg file to an ascii image and displays it onto the spreadsheet.
        /// 
        /// The set dimension is 132, which spans 12 columns.
        ///     If user specifies a larger or smaller dimension we should 
        ///     change the dimension of the photo accordingly.
        ///     
        ///     Note that the dimension must be divisible by 11, 
        ///     because that's how many characters can fit into the graph cells.
        /// </summary>
        /// <param name="filename">Filepath to JPG file</param>
        private void CreateSpreadsheetImageFromFile(string filename, int width = 88)
        {

            Spreadsheet ss = new Spreadsheet(SpreadSheetValidator, s => s.ToUpper(), "ps6");
            GenXMLFromJPG generator = new GenXMLFromJPG(filename, ss, width, ImageWidthValidator, 11);
            ss = generator.GenFilledSpread();
            PS6ApplicationContext.GetAppContext().RunForm(new SpreadsheetForm(ss, filename));
        }

        /// <summary>
        /// Essentially reading a normal .XML however with the special case of an error we will display danny.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="width"></param>
        private void CreateDannyImageFromFile(string filename, int width = 88)
        {
            Spreadsheet ss;

            try
            {
                ss = new Spreadsheet(filename, SpreadSheetValidator, s => s.ToUpper(), "ps6");
                //generator = new GenXMLFromJPG(filename, ss, width, ImageWidthValidator, 11);
                //ss = generator.GenFilledSpread();
            }
            catch (Exception)
            {
                ss = new Spreadsheet(SpreadSheetValidator, s => s.ToUpper(), "ps6");
                GenXMLFromJPG generator = new GenXMLFromJPG(Danny_Ascii_String._DANNY_ASCII, ss, 11);
                ss = generator.GenFilledSpread();
            }
            PS6ApplicationContext.GetAppContext().RunForm(new SpreadsheetForm(ss, filename));
            // Now we don't have to reload the file from a string everytime and can replace the original file.
            ss.Save("danny.sprd");
        }


        /// <summary>
        /// Validator that only accepts variables that start with 1 Upper case character in the range A-Z
        ///     followed by a digit in the range of 1-99
        /// </summary>
        private bool SpreadSheetValidator(string s)
        {
            return Regex.IsMatch(s, @"^[A-Z]{1}[1-9]{1}\d?$");
        }

        /// <summary>
        /// Validates the width of the ascii image.
        /// </summary>
        private bool ImageWidthValidator(int n)
        {
            if (n / 11 > 26) return false;
            else return (n % 11 == 0);
        }


        /// <summary>
        /// Activated when the Keyboard Shortcuts is clicked in the Help menu. 
        /// </summary>
        private void KeyboardShortcutsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DisplayShortcuts();
        }

        /// <summary>
        /// Unused event but cannot delete without error
        /// </summary>
        private void OpenFileDialog_FileOk(object sender, CancelEventArgs e)
        {
        }

        /// <summary>
        /// Unused event but cannot delete without error
        /// </summary>
        private void SaveFileDialog_FileOk(object sender, CancelEventArgs e)
        {
        }

        /// <summary>
        /// Renders an ascii version of our favorite Prof. in the Spreadsheet!
        /// </summary>
        private void chatWithDannyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateDannyImageFromFile("Danny.sprd");
        }

        /// <summary>
        /// Control for the OpenNewImage menu item.
        /// </summary>
        private void openNewImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileBrowser(true);
        }

        /// <summary>
        /// Displays a list of the keyboard shortcuts for this spreadsheet GUI.
        /// </summary>
        private void DisplayShortcuts()
        {
            MessageBox.Show(
                "- Navigate grid using the arrow keys. \n\n" +
                "- You can also navigate grid using K (up), J (down), H (left), and L (right)\n\n" +
                "- Enter EDIT MODE for a cell: E\n\n" +
                "- Leave EDIT MODE and return to grid navigation: Ctrl + Q\n\n" +
                "- Save File: Ctrl + S\n\n" +
                "- Save As: Ctrl + Shift + S\n\n" +
                "- Open File: Ctrl + N\n\n" +
                "- New Spreadsheet: Ctrl + N\n\n" +
                "- Close Spreadsheet: Ctrl + Shift + C\n\n" +
                "- Set Contents of Cell: Enter\n\n", "Keyboard Shortcuts"
                );
        }

        /// <summary>
        /// Shows a MessageBox containing instructions for the use of this spreadsheet.
        /// </summary>
        private void howToUseThisSpreadsheetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Navigate grid by clicking on them with the mouse.  Alternatively, you can navigate cells using the keyboard.\n\n" +
                "Click on the \"Keyboard Shortcuts\" menu item for a list of supported keyboard shortcuts.\n\n" +
                "Options for saving and loading spreadsheets are located under the File menu.  The default extension used is .sprd and the default version is \"ps6.\"\n\n" +
                "You can set the contents by typing into the text box at the top of the spreadsheet and clicking \"Set.\" Alternatively, you can press the enter key.\n\n" +
                "Valid cell contents are: formulas, numbers, or strings (e.g. for labelling).  Formulas are indicated by typing '=' followed by the expression.  Infix expression syntax should be used.\n\n" +
                "Supported operations are multiplication, division, addition, and subtraction, as well as grouping expressions with paranthesis.  Any formula syntax errors will be reported above the Contents textbox.\n\n" +
                "Additionally, formulas that result in circular references between cells are disallowed, and this error will be reported.\n\n" +
                "Please see the ReadMe.txt file in the Resources folder for more information about design considerations and special features.", "Spreadsheet User Guide"); ;
        }


        /// <summary>
        /// Static class that implements a dialog box that takes input from the user.  This code was taken from 
        /// </summary>
        private static class Prompt
        {
            /// <summary>
            /// Displays a dialog box and records the user input as a string.
            /// </summary>
            public static string ShowDialog(string text, string caption)
            {
                Form prompt = new Form()
                {
                    Width = 500,
                    Height = 150,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Text = caption,
                    StartPosition = FormStartPosition.CenterScreen
                };
                //setup form positioning and captions
                Label textLabel = new Label() { Left = 50, Top = 20, Width = 400, Text = text };
                TextBox textBox = new TextBox() { Left = 50, Top = 50, Width = 400 };
                Button confirmation = new Button() { Text = "Ok", Left = 350, Width = 100, Top = 70, DialogResult = DialogResult.OK };
                confirmation.Click += (sender, e) => { prompt.Close(); };
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(confirmation);
                prompt.Controls.Add(textLabel);
                prompt.AcceptButton = confirmation;

                return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
            }
        }

        /// <summary>
        /// Static class just to hold the hardcoded ascii value of danny. This was not wanted, and if this were actually deployed we would NOT have this.
        /// </summary>
        private static class Danny_Ascii_String
        {
            // We DID NOT WANT to hardcode this string. However, after talking with the TA Zack this is a backup option just in case loading from the .sprd
            //  didn't work for some reason. (e.g. the file wasn't able to load from where the TA's were running it, or somehow it got deleted by the user or OS.
             public static string _DANNY_ASCII = "#####################################################################################################################################################################"+ System.Environment.NewLine +
                "#####################################################################################################################################################################" + System.Environment.NewLine +
                "#####################################################################################################################################################################" + System.Environment.NewLine +
                "#####################################################################################################################################################################" + System.Environment.NewLine +
                "#####################################################################################################################################################################" + System.Environment.NewLine +
                "#####################################################################################################################################################################" + System.Environment.NewLine +
                "#####################################################################################################################################################################" + System.Environment.NewLine +
                "#####################################################################################################################################################################" + System.Environment.NewLine +
                "############################################################################%+:*:+###################################################################################" + System.Environment.NewLine +
                "#########################################################################%. ... .......... ...    .%#################################################################" + System.Environment.NewLine +
                "###############################################################@@@##+-+*-.-.--::**:-:::**:---.....    +##############################################################" + System.Environment.NewLine +
                "##############################################################*  .::::---::*:::--.--....-:**:---::-.   .:############################################################" + System.Environment.NewLine +
                "##############################################################- -::*-..:****+*-.-*+**++xxxx++x+:--:*+*    @##########################################################" + System.Environment.NewLine +
                "##############################################################@...:+*+++xxx*:+*+++xxx%%%%%xxx+++++***:- . %##########################################################" + System.Environment.NewLine +
                "##############################################################+ :*x%xx%%%%xx+xx++x%+****:::::+xxx++xx*:::-%##########################################################" + System.Environment.NewLine +
                "##########################################################*.*  :*x%%x%xxxxxxx:**:----:::::--:+xx%%x+**x%+**-:%@######################################################" + System.Environment.NewLine +
                "#########################################################%....:*+*****:*++:*+*+xxx++++**:*++x++xxxxx+x++xx%%:.---@###################################################" + System.Environment.NewLine +
                "########################################################@:++**--:**::*+*++*:**::+x%x%%%xx+x%%%@@@%%%%%+x@%%%%%+*-:###################################################" + System.Environment.NewLine +
                "########################################################*-....::*+++*:::::---.:::*xxxx++xx++x%%%%%xx++x++@@%%%x+:%###################################################" + System.Environment.NewLine +
                "######################################################x  ..-:-:*++:--:----:.--:-**+xxx%%%+x%x%x%%%%%%%xxx+%@%xx-*%###################################################" + System.Environment.NewLine +
                "####################################################@:---**:::-----.. .--......--*+++xx%x%+%x%%%%%%%%%x%%@@%%%x**:.##################################################" + System.Environment.NewLine +
                "###################################################--:-:::*::.  .   ..         --:**+xxxxxxx%%%x%%%%%%%@%%%@xx%%+:. @################################################" + System.Environment.NewLine +
                "##################################################%-*-*+--:-                  .-:*+++++xxx%%%%%%%%%@%%%%%%%%%+x%x*. %################################################" + System.Environment.NewLine +
                "################################################%----:*:::.                  .-::-**+x+xxxx%%%%%%%%%@%%%%%x%%x%@x:-.+################################################" + System.Environment.NewLine +
                "################################################-::-:**:::.                  .-::******++++%%%x%%xxxx%x%%xx%x%@@%*--x################################################" + System.Environment.NewLine +
                "################################################:--::---+-                       .---::::*++++x++**++++x++*::*%@%+-:.x###############################################" + System.Environment.NewLine +
                "#################################################-:----*-.                            .-::::**:::::::*+***-..-x%@%*..x###############################################" + System.Environment.NewLine +
                "#################################################x.--*---                                .....------::**:-....*%%%+: x###############################################" + System.Environment.NewLine +
                "#################################################+-::---.                                   . ....---::-:-....-x%%x*-x###############################################" + System.Environment.NewLine +
                "#################################################x:*::---                                      ......------...-+%%++-x###############################################" + System.Environment.NewLine +
                "#################################################@::::-.                                         .....-----...-+%%+**################################################" + System.Environment.NewLine +
                "#################################################+:+:::                                           .....------.:xx+x:%################################################" + System.Environment.NewLine +
                "###############################################@..**:*-                                           ....--------*xxx**#################################################" + System.Environment.NewLine +
                "###############################################%:::*::                                           ......--:---::x%*x*@################################################" + System.Environment.NewLine +
                "#################################################@:*:-                                     .............-------*%%xx#################################################" + System.Environment.NewLine +
                "##############################################- .-*:--       ..--:::*+xxxxx+*-....... .........---::::-.....---:xx+@#################################################" + System.Environment.NewLine +
                "#############################################*    .--.           .--:::*:-.-.---...  ...-:*+x%%xxx%%xx++*-...-.-xxx##################################################" + System.Environment.NewLine +
                "#############################################:     .:.        .: .:*+xxx+*x+-..       -:+++++**++*****+xxx+- ..*x+@##################################################" + System.Environment.NewLine +
                "#############################################:.               .--  -x%%%x:+:*:.       -+x++%%xxxxxxxxx%+**::.. *%%+##################################################" + System.Environment.NewLine +
                "#############################################:.                     -:::-::*:.         :%%xx%++@%@@@xxx+x**:-. :x*:. x###############################################" + System.Environment.NewLine +
                "#############################################:    ..                ..----..           .*xxx+*:*x%%++xx+++*:-. :*::*-%###############################################" + System.Environment.NewLine +
                "#############################################-  ..-.                                  .--:*+****:::*******::.. -::::*################################################" + System.Environment.NewLine +
                "#############################################- .                                      .--::-:::::::*+**::---...-:*-:+################################################" + System.Environment.NewLine +
                "#############################################-                                        .--::-..-------::----...-+::::#################################################" + System.Environment.NewLine +
                "#############################################-                                         .----............ ... :-:*:-*#################################################" + System.Environment.NewLine +
                "#############################################*                                          .-*-...  .  .   .--.-*-:*+*%#################################################" + System.Environment.NewLine +
                "##############################################%+    +*                                ..-.-*-......  ..-.--.::*+:-:##################################################" + System.Environment.NewLine +
                "#####################################################@               ..       .    ..-*::-*+:-........-..---**---:###################################################" + System.Environment.NewLine +
                "######################################################*            ..             -::*x%x++*:::-...--------+*-::..@##################################################" + System.Environment.NewLine +
                "######################################################@          .-             -::*++xxxx*:::*:-.---::::.+##+*+x@###################################################" + System.Environment.NewLine +
                "#######################################################+        -.             .-:::-*x+++**:::+*---:::-.-###########################################################" + System.Environment.NewLine +
                "########################################################        .  .             ..-.-:*********+:.-::--.%###########################################################" + System.Environment.NewLine +
                "########################################################*       . .-:x*.........-::**********++***.-::-.*############################################################" + System.Environment.NewLine +
                "########################################################*             :+ .. . ..::--+**x*x%@@x+:*-.--:--#############################################################" + System.Environment.NewLine +
                "########################################################%                .--.      .-:+%%@@%x+*:-.-:-:-x#############################################################" + System.Environment.NewLine +
                "########################################################%                     .-.-:-:***+++**:-..-::*-*##############################################################" + System.Environment.NewLine +
                "########################################################%                    ... ...--:*+***:-----:*:.@##############################################################" + System.Environment.NewLine +
                "########################################################%                 ..--------::*****:::-:::*:.-@##############################################################" + System.Environment.NewLine +
                "#########################################################*                ..--::::::::**+*:::::::::-::@##############################################################" + System.Environment.NewLine +
                "#########################################################-                       .-:::::--::*::**::**-@##############################################################" + System.Environment.NewLine +
                "#########################################################.                    .....---:-:::**:::-:+*:-@##############################################################" + System.Environment.NewLine +
                "#########################################################.                    ....-:::*******:::*++**-@##############################################################" + System.Environment.NewLine +
                "#####################################################@:*                  ...-----::**++*******+++***- %#############################################################" + System.Environment.NewLine +
                "####################################################:-xx              ...--:::::*:*++++++***+++******.:-:############################################################" + System.Environment.NewLine +
                "##################################################+.*+@+                 ..--::*:********+++++*******-x%:@###########################################################" + System.Environment.NewLine +
                "################################################@..*xx#*                    ...-:*****+++++++++**+++**@#%:x##########################################################" + System.Environment.NewLine +
                "###############################################+ -+xx@#.                     ..-:::***++++++***++++++x@#@x: -########################################################" + System.Environment.NewLine +
                "##############################################+ -*x%@#+                      ..-:*****++++*****+++++x%@@@@x:  x######################################################" + System.Environment.NewLine +
                "#############################################+ -*x%%#@:                     ..-::::***+++********++x%@#@@%xx*  .:####################################################" + System.Environment.NewLine +
                "###########################################@..-*x%%#@%:                      ..--::***++*******+++xx%@#@@@%x*:. .  -+@###############################################" + System.Environment.NewLine +
                "#########################################+  -::*xx%#@x:.                       ..--:*+++:::::**++xxx%%@@%%%x+*-. .. -*::+############################################" + System.Environment.NewLine +
                "####################################x::  ..---*x%x%#@x:.                        ..-:*++*:::::*++xxxx%%@@%%%xx+**-   ..-.:-  *%#######################################" + System.Environment.NewLine +
                "###############################%:..... .. --:+*+xx%#%+:..                      ..-:*++*:---:*+++xxxxx%@@%x%x++*:::.  ...-..::. .:%###################################" + System.Environment.NewLine +
                "###########################@*. ..  ...-....-:+*+xx@@%+:-.                    ..--:::**:---::*++++++%@@@%xxx++***:::-   ..----::-... .+###############################" + System.Environment.NewLine +
                "########################+.  ......-... -x%@####++%##%+:-..                   ....--::----::*++++x@@@@#@xx++x@@#@@%x*-.. .---------..     -+##########################" + System.Environment.NewLine +
                "####################%-   .---....-.- .%xx@@###@:@%###@+:-.                    ...-----..-:**+%%@@@%+++%%+x###########@x:   .-..     .-:-     .x@#####################" + System.Environment.NewLine +
                "################x-    .---------... %x:%@@@@##+.x#@@@@%%x:                    ....--...-*+%@@@@@x+@@@@@@@#############@@%+. .:**++x%%@%xx:..--..-:@##################" + System.Environment.NewLine +
                "############x-.   .--:::::--::..  ++.:%@#@@@@#x:.x#@@@%%x+x+:. -.        ...........:+%@@@@@@@@+%@@@@@@###############@@@@%%- *%%%%@@%xx++*--------.:%###############" + System.Environment.NewLine +
                "#####+::+.  .---::::*::**::::  -+*::+@@@#@@@@@#*-@#%%%%%xx+++*+++*:-.   . ..--*+x%@%%%%%@@%%%x-x@@##################@@@@@@@@%x x@@@%%%xx+++*----::-.-..+#############" + System.Environment.NewLine +
                "#%*     .--.-:::*+*+**:**:*..x+::*+%%@@@@@@@@@@+-%#@%%%%%++++++++++++xxxxx%%%%%%%%%%%%%%%x%%+%####################@@@@@@@@@@@@x:%@%%%%%%x+++*:::::::::--. :x#########" + System.Environment.NewLine +
                "      .-.--::****++++*+***-x+:::++x%@@@@@@%%@@%*.x@@@%%%%%x+****++++++xxxxxx%%%%xxxxxx%%xx++*%@@@@@@#######@@@@@@@@@@@@@@@@@@@%+%@%%%%xxxxx++***:::::::----..:@######" + System.Environment.NewLine +
                "    ...---:::*:****++*+++**+*:*+x%xx%%%@%%%%%%x.-+#@xxxx%%%x++xx+++++++++xxxxxx%%xx++***++**x@@@@@@#####@@@@@@@@@@@@@@@@@@@@@@@@@%%%xxxxxx+++++++*::*:::-:---.:+@####" + System.Environment.NewLine +
                ".  ........----:::::***++++++++xxxxxxx%%%%xxx%*x##@%+++*+x%%+*********:::::********::**:::::@@@@@@#####@@@@@%%%%%%@@@@@@@@@@@@@%%%%xxxxx+++++++++****::::-:*:----@###";
        }
    }

}
