using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpreadsheetUtilities;
using System;
using System.Collections.Generic;
using System.Text;



namespace FormulaTester
{

    [TestClass]
    public class FormulaTestSuite
    {

        [TestMethod]
        public void TestNormalizedVariables()
        {
            Formula f = new Formula("a1 + a2 + a3", s => s.ToUpper(), s => true);
            HashSet<string> vars = (HashSet<string>)f.GetVariables();

            Assert.IsTrue(vars.Contains("A1"));
            Assert.IsTrue(vars.Contains("A2"));
            Assert.IsTrue(vars.Contains("A3"));
            Assert.IsFalse(vars.Contains("a1"));
            Assert.IsFalse(vars.Contains("a2"));
            Assert.IsFalse(vars.Contains("a3"));
        }

        [TestMethod]
        public void MoreNormalizationTesting()
        {
            Formula f1 = new Formula("x + X + y + Y + z + Z");
            HashSet<string> vars = (HashSet<string>)f1.GetVariables();

            //make sure default normalizer includes both cases
            vars = (HashSet<string>)f1.GetVariables();
            Assert.AreEqual(6, vars.Count);
            Assert.IsTrue(vars.Contains("x"));
            Assert.IsTrue(vars.Contains("X"));
            Assert.IsTrue(vars.Contains("y"));
            Assert.IsTrue(vars.Contains("Y"));
            Assert.IsTrue(vars.Contains("z"));
            Assert.IsTrue(vars.Contains("Z"));

            //Make sure normalizer with x -> X only returns capitalized
            Formula f2 = new Formula("x + X + y + Y + z + Z", s => s.ToUpper(), s => true);
            vars = (HashSet<string>)f2.GetVariables();
            Assert.AreEqual(3, vars.Count);
            Assert.IsFalse(vars.Contains("x"));
            Assert.IsTrue(vars.Contains("X"));
            Assert.IsFalse(vars.Contains("y"));
            Assert.IsTrue(vars.Contains("Y"));
            Assert.IsFalse(vars.Contains("z"));
            Assert.IsTrue(vars.Contains("Z"));
        }

        [TestMethod]
        public void BasicTestConstructorAndToStringAndEquals()
        {
            Formula f = new Formula("z1 + 3 - A6 * (500 * (50))");
            Formula f1 = new Formula("z1 + 3 - A6 * (500 * (50))");
            string expected = ("z1+3-A6*(500*(50))");
            Assert.AreEqual(expected, f.ToString());
            Assert.IsTrue(f.Equals(f1));
        }

        [TestMethod]
        public void TestEqualsWithNull()
        {
            Formula f = null;
            Formula f1 = new Formula("z1 + 3 - A6 * (500 * (50))");
            Assert.IsFalse(f1.Equals(f));
        }

        [TestMethod]
        public void TestParensMultiplyDivide()
        {
            Formula f1 = new Formula("((1*2)*(2)*(5)/5)");
            Assert.AreEqual((double)4, f1.Evaluate(x => 0));
        }



        [TestMethod]
        public void TestEqualsWithDifferentDoubleFormats()
        {
            Formula f1 = new Formula("z1 + 3 - A6 * (500 * (50))");
            Formula f = new Formula("z1 + 3 - A6 * (5E2 * (50.000000000000000000))");
            f.ToString();
            Assert.IsTrue(f1.Equals(f));
        }


        [TestMethod]
        public void TestHashCode()
        {
            Formula f1 = new Formula("z1 + 3 - A6 * (500 * (50))");
            Formula f = new Formula("z1 + 3 - A6 * (5E2 * (50.000000000000000000))");
            Assert.IsTrue(f.GetHashCode() == f1.GetHashCode());
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestExtraRightParenthesesThrows()
        {
            Formula f1 = new Formula("(5)*3)");
        }


        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestNormalizerCreatesBadVarThrows()
        {
            Formula f1 = new Formula("5 + x1", s => "$$$", s=> true);
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestBadVariableThrows()
        {
            Formula f1 = new Formula("5 + _X1__$54");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void AllBadTokensExceptOps()
        {
            Formula f1 = new Formula("@ - $ + ~ * !");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestInvalidatedVariableThrows()
        {
            Formula f1 = new Formula("5 + x5 + 6 + 7", s=> s, s => false);
        }



        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestBeginningOperatorThrows()
        {
            Formula f1 = new Formula("+ 1 + 2 + 3");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestEndingOperatorThrows()
        {
            Formula f1 = new Formula("1 + 2 + 3 +");
        }


        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestZeroValidTokensThrows()
        {
            Formula f1 = new Formula("@,;");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestZeroTokensThrows()
        {
            Formula f1 = new Formula("");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestRightParenAfterLeftThrows()
        {
            Formula f1 = new Formula("()");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestRightParenAfterOpThrows()
        {
            Formula f1 = new Formula("(5 + )5");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestDoubleOpThrows()
        {
            Formula f1 = new Formula("5 + + 5");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestImplicitMultiplicationThrows()
        {
            Formula f1 = new Formula("A5(5) + 6");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestNegValuesThrows()
        {
            Formula f1 = new Formula("5 + -5");
        }


        [TestMethod]
        public void TestEqualsOp()
        {
            Formula f1 = new Formula("5 + 5");
            Formula f2 = new Formula("5 + 5");
            Formula f3 = null;
            Formula f4 = null;

            Assert.IsTrue(f1 == f2);
            Assert.IsTrue(f3 == f4);
            Assert.IsFalse(f1 == f3);
            Assert.IsFalse(f4 == f2);
        }

        [TestMethod]
        public void TestNotEqualsOp()
        {
            Formula f1 = new Formula("5 + 5");
            Formula f2 = new Formula("5 + 5");
            Formula f3 = null;
            Formula f4 = null;

            Assert.IsFalse(f1 != f2);
            Assert.IsFalse(f3 != f4);
            Assert.IsTrue(f1 != f3);
            Assert.IsTrue(f4 != f2);
        }

        [TestMethod]
        public void SimpleEvaluateTest()
        {
            Formula f1 = new Formula("10 + 1 - 1 + 1 - 1 + 1 - 1");
            Formula f2 = new Formula("(5 * 5)");
            Formula f3 = new Formula("(((((5))))) * 5");
            Formula f4 = new Formula("((((A1 + A2) * A3) - A4) / A5) + A6 / A7 * A8");
            Formula f5 = new Formula("2 + 3 * 5 + (3 + 4 * 8) * 5 + 2");
            Formula f6 = new Formula("y1 + x7");

            Assert.AreEqual((double)10, f1.Evaluate(s => 0));
            Assert.AreEqual((double)25, f2.Evaluate(s => 0));
            Assert.AreEqual((double)25, f3.Evaluate(s => 0));
            Assert.AreEqual((double)5, f4.Evaluate(s => 2));
            Assert.AreEqual((double)194, f5.Evaluate(s => 0));
            Assert.AreEqual((double)5, f6.Evaluate(s => (s == "x7") ? 1 : 4));
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestFormulaErrorUnbalancedLeftParens()
        {
            Formula f1 = new Formula("((10 + 1) - 1 + 1 - 1 + 1 - 1");
        }

        [TestMethod]
        public void TestDivZeroError()
        {
            Formula f1 = new Formula("1 / 0");
            Assert.IsInstanceOfType(f1.Evaluate(x => 0), typeof(FormulaError));
        }

        [TestMethod]
        public void TestThrowingLookupJustReturnsFormulaError()
        {
            Formula f1 = new Formula("1 + x5");
            Assert.IsInstanceOfType(f1.Evaluate(LookupThrow), typeof(FormulaError));
        }

        [TestMethod]
        public void FormulaStressTest()
        {
            //Test a giant expression to stress test Formula
            StringBuilder sb = new StringBuilder();
            for (int i = 1; i < 10000; i++)
            {
                sb.Append(i + "+");
            }
            sb.Append(10000);

            double CantBelieveGaussDiscoveredThisFormulaWhenHeWasNine = ((10000 * 10001) / 2);
            Formula f1 = new Formula(sb.ToString());
            Assert.AreEqual(CantBelieveGaussDiscoveredThisFormulaWhenHeWasNine, (double)f1.Evaluate(s => 0));
        }

        public static double LookupThrow(string s)
        {
            throw new ArgumentException();
        }
    }
}
