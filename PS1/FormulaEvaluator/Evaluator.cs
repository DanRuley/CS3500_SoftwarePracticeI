using System;
using System.Collections.Generic;

using System.Text.RegularExpressions;

namespace FormulaEvaluator
{
    /// <summary>
    /// Static class containing the Evaluate method, which solves a given infix expression.
    /// 
    /// Dan Ruley, August 2019
    /// </summary>
    public static class Evaluator
    {
        
        public delegate int Lookup(string v);

        /// <summary>
        /// Evalutes an infix expression using the given input expression and variableEvaluator delegate.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="variableEvaluator"></param>
        /// <returns></returns>
        public static int Evaluate(String expression, Lookup variableEvaluator)
        {

            Stack<int> Values = new Stack<int>();

            Stack<char> Operators = new Stack<char>();

            string[] substrings = Regex.Split(expression, "(\\()|(\\))|(-)|(\\+)|(\\*)|(/)");

            foreach (string s in substrings)
            {
                string token = s.Trim();

                int CheckedToken = DetermineToken(token);

                //case where the token fails the validity check (e.g. bad variable name)
                if (CheckedToken == -1)
                    throw new ArgumentException("Error: invalid expression syntax.");


                //If token is whitespace or empty
                if (CheckedToken == 0)
                    continue;


                //case where token is a + or - operator
                if (CheckedToken == 1)
                {
                    if (Operators.CheckStack('+') || Operators.CheckStack('-'))
                    {
                        if (Values.Count < 2)
                        {
                            throw new ArgumentException("Error: invalid expression syntax.");
                        }

                        else
                        {
                            Values.Push(Calculate(Operators.Pop(), Values.Pop(), Values.Pop()));

                        }
                    }

                    Operators.Push(Char.Parse(token));
                    continue;
                }





                //case where token is a * or / or ( operator
                if (CheckedToken == 2 || CheckedToken == 3)
                {
                    Operators.Push(Char.Parse(token));
                    continue;
                }




                //case where token is a ) operator
                if (CheckedToken == 4)
                {

                    if (Operators.CheckStack('+') || Operators.CheckStack('-'))
                    {
                        if (Values.Count < 2)
                        {
                            throw new ArgumentException("Error: invalid expression syntax.");
                        }

                        else
                        {
                            Values.Push(Calculate(Operators.Pop(), Values.Pop(), Values.Pop()));

                        }
                    }

                    if (Operators.CheckStack('('))
                    {
                        Operators.Pop();

                    }
                    else
                        throw new ArgumentException("Error: invalid expression syntax (Check parenthesis).");


                    if (Operators.CheckStack('*') || Operators.CheckStack('/'))
                    {
                        if (Values.Count < 2)
                            throw new ArgumentException("Error: invalid expression syntax.");

                        else
                        {
                            Values.Push(Calculate(Operators.Pop(), Values.Pop(), Values.Pop()));
                            continue;
                        }
                    }

                }





                //case where the token is an integer or a variable
                if (CheckedToken == 5 || CheckedToken == 6)
                {

                    int number;

                    //use lookup delegate to determine value of variable
                    if (CheckedToken == 6)
                        number = variableEvaluator(token);
                    else
                        number = int.Parse(token);


                    if (Operators.CheckStack('*') || Operators.CheckStack('/'))
                    {
                        if (Values.Count < 1)
                            throw new ArgumentException("Error, invalid expression syntax.");
                        else
                        {
                            Values.Push(Calculate(Operators.Pop(), number, Values.Pop()));

                        }
                    }

                    else
                    {
                        Values.Push(number);
                    }


                }
            }





            //last token has been processed, time to compute final result
            int result;

            //Operators stack is empty
            if (Operators.Count == 0)
            {
                if (Values.Count == 1)
                    result = Values.Pop();
                else
                    throw new ArgumentException("Error: invalid expression syntax.");
            }

            //Operators stack is not empty
            else
            {
                if (Operators.Count == 1 && Values.Count == 2)
                    result = Calculate(Operators.Pop(), Values.Pop(), Values.Pop());
                else
                    throw new ArgumentException("Error: invalid expression syntax.");
            }

            return result;
        }


        /// <summary>
        /// Performs a calculation given an operator, and two numbers.  The first value popped becomes the right operand, while the second becomes the left in order to satisfy the proper infix evaluation precedence.
        /// </summary>
        /// <param name="op"></param>
        /// <param name="num1"></param>
        /// <param name="num2"></param>
        /// <returns></returns>
        private static int Calculate(char op, int rightnum, int leftnum)
        {
            if (op == '+')
                return (leftnum + rightnum);

            if (op == '-')
                return (leftnum - rightnum);

            if (op == '*')
                return (leftnum * rightnum);

            if (op == '/')
            {

                if (rightnum == 0)
                    throw new ArgumentException("Error: division by 0.");
                else
                    return leftnum / rightnum;
            }


            return 0;
        }






        /// <summary>
        /// Examines the input token and returns an integer value based on what the token is.
        /// </summary>
        /// <param name="input">
        /// token
        /// </param>
        /// <returns>
        /// 0 for a null or whitespace token
        /// -1 for an invalid token (e.g. improper variable syntax)
        /// 1 for a + or - operator
        /// 2 for a * or / operator
        /// 3 for a ( operator
        /// 4 for a ) operator
        /// 5 for an integer
        /// 6 for a variable
        /// </returns>
        private static int DetermineToken(String token)
        {
            //check if the symbol is whitespace
            if (token == "" || token == " ")
                return 0;


            //check if symbol is an operator, and of which type
            if (token == "+" || token == "-")
            {
                return 1;
            }

            if (token == "/" || token == "*")
            {
                return 2;
            }

            if (token == "(")
            {
                return 3;
            }

            if (token == ")")
            {
                return 4;
            }


            //check if symbol is an integer
            try
            {
                int.Parse(token);
                return 5;
            }

            catch (FormatException)
            {

            }


            //check if symbol is a valid variable
            bool FoundLetter = false;
            bool FoundDigit = false;
            int i;

            for (i = 0; i < token.Length; i++)
            {
                if (Char.IsLetter(token[i]))
                {
                    FoundLetter = true;
                    continue;
                }
                else
                    break;
            }

            for (; i < token.Length; i++)
            {
                if (Char.IsDigit(token[i]))
                {
                    FoundDigit = true;
                    continue;
                }
                else
                    break;
            }

            if (i == token.Length && FoundLetter && FoundDigit)
            {
                return 6;
            }


            //return if symbol fails all checks
            return -1;
        }


    }

    /// <summary>
    /// Static class that offers an extension for a generic stack.
    /// </summary>
    public static class PS1StackExt
    {
        /// <summary>
        /// Checks if the stack contains at least one value, and if the value at the top of the stack matches the input symbol.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stack"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static bool CheckStack<T>(this Stack<T> stack, T symbol)
        {
            if (stack.Count < 1)
                return false;

            return stack.Peek().Equals(symbol);
        }

    }
}

