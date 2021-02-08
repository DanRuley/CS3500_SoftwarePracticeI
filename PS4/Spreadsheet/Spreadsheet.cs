//Written by Dan Ruley, September 2019

using SpreadsheetUtilities;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace SS
{
    /// <summary>
    /// An AbstractSpreadsheet object represents the state of a simple spreadsheet.  A 
    /// spreadsheet consists of an infinite number of named cells.
    /// 
    /// A string is a valid cell name if and only if:
    ///   (1) its first character is an underscore or a letter
    ///   (2) its remaining characters (if any) are underscores and/or letters and/or digits
    /// Note that this is the same as the definition of valid variable from the PS3 Formula class.
    /// 
    /// For example, "x", "_", "x2", "y_15", and "___" are all valid cell  names, but
    /// "25", "2x", and "&" are not.  Cell names are case sensitive, so "x" and "X" are
    /// different cell names.
    /// 
    /// A spreadsheet contains a cell corresponding to every possible cell name.  (This
    /// means that a spreadsheet contains an infinite number of cells.)  In addition to 
    /// a name, each cell has a contents and a value.  The distinction is important.
    /// 
    /// The contents of a cell can be (1) a string, (2) a double, or (3) a Formula.  If the
    /// contents is an empty string, we say that the cell is empty.  (By analogy, the contents
    /// of a cell in Excel is what is displayed on the editing line when the cell is selected.)
    /// 
    /// In a new spreadsheet, the contents of every cell is the empty string.
    ///  
    /// The value of a cell can be (1) a string, (2) a double, or (3) a FormulaError.  
    /// (By analogy, the value of an Excel cell is what is displayed in that cell's position
    /// in the grid.)
    /// 
    /// If a cell's contents is a string, its value is that string.
    /// 
    /// If a cell's contents is a double, its value is that double.
    /// 
    /// If a cell's contents is a Formula, its value is either a double or a FormulaError,
    /// as reported by the Evaluate method of the Formula class.  The value of a Formula,
    /// of course, can depend on the values of variables.  The value of a variable is the 
    /// value of the spreadsheet cell it names (if that cell's value is a double) or 
    /// is undefined (otherwise).
    /// 
    /// Spreadsheets are never allowed to contain a combination of Formulas that establish
    /// a circular dependency.  A circular dependency exists when a cell depends on itself.
    /// For example, suppose that A1 contains B1*2, B1 contains C1*2, and C1 contains A1*2.
    /// A1 depends on B1, which depends on C1, which depends on A1.  That's a circular
    /// dependency.
    /// </summary>
    public class Spreadsheet : AbstractSpreadsheet
    {
        /// <summary>
        /// Graph containing the dependency relations of the cells in this spreadsheet.
        /// </summary>
        private DependencyGraph SSDependencies;

        /// <summary>
        /// Set containing all non-empty cells in this spreadsheet.
        /// </summary>
        private Dictionary<string, SpreadsheetCell> NonEmptyCells;

        /// <summary>
        /// True if this spreadsheet has been modified since it was created or saved                  
        /// (whichever happened most recently); false otherwise.
        /// </summary>
        public override bool Changed { get; protected set; }

        /// <summary>
        /// Builds a new spreadsheet, consisting of an infinite number of empty cells.
        /// </summary>
        public Spreadsheet() : this(s => true, s => s, "default")
        {
        }

        /// <summary>
        /// 3 arg validator, normalizer, version
        /// </summary>
        /// <param name="isValid"></param>
        /// <param name="normalize"></param>
        /// <param name="version"></param>
        public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version) : base(isValid, normalize, version)
        {
            SSDependencies = new DependencyGraph();
            NonEmptyCells = new Dictionary<string, SpreadsheetCell>();
        }

        /// <summary>
        /// Four arg - filename
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="isValid"></param>
        /// <param name="normalize"></param>
        /// <param name="version"></param>
        public Spreadsheet(string filename, Func<string, bool> isValid, Func<string, string> normalize, string version) : this(isValid, normalize, version)
        {
            string fileversion = ReadFile(filename);

            if (fileversion != version)
                throw new SpreadsheetReadWriteException("Error: version contained in the file does match.");

            Changed = false;
        }


        /// <summary>
        /// Enumerates the names of all the non-empty cells in the spreadsheet.
        /// </summary>
        public override IEnumerable<string> GetNamesOfAllNonemptyCells()
        {
            return new HashSet<string>(NonEmptyCells.Keys);
        }

        /// <summary>
        /// If name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, returns the contents (as opposed to the value) of the named cell.  The return
        /// value should be either a string, a double, or a Formula.
        /// </summary>
        public override object GetCellContents(string name)
        {
            if (name == null || !Regex.IsMatch(name, "^[a-zA-Z](?: [a-zA-Z]|[0-9])*"))
                throw new InvalidNameException();

            SpreadsheetCell cell;

            name = Normalize(name);

            //Return the contents if it is non empty
            if (NonEmptyCells.TryGetValue(name, out cell))
                return cell.GetContents();

            //else return "" since it is an empty cell
            return "";
        }


        // ADDED FOR PS5
        /// <summary>
        /// If content is null, throws an ArgumentNullException.
        /// 
        /// Otherwise, if name is null or invalid, throws an InvalidNameException.
        /// 
        /// Otherwise, if content parses as a double, the contents of the named
        /// cell becomes that double.
        /// 
        /// Otherwise, if content begins with the character '=', an attempt is made
        /// to parse the remainder of content into a Formula f using the Formula
        /// constructor.  There are then three possibilities:
        /// 
        ///   (1) If the remainder of content cannot be parsed into a Formula, a 
        ///       SpreadsheetUtilities.FormulaFormatException is thrown.
        ///       
        ///   (2) Otherwise, if changing the contents of the named cell to be f
        ///       would cause a circular dependency, a CircularException is thrown,
        ///       and no change is made to the spreadsheet.
        ///       
        ///   (3) Otherwise, the contents of the named cell becomes f.
        /// 
        /// Otherwise, the contents of the named cell becomes content.
        /// 
        /// If an exception is not thrown, the method returns a list consisting of
        /// name plus the names of all other cells whose value depends, directly
        /// or indirectly, on the named cell. The order of the list should be any
        /// order such that if cells are re-evaluated in that order, their dependencies 
        /// are satisfied by the time they are evaluated.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// list {A1, B1, C1} is returned.
        /// </summary>
        public override IList<string> SetContentsOfCell(string name, string content)
        {
            if (content == null)
                throw new ArgumentNullException();

            if (name == null || !Regex.IsMatch(name, "^[a-zA-Z](?: [a-zA-Z]|[0-9])*") || !IsValid(name))
                throw new InvalidNameException();

            if (Double.TryParse(content, out _))
                return SetCellContents(Normalize(name), Double.Parse(content));

            else if (content.Length > 0 && content[0] == '=')
                return SetCellContents(Normalize(name), new Formula(content.Trim('='), Normalize, IsValid));

            else return SetCellContents(Normalize(name), content);

        }

        // MODIFIED PROTECTION FOR PS5
        /// <summary>
        /// The contents of the named cell becomes number.  The method returns a
        /// list consisting of name plus the names of all other cells whose value depends, 
        /// directly or indirectly, on the named cell. The order of the list should be any
        /// order such that if cells are re-evaluated in that order, their dependencies 
        /// are satisfied by the time they are evaluated.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// list {A1, B1, C1} is returned.
        /// </summary>
        protected override IList<string> SetCellContents(string name, double number)
        {
            Changed = true;

            //if old cell value was a formula, need to erase any dependees [name] may have had
            if (NonEmptyCells.ContainsKey(name) && NonEmptyCells[name].GetContents() is Formula)
                SSDependencies.ReplaceDependees(name, new HashSet<string>());
            UpdateNonEmptyCellSetAndAdd(name, number);

            List<string> DependentCells = new List<string>(GetCellsToRecalculate(name));

            foreach (string cell in DependentCells)
                Recalculate(cell);

            return DependentCells;
        }

        // MODIFIED PROTECTION FOR PS5
        /// <summary>
        /// The contents of the named cell becomes text.  The method returns a
        /// list consisting of name plus the names of all other cells whose value depends, 
        /// directly or indirectly, on the named cell. The order of the list should be any
        /// order such that if cells are re-evaluated in that order, their dependencies 
        /// are satisfied by the time they are evaluated.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// list {A1, B1, C1} is returned.
        /// </summary>
        protected override IList<string> SetCellContents(string name, string text)
        {
            Changed = true;

            //if old cell value was a formula, need to erase any dependees [name] may have had
            if (NonEmptyCells.ContainsKey(name) && NonEmptyCells[name].GetContents() is Formula)
                SSDependencies.ReplaceDependees(name, new HashSet<string>());
            UpdateNonEmptyCellSetAndAdd(name, text);

            List<string> DependentCells = new List<string>(GetCellsToRecalculate(name));

            foreach (string cell in DependentCells)
                Recalculate(cell);

            return DependentCells;
        }

        // MODIFIED PROTECTION FOR PS5
        /// <summary>
        /// If changing the contents of the named cell to be the formula would cause a 
        /// circular dependency, throws a CircularException, and no change is made to the spreadsheet.
        /// 
        /// Otherwise, the contents of the named cell becomes formula. The method returns a
        /// list consisting of name plus the names of all other cells whose value depends,
        /// directly or indirectly, on the named cell. The order of the list should be any
        /// order such that if cells are re-evaluated in that order, their dependencies 
        /// are satisfied by the time they are evaluated.
        /// 
        /// For example, if name is A1, B1 contains A1*2, and C1 contains B1+A1, the
        /// list {A1, B1, C1} is returned.
        /// </summary>
        protected override IList<string> SetCellContents(string name, Formula formula)
        {
            SpreadsheetCell OldCell = null;
            if (NonEmptyCells.ContainsKey(name))
                OldCell = new SpreadsheetCell(NonEmptyCells[name].GetContents());

            UpdateNonEmptyCellSetAndAdd(name, formula);

            HashSet<string> FormulaVariables = new HashSet<string>(formula.GetVariables());

            //Preserve [name]'s old dependees so we can restore graph is a cycle is detected.
            HashSet<string> OldDependees = new HashSet<string>(SSDependencies.GetDependees(name));

            //New dependees of [name] will be the variables in the formula
            SSDependencies.ReplaceDependees(name, FormulaVariables);

            //[name] will be a dependent of all variables in formula - note: does not replace
            foreach (string s in FormulaVariables)
                SSDependencies.AddDependency(s, name);

            try
            {
                //recalculate all values for this cell and all dependents
                List<string> DependentCells = new List<string>(GetCellsToRecalculate(name));

                //Only set changed to true if no circular dependency was detected.
                Changed = true;
                foreach (string cell in DependentCells)
                    Recalculate(cell);
                return DependentCells;
            }

            //Circular exception was thrown, we must restore dependency graph.
            catch (CircularException e)
            {
                //restore old contents of cell if they existed
                if (OldCell != null)
                    NonEmptyCells[name] = OldCell;

                SSDependencies.ReplaceDependees(name, OldDependees);
                foreach (string s in FormulaVariables)
                    SSDependencies.RemoveDependency(s, name);
                throw e;
            }
        }

        /// <summary>
        /// If name is null, throws an ArgumentNullException.
        /// 
        /// Otherwise, if name isn't a valid cell name, throws an InvalidNameException.
        /// 
        /// Otherwise, returns an enumeration, without duplicates, of the names of all cells whose
        /// values depend directly on the value of the named cell.  In other words, returns
        /// an enumeration, without duplicates, of the names of all cells that contain
        /// formulas containing name.
        /// 
        /// For example, suppose that
        /// A1 contains 3
        /// B1 contains the formula A1 * A1
        /// C1 contains the formula B1 + A1
        /// D1 contains the formula B1 - C1
        /// The direct dependents of A1 are B1 and C1
        /// </summary>
        protected override IEnumerable<string> GetDirectDependents(string name)
        {
            return new HashSet<string>(SSDependencies.GetDependents(name));
        }

        /// <summary>
        /// Recalculate the value of the given cell.  If the cell contains a formula, the value is the result of the formula Evaluate method.  Otherwise, the value is set to be either the double or string contents of the cell.
        /// </summary>
        /// <param name="cell"></param>
        private void Recalculate(string cell)
        {
            object value;
            //if contents of cell is a formula, evaluate that formula and set its result to value, otherwise value = cell contents (double or string)
            if (GetCellContents(cell) is Formula)
            {
                Formula f = (Formula)GetCellContents(cell);
                value = f.Evaluate(CellLookup);
            }
            else
                value = GetCellContents(cell);

            //Set the value of the cell if it is nonempty
            if (NonEmptyCells.ContainsKey(cell))
                NonEmptyCells[cell].SetValue(value);
        }

        /// <summary>
        /// Retrieves a cell's value for Formula's evaluate method.  Either returns a double if the value exists, or throws an ArgumentException.
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        private double CellLookup(string cell)
        {
            //We can only return a double if the cell is nonempty and its value is a double.
            if (NonEmptyCells.ContainsKey(cell))
            {
                if (Double.TryParse(GetCellValue(cell).ToString(), out _))
                    return (double)GetCellValue(cell);
            }

            //Otherwise, throw ArEx to notify formula that the cell's value should be a FormulaError.
            throw new ArgumentException();

        }



        /// <summary>
        /// Updates the set of non-empty cells in the spreadsheet and also sets the contents of the cell [name].
        /// </summary>
        /// <param name="name"></param>
        /// <param name="contents"></param>
        private void UpdateNonEmptyCellSetAndAdd(string name, object contents)
        {
            //If cell considered nonempty and its contents are changed to "", remove it.
            if (NonEmptyCells.ContainsKey(name) && contents is string && (string)contents == "")
            {
                NonEmptyCells.Remove(name);
                return;
            }
            //If it is not considered empty and its contents are set to "", do nothing.
            if (!NonEmptyCells.ContainsKey(name) && contents is string && (string)contents == "")
                return;

            //If [name] is already nonempty, change its contents.
            if (NonEmptyCells.ContainsKey(name))
                NonEmptyCells[name].SetContents(contents);
            //Otherwise, add a new cell [name] with the contents.
            else
                NonEmptyCells.Add(name, new SpreadsheetCell(contents));
        }

        // ADDED FOR PS5
        /// <summary>
        /// Returns the version information of the spreadsheet saved in the named file.
        /// If there are any problems opening, reading, or closing the file, the method
        /// should throw a SpreadsheetReadWriteException with an explanatory message.
        /// </summary>
        public override string GetSavedVersion(string filename)
        {
            return ReadFile(filename);
        }


        /// <summary>
        /// Helper method for constructing a spreadsheet based on a given file in XML format.  Throws SpreadsheetReadWriteException if the XML file is not formatted correctly or missing a version.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private string ReadFile(string filename)
        {
            string fileversion = "";
            try
            {
                // Create an XmlReader inside this block, and automatically Dispose() it at the end.
                using (XmlReader reader = XmlReader.Create(filename))
                {
                    string CellName = "";

                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            string s = reader.Name;
                            if (s.ToLower() == "spreadsheet")
                                fileversion = reader["version"];


                            else if (s.ToLower() == "cell")
                            {
                            }


                            else if (s.ToLower() == "name")
                            {
                                reader.Read();
                                CellName = reader.Value;
                            }


                            else if (s.ToLower() == "contents")
                            {
                                reader.Read();
                                SetContentsOfCell(CellName, reader.Value);
                            }

                            else
                                throw new SpreadsheetReadWriteException("Error: Invalid spreadsheet XML format.");

                        }
                        else; // If it's not a start element, it's an end element -> do nothing
                    }
                        
                        
                }
            }
            
            //throw any parsing errors as a SSReadWriteException
            catch (Exception e)
            {
                throw new SpreadsheetReadWriteException(e.Message);
    }

            return fileversion;
        }

// ADDED FOR PS5
/// <summary>
/// Writes the contents of this spreadsheet to the named file using an XML format.
/// The XML elements should be structured as follows:
/// 
/// <spreadsheet version="version information goes here">
/// 
/// <cell>
/// <name>
/// cell name goes here
/// </name>
/// <contents>
/// cell contents goes here
/// </contents>    
/// </cell>
/// 
/// </spreadsheet>
/// 
/// There should be one cell element for each non-empty cell in the spreadsheet.  
/// If the cell contains a string, it should be written as the contents.  
/// If the cell contains a double d, d.ToString() should be written as the contents.  
/// If the cell contains a Formula f, f.ToString() with "=" prepended should be written as the contents.
/// 
/// If there are any problems opening, writing, or closing the file, the method should throw a
/// SpreadsheetReadWriteException with an explanatory message.
/// </summary>
public override void Save(string filename)
{
    try
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Indent = true;
        settings.IndentChars = "  ";

        // Create an XmlWriter inside this block, and automatically Dispose() it at the end.
        using (XmlWriter writer = XmlWriter.Create(filename, settings))
        {

            writer.WriteStartDocument();
            writer.WriteStartElement("Spreadsheet");
            //Add the version as an attribute
            writer.WriteAttributeString("version", Version);

            foreach (string s in NonEmptyCells.Keys)
            {
                writer.WriteStartElement("Cell");

                writer.WriteStartElement("Name");
                writer.WriteValue(s);
                writer.WriteEndElement();   //End name block

                writer.WriteStartElement("Contents");
                object contents = NonEmptyCells[s].GetContents();

                if (contents is Formula)
                    writer.WriteValue("=" + contents.ToString());
                else
                    writer.WriteValue(contents.ToString());

                writer.WriteEndElement();   //end Content block

                writer.WriteEndElement();   //end Cell block
            }

            writer.WriteEndElement(); // Ends the Spreadsheet block
            writer.WriteEndDocument();
        }
    }

    catch (Exception)
    {
        throw new SpreadsheetReadWriteException("Error: invalid filename");
    }

    //no exception thrown, so save successful; reset changed.
    Changed = false;
}


// ADDED FOR PS5
/// <summary>
/// If name is null or invalid, throws an InvalidNameException.
/// 
/// Otherwise, returns the value (as opposed to the contents) of the named cell.  The return
/// value should be either a string, a double, or a SpreadsheetUtilities.FormulaError.
/// </summary>
public override object GetCellValue(string name)
{
    if (name == null || !Regex.IsMatch(name, "^[a-zA-Z](?: [a-zA-Z]|[0-9])*"))
        throw new InvalidNameException();

    if (NonEmptyCells.ContainsKey(name))
        return NonEmptyCells[name].GetValue();

    else
        return "";
}

/// <summary>
/// Private class used by the Spreadsheet to define a SpreadsheetCell object.
/// </summary>
private class SpreadsheetCell
{
    /// <summary>
    /// The contents of the spreadsheet cell.
    /// </summary>
    private object CellContents;

    /// <summary>
    /// The value of the spreadsheet cell.
    /// </summary>
    private object Value;


    /// <summary>
    /// Constructor that assigns this.contents to the input.
    /// </summary>
    /// <param name="contents"></param>
    public SpreadsheetCell(object contents)
    {
        CellContents = contents;
    }

    /// <summary>
    /// Returns the contents of this cell.
    /// </summary>
    /// <returns></returns>
    public object GetContents()
    {
        return CellContents;
    }

    /// <summary>
    /// Sets the contents of this cell.
    /// </summary>
    /// <param name="o"></param>
    public void SetContents(object o)
    {
        CellContents = o;
    }

    /// <summary>
    /// Gets the value of this cell.
    /// </summary>
    /// <returns></returns>
    public object GetValue()
    {
        return Value;
    }

    /// <summary>
    /// Sets the value of this cell.
    /// </summary>
    /// <param name="o"></param>
    public void SetValue(object o)
    {
        Value = o;
    }
}
    }
}
