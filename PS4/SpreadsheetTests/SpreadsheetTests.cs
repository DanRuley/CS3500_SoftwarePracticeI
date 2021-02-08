//Written by Dan Ruley, September 2019
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpreadsheetUtilities;
using SS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SpreadsheetTests
{
    /// <summary>
    /// Test suite for the Spreadsheet class.
    /// </summary>
    [TestClass]
    public class SpreadsheetTests
    {
        [TestMethod]
        public void TestSimpleSetCellFormulaContents()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "=B1 + C1");
            HashSet<string> cells = new HashSet<string>(ss.GetNamesOfAllNonemptyCells());
            Assert.IsTrue(cells.Contains("A1"));

            //A1 contents should be a Formula with a tostring value of "B1+C1", it's being returned as an object but since it overrides object tostring, will return the Formula ToString
            Assert.AreEqual("B1+C1", ss.GetCellContents("A1").ToString());
        }

        [TestMethod]
        public void TestComplexNonEmptyValuesAndCellValues()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "=B1 + C1");
            ss.SetContentsOfCell("B1", "=D1 + E1");
            ss.SetContentsOfCell("C1", "=F1 + G1");
            ss.SetContentsOfCell("D1", "3500 is fun");
            ss.SetContentsOfCell("E1", "2.71828");
            ss.SetContentsOfCell("F1", "1");
            ss.SetContentsOfCell("G1", "meow");

            HashSet<string> expected = new HashSet<string> { "A1", "B1", "C1", "D1", "E1", "F1", "G1" };

            //Make sure all those cells are indeed included in SS' nonempty set
            foreach (string s in ss.GetNamesOfAllNonemptyCells())
                Assert.IsTrue(expected.Contains(s));

            //A bunch of content checks
            Assert.AreEqual("B1+C1", ss.GetCellContents("A1").ToString());
            Assert.AreEqual("D1+E1", ss.GetCellContents("B1").ToString());
            Assert.AreEqual("F1+G1", ss.GetCellContents("C1").ToString());
            Assert.AreEqual("3500 is fun", ss.GetCellContents("D1"));
            Assert.AreEqual(2.71828, ss.GetCellContents("E1"));
            Assert.AreEqual(1.0, ss.GetCellContents("F1"));
            Assert.AreEqual("meow", ss.GetCellContents("G1"));
        }

        [TestMethod]
        public void TestMoreComplexFormulaValue()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            //build up graph
            ss.SetContentsOfCell("A1", "=B1 + C1");
            ss.SetContentsOfCell("B1", "=D1 + E1");
            ss.SetContentsOfCell("C1", "=F1 + G1");

            List<string> F1Dependents = (List<string>)ss.SetContentsOfCell("F1", "3.14");
            Assert.AreEqual(3, F1Dependents.Count);
            //proper topological ordering should be F1, C1, A1
            Assert.IsTrue(F1Dependents[0] == "F1");
            Assert.IsTrue(F1Dependents[1] == "C1");
            Assert.IsTrue(F1Dependents[2] == "A1");

            List<string> G1Dependents = (List<string>)ss.SetContentsOfCell("G1", "asdf");
            Assert.AreEqual(3, G1Dependents.Count);
            //proper topological ordering should be G1, C1, A1
            Assert.IsTrue(G1Dependents[0] == "G1");
            Assert.IsTrue(G1Dependents[1] == "C1");
            Assert.IsTrue(G1Dependents[2] == "A1");

            List<string> D1Dependents = (List<string>)ss.SetContentsOfCell("D1", "asdf");
            Assert.AreEqual(3, D1Dependents.Count);
            //proper topological ordering should be D1, B1, A1
            Assert.IsTrue(D1Dependents[0] == "D1");
            Assert.IsTrue(D1Dependents[1] == "B1");
            Assert.IsTrue(D1Dependents[2] == "A1");

            List<string> E1Dependents = (List<string>)ss.SetContentsOfCell("E1", "asdf");
            Assert.AreEqual(3, E1Dependents.Count);
            //proper topological ordering should be E1, B1, A1
            Assert.IsTrue(E1Dependents[0] == "E1");
            Assert.IsTrue(E1Dependents[1] == "B1");
            Assert.IsTrue(E1Dependents[2] == "A1");
        }



        [TestMethod]
        [ExpectedException(typeof(CircularException))]
        public void TestSimpleSetCellFormulaValueThrowsCircular()
        {
            Spreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "=A1 + B1 + C1");
        }


        [TestMethod]
        [ExpectedException(typeof(CircularException))]
        public void TestMoreComplexSetCellFormulaValueThrowsCircular()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            //set up a graph 
            ss.SetContentsOfCell("A1", "=B1 + C1");
            ss.SetContentsOfCell("B1", "=D1 + E1");
            ss.SetContentsOfCell("C1", "=F1 + G1");

            //creates a cycle
            ss.SetContentsOfCell("G1", "=A1 + 5");
        }

        //asdf
        [TestMethod]
        public void TestSimpleSetCellValueDouble()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "3.14");
            Assert.AreEqual(3.14, ss.GetCellContents("A1"));
        }

        [TestMethod]
        public void TestSimpleSetCellValueDouble2()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "3.14");
            Assert.AreEqual(3.14, ss.GetCellContents("A1"));

            //make sure resetting cell contents correctly updates
            ss.SetContentsOfCell("A1", "1.12345");
            Assert.AreEqual(1.12345, ss.GetCellContents("A1"));
        }

        [TestMethod]
        public void TestSimpleSetCellValueString()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "Hello");
            Assert.AreEqual("Hello", ss.GetCellContents("A1"));
        }

        [TestMethod]
        public void TestEmptyCellContentsAreEmptyString()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            Assert.AreEqual("", ss.GetCellContents("A1"));
            Assert.AreEqual("", ss.GetCellContents("A2"));

            Assert.IsFalse(ss.GetNamesOfAllNonemptyCells().GetEnumerator().MoveNext());
        }


        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestGetCellContentsNullName()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.GetCellContents(null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestGetCellContentsInvalidName()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.GetCellContents("1_A123");
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestNameTextSetContentsOfCellNullText()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", (string)null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestNameTextSetContentsOfCellNullName()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell((string)null, "hello");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestNameTextSetContentsOfCellInvalidName()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("11A__", "hello");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestNameFormulaSetContentsOfCellInvalidName()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("&&abcd", "=1 + 1");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestNameFormulaSetContentsOfCellNullName()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell(null, "=1 + 1");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestNameFormulaSetContentsOfCellNullFormula()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("&&abcd", null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestNameDoubleSetContentsOfCellNullName()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell(null, "3.14");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestNameDoubleSetContentsOfCellInvalidName()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("1_abcd", "3.14");
        }


        [TestMethod]
        public void StressTest()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            Dictionary<string, double> expected = new Dictionary<string, double>();

            //build up large spreadsheet
            for (int i = 0; i < 1000; i++)
            {
                string s = "A" + i;
                ss.SetContentsOfCell(s, "" + i);
                expected.Add(s, i);
            }

            //check spreadsheet contains correct cells with correct contents
            foreach (string s in ss.GetNamesOfAllNonemptyCells())
            {
                Assert.IsTrue(expected.ContainsKey(s));
                Assert.IsTrue(expected[s] == (double)ss.GetCellContents(s));
            }

            //change contents of all cells
            for (int i = 0; i < 1000; i++)
            {
                string s = "A" + i;
                ss.SetContentsOfCell(s, "3500 is fun");
            }

            //check spreadsheet contains correct cells with correct contents
            foreach (string s in ss.GetNamesOfAllNonemptyCells())
            {
                Assert.IsTrue(expected.ContainsKey(s));
                Assert.IsTrue("3500 is fun" == (string)ss.GetCellContents(s));
            }

            //Empty out the cells
            for (int i = 0; i < 1000; i++)
            {
                string s = "A" + i;
                ss.SetContentsOfCell(s, "");
            }

            //Make sure non-empty cell enumerator does not have a next (there should only be empty cells).
            Assert.IsFalse(ss.GetNamesOfAllNonemptyCells().GetEnumerator().MoveNext());

            //build back up with formulas
            for (int i = 0; i < 1000; i++)
            {
                string s = "A" + i;
                ss.SetContentsOfCell(s, "=B1 + C1");
            }

            //check values and that all cells are accounted for
            foreach (string s in ss.GetNamesOfAllNonemptyCells())
            {
                Assert.AreEqual("B1+C1", ss.GetCellContents(s).ToString());
            }

        }

        [TestMethod]
        public void TestAddingEmptyCellDoesNothing()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "");
            Assert.IsFalse(ss.GetNamesOfAllNonemptyCells().GetEnumerator().MoveNext());
        }

        [TestMethod]
        public void TestConvolutedSetCellFormulaDependencyList()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "=B1 + C1");
            ss.SetContentsOfCell("B1", "=D1 + E1");
            ss.SetContentsOfCell("C1", "=F1 + G1");
            ss.SetContentsOfCell("A2", "=F1 + G1");
            ss.SetContentsOfCell("A3", "=A2 + C1");
            ss.SetContentsOfCell("A4", "=A1 * A3");
            ss.SetContentsOfCell("A5", "=A4 / A3");
            ss.SetContentsOfCell("A6", "=A5");

            List<string> dependentlist = new List<string>(ss.SetContentsOfCell("F1", "3.14"));

            //first should be F1
            Assert.IsTrue(dependentlist[0] == "F1");

            //next come A2 or C1, (1 edge away)
            Assert.IsTrue(dependentlist[1] == "A2" || dependentlist[1] == "C1");
            Assert.IsTrue(dependentlist[2] == "A2" || dependentlist[2] == "C1");

            //next come A1 or A3 (2 edges away)
            Assert.IsTrue(dependentlist[3] == "A1" || dependentlist[3] == "A3");
            Assert.IsTrue(dependentlist[4] == "A1" || dependentlist[4] == "A3");

            //next two should be A4 or A5 (3 edges away)
            Assert.IsTrue(dependentlist[5] == "A4" || dependentlist[5] == "A5");
            Assert.IsTrue(dependentlist[6] == "A4" || dependentlist[6] == "A5");

            //finally, A6 should be 4 edges away and last in list
            Assert.IsTrue(dependentlist[7] == "A6");
        }

        [TestMethod]
        public void TestSpreadsheetFormulaNoVariables()
        {
            AbstractSpreadsheet ss = new Spreadsheet();

            List<string> list = new List<string>(ss.SetContentsOfCell("A1", "=1+2+3"));
            List<string> list1 = new List<string>(ss.SetContentsOfCell("B1", "=7/3 + 1.234"));

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(1, list1.Count);
            Assert.AreEqual("A1", list[0]);
            Assert.AreEqual("B1", list1[0]);
            Assert.IsTrue(ss.GetCellContents("A1").ToString() == "1+2+3");
            Assert.IsTrue(ss.GetCellContents("B1").ToString() == "7/3+1.234");
        }


        [TestMethod]
        public void TestReplacingFormulaWithDoubleChangesGraph()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "=B1 + C1");
            ss.SetContentsOfCell("B1", "=D1 + E1");
            ss.SetContentsOfCell("C1", "=F1 + G1");

            List<string> list = new List<string>(ss.SetContentsOfCell("F1", "5"));
            Assert.AreEqual(3, list.Count);
            Assert.IsTrue(list[0] == "F1");
            Assert.IsTrue(list[1] == "C1");
            Assert.IsTrue(list[2] == "A1");

            ss.SetContentsOfCell("C1", "1.234");

            list = new List<string>(ss.SetContentsOfCell("F1", "5"));
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("F1", list[0]);

        }

        [TestMethod]
        public void TestReplacingFormulaWithStringChangesGraph()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "=B1 + C1");
            ss.SetContentsOfCell("B1", "=D1 + E1");
            ss.SetContentsOfCell("C1", "=F1 + G1");

            List<string> list = new List<string>(ss.SetContentsOfCell("F1", "hello"));
            Assert.AreEqual(3, list.Count);
            Assert.IsTrue(list[0] == "F1");
            Assert.IsTrue(list[1] == "C1");
            Assert.IsTrue(list[2] == "A1");

            ss.SetContentsOfCell("C1", "hello");

            list = new List<string>(ss.SetContentsOfCell("F1", "hello"));
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("F1", list[0]);

        }

        [TestMethod(), Timeout(5000)]
        [ExpectedException(typeof(CircularException))]
        public void TestUndoCircular()
        {
            Spreadsheet s = new Spreadsheet();
            try
            {
                s.SetContentsOfCell("A1", "=A2+A3");
                s.SetContentsOfCell("A2", "15");
                s.SetContentsOfCell("A3", "30");
                s.SetContentsOfCell("A2", "=A3*A1");
            }
            catch (CircularException e)
            {
                Assert.AreEqual(15, (double)s.GetCellContents("A2"), 1e-9);
                throw e;
            }
        }


        //
        //
        //NEW TESTS FOR PS5
        //
        //

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestInvalidFormulaThrows()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "=");
        }

        [TestMethod]
        public void TestMoreComplexCellValue()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "3");
            ss.SetContentsOfCell("A2", "6");
            ss.SetContentsOfCell("A3", "=A1*A2");
            ss.SetContentsOfCell("A4", "=A3/18*100");
            Assert.AreEqual(100, (double)ss.GetCellValue("A4"), 1e-9);
            ss.SetContentsOfCell("A5", "=A4/3");
            Assert.AreEqual(33.33, (double)ss.GetCellValue("A5"), 1e-2);
        }

     
        [TestMethod]
        public void TestSimpleFormulaValue()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "=B1 + C1");
            ss.SetContentsOfCell("B1", "5.5");
            ss.SetContentsOfCell("C1", "5.5");
            Assert.AreEqual(11.0, (double)ss.GetCellValue("A1"), 1e-9);
        }

        [TestMethod]
        public void TestMoreComplexFormulaValueWithChainRecalculation()
        {
            AbstractSpreadsheet ss = new Spreadsheet();

            ss.SetContentsOfCell("A1", "=B1 + C1");
            ss.SetContentsOfCell("D1", "=A1 + 1");
            ss.SetContentsOfCell("E1", "=D1 + 1");
            ss.SetContentsOfCell("F1", "=E1 + 1");

            ss.SetContentsOfCell("B1", "5.5");
            ss.SetContentsOfCell("C1", "5.5");

            Assert.AreEqual(11.0, (double)ss.GetCellValue("A1"), 1e-9);
            Assert.AreEqual(12.0, (double)ss.GetCellValue("D1"), 1e-9);
            Assert.AreEqual(13.0, (double)ss.GetCellValue("E1"), 1e-9);
            Assert.AreEqual(14.0, (double)ss.GetCellValue("F1"), 1e-9);
        }

        [TestMethod]
        public void TestCellWithFormulaDependingOnBlankCellsValueIsFormulaError()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "=B1 + C1");
            Assert.IsTrue(ss.GetCellValue("A1") is FormulaError);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestInvalidNameGetValueThrows()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.GetCellValue("_A1");

        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void TestNullNameGetValueThrows()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.GetCellValue(null);
        }


        //[TestMethod]
        //[ExpectedException(typeof(SpreadsheetReadWriteException))]
        //public void TestInvalidFileThrowsReadWriteException()
        //{
        //    AbstractSpreadsheet ss = new Spreadsheet();

        //    ss.SetContentsOfCell("A1", "=B1 + C1");

        //    ss.Save("Illegal*Filename");
        //}


        [TestMethod]
        public void TestSimpleXMLSave()
        {
            AbstractSpreadsheet ss = new Spreadsheet();

            ss.SetContentsOfCell("A1", "=B1 + C1");
            ss.SetContentsOfCell("B1", "5.5");
            ss.SetContentsOfCell("C1", "5.5");
            ss.SetContentsOfCell("D1", "5.5");

            ss.Save("TestSheet");
            AbstractSpreadsheet ss1 = new Spreadsheet("TestSheet", s => true, s => s, "default");
            Assert.AreEqual(ss.GetCellValue("A1"), ss1.GetCellValue("A1"));
            Assert.AreEqual(ss.GetCellValue("B1"), ss1.GetCellValue("B1"));
            Assert.AreEqual(ss.GetCellValue("C1"), ss1.GetCellValue("C1"));
            Assert.AreEqual(ss.GetCellValue("D1"), ss1.GetCellValue("D1"));
        }

        [TestMethod]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void TestThrowsInvalidFile()
        {
            AbstractSpreadsheet ss = new Spreadsheet("Nonexistant file", s => true, s => s, "default");
        }

        [TestMethod]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void TestMismatchVersionThrows()
        {
            AbstractSpreadsheet ss = new Spreadsheet();

            ss.SetContentsOfCell("A1", "=B1 + C1");
            ss.SetContentsOfCell("B1", "5.5");
            ss.SetContentsOfCell("C1", "5.5");
            ss.SetContentsOfCell("D1", "5.5");

            ss.Save("TestSheet");

            AbstractSpreadsheet ss1 = new Spreadsheet("TestSheet", s => true, s => s, "snoogans");
        }

        [TestMethod]
        public void TestChangedUpdates()
        {
            AbstractSpreadsheet ss = new Spreadsheet();

            ss.SetContentsOfCell("A1", "=B1 + C1");
            ss.SetContentsOfCell("B1", "5.5");
            ss.SetContentsOfCell("C1", "5.5");
            ss.SetContentsOfCell("D1", "5.5");

            ss.Save("TestSheet");
            Assert.IsFalse(ss.Changed);
            ss.SetContentsOfCell("D1", "6.66");
            Assert.IsTrue(ss.Changed);
        }

        [TestMethod]
        public void TestGetVersion()
        {
            AbstractSpreadsheet ss = new Spreadsheet(s => true, s => s, "blah");

            ss.SetContentsOfCell("A1", "=B1 + C1");
            ss.SetContentsOfCell("B1", "5.5");
            ss.SetContentsOfCell("C1", "5.5");
            ss.SetContentsOfCell("D1", "5.5");

            ss.Save("TestSheet");
            Assert.AreEqual("blah", ss.GetSavedVersion("TestSheet"));
        }

        [TestMethod]
        public void StressTestChainedValues()
        {
            AbstractSpreadsheet ss = new Spreadsheet();

            //build up large chain of dependent cells w/ formulas
            for (int i = 1; i < 100; i++)
            {
                int nextCell = i + 1;
                ss.SetContentsOfCell("A" + i, "=A" + nextCell + "+1");
            }

            ss.SetContentsOfCell("A100", "1");
            ss.GetCellValue("A1");

            //assert that the values are all calculated correctly
            int thisCell = 1;
            for (int i = 100; i > 0; i--)
            {
                Assert.AreEqual((double)i, ss.GetCellValue("A" + thisCell));
                thisCell++;
            }
        }

        [TestMethod]
        public void TestValueOfEmptyCellIsEmptyString()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            Assert.AreEqual("", ss.GetCellValue("A1"));
        }

        [TestMethod]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void TestBadFilePathThrows()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.SetContentsOfCell("A1", "=A2+A3");
            ss.Save("/Users/Rasputin/NonexistantPath/File");
        }

        [TestMethod]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void TestInvalidXMLFormatThrows()
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";

            // Create an XmlWriter inside this block, and automatically Dispose() it at the end.
            using (XmlWriter writer = XmlWriter.Create("TestSheet", settings))
            {

                writer.WriteStartDocument();
                writer.WriteStartElement("Spreadsheet");
                //Add the version as an attribute
                writer.WriteAttributeString("version", "default");

                writer.WriteStartElement("Cell");

                //invalid
                writer.WriteStartElement("Bleh");
                writer.WriteValue("asdf");
                writer.WriteEndElement();   //Invalid XML tag

                writer.WriteStartElement("Contents");
                writer.WriteValue("asdf");
                writer.WriteEndElement();   //end Content block

                writer.WriteEndElement();   //end Cell block

                writer.WriteEndElement(); // Ends the Spreadsheet block
                writer.WriteEndDocument();
            }

            AbstractSpreadsheet ss = new Spreadsheet("TestSheet", s => true, s => s, "default");

        }





        // ########################################## PS5 Grading Tests ######################################################







    }



    /// <summary>
    ///This is a test class for SpreadsheetTest and is intended
    ///to contain all SpreadsheetTest Unit Tests
    ///</summary>
    [TestClass()]
    public class GradingTests
    {

        // Verifies cells and their values, which must alternate.
        public void VV(AbstractSpreadsheet sheet, params object[] constraints)
        {
            for (int i = 0; i < constraints.Length; i += 2)
            {
                if (constraints[i + 1] is double)
                {
                    Assert.AreEqual((double)constraints[i + 1], (double)sheet.GetCellValue((string)constraints[i]), 1e-9);
                }
                else
                {
                    Assert.AreEqual(constraints[i + 1], sheet.GetCellValue((string)constraints[i]));
                }
            }
        }


        // For setting a spreadsheet cell.
        public IEnumerable<string> Set(AbstractSpreadsheet sheet, string name, string contents)
        {
            List<string> result = new List<string>(sheet.SetContentsOfCell(name, contents));
            return result;
        }

        // Tests IsValid
        [TestMethod, Timeout(5000)]
        public void IsValidTest1()
        {
            AbstractSpreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A1", "x");
        }

        [TestMethod, Timeout(5000)]
        [ExpectedException(typeof(InvalidNameException))]
        public void IsValidTest2()
        {
            AbstractSpreadsheet ss = new Spreadsheet(s => s[0] != 'A', s => s, "");
            ss.SetContentsOfCell("A1", "x");
        }

        [TestMethod, Timeout(5000)]
        public void IsValidTest3()
        {
            AbstractSpreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("B1", "= A1 + C1");
        }

        [TestMethod, Timeout(5000)]
        [ExpectedException(typeof(FormulaFormatException))]
        public void IsValidTest4()
        {
            AbstractSpreadsheet ss = new Spreadsheet(s => s[0] != 'A', s => s, "");
            ss.SetContentsOfCell("B1", "= A1 + C1");
        }

        // Tests Normalize
        [TestMethod, Timeout(5000)]
        public void NormalizeTest1()
        {
            AbstractSpreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("B1", "hello");
            Assert.AreEqual("", s.GetCellContents("b1"));
        }

        [TestMethod, Timeout(5000)]
        public void NormalizeTest2()
        {
            AbstractSpreadsheet ss = new Spreadsheet(s => true, s => s.ToUpper(), "");
            ss.SetContentsOfCell("B1", "hello");
            Assert.AreEqual("hello", ss.GetCellContents("b1"));
        }

        [TestMethod, Timeout(5000)]
        public void NormalizeTest3()
        {
            AbstractSpreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("a1", "5");
            s.SetContentsOfCell("A1", "6");
            s.SetContentsOfCell("B1", "= a1");
            Assert.AreEqual(5.0, (double)s.GetCellValue("B1"), 1e-9);
        }

        [TestMethod, Timeout(5000)]
        public void NormalizeTest4()
        {
            AbstractSpreadsheet ss = new Spreadsheet(s => true, s => s.ToUpper(), "");
            ss.SetContentsOfCell("a1", "5");
            ss.SetContentsOfCell("A1", "6");
            ss.SetContentsOfCell("B1", "= a1");
            Assert.AreEqual(6.0, (double)ss.GetCellValue("B1"), 1e-9);
        }

        // Simple tests
        [TestMethod, Timeout(5000)]
        public void EmptySheet()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            VV(ss, "A1", "");
        }


        [TestMethod, Timeout(5000)]
        public void OneString()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            OneString(ss);
        }

        public void OneString(AbstractSpreadsheet ss)
        {
            Set(ss, "B1", "hello");
            VV(ss, "B1", "hello");
        }


        [TestMethod, Timeout(5000)]
        public void OneNumber()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            OneNumber(ss);
        }

        public void OneNumber(AbstractSpreadsheet ss)
        {
            Set(ss, "C1", "17.5");
            VV(ss, "C1", 17.5);
        }


        [TestMethod, Timeout(5000)]
        public void OneFormula()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            OneFormula(ss);
        }

        public void OneFormula(AbstractSpreadsheet ss)
        {
            Set(ss, "A1", "4.1");
            Set(ss, "B1", "5.2");
            Set(ss, "C1", "= A1+B1");
            VV(ss, "A1", 4.1, "B1", 5.2, "C1", 9.3);
        }


        [TestMethod, Timeout(5000)]
        public void Changed()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            Assert.IsFalse(ss.Changed);
            Set(ss, "C1", "17.5");
            Assert.IsTrue(ss.Changed);
        }


        [TestMethod, Timeout(5000)]
        public void DivisionByZero1()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            DivisionByZero1(ss);
        }

        public void DivisionByZero1(AbstractSpreadsheet ss)
        {
            Set(ss, "A1", "4.1");
            Set(ss, "B1", "0.0");
            Set(ss, "C1", "= A1 / B1");
            Assert.IsInstanceOfType(ss.GetCellValue("C1"), typeof(FormulaError));
        }

        [TestMethod, Timeout(5000)]
        public void DivisionByZero2()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            DivisionByZero2(ss);
        }

        public void DivisionByZero2(AbstractSpreadsheet ss)
        {
            Set(ss, "A1", "5.0");
            Set(ss, "A3", "= A1 / 0.0");
            Assert.IsInstanceOfType(ss.GetCellValue("A3"), typeof(FormulaError));
        }



        [TestMethod, Timeout(5000)]
        public void EmptyArgument()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            EmptyArgument(ss);
        }

        public void EmptyArgument(AbstractSpreadsheet ss)
        {
            Set(ss, "A1", "4.1");
            Set(ss, "C1", "= A1 + B1");
            Assert.IsInstanceOfType(ss.GetCellValue("C1"), typeof(FormulaError));
        }


        [TestMethod, Timeout(5000)]
        public void StringArgument()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            StringArgument(ss);
        }

        public void StringArgument(AbstractSpreadsheet ss)
        {
            Set(ss, "A1", "4.1");
            Set(ss, "B1", "hello");
            Set(ss, "C1", "= A1 + B1");
            Assert.IsInstanceOfType(ss.GetCellValue("C1"), typeof(FormulaError));
        }


        [TestMethod, Timeout(5000)]
        public void ErrorArgument()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ErrorArgument(ss);
        }

        public void ErrorArgument(AbstractSpreadsheet ss)
        {
            Set(ss, "A1", "4.1");
            Set(ss, "B1", "");
            Set(ss, "C1", "= A1 + B1");
            Set(ss, "D1", "= C1");
            Assert.IsInstanceOfType(ss.GetCellValue("D1"), typeof(FormulaError));
        }


        [TestMethod, Timeout(5000)]
        public void NumberFormula1()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            NumberFormula1(ss);
        }

        public void NumberFormula1(AbstractSpreadsheet ss)
        {
            Set(ss, "A1", "4.1");
            Set(ss, "C1", "= A1 + 4.2");
            VV(ss, "C1", 8.3);
        }


        [TestMethod, Timeout(5000)]
        public void NumberFormula2()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            NumberFormula2(ss);
        }

        public void NumberFormula2(AbstractSpreadsheet ss)
        {
            Set(ss, "A1", "= 4.6");
            VV(ss, "A1", 4.6);
        }


        // Repeats the simple tests all together
        [TestMethod, Timeout(5000)]
        public void RepeatSimpleTests()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            Set(ss, "A1", "17.32");
            Set(ss, "B1", "This is a test");
            Set(ss, "C1", "= A1+B1");
            OneString(ss);
            OneNumber(ss);
            OneFormula(ss);
            DivisionByZero1(ss);
            DivisionByZero2(ss);
            StringArgument(ss);
            ErrorArgument(ss);
            NumberFormula1(ss);
            NumberFormula2(ss);
        }

        // Four kinds of formulas
        [TestMethod, Timeout(5000)]
        public void Formulas()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            Formulas(ss);
        }

        public void Formulas(AbstractSpreadsheet ss)
        {
            Set(ss, "A1", "4.4");
            Set(ss, "B1", "2.2");
            Set(ss, "C1", "= A1 + B1");
            Set(ss, "D1", "= A1 - B1");
            Set(ss, "E1", "= A1 * B1");
            Set(ss, "F1", "= A1 / B1");
            VV(ss, "C1", 6.6, "D1", 2.2, "E1", 4.4 * 2.2, "F1", 2.0);
        }

        [TestMethod, Timeout(5000)]
        public void Formulasa()
        {
            Formulas();
        }

        [TestMethod, Timeout(5000)]
        public void Formulasb()
        {
            Formulas();
        }


        // Are multiple spreadsheets supported?
        [TestMethod, Timeout(5000)]
        public void Multiple()
        {
            AbstractSpreadsheet s1 = new Spreadsheet();
            AbstractSpreadsheet s2 = new Spreadsheet();
            Set(s1, "X1", "hello");
            Set(s2, "X1", "goodbye");
            VV(s1, "X1", "hello");
            VV(s2, "X1", "goodbye");
        }

        [TestMethod, Timeout(5000)]
        public void Multiplea()
        {
            Multiple();
        }

        [TestMethod, Timeout(5000)]
        public void Multipleb()
        {
            Multiple();
        }

        [TestMethod, Timeout(5000)]
        public void Multiplec()
        {
            Multiple();
        }

        // Reading/writing spreadsheets
        [TestMethod, Timeout(5000)]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void SaveTest1()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.Save(Path.GetFullPath("/missing/save.txt"));
        }

        [TestMethod, Timeout(5000)]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void SaveTest2()
        {
            AbstractSpreadsheet ss = new Spreadsheet(Path.GetFullPath("/missing/save.txt"), s => true, s => s, "");
        }

        [TestMethod, Timeout(5000)]
        public void SaveTest3()
        {
            AbstractSpreadsheet s1 = new Spreadsheet();
            Set(s1, "A1", "hello");
            s1.Save("save1.txt");
            s1 = new Spreadsheet("save1.txt", s => true, s => s, "default");
            Assert.AreEqual("hello", s1.GetCellContents("A1"));
        }

        [TestMethod, Timeout(5000)]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void SaveTest4()
        {
            using (StreamWriter writer = new StreamWriter("save2.txt"))
            {
                writer.WriteLine("This");
                writer.WriteLine("is");
                writer.WriteLine("a");
                writer.WriteLine("test!");
            }
            AbstractSpreadsheet ss = new Spreadsheet("save2.txt", s => true, s => s, "");
        }

        [TestMethod, Timeout(5000)]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void SaveTest5()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            ss.Save("save3.txt");
            ss = new Spreadsheet("save3.txt", s => true, s => s, "version");
        }

        [TestMethod, Timeout(5000)]
        public void SaveTest6()
        {
            AbstractSpreadsheet ss = new Spreadsheet(s => true, s => s, "hello");
            ss.Save("save4.txt");
            Assert.AreEqual("hello", new Spreadsheet().GetSavedVersion("save4.txt"));
        }

        [TestMethod, Timeout(5000)]
        public void SaveTest7()
        {
            using (XmlWriter writer = XmlWriter.Create("save5.txt"))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("spreadsheet");
                writer.WriteAttributeString("version", "");

                writer.WriteStartElement("cell");
                writer.WriteElementString("name", "A1");
                writer.WriteElementString("contents", "hello");
                writer.WriteEndElement();

                writer.WriteStartElement("cell");
                writer.WriteElementString("name", "A2");
                writer.WriteElementString("contents", "5.0");
                writer.WriteEndElement();

                writer.WriteStartElement("cell");
                writer.WriteElementString("name", "A3");
                writer.WriteElementString("contents", "4.0");
                writer.WriteEndElement();

                writer.WriteStartElement("cell");
                writer.WriteElementString("name", "A4");
                writer.WriteElementString("contents", "= A2 + A3");
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
            AbstractSpreadsheet ss = new Spreadsheet("save5.txt", s => true, s => s, "");
            VV(ss, "A1", "hello", "A2", 5.0, "A3", 4.0, "A4", 9.0);
        }



        // Fun with formulas
        [TestMethod, Timeout(5000)]
        public void Formula1()
        {
            Formula1(new Spreadsheet());
        }
        public void Formula1(AbstractSpreadsheet ss)
        {
            Set(ss, "a1", "= a2 + a3");
            Set(ss, "a2", "= b1 + b2");
            Assert.IsInstanceOfType(ss.GetCellValue("a1"), typeof(FormulaError));
            Assert.IsInstanceOfType(ss.GetCellValue("a2"), typeof(FormulaError));
            Set(ss, "a3", "5.0");
            Set(ss, "b1", "2.0");
            Set(ss, "b2", "3.0");
            VV(ss, "a1", 10.0, "a2", 5.0);
            Set(ss, "b2", "4.0");
            VV(ss, "a1", 11.0, "a2", 6.0);
        }

        [TestMethod, Timeout(5000)]
        public void Formula2()
        {
            Formula2(new Spreadsheet());
        }
        public void Formula2(AbstractSpreadsheet ss)
        {
            Set(ss, "a1", "= a2 + a3");
            Set(ss, "a2", "= a3");
            Set(ss, "a3", "6.0");
            VV(ss, "a1", 12.0, "a2", 6.0, "a3", 6.0);
            Set(ss, "a3", "5.0");
            VV(ss, "a1", 10.0, "a2", 5.0, "a3", 5.0);
        }

        [TestMethod, Timeout(5000)]
        public void Formula3()
        {
            Formula3(new Spreadsheet());
        }
        public void Formula3(AbstractSpreadsheet ss)
        {
            Set(ss, "a1", "= a3 + a5");
            Set(ss, "a2", "= a5 + a4");
            Set(ss, "a3", "= a5");
            Set(ss, "a4", "= a5");
            Set(ss, "a5", "9.0");
            VV(ss, "a1", 18.0);
            VV(ss, "a2", 18.0);
            Set(ss, "a5", "8.0");
            VV(ss, "a1", 16.0);
            VV(ss, "a2", 16.0);
        }

        [TestMethod, Timeout(5000)]
        public void Formula4()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            Formula1(ss);
            Formula2(ss);
            Formula3(ss);
        }

        [TestMethod, Timeout(5000)]
        public void Formula4a()
        {
            Formula4();
        }


        [TestMethod, Timeout(5000)]
        public void MediumSheet()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            MediumSheet(ss);
        }

        public void MediumSheet(AbstractSpreadsheet ss)
        {
            Set(ss, "A1", "1.0");
            Set(ss, "A2", "2.0");
            Set(ss, "A3", "3.0");
            Set(ss, "A4", "4.0");
            Set(ss, "B1", "= A1 + A2");
            Set(ss, "B2", "= A3 * A4");
            Set(ss, "C1", "= B1 + B2");
            VV(ss, "A1", 1.0, "A2", 2.0, "A3", 3.0, "A4", 4.0, "B1", 3.0, "B2", 12.0, "C1", 15.0);
            Set(ss, "A1", "2.0");
            VV(ss, "A1", 2.0, "A2", 2.0, "A3", 3.0, "A4", 4.0, "B1", 4.0, "B2", 12.0, "C1", 16.0);
            Set(ss, "B1", "= A1 / A2");
            VV(ss, "A1", 2.0, "A2", 2.0, "A3", 3.0, "A4", 4.0, "B1", 1.0, "B2", 12.0, "C1", 13.0);
        }

        [TestMethod, Timeout(5000)]
        public void MediumSheeta()
        {
            MediumSheet();
        }


        [TestMethod, Timeout(5000)]
        public void MediumSave()
        {
            AbstractSpreadsheet ss = new Spreadsheet();
            MediumSheet(ss);
            ss.Save("save7.txt");
            ss = new Spreadsheet("save7.txt", s => true, s => s, "default");
            VV(ss, "A1", 2.0, "A2", 2.0, "A3", 3.0, "A4", 4.0, "B1", 1.0, "B2", 12.0, "C1", 13.0);
        }

        [TestMethod, Timeout(5000)]
        public void MediumSavea()
        {
            MediumSave();
        }


        // A long chained formula. Solutions that re-evaluate 
        // cells on every request, rather than after a cell changes,
        // will timeout on this test.
        // This test is repeated to increase its scoring weight
        [TestMethod, Timeout(7000)]
        public void LongFormulaTest()
        {
            object result = "";
            LongFormulaHelper(out result);
            Assert.AreEqual("ok", result);
        }

        [TestMethod, Timeout(7000)]
        public void LongFormulaTest2()
        {
            object result = "";
            LongFormulaHelper(out result);
            Assert.AreEqual("ok", result);
        }

        [TestMethod, Timeout(7000)]
        public void LongFormulaTest3()
        {
            object result = "";
            LongFormulaHelper(out result);
            Assert.AreEqual("ok", result);
        }

        [TestMethod, Timeout(7000)]
        public void LongFormulaTest4()
        {
            object result = "";
            LongFormulaHelper(out result);
            Assert.AreEqual("ok", result);
        }

        [TestMethod, Timeout(7000)]
        public void LongFormulaTest5()
        {
            object result = "";
            LongFormulaHelper(out result);
            Assert.AreEqual("ok", result);
        }

        public void LongFormulaHelper(out object result)
        {
            try
            {
                AbstractSpreadsheet s = new Spreadsheet();
                s.SetContentsOfCell("sum1", "= a1 + a2");
                int i;
                int depth = 100;
                for (i = 1; i <= depth * 2; i += 2)
                {
                    s.SetContentsOfCell("a" + i, "= a" + (i + 2) + " + a" + (i + 3));
                    s.SetContentsOfCell("a" + (i + 1), "= a" + (i + 2) + "+ a" + (i + 3));
                }
                s.SetContentsOfCell("a" + i, "1");
                s.SetContentsOfCell("a" + (i + 1), "1");
                Assert.AreEqual(Math.Pow(2, depth + 1), (double)s.GetCellValue("sum1"), 1.0);
                s.SetContentsOfCell("a" + i, "0");
                Assert.AreEqual(Math.Pow(2, depth), (double)s.GetCellValue("sum1"), 1.0);
                s.SetContentsOfCell("a" + (i + 1), "0");
                Assert.AreEqual(0.0, (double)s.GetCellValue("sum1"), 0.1);
                result = "ok";
            }
            catch (Exception e)
            {
                result = e;
            }
        }

    }
}

