// Skeleton implementation written by Joe Zachary and Dan Ruley for CS 3500, September 2013.  Updated September 2019.
// Version 1.1 (Fixed error in comment for RemoveDependency.)
// Version 1.2 - Daniel Kopta 
//               (Clarified meaning of dependent and dependee.)
//               (Clarified names in solution/project structure.)
//Version 1.3 - Dan Ruley
//              (Completed implementation)

using System.Collections.Generic;

namespace SpreadsheetUtilities
{

    /// <summary>
    /// (s1,t1) is an ordered pair of strings
    /// t1 depends on s1; s1 must be evaluated before t1
    /// 
    /// A DependencyGraph can be modeled as a set of ordered pairs of strings.  Two ordered pairs
    /// (s1,t1) and (s2,t2) are considered equal if and only if s1 equals s2 and t1 equals t2.
    /// Recall that sets never contain duplicates.  If an attempt is made to add an element to a 
    /// set, and the element is already in the set, the set remains unchanged.
    /// 
    /// Given a DependencyGraph DG:
    /// 
    ///    (1) If s is a string, the set of all strings t such that (s,t) is in DG is called dependents(s).
    ///        (The set of things that depend on s)    
    ///        
    ///    (2) If s is a string, the set of all strings t such that (t,s) is in DG is called dependees(s).
    ///        (The set of things that s depends on) 
    ///
    /// For example, suppose DG = {("a", "b"), ("a", "c"), ("b", "d"), ("d", "d")}
    ///     dependents("a") = {"b", "c"}
    ///     dependents("b") = {"d"}
    ///     dependents("c") = {}
    ///     dependents("d") = {"d"}
    ///     dependees("a") = {}
    ///     dependees("b") = {"a"}
    ///     dependees("c") = {"a"}
    ///     dependees("d") = {"b", "d"}
    /// </summary>
    public class DependencyGraph
    {
        /// <summary>
        /// Contains string keys that map to a set of dependents.
        /// </summary>
        private Dictionary<string, HashSet<string>> Dependents;

        /// <summary>
        /// Contains string keys that map to a set of dependees.
        /// </summary>
        private Dictionary<string, HashSet<string>> Dependees;



        /// <summary>
        /// Creates an empty DependencyGraph.
        /// </summary>
        public DependencyGraph()
        {
            Dependents = new Dictionary<string, HashSet<string>>();
            Dependees = new Dictionary<string, HashSet<string>>();
            Size = 0;
        }


        /// <summary>
        /// The number of ordered pairs in the DependencyGraph.
        /// </summary>
        public int Size
        {
            get;

            private set;

        }


        /// <summary>
        /// The size of dependees(s).
        /// This property is an example of an indexer.  If dg is a DependencyGraph, you would
        /// invoke it like this:
        /// dg["a"]
        /// It should return the size of dependees("a")
        /// </summary>
        public int this[string s]
        {
            get
            {
                if (Dependees.ContainsKey(s))
                    return Dependees[s].Count;
                else
                    return 0;
            }
        }


        /// <summary>
        /// Reports whether dependents(s) is non-empty.
        /// </summary>
        public bool HasDependents(string s)
        {
            HashSet<string> dents;

            if (Dependents.TryGetValue(s, out dents))
                return dents.Count > 0;

            return false;
        }


        /// <summary>
        /// Reports whether dependees(s) is non-empty.
        /// </summary>
        public bool HasDependees(string s)
        {
            HashSet<string> dees;

            if (Dependees.TryGetValue(s, out dees))
                return dees.Count > 0;

            return false;

        }


        /// <summary>
        /// Enumerates dependents(s).
        /// </summary>
        public IEnumerable<string> GetDependents(string s)
        {
            if (Dependents.TryGetValue(s, out _))
                return new HashSet<string>(Dependents[s]);

            return new HashSet<string>();
        }

        /// <summary>
        /// Enumerates dependees(s).
        /// </summary>
        public IEnumerable<string> GetDependees(string s)
        {
            if (Dependees.TryGetValue(s, out _))
                return new HashSet<string>(Dependees[s]);

            return new HashSet<string>();
        }


        /// <summary>
        /// <para>Adds the ordered pair (s,t), if it doesn't exist</para>
        /// 
        /// <para>This should be thought of as:</para>   
        /// 
        ///   t depends on s
        ///
        /// </summary>
        /// <param name="s"> s must be evaluated first. T depends on S</param>
        /// <param name="t"> t cannot be evaluated until s is</param>        /// 
        public void AddDependency(string s, string t)
        {
            //if dependency already exists in graph, do nothing
            if (IsDependencyInGraph(s, t))
                return;

            //increment size
            Size++;

            AddDependent(s, t);
            AddDependee(s, t);

        }


        /// <summary>
        /// Removes the ordered pair (s,t), if it exists
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public void RemoveDependency(string s, string t)
        {

            //if the dependency is not in the graph, do nothing
            if (!IsDependencyInGraph(s, t))
                return;

            //there is a dependency to remove: decrement size
            Size--;

            //remove t from S' hashset
            Dependents[s].Remove(t);

            //remove s from T's hashset
            Dependees[t].Remove(s);

            //If these removes result in either s or t becoming a "stray" node, remove the orphan from the graph
            if (Dependents[s].Count == 0 && Dependees[s].Count == 0)
            {
                Dependents.Remove(s);
                Dependees.Remove(s);
            }

            //only need this branch if s and t are different values
            if (t != s)
            {
                if (Dependents[t].Count == 0 && Dependees[t].Count == 0)
                {
                    Dependents.Remove(t);
                    Dependees.Remove(t);
                }
            }

        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (s,r).  Then, for each
        /// t in newDependents, adds the ordered pair (s,t).
        /// </summary>
        public void ReplaceDependents(string s, IEnumerable<string> newDependents)
        {

            foreach (string temp in new HashSet<string>(GetDependents(s)))
                RemoveDependency(s, temp);



            //add the new dependents
            foreach (string temp in new HashSet<string>(newDependents))
                AddDependency(s, temp);
        }

    
    


        


        /// <summary>
        /// Removes all existing ordered pairs of the form (r,s).  Then, for each 
        /// t in newDependees, adds the ordered pair (t,s).
        /// </summary>
        public void ReplaceDependees(string s, IEnumerable<string> newDependees)
        {
           
            foreach (string tempdependee in GetDependees(s))
                RemoveDependency(tempdependee, s);


            foreach (string newdependee in new HashSet<string>(newDependees))
                AddDependency(newdependee, s);

        }

        /// <summary>
        /// Add t to s's set of Dependents.  Also adds t to Dependents as a kep mapping to an empty set, if it is not already there.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        private void AddDependent(string s, string t)
        {
            
            if (!Dependents.ContainsKey(s))
            {
                Dependents.Add(s, new HashSet<string>());
                Dependents[s].Add(t);
            }
            else
                Dependents[s].Add(t);


            if (!Dependents.ContainsKey(t))
            {
                Dependents.Add(t, new HashSet<string>());
            }


        }

        /// <summary>
        /// Adds s to t's set of Dependee's.  Also adds s to Dependees as a key mapping to an empty set if it is not already there.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        private void AddDependee(string s, string t)
        {

            if (!Dependees.ContainsKey(t))
            {
                Dependees.Add(t, new HashSet<string>());
                Dependees[t].Add(s);
            }

            else
                Dependees[t].Add(s);

            if (!Dependees.ContainsKey(s))
            {
                Dependees.Add(s, new HashSet<string>());
            }


        }

        /// <summary>
        /// Determines if a given dependency is already in the graph.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns>
        /// True if the dependency already exists in the graph, false otherwise.
        /// </returns>
        private bool IsDependencyInGraph(string s, string t)
        {
            HashSet<string> deps;

            if (Dependents.TryGetValue(s, out deps))
            {
                return deps.Contains(t);
            }

            return false;
        }



    }

}

