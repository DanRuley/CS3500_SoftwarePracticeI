
using FormulaEvaluator;
using System;


namespace FormulaEvaluatorTester
{
    /// <summary>
    /// Tester class for the Evaluate method in the Evaluator class.
    /// 
    /// Dan Ruley, August 2019
    /// </summary>
    class Test
    {
        /// <summary>
        /// Basic Lookup function, returns 2 every time.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int TestLookup(string s)
        {
            return 2;
        }

        /// <summary>
        /// Use this lookup when there are only integers in the expression.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int NoVariableLookup(string s)
        {
            return 2;
        }

        /// <summary>
        /// Returns 0 for testing purposes.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int DivZeroLookup(string s)
        {
            return 0;
        }

        /// <summary>
        /// Runs several tests and prints the expected result, followed by the observed result.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            Console.WriteLine("TestJustNum should return 5.  It returned: " + TestJustNum());

            Console.WriteLine("TestJustNumParens should return 5.  It returned: " + TestJustNumParens());

            Console.WriteLine("TestBasicExpressionAddSub should return 10.  It returned: " + TestBasicExpressionAddSub());

            Console.WriteLine("TestBasicDivide should return 5.  It returned: " + TestBasicDivide());

            Console.WriteLine("TestTrickerException should return 10.  It returned: " + TestTrickerException());

            Console.WriteLine("TestSimpleVaribale should return 12.  It returned: " + TestSimpleVariable());

            Console.WriteLine("TestTrickierVaribale should return 15.  It returned: " + TestTrickierVariable());

            Console.WriteLine("TestLongVaribale should return 5.  It returned: " + TestLongVariable());

            Console.WriteLine("TestLargeNums should return 10000000.  It returned: " + TestLargeNums());

            Console.WriteLine("TestTrickierVaribale should throw, and return -1.  It returned: " + TestInvalidVariable());

            Console.WriteLine("TestDivByZero should throw, and return -1.  It returned: " + TestDivByZero());

            Console.WriteLine("TestDivByZeroVar should throw, and return -1.  It returned: " + TestDivByZeroVar());

            Console.WriteLine("TestInvalidSyntax should throw, and return -1.  It returned: " + TestInvalidSyntax());

            Console.WriteLine("TestInvalidSyntax1 should throw, and return -1.  It returned: " + TestInvalidSyntax1());

            Console.WriteLine("TestOnlyWhitespace should throw, and return -1.  It returned: " + TestOnlyWhitespace());

            Console.WriteLine("TestOnlyParens should throw, and return -1.  It returned: " + TestOnlyParens());

            Console.WriteLine("TestIllegalParens should throw, and return -1.  It returned: " + TestIllegalParens());


            Console.ReadLine();
        }

        /// <summary>
        /// Test just a number.
        /// </summary>
        /// <returns></returns>
        static int TestJustNum()
        {
            return Evaluator.Evaluate("5", NoVariableLookup);
        }

        /// <summary>
        /// Test just a number with parens.
        /// </summary>
        /// <returns></returns>
        static int TestJustNumParens()
        {
            return Evaluator.Evaluate("(((((5)))))", NoVariableLookup);
        }


        /// <summary>
        /// Test basic addition and subtraction.
        /// </summary>
        /// <returns></returns>
        static int TestBasicExpressionAddSub()
        {
            return Evaluator.Evaluate("10 + 1 - 1 + 1 - 1 + 1 - 1", NoVariableLookup);
        }

        /// <summary>
        /// Basic division test.
        /// </summary>
        /// <returns></returns>
        static int TestBasicDivide()
        {
            return Evaluator.Evaluate("10 / 2", NoVariableLookup);
        }

        /// <summary>
        /// Test proper behavior with a more complex expression.
        /// </summary>
        /// <returns></returns>
        static int TestTrickerException()
        {
            return Evaluator.Evaluate("(5 * (10 + 1 - 1 + 1 - 1 + 1 - 1)) / 5", NoVariableLookup);
        }

        /// <summary>
        /// Simple test with a variable
        /// </summary>
        /// <returns></returns>
        static int TestSimpleVariable()
        {
            return Evaluator.Evaluate("10 + A5", TestLookup);
        }

        /// <summary>
        /// Test a complex expression with a weird variable name.
        /// </summary>
        /// <returns></returns>
        static int TestTrickierVariable()
        {
            return Evaluator.Evaluate("((((2 * 10) + 10) / AaAazajfn12345678) * 1) / 1", TestLookup);
        }

        /// <summary>
        /// Test a long expression with all variables.
        /// </summary>
        /// <returns></returns>
        static int TestLongVariable()
        {
            return Evaluator.Evaluate("((((A1 + A2) * A3) - A4) / A5) + A6 / A7 * A8", TestLookup);
        }

        /// <summary>
        /// Test proper behavior for larger integers.
        /// </summary>
        /// <returns></returns>
        static int TestLargeNums()
        {
            return Evaluator.Evaluate("((10000000 / 2) * 10) / 5", NoVariableLookup);
        }

        /// <summary>
        /// Test for an invalid variable name
        /// </summary>
        /// <returns></returns>
        static int TestInvalidVariable()
        {
            try
            {
                return Evaluator.Evaluate("((((2 * 10) + 10) / AaAazajfn_12345678) * 1) / 1", TestLookup);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// Test for an invalid variable name
        /// </summary>
        /// <returns></returns>
        static int TestInvalidVariable1()
        {
            try
            {
                return Evaluator.Evaluate("((((2 * 10) + 10) / 1A) * 1) / 1", TestLookup);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// Test division by zero.
        /// </summary>
        /// <returns></returns>
        static int TestDivByZero()
        {
            try
            {
                return Evaluator.Evaluate("((((2 * 10) + 10) / AaAazajfn_12345678) * 1) / 0", TestLookup);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// Test division by zero with a variable.
        /// </summary>
        /// <returns></returns>
        static int TestDivByZeroVar()
        {
            try
            {
                return Evaluator.Evaluate("((((2 * 10) + 10) / AaAazajfn_12345678) * 1) / A9", DivZeroLookup);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        //Test invalid syntax: trying to negate an entire expression
        static int TestInvalidSyntax()
        {
            try
            {
                return Evaluator.Evaluate("-(((((2 * 10) + 10) / AaAazajfn_12345678) * 1) / A9)", TestLookup);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// Test invalid syntax with a duplicate operator.
        /// </summary>
        /// <returns></returns>
        static int TestInvalidSyntax1()
        {
            try
            {
                return Evaluator.Evaluate("10 + + 1 - 1 + 1 - 1 + 1 - 1", NoVariableLookup);
            }
            catch (Exception)
            {
                return -1;
            }
        }
        /// <summary>
        /// Test an only whitespace expression.
        /// </summary>
        /// <returns></returns>
        static int TestOnlyWhitespace()
        {
            try
            {
                return Evaluator.Evaluate("          ", NoVariableLookup);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// Test that an expression with only parens throws an exception.
        /// </summary>
        /// <returns></returns>
        static int TestOnlyParens()
        {
            try
            {
                return Evaluator.Evaluate("(())", NoVariableLookup);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        /// <summary>
        /// Test that an expression with illegal parens throws an exception.
        /// </summary>
        /// <returns></returns>
        static int TestIllegalParens()
        {
            try
            {
                return Evaluator.Evaluate("(5(+)5)", NoVariableLookup);
            }
            catch (Exception)
            {
                return -1;
            }
        }


    }
}
