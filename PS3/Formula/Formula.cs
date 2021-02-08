// Skeleton written by Joe Zachary for CS 3500, September 2013
// Read the entire skeleton carefully and completely before you
// do anything else!

// Implementation completed by Dan Ruley, September 2019.

// Version 1.1 (9/22/13 11:45 a.m.)\
// Change log:
//  (Version 1.1) Repaired mistake in GetTokens
//  (Version 1.1) Changed specification of second constructor to
//                clarify description of how validation works\
// (Daniel Kopta) 
// Version 1.2 (9/10/17) 
// Change log:
//  (Version 1.2) Changed the definition of equality with regards
//                to numeric tokens


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SpreadsheetUtilities
{
    /// <summary>
    /// Represents formulas written in standard infix notation using standard precedence
    /// rules.  The allowed symbols are non-negative numbers written using double-precision 
    /// floating-point syntax (without unary preceeding '-' or '+'); 
    /// variables that consist of a letter or underscore followed by 
    /// zero or more letters, underscores, or digits; parentheses; and the four operator 
    /// symbols +, -, *, and /.  
    /// 
    /// Spaces are significant only insofar that they delimit tokens.  For example, "xy" is
    /// a single variable, "x y" consists of two variables "x" and y; "x23" is a single variable; 
    /// and "x 23" consists of a variable "x" and a number "23".
    /// 
    /// Associated with every formula are two delegates:  a normalizer and a validator.  The
    /// normalizer is used to convert variables into a canonical form, and the validator is used
    /// to add extra restrictions on the validity of a variable (beyond the standard requirement 
    /// that it consist of a letter or underscore followed by zero or more letters, underscores,
    /// or digits.)  Their use is described in detail in the constructor and method comments.
    /// </summary>
    public class Formula
    {
        private Stack<double> Values;
        private Stack<char> Operators;
        private int LeftParensCount;
        private int RightParensCount;
        private List<string> FormulaTokens;
        private HashSet<string> NormalizedVariables;
        private StringBuilder Builder;

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically invalid,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer is the identity function, and the associated validator
        /// maps every string to true.  
        /// </summary>
        public Formula(String formula) :
            this(formula, s => s, s => true)
        {

        }

        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically incorrect,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer and validator are the second and third parameters,
        /// respectively.  
        /// 
        /// If the formula contains a variable v such that normalize(v) is not a legal variable, 
        /// throws a FormulaFormatException with an explanatory message. 
        /// 
        /// If the formula contains a variable v such that isValid(normalize(v)) is false,
        /// throws a FormulaFormatException with an explanatory message.
        /// 
        /// Suppose that N is a method that converts all the letters in a string to upper case, and
        /// that V is a method that returns true only if a string consists of one letter followed
        /// by one digit.  Then:
        /// 
        /// new Formula("x2+y3", N, V) should succeed
        /// new Formula("x+y3", N, V) should throw an exception, since V(N("x")) is false
        /// new Formula("2x+y3", N, V) should throw an exception, since "2x+y3" is syntactically incorrect.
        /// </summary>
        public Formula(String formula, Func<string, string> normalize, Func<string, bool> isValid)
        {
            LeftParensCount = 0;
            RightParensCount = 0;
            FormulaTokens = new List<string>();
            NormalizedVariables = new HashSet<string>();
            Builder = new StringBuilder();

            try
            {
                MainConstructorHelper(formula, normalize, isValid);
            }

            //Propagate Formula exceptions back to user
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Parses the input formula string into tokens.  Performs necessary validity checks and builds the list of tokens, the set of normalized variables, and the String representation of the formula.
        /// </summary>
        /// <param name="formula"></param>
        /// <param name="normalize"></param>
        /// <param name="isValid"></param>
        private void MainConstructorHelper(String formula, Func<string, string> normalize,
           Func<string, bool> isValid)
        {
            foreach (string token in GetTokens(formula))
            {
                if (FormulaTokens.Count > 0)
                    CheckFollowingRule(token);

                if (IsOperator(token))
                {
                    Builder.Append(token);
                    FormulaTokens.Add(token);
                    continue;
                }

                if (token == "(" || token == ")")
                {
                    CheckParensAndAdd(token);
                    continue;
                }

                if (IsValidVariable(token))
                {
                    ValidateAndNormalizeVariableAndAdd(token, normalize, isValid);
                    continue;
                }

                if (Double.TryParse(token, out _))
                {
                    double d = Double.Parse(token);

                    FormulaTokens.Add(d.ToString());
                    Builder.Append(d.ToString());
                    continue;
                }
            }
            FinalCheck();
        }

        /// <summary>
        /// Checks validity of the first and last tokens in the expression.  Also make sure the formula consisted of zero tokens and whether the final parenthesis counts matched.  Throws FFEs for all of these cases.
        /// </summary>
        private void FinalCheck()
        {
            if (FormulaTokens.Count == 0)
                throw new FormulaFormatException("Error: Formula is empty.  Please enter a valid formula.");

            if (IsOperator(FormulaTokens[0]) || FormulaTokens[0] == ")")
                throw new FormulaFormatException("Error: Formula cannot begin with an operator or right parens.  Please reformat formula.");

            if (IsOperator(FormulaTokens[FormulaTokens.Count - 1]) || FormulaTokens[FormulaTokens.Count - 1] == "(")
                throw new FormulaFormatException("Error: Formula cannot end with an operator or left parens.  Please reformat formula.");

            if (LeftParensCount != RightParensCount)
                throw new FormulaFormatException("Error: imbalanced left parenthesis in formula.  Check for typos.");
        }

        /// <summary>
        /// Evaluates this Formula, using the lookup delegate to determine the values of
        /// variables.  When a variable symbol v needs to be determined, it should be looked up
        /// via lookup(normalize(v)). (Here, normalize is the normalizer that was passed to 
        /// the constructor.)
        /// 
        /// For example, if L("x") is 2, L("X") is 4, and N is a method that converts all the letters 
        /// in a string to upper case:
        /// 
        /// new Formula("x+7", N, s => true).Evaluate(L) is 11
        /// new Formula("x+7").Evaluate(L) is 9
        /// 
        /// Given a variable symbol as its parameter, lookup returns the variable's value 
        /// (if it has one) or throws an ArgumentException (otherwise).
        /// 
        /// If no undefined variables or divisions by zero are encountered when evaluating 
        /// this Formula, the value is returned.  Otherwise, a FormulaError is returned.  
        /// The Reason property of the FormulaError should have a meaningful explanation.
        ///
        /// This method should never throw an exception.
        /// </summary>
        public object Evaluate(Func<string, double> lookup)
        {
            Values = new Stack<double>();
            Operators = new Stack<char>();

            return EvaluatorLogic(lookup);
        }

        /// <summary>
        /// Private method that performs the logic and computations for the public Evaluate method.
        /// </summary>
        /// <param name="lookup"></param>
        /// <returns></returns>
        private object EvaluatorLogic(Func<string, double> lookup)
        {
            foreach (string token in FormulaTokens)
            {
                //case where token is a + or - operator
                if (token == "+" || token == "-")
                {
                    if (Operators.CheckStack('+', '-'))
                    {
                            object result = Calculate(Operators.Pop(), Values.Pop(), Values.Pop());
                            Values.Push((double)result);
                    }
                    Operators.Push(Char.Parse(token));
                    continue;
                }

                //case where token is a * or / or ( operator
                if (token == "*" || token == "/" || token == "(")
                {
                    Operators.Push(Char.Parse(token));
                    continue;
                }

                //case where token is a ) operator
                if (token == ")")
                {
                    if (Operators.CheckStack('+', '-'))
                    {
                            object result = Calculate(Operators.Pop(), Values.Pop(), Values.Pop());
                            Values.Push((double)result);
                    }

                    if (Operators.CheckStack('('))
                        Operators.Pop();

                    if (Operators.CheckStack('*', '/'))
                    {
                            object result = Calculate(Operators.Pop(), Values.Pop(), Values.Pop());
                            if (result is FormulaError)
                                return result;

                            Values.Push((double)result);
                            continue;
                    }
                }

                //case where the token is an integer or a variable
                if (Double.TryParse(token, out _) || IsValidVariable(token))
                {
                    double number;
                    //use lookup delegate to determine value of variable
                    if (IsValidVariable(token))
                    {
                        try
                        {
                            number = lookup(token);
                        }
                        catch (ArgumentException e)
                        {
                            return new FormulaError(e.Message);
                        }
                    }
                    else
                        number = Double.Parse(token);

                    if (Operators.CheckStack('*', '/'))
                    {
                            object result = Calculate(Operators.Pop(), number, Values.Pop());
                            if (result is FormulaError)
                                return result;
                            Values.Push((double)result);
                    }
                    else
                        Values.Push(number);
                }
            }

            //last token has been processed, time to compute final result
            object FinalResult;

            //Operators stack is empty
            if (Operators.Count == 0)
            {
                    FinalResult = Values.Pop();
            }

            //Operators stack is not empty
            else { 
              FinalResult = Calculate(Operators.Pop(), Values.Pop(), Values.Pop());
            }
            return FinalResult;
        }

        /// <summary>
        /// Performs a calculation given an operator, and two numbers.  The first value popped becomes the right operand, while the second becomes the left in order to satisfy the proper infix evaluation precedence.
        /// </summary>
        /// <param name="op"></param>
        /// <param name="num1"></param>
        /// <param name="num2"></param>
        /// <returns></returns>
        private object Calculate(char op, double rightnum, double leftnum)
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
                    return new FormulaError("Error: division by 0.");
                else
                    return leftnum / rightnum;
            }
            return 0;
        }

        /// <summary>
        /// Enumerates the normalized versions of all of the variables that occur in this 
        /// formula.  No normalization may appear more than once in the enumeration, even 
        /// if it appears fmore than once in this Formula.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x+y*z", N, s => true).GetVariables() should enumerate "X", "Y", and "Z"
        /// new Formula("x+X*z", N, s => true).GetVariables() should enumerate "X" and "Z".
        /// new Formula("x+X*z").GetVariables() should enumerate "x", "X", and "z".
        /// </summary>
        public IEnumerable<String> GetVariables()
        {
            HashSet<string> NormVarCopy = new HashSet<string>(NormalizedVariables);
            return NormVarCopy;
        }

        /// <summary>
        /// Returns a string containing no spaces which, if passed to the Formula
        /// constructor, will produce a Formula f such that this.Equals(f).  All of the
        /// variables in the string should be normalized.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x + y", N, s => true).ToString() should return "X+Y"
        /// new Formula("x + Y").ToString() should return "x+Y"
        /// </summary>
        public override string ToString()
        {
            return Builder.ToString();
        }

        /// <summary>
        /// If obj is null or obj is not a Formula, returns false.  Otherwise, reports
        /// whether or not this Formula and obj are equal.
        /// 
        /// Two Formulae are considered equal if they consist of the same tokens in the
        /// same order.  To determine token equality, all tokens are compared as strings 
        /// except for numeric tokens and variable tokens.
        /// Numeric tokens are considered equal if they are equal after being "normalized" 
        /// by C#'s standard conversion from string to double, then back to string. This 
        /// eliminates any inconsistencies due to limited floating point precision.
        /// Variable tokens are considered equal if their normalized forms are equal, as 
        /// defined by the provided normalizer.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        ///  
        /// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
        /// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
        /// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
        /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Formula))
                return false;

            else return (this.ToString() == obj.ToString());
        }

        /// <summary>
        /// Reports whether f1 == f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return true.  If one is
        /// null and one is not, this method should return false.
        /// </summary>
        public static bool operator ==(Formula f1, Formula f2)
        {
            if (ReferenceEquals(f1, null) && ReferenceEquals(f2, null))
                return true;

            if ((ReferenceEquals(f1, null) && !ReferenceEquals(f2, null)) || (!ReferenceEquals(f1, null) && ReferenceEquals(f2, null)))
                return false;

            return f1.Equals(f2);
        }

        /// <summary>
        /// Reports whether f1 != f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return false.  If one is
        /// null and one is not, this method should return true.
        /// </summary>
        public static bool operator !=(Formula f1, Formula f2)
        {
            if (ReferenceEquals(f1, null) && ReferenceEquals(f2, null))
                return false;

            if ((ReferenceEquals(f1, null) && !ReferenceEquals(f2, null)) || (!ReferenceEquals(f1, null) && ReferenceEquals(f2, null)))
                return true;

            return !f1.Equals(f2);
        }

        /// <summary>
        /// Returns a hash code for this Formula.  If f1.Equals(f2), then it must be the
        /// case that f1.GetHashCode() == f2.GetHashCode().  Ideally, the probability that two 
        /// randomly-generated unequal Formulae have the same hash code should be extremely small.
        /// </summary>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// Given an expression, enumerates the tokens that compose it.  Tokens are left paren;
        /// right paren; one of the four operator symbols; a string consisting of a letter or underscore
        /// followed by zero or more letters, digits, or underscores; a double literal; and anything that doesn't
        /// match one of those patterns.  There are no empty tokens, and no token contains white space.
        /// </summary>
        private static IEnumerable<string> GetTokens(String formula)
        {
            // Patterns for individual tokens
            String lpPattern = @"\(";
            String rpPattern = @"\)";
            String opPattern = @"[\+\-*/]";
            String varPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
            String doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?";
            String spacePattern = @"\s+";

            // Overall pattern
            String pattern = String.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5})",
                                            lpPattern, rpPattern, opPattern, varPattern, doublePattern, spacePattern);

            // Enumerate matching tokens that don't consist solely of white space.
            foreach (String s in Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace))
            {
                if (!Regex.IsMatch(s, @"^\s*$", RegexOptions.Singleline))
                {
                    yield return s;
                }
            }
        }

        /// <summary>
        /// Determines if the given string is a valid variable (a letter or underscore followed by one or more digits, letters, or underscores)
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private bool IsValidVariable(string t)
        {
            //token is a valid symbol, return false (don't throw)
            if (IsOperator(t) || Double.TryParse(t, out _) || t == "(" || t == ")")
                return false;

            //Make sure token matches proper variable format
            if (t[0] == '_' || Char.IsLetter(t[0]))
            {
                return true;
            }
            else
                throw new FormulaFormatException("Error: Variables must begin with a letter or underscore character.");
        }



        /// <summary>
        /// Takes an input string, converts it to its normalized form, and then validates it.  Throws a FormulaFormatException if the variable is found to be invalid.  Also adds the normalized variable to the set of NormalizedVariables, the list of Formula Tokens, and the Formula string.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="normalize"></param>
        /// <param name="isValid"></param>
        private void ValidateAndNormalizeVariableAndAdd(string v, Func<string, string> normalize, Func<string, bool> isValid)
        {
            string normalizedVar;
            normalizedVar = normalize(v);
            if (!isValid(normalizedVar))
                throw new FormulaFormatException("Error: invalid variable in formula.  Check the input Validator function for errors.");

            //Make sure it's still formatted correctly after normalization
            IsValidVariable(normalizedVar);
            NormalizedVariables.Add(normalizedVar);
            FormulaTokens.Add(normalizedVar);
            Builder.Append(normalizedVar);
        }

        /// <summary>
        /// Determines if the input string is an operator.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private bool IsOperator(string s)
        {
            return (s == "+" || s == "-" || s == "*" || s == "/");
        }

        /// <summary>
        /// Determines if the input string is a parens.  If it is, it adjusts the left and right parens count and throws a FormulaFormatException if they are imbalanced.  Also adds parens to the Token list and Formula string.
        /// </summary>
        /// <param name="t"></param>
        private void CheckParensAndAdd(string t)
        {
            if (t == "(")
                LeftParensCount++;

            if (t == ")")
                RightParensCount++;

            if (RightParensCount > LeftParensCount)
                throw new FormulaFormatException("Error: unbalanced parenthesis in formula.  Check for duplicate right parenthesis characters.");

            FormulaTokens.Add(t);
            Builder.Append(t);
        }

        /// <summary>
        /// Checks the the token preceding this meets the proper expectations for the overall formula format.
        /// </summary>
        /// <param name="token"></param>
        private void CheckFollowingRule(string token)
        {
            string PrecedingToken = FormulaTokens[FormulaTokens.Count - 1];

            //Following an operator or left parens case
            if (IsOperator(PrecedingToken) || PrecedingToken == "(")
            {
                //Check if this token is either left parens, a number, or a variable
                if (token == ")" || IsOperator(token))
                    throw new FormulaFormatException("Error: Any token that immediately follows an opening parenthesis or an operator must be either a number, a variable, or an opening parenthesis. Suggestion: reformat formula.");
            }

            //Following a number, a variable, or a right parens case
            if (Double.TryParse(PrecedingToken, out _) || IsValidVariable(PrecedingToken) || PrecedingToken == ")")
            {
                //Check if this token is either a right parens or an operator
                if (token == "(" || IsValidVariable(token) || Double.TryParse(token, out _))
                    throw new FormulaFormatException("Error: Any token that immediately follows a number, a variable, or a closing parenthesis must be either an operator or a closing parenthesis. Suggestion: reformat formula.");
            }
        }
    }

    /// <summary>
    /// Used to report syntactic errors in the argument to the Formula constructor.
    /// </summary>
    public class FormulaFormatException : Exception
    {
        /// <summary>
        /// Constructs a FormulaFormatException containing the explanatory message.
        /// </summary>
        public FormulaFormatException(String message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Used as a possible return value of the Formula.Evaluate method.
    /// </summary>
    public struct FormulaError
    {
        /// <summary>
        /// Constructs a FormulaError containing the explanatory reason.
        /// </summary>
        /// <param name="reason"></param>
        public FormulaError(String reason)
            : this()
        {
            Reason = reason;
        }

        /// <summary>
        ///  The reason why this FormulaError was created.
        /// </summary>
        public string Reason { get; private set; }
    }

    public static class PS1StackExt
    {
        /// <summary>
        /// Checks if the stack contains at least one value, and if the value at the top of the stack matches the input symbols.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stack"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static bool CheckStack<T>(this Stack<T> stack, T symbol1, T symbol2)
        {
            if (stack.Count < 1)
                return false;
            return stack.Peek().Equals(symbol1) || stack.Peek().Equals(symbol2);
        }

        /// <summary>
        /// Overload when it is desired to only check the stack for one symbol.
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

