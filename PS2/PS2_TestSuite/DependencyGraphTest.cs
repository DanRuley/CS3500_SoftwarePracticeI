//Dan Ruley, September 2019
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpreadsheetUtilities;
using System;
using System.Collections.Generic;


namespace DevelopmentTests
{
    /// <summary>
    ///This is a test class for DependencyGraphTest and is intended
    ///to contain all DependencyGraphTest Unit Tests
    ///</summary>
    [TestClass()]
    public class DependencyGraphTest
    {

        /// <summary>
        ///Empty graph should contain nothing
        ///</summary>
        [TestMethod()]
        public void SimpleEmptyTest()
        {
            DependencyGraph t = new DependencyGraph();
            Assert.AreEqual(0, t.Size);
        }


        /// <summary>
        ///Empty graph should contain nothing
        ///</summary>
        [TestMethod()]
        public void SimpleEmptyRemoveTest()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("x", "y");
            Assert.AreEqual(1, t.Size);
            t.RemoveDependency("x", "y");
            Assert.AreEqual(0, t.Size);
        }


        /// <summary>
        ///Empty graph should contain nothing
        ///</summary>
        [TestMethod()]
        public void EmptyEnumeratorTest()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("x", "y");
            IEnumerator<string> e1 = t.GetDependees("y").GetEnumerator();
            Assert.IsTrue(e1.MoveNext());
            Assert.AreEqual("x", e1.Current);
            IEnumerator<string> e2 = t.GetDependents("x").GetEnumerator();
            Assert.IsTrue(e2.MoveNext());
            Assert.AreEqual("y", e2.Current);
            t.RemoveDependency("x", "y");
            Assert.IsFalse(t.GetDependees("y").GetEnumerator().MoveNext());
            Assert.IsFalse(t.GetDependents("x").GetEnumerator().MoveNext());
        }


        /// <summary>
        ///Replace on an empty DG shouldn't fail
        ///</summary>
        [TestMethod()]
        public void SimpleReplaceTest()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("x", "y");
            Assert.AreEqual(t.Size, 1);
            t.RemoveDependency("x", "y");
            t.ReplaceDependents("x", new HashSet<string>());
            t.ReplaceDependees("y", new HashSet<string>());
        }




        ///<summary>
        ///It should be possibe to have more than one DG at a time.
        ///</summary>
        [TestMethod()]
        public void StaticTest()
        {
            DependencyGraph t1 = new DependencyGraph();
            DependencyGraph t2 = new DependencyGraph();
            t1.AddDependency("x", "y");
            Assert.AreEqual(1, t1.Size);
            Assert.AreEqual(0, t2.Size);
        }




        /// <summary>
        ///Non-empty graph contains something
        ///</summary>
        [TestMethod()]
        public void SizeTest()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("a", "b");
            t.AddDependency("a", "c");
            t.AddDependency("c", "b");
            t.AddDependency("b", "d");
            Assert.AreEqual(4, t.Size);
        }


        /// <summary>
        ///Non-empty graph contains something
        ///</summary>
        [TestMethod()]
        public void EnumeratorTest()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("a", "b");
            t.AddDependency("a", "c");
            t.AddDependency("c", "b");
            t.AddDependency("b", "d");

            IEnumerator<string> e = t.GetDependees("a").GetEnumerator();
            Assert.IsFalse(e.MoveNext());

            e = t.GetDependees("b").GetEnumerator();
            Assert.IsTrue(e.MoveNext());
            String s1 = e.Current;
            Assert.IsTrue(e.MoveNext());
            String s2 = e.Current;
            Assert.IsFalse(e.MoveNext());
            Assert.IsTrue(((s1 == "a") && (s2 == "c")) || ((s1 == "c") && (s2 == "a")));

            e = t.GetDependees("c").GetEnumerator();
            Assert.IsTrue(e.MoveNext());
            Assert.AreEqual("a", e.Current);
            Assert.IsFalse(e.MoveNext());

            e = t.GetDependees("d").GetEnumerator();
            Assert.IsTrue(e.MoveNext());
            Assert.AreEqual("b", e.Current);
            Assert.IsFalse(e.MoveNext());
        }




        /// <summary>
        ///Non-empty graph contains something
        ///</summary>
        [TestMethod()]
        public void ReplaceThenEnumerate()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("x", "b");
            t.AddDependency("a", "z");
            t.ReplaceDependents("b", new HashSet<string>());
            t.AddDependency("y", "b");
            t.ReplaceDependents("a", new HashSet<string>() { "c" });
            t.AddDependency("w", "d");
            t.ReplaceDependees("b", new HashSet<string>() { "a", "c" });
            t.ReplaceDependees("d", new HashSet<string>() { "b" });

            IEnumerator<string> e = t.GetDependees("a").GetEnumerator();
            Assert.IsFalse(e.MoveNext());

            e = t.GetDependees("b").GetEnumerator();
            Assert.IsTrue(e.MoveNext());
            String s1 = e.Current;
            Assert.IsTrue(e.MoveNext());
            String s2 = e.Current;
            Assert.IsFalse(e.MoveNext());
            Assert.IsTrue(((s1 == "a") && (s2 == "c")) || ((s1 == "c") && (s2 == "a")));

            e = t.GetDependees("c").GetEnumerator();
            Assert.IsTrue(e.MoveNext());
            Assert.AreEqual("a", e.Current);
            Assert.IsFalse(e.MoveNext());

            e = t.GetDependees("d").GetEnumerator();
            Assert.IsTrue(e.MoveNext());
            Assert.AreEqual("b", e.Current);
            Assert.IsFalse(e.MoveNext());
        }



        /// <summary>
        ///Using lots of data
        ///</summary>
        [TestMethod()]
        public void StressTest()
        {
            // Dependency graph
            DependencyGraph t = new DependencyGraph();

            // A bunch of strings to use
            const int SIZE = 200;
            string[] letters = new string[SIZE];
            for (int i = 0; i < SIZE; i++)
            {
                letters[i] = ("" + (char)('a' + i));
            }

            // The correct answers
            HashSet<string>[] dents = new HashSet<string>[SIZE];
            HashSet<string>[] dees = new HashSet<string>[SIZE];
            for (int i = 0; i < SIZE; i++)
            {
                dents[i] = new HashSet<string>();
                dees[i] = new HashSet<string>();
            }

            // Add a bunch of dependencies
            for (int i = 0; i < SIZE; i++)
            {
                for (int j = i + 1; j < SIZE; j++)
                {
                    t.AddDependency(letters[i], letters[j]);
                    dents[i].Add(letters[j]);
                    dees[j].Add(letters[i]);
                }
            }

            // Remove a bunch of dependencies
            for (int i = 0; i < SIZE; i++)
            {
                for (int j = i + 4; j < SIZE; j += 4)
                {
                    t.RemoveDependency(letters[i], letters[j]);
                    dents[i].Remove(letters[j]);
                    dees[j].Remove(letters[i]);
                }
            }

            // Add some back
            for (int i = 0; i < SIZE; i++)
            {
                for (int j = i + 1; j < SIZE; j += 2)
                {
                    t.AddDependency(letters[i], letters[j]);
                    dents[i].Add(letters[j]);
                    dees[j].Add(letters[i]);
                }
            }

            // Remove some more
            for (int i = 0; i < SIZE; i += 2)
            {
                for (int j = i + 3; j < SIZE; j += 3)
                {
                    t.RemoveDependency(letters[i], letters[j]);
                    dents[i].Remove(letters[j]);
                    dees[j].Remove(letters[i]);
                }
            }

            // Make sure everything is right
            for (int i = 0; i < SIZE; i++)
            {
                Assert.IsTrue(dents[i].SetEquals(new HashSet<string>(t.GetDependents(letters[i]))));
                Assert.IsTrue(dees[i].SetEquals(new HashSet<string>(t.GetDependees(letters[i]))));
            }
        }




        [TestMethod()]
        public void TestGetDependentsSizeAndValues()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("a", "b");
            t.AddDependency("a", "c");
            t.AddDependency("c", "b");
            t.AddDependency("b", "d");

            HashSet<string> s = (HashSet<string>)t.GetDependents("d");
            Assert.AreEqual(0, s.Count);

            s = (HashSet<string>)t.GetDependents("a");
            Assert.AreEqual(2, s.Count);
            Assert.IsTrue(s.Contains("b"));
            Assert.IsTrue(s.Contains("c"));
            Assert.IsFalse(s.Contains("a"));
            Assert.IsFalse(s.Contains("d"));

            s = (HashSet<string>)t.GetDependents("b");
            Assert.AreEqual(1, s.Count);
            Assert.IsTrue(s.Contains("d"));

            s = (HashSet<string>)t.GetDependents("c");
            Assert.AreEqual(1, s.Count);
            Assert.IsTrue(s.Contains("b"));
        }



        [TestMethod()]
        public void TestGetDependeesSizeAndValues()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("a", "b");
            t.AddDependency("a", "c");
            t.AddDependency("c", "b");
            t.AddDependency("b", "d");

            HashSet<string> s = (HashSet<string>)t.GetDependees("d");
            Assert.AreEqual(1, s.Count);
            Assert.IsTrue(s.Contains("b"));

            s = (HashSet<string>)t.GetDependees("b");
            Assert.AreEqual(2, s.Count);
            Assert.IsTrue(s.Contains("a"));
            Assert.IsTrue(s.Contains("c"));
            Assert.IsFalse(s.Contains("b"));
            Assert.IsFalse(s.Contains("d"));

            s = (HashSet<string>)t.GetDependees("a");
            Assert.AreEqual(0, s.Count);

            s = (HashSet<string>)t.GetDependees("c");
            Assert.AreEqual(1, s.Count);
            Assert.IsTrue(s.Contains("a"));
        }



        [TestMethod()]
        public void TestHasDependentsFull()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("a", "b");
            t.AddDependency("a", "c");
            t.AddDependency("c", "b");
            t.AddDependency("b", "d");

            Assert.IsTrue(t.HasDependents("a"));
            Assert.IsTrue(t.HasDependents("b"));
            Assert.IsTrue(t.HasDependents("c"));
            Assert.IsFalse(t.HasDependents("d"));

        }



        [TestMethod()]
        public void TestHasDependeesFull()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("a", "b");
            t.AddDependency("a", "c");
            t.AddDependency("c", "b");
            t.AddDependency("b", "d");

            Assert.IsFalse(t.HasDependees("a"));
            Assert.IsTrue(t.HasDependees("b"));
            Assert.IsTrue(t.HasDependees("c"));
            Assert.IsTrue(t.HasDependees("d"));

        }



        [TestMethod()]
        public void TestHasDependentsEmptyWorks()
        {
            DependencyGraph t = new DependencyGraph();

            Assert.IsFalse(t.HasDependents("d"));
        }



        [TestMethod()]
        public void TestHasDependeesEmptyWorks()
        {
            DependencyGraph t = new DependencyGraph();

            Assert.IsFalse(t.HasDependees("d"));
        }



        [TestMethod()]
        public void TestDependeeIndexFullOrEmpty()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("a", "b");
            t.AddDependency("a", "c");
            t.AddDependency("c", "b");
            t.AddDependency("b", "d");

            Assert.AreEqual(0, t["a"]);
            Assert.AreEqual(2, t["b"]);
            Assert.AreEqual(1, t["c"]);
            Assert.AreEqual(1, t["d"]);
            Assert.AreEqual(0, t["asdfj"]);

        }




        [TestMethod()]
        public void ReplaceDependentsAndThenRemoveStressTest()
        {
            DependencyGraph t = new DependencyGraph();

            HashSet<string> expected = new HashSet<string>();
            HashSet<string> actual = new HashSet<string>();

            //build giant set of chars
            for (int i = 0; i < 1000; i++)
            {
                expected.Add("" + (char)('a' + i));
            }

            t.AddDependency("a", "b");

            t.ReplaceDependents("a", expected);

            actual = (HashSet<string>)t.GetDependents("a");

            foreach (string s in expected)
            {
                Assert.IsTrue(actual.Contains(s));
            }

        }




        [TestMethod()]
        public void ReplaceDependeesAndIndexerStressTest()
        {
            DependencyGraph t = new DependencyGraph();

            HashSet<string> expected = new HashSet<string>();
            HashSet<string> actual = new HashSet<string>();

            //build giant set of chars
            for (int i = 0; i < 1000; i++)
            {
                expected.Add("" + (char)('a' + i));
            }

            t.AddDependency("a", "b");

            t.ReplaceDependees("b", expected);

            actual = (HashSet<string>)t.GetDependees("b");

            foreach (string s in expected)
            {
                if (s != "b")
                    Assert.AreEqual(0, t[s]);

                Assert.IsTrue(actual.Contains(s));
            }

            Assert.AreEqual(1000, t["b"]);

            foreach (string s in expected)
            {
                t.RemoveDependency(s, "b");
            }

            Assert.AreEqual(0, t.Size);

        }


        [TestMethod()]
        public void ReplaceSelfCycleWithNewDependentTest()
        {
            DependencyGraph t = new DependencyGraph();
            t.AddDependency("x", "x");
            Assert.AreEqual(1, t.Size);
            t.ReplaceDependents("x", new HashSet<string> { "y" });
            Assert.AreEqual(1, t.Size);

            IEnumerator<string> e2 = t.GetDependents("x").GetEnumerator();
            Assert.IsTrue(e2.MoveNext());
            Assert.AreEqual("y", e2.Current);

        }




    }
}
