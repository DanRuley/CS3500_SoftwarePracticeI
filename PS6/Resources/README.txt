Project started September 29, 2019, last modified October 4th, 2019
University of Utah CS 3500
Team 167: 
	-Gavin Gray : u1040250
	-Dan Ruley  : u0956834


Attributions:
Spreadsheet .ICO found at: https://icon-icons.com/icon/data-information-open-tech/112856
Error .ICO found at: https://i2.wp.com/envirobites.org/wp-content/uploads/2018/02/Nuclear_symbol.png
static Prompt class to get number of columns taken from: https://stackoverflow.com/questions/5427020/prompt-dialog-in-windows-forms

Project Considerations:
	-Extra features: hotkey mapping, automatic keyboard scrolling, additional save/open features, a special drawing feature, better SS window and error icons.
	-Circular Exception and FormulaFormatExceptions handled by a temporary text notification instead of a message box.  That way the user does not have to constantly click through windows if they make simple errors.
	
MAIN ADDITIONAL FEATURE:
	- We have decided that it would be fun to display ASCII images in the spreadsheet. 
	- File > Open Image > Chat with Danny : opens a new spreadsheet with an ascii Daniel Kopta.
	- File > Open Image > Open New Image : lets the user open whatever image they would like from their machine that will get converted and displayed in ASCII.

	- How this was implemented:
		class ConvertFileToBitmap : namespace CVRT
			- An object of type ConvertFileToBitmap must receive a string in its constructor, which is the filepath to the image that is to be converted.
			- An overloaded constructor is also available which takes in the desired width of the image (in characters).

			- Upon creation of the object a System.Drawing.Bitmap is created from the file specified.
		Method GenerateAscii():
			- If necessary the Bitmap is resized.
			- Then the Bitmap is normalized into grayscale.
			- Based off of the grayscale value an ascii char is chosen to match the darkness. From a set of ten chars {"#", "@", "%", "x", "+", "*", ":", "-", ".", " "}
				* Note that we could not use the char '=' because if it fell at the beginning of a cell the spreadsheet wants it to be a formula, which is invalid.
			- A string is returned that contains the full ascii image.

		class GenXMLFromJPG : namespace Convert
			- The GenXMLFromJPG is basically just a wrapper on top of the ConvertFileToBitmap class in order to create a Spreadsheet from the ascii string.
				* Also I now realize that it is a poorly named class because the image isn't necessarily a .jpg
			- Upon construction this object instantiates a ConvertFileToBitmap object and saves the returned ascii string.

			- The constructor for this object takes in a filepath, width, and delegate validator (for the width) as parameters.
			- There is also an overloaded contructor which takes in a spreadsheet object which is where the ascii string will be saved.
			- The width is desired width of the image (in chars) and the delegator makes sure that the width will fit okay in our spreadsheet.
				* If the width does not fit in the spreadsheet then an Exception is thrown.
		Method GenFilledSpread():
			- Takes the ascii string that was returned by the ConvertFileToBitmap object and parses it such that it fills the spreadsheet.

		- Useful links in the process of creation:
			- The main portion of the ConvertFileToBitmap object is found here: https://dotnetfiddle.net/neyqDF
				* Things were edited in order to acommodate the different charset, reading from a file and not a URL as well as a few other minor changes.
	  		- The following sites were read in order to make accurate changes to the above project to conform to our specific needs.
			- https://www.geeksforgeeks.org/converting-image-ascii-image-python/
			- https://bitesofcode.wordpress.com/2017/01/19/converting-images-to-ascii-art-part-1/
			- http://paulbourke.net/dataformats/asciiart/

Other additional features:
	- Quick Navigation and Editing with keyboard shortcuts, allowing the user to never leave the keyboard if desired.
	- Scrolling to always display the selected cell.
	- Double clicks by the mouse allow you to edit a cell.


GUI variables:
	- form_spreadsheet: Underlying Spreadsheet object.
	- SpreadsheetGrid: The main drawing of spreadsheet cells
	- CoordDisplayBox: The text box that displays the current selected coordinate.
	- DisplayValueBox: Displays the value of the selected cell.
	- DisplayContentsBox: Displays the contents of the selected cell, also available for editing to edit the value of the cell.
	- SetContentsButton: Changes the contents of the selected cell, wired with the eneter button.

	//Menu
	- MenuStrip: Main strip containing the menus.
	- FileMenuHeader: The top of the File menu.
	- HelpMenuHeader: The top of the Help menu.
	- OpenFileMenuItem: The "OpenFileMenu" menu item that triggers file explorer.
	- SaveFileMenuItem: The "SaveFileMenu" menu item that triggers file explorer.
	- NewFileMenuItem: The "New" menu item that creates a new blank spreadsheet.
	- KBShortcutsMenuItem: The "KeyboardShortcuts" menu item that displays a list of hotkeys.
	- OpenFileDialog: File explorer dialog window for opening files.
	- SaveFileDialog: File explorer dialog window for saving files.