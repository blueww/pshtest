using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Data;
using System.Collections;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;

namespace MS.Test.Common.MsTestLib
{
    class Program
    {
        static int DEFAULT_TIME_OUT = 3600000; // default timeout for test case execution
        const string DefaultAnswerFileName = "RerunCandidates.txt";

        static int Main(string[] args)
        {
            //parse the commandline
            Dictionary<string, string> argsGroup = new Dictionary<string, string>();
            List<string> testCategories = new List<string>();
            List<string> switchGroup = new List<string>();
            List<string> ExcludedCases = new List<string>();
            foreach (string arg in args)
            {
                Regex rp = new Regex("(?<=^/).*?(?=:|$)");
                Match pMatch = rp.Match(arg);
                if (pMatch.Success)
                {
                    string paramName = pMatch.Value;
                    Regex rv = new Regex("(?<=(^/.*?:)).*$");
                    Match vMatch = rv.Match(arg);
                    if (vMatch.Success)
                    {
                        string paramValue = vMatch.Value;
                        if ("tag" == paramName)
                        {
                            testCategories.Add(paramValue);
                            continue;
                        }
                        if (!argsGroup.ContainsKey(paramName))
                        {
                            argsGroup.Add(paramName, paramValue);
                        }
                    }
                    else
                    {
                        if (!switchGroup.Contains(paramName))
                        {
                            switchGroup.Add(paramName);
                        }
                    }
                }
            }

            //for -args value -switches type
            string preArg = string.Empty;
            foreach (string arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    string argTrimmed = arg.TrimStart(new char[] { '-' });
                    if (preArg == string.Empty)
                    {
                        if (!switchGroup.Contains(argTrimmed))
                        {
                            switchGroup.Add(argTrimmed);
                        }
                    }
                    preArg = argTrimmed;
                }
                else
                {
                    if (preArg != string.Empty)
                    {
                        if ("tag" == preArg)
                        {
                            testCategories.Add(arg);
                            preArg = string.Empty;
                            continue;
                        }
                        if (!argsGroup.ContainsKey(preArg))
                        {
                            argsGroup.Add(preArg, arg);
                            preArg = string.Empty;
                        }
                    }
                }
            }

            string testDllName = string.Empty;
            if (argsGroup.ContainsKey("lib"))
            {
                testDllName = argsGroup["lib"];
            }

            string testMethodName = string.Empty;
            List<string> testMethodNames = null;
            if (argsGroup.ContainsKey("case"))
            {
                testMethodName = argsGroup["case"];
                testMethodNames = new List<string>(testMethodName.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }

            List<string> rerunCases = null;
            if (argsGroup.ContainsKey("answerfile"))
            {
                string answerFileName = argsGroup["answerfile"];

                string[] caseNames;
                try
                {
                    caseNames = File.ReadAllLines(answerFileName);
                }
                catch (IOException e)
                {
                    Console.WriteLine("Exception in read answer file : {0}", e.ToString());
                    return -1;
                }

                rerunCases = new List<string>();
                foreach (string name in caseNames)
                {
                    rerunCases.Add(name);
                }
            }

            string testClassName = string.Empty;
            List<string> testClassNames = null;
            if (argsGroup.ContainsKey("group"))
            {
                testClassName = argsGroup["group"];
                testClassNames = new List<string>(testClassName.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }

            string excludeTestCategory = string.Empty;
            List<string> excludeTestCategories = new List<string>();
            if (argsGroup.ContainsKey("extag"))
            {
                excludeTestCategory = argsGroup["extag"];
                excludeTestCategories = new List<string>(excludeTestCategory.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }

            string testDataFile = string.Empty;
            if (argsGroup.ContainsKey("config"))
            {
                testDataFile = argsGroup["config"];
                Test.TestDataFile = testDataFile;
            }

            int testCaseReRunCount = 0;
            if (argsGroup.ContainsKey("rerun"))
            {
                testCaseReRunCount = int.Parse(argsGroup["rerun"]);
            }

            string language = string.Empty;
            if (argsGroup.ContainsKey("lang"))
            {
                language = argsGroup["lang"];
            }

            int testCaseTimeout = System.Threading.Timeout.Infinite;
            if (argsGroup.ContainsKey("timeout"))
            {
                testCaseTimeout = int.Parse(argsGroup["timeout"]) * 1000;
            }

            string testOffset = string.Empty;
            if (argsGroup.ContainsKey("offset"))
            {
                testOffset = argsGroup["offset"];
            }

            string testLength = string.Empty;
            if (argsGroup.ContainsKey("length"))
            {
                testLength = argsGroup["length"];
            }

            if (argsGroup.Count == 0 || string.IsNullOrEmpty(testDllName))
            {
                PrintHelp();
                return -9009;
            }

            // check if input satisfies certain condition
            if (switchGroup.Contains("answerfile"))
            {
                Console.Error.WriteLine("When use /answerfile option, you must explicitly specify an answerfile.");
                Console.Error.WriteLine("MsTest2.exe will exit now.");
                return -1;
            }
            if ((argsGroup.ContainsKey("answerfile"))
                && (argsGroup.ContainsKey("group") || argsGroup.ContainsKey("case") || argsGroup.ContainsKey("tag") || argsGroup.ContainsKey("extag")))
            {
                Console.Error.WriteLine("Cannot use /answerfile option with /group, /case, /tag, /extag options..");
                Console.Error.WriteLine("MsTest2.exe will exit now.");
                return -1;
            }

            //Assembly testAssembly = Assembly.LoadFrom("TestSample.dll");
            Assembly testAssembly = Assembly.LoadFrom(testDllName);

            TestClassUnit[] testClasses = TestClassUnit.GetTestGroupUnits(testAssembly);

            //filter the test cases
            int totalActiveCases = 0;
            if (rerunCases == null)
            {
                if (string.IsNullOrEmpty(testClassName) && string.IsNullOrEmpty(testMethodName) && testCategories.Count == 0 && testMethodNames == null)
                {
                    // no fileter
                    // recount ActiveCases for each test class
                    foreach (TestClassUnit testClass in testClasses)
                    {
                        foreach (TestMethodUnit testMethod in testClass.TestCaseUnits)
                        {
                            if (testMethod.Enable == true)
                            {
                                testClass.ActiveCases++;
                            }
                        }
                        totalActiveCases += testClass.ActiveCases;
                    }
                }
                else
                {
                    foreach (TestClassUnit testClass in testClasses)
                    {
                        if (testClassNames == null)
                        {
                            // testClass.Enable = true;
                        }
                        else
                        {
                            if (testClassNames.Contains(testClass.Name))
                            {
                                // testClass.Enable = true;
                            }
                            else
                            {
                                testClass.Enable = false;
                                continue;
                            }
                        }

                        foreach (TestMethodUnit testMethod in testClass.TestCaseUnits)
                        {
                            if (testMethodNames == null)
                            {
                                //testMethod.Enable = true;
                                //testClass.ActiveCases++;
                            }
                            else
                            {
                                if (QualifyWithWildcard(testMethodNames, testMethod))
                                {
                                    //testMethod.Enable = true;
                                    //testClass.ActiveCases++;
                                }
                                else
                                {
                                    testMethod.Enable = false;
                                    continue;
                                }
                            }

                            if ((testCategories.Count > 0 || excludeTestCategories.Count > 0) && (testMethod.Tag != null))
                            {
                                if (testCategories.Count == 0 || QualifyWithDupTags(testCategories, testMethod))
                                {
                                    if (excludeTestCategories.Intersect<string>(testMethod.Tag).Count<string>() > 0)
                                    {
                                        ExcludedCases.Add(String.Format("{0}.{1}", testClass.Name, testMethod.Name));
                                        testMethod.Enable = false;
                                    }
                                    else
                                    {
                                        //testMethod.Enable = true;
                                    }
                                }
                                else
                                {
                                    testMethod.Enable = false;
                                }
                            }
                        }
                    }
                }
            }
            else // execute answerfile specified cases
            {
                foreach (TestClassUnit testClass in testClasses)
                {
                    foreach (TestMethodUnit testMethod in testClass.TestCaseUnits)
                    {
                        if (rerunCases.Contains(testClass.Name + "." + testMethod.Name))
                        {
                            //testMethod.Enable = true;
                            //testClass.ActiveCases++;
                        }
                        else
                        {
                            testMethod.Enable = false;
                        }
                    }
                }
            }

            //count the active test cases
            foreach (TestClassUnit testClass in testClasses)
            {
                foreach (TestMethodUnit testMethod in testClass.TestCaseUnits)
                {
                    if (testMethod.Enable == true)
                    {
                        testClass.ActiveCases++;
                    }
                }

                totalActiveCases += testClass.ActiveCases;
            }

            // filter test cases according to specified offset and length
            if (string.IsNullOrEmpty(testOffset) && string.IsNullOrEmpty(testLength))
            {
                // no filter
            }
            else
            {
                int offset, length;
                try
                {
                    offset = string.IsNullOrEmpty(testOffset) ? 0 : int.Parse(testOffset);
                    length = string.IsNullOrEmpty(testLength) ? totalActiveCases : int.Parse(testLength);
                }
                catch (FormatException)
                {
                    Console.Error.WriteLine("The value you specified for length or offset is not a number.");
                    return -1;
                }

                if (offset >= totalActiveCases)
                {
                    Console.Error.WriteLine("Offset is bigger than total active cases number, thus no cases will be executed.");
                    return -1;
                }

                if (offset + length > totalActiveCases)
                {
                    Console.Error.WriteLine("Specified offset and length is out of range, test cases from the offset to the end will be executed.");
                }

                int index = -1;
                foreach (TestClassUnit testClass in testClasses)
                {
                    if (!testClass.Enable || testClass.ActiveCases == 0)
                    {
                        continue;
                    }

                    foreach (TestMethodUnit testMethod in testClass.TestCaseUnits)
                    {
                        if (testMethod.Enable == true)
                        {
                            index++;
                            if (index < offset || index >= offset + length)
                            {
                                testMethod.Enable = false;
                                testClass.ActiveCases--;
                            }
                        }
                    }
                }
            }

            //if 'list' is specified, only list the enabled test cases
            if (switchGroup.Contains("list"))
            {
                foreach (TestClassUnit testClass in testClasses)
                {
                    if (testClass.AssemblyInitMethod != null)
                    {
                        Console.WriteLine("[Test Init] : {0}", testClass.AssemblyInitMethod.Name);
                    }
                }

                int totalCases = 0;
                foreach (TestClassUnit testClass in testClasses)
                {
                    Console.WriteLine("[Test Class] : {0}", testClass.Name);

                    foreach (TestMethodUnit testMethod in testClass.TestCaseUnits)
                    {
                        Console.WriteLine("      [Test Method] {0} {1}", testMethod.Name, testMethod.Enable ? "Enabled" : "Disabled");
                    }

                    Console.WriteLine("[Active Cases] : {0}", testClass.ActiveCases);
                    totalCases += testClass.ActiveCases;
                }

                foreach (TestClassUnit testClass in testClasses)
                {
                    if (testClass.AssemblyCleanupMethod != null)
                    {
                        Console.WriteLine("[Test Cleanup] : {0}", testClass.AssemblyCleanupMethod.Name);
                    }
                }

                Console.WriteLine("[Test Total] : {0}", totalCases);

                return 0;
            }

            //execute assembly init
            foreach (TestClassUnit testClass in testClasses)
            {
                if (testClass.AssemblyInitMethod != null)
                {
                    try
                    {
                        TestContext2 testContext = new TestContext2();
                        testContext.Properties.Add("config", testDataFile);

                        testClass.AssemblyInitMethod.Invoke(null, new object[] { testContext });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception in AssemblyInit : {0}", e.ToString());
                        return -1;
                    }
                    // overwrite the delegate for AssertFail
                    Test.AssertFail = new AssertFailDelegate((string a) => { });
                }
            }

            //execute test classes and test methods
            foreach (TestClassUnit testClass in testClasses)
            {
                //init the class
                if (!testClass.Enable || testClass.ActiveCases == 0)
                {
                    continue;
                }

                TestContext2 testContext = new TestContext2();
                testContext.fullyQualifiedTestClassName = testClass.TestGroupClass.FullName;
                testContext.Properties.Add("lang", language);

                bool classInitOK = true;
                if (testClass.ClassInitMethod != null)
                {
                    try
                    {
                        testClass.ClassInitMethod.Invoke(null, new object[] { testContext });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Class {0} init exception : {1}", testClass.Name, e.ToString());

                        //Add all enabled cases in the class to skipped test cases list
                        foreach (TestMethodUnit testMethod in testClass.TestCaseUnits)
                        {
                            if (!testMethod.Enable)
                            {
                                continue;
                            }
                            else
                            {
                                Test.AssertFail(string.Format("The case {0}.{1} is skipped since class init fail. Please check the detailed case log.", testClass.Name, testMethod.Name));
                                Test.SkippedCases.Add(String.Format("{0}.{1}", testClass.Name, testMethod.Name));
                                Test.TestCount++;
                                Test.SkipCount++;
                            }
                        }
                        classInitOK = false; // this means to skip the method execution but still do the cleanup
                    }
                }

                if (classInitOK)
                {
                    object testObject = Activator.CreateInstance(testClass.TestGroupClass);
                    PropertyInfo pInfo = testClass.TestGroupClass.GetProperty("TestContext");
                    if (pInfo != null)
                    {
                        pInfo.SetValue(testObject, testContext, null);
                    }

                    foreach (TestMethodUnit testMethod in testClass.TestCaseUnits)
                    {
                        if (!testMethod.Enable)
                        {
                            continue;
                        }

                        //init the test method
                        testContext.testName = testMethod.TestCase.Name;
                        if (pInfo != null)
                        {
                            pInfo.SetValue(testObject, testContext, null);
                        }

                        // rerun the case if rerunCount > 0
                        for (int cr = 0; cr <= testCaseReRunCount; cr++)
                        {
                            bool testInitOK = true;

                            if (testClass.TestInitMethod != null)
                            {
                                try
                                {
                                    testClass.TestInitMethod.Invoke(testObject, new object[] { });
                                }
                                catch (Exception e)
                                {
                                    Test.SkipError("Method {0} init exception : {1}", testMethod.Name, e.ToString());
                                    testInitOK = false;
                                }
                            }

                            if (testInitOK)
                            {
                                // deal with timeout
                                //testMethod.TestCase.Invoke(testObject, new object[] { });

                                //Exception innerException = null;
                                //AutoResetEvent testExecutionDone = new AutoResetEvent(false);

                                TestThreadArgs threadArgs = new TestThreadArgs();
                                threadArgs.InnerException = null;
                                threadArgs.TestExecutionDone = new AutoResetEvent(false);
                                threadArgs.TestObject = testObject;
                                threadArgs.Method = testMethod.TestCase;

                                Thread testThread = new Thread(threadArgs.TestCase);
#if !DOTNET5_4
                                testThread.SetApartmentState(System.Threading.ApartmentState.STA);
#endif
                                testThread.Start();

                                bool executionResult = false;
                                // Per case Timeout will overrule general test case timeout setting
                                int timeout = 0;
                                if (testMethod.Timeout > 0)
                                {
                                    timeout = testMethod.Timeout;

                                }
                                else if (testCaseTimeout > 0)
                                {
                                    timeout = testCaseTimeout;
                                }
                                else
                                {
                                    if (testCaseTimeout == 0)
                                    {
                                        // set timeout to -1
                                        timeout = System.Threading.Timeout.Infinite;
                                    }
                                    else
                                    {
                                        timeout = DEFAULT_TIME_OUT;
                                    }
                                }
                                // if timeout is -1, this case has infinite running time.
                                executionResult = threadArgs.TestExecutionDone.WaitOne(timeout, false);
                                if (!executionResult)
                                {
                                    testThread.Abort();
                                    Console.WriteLine("Test Case execution is too long, timeout happens and testing aborted, expected runtime: " + timeout + " milliseconds");
                                    Test.Error("Test {0} timeout after {1} ms.", testMethod.Name, timeout);
                                }
                                else
                                {
                                    if (threadArgs.InnerException != null)
                                    {
                                        Console.WriteLine("Test Case execution throws exception {0}", threadArgs.InnerException.ToString());
                                        Test.Error("Test Case execution throws exception {0}", threadArgs.InnerException.ToString());

                                        if (threadArgs.InnerException is TestPauseException)
                                        {
                                            // if test pause exception is thrown, pause the test run to wait for investigation
                                            Console.BackgroundColor = ConsoleColor.Red;
                                            Console.ForegroundColor = ConsoleColor.Black;
                                            Console.Write("Test run is paused for TestPauseException is thrown in the case. Press ESC to continue the run after investigation.");
                                            Console.ResetColor();
                                            Console.WriteLine();
                                            ConsoleKeyInfo ki;
                                            do
                                            {
                                                ki = Console.ReadKey(true);
                                            } while (ki.Key != ConsoleKey.Escape);
                                        }
                                    }
                                }
                            }

                            //cleanup the test method
                            if (testClass.TestCleanupMethod != null)
                            {
                                try
                                {
                                    testClass.TestCleanupMethod.Invoke(testObject, new object[] { });
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Method {0} cleanup exception : {1}", testMethod.Name, e.ToString());
                                }
                            }
                        }
                        // case rerun
                    }
                }

                //cleanup the class
                if (testClass.ClassCleanupMethod != null)
                {
                    try
                    {
                        testClass.ClassCleanupMethod.Invoke(null, new object[] { });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Class {0} cleanup exception : {1}", testClass.Name, e.ToString());
                    }
                }
            }

            //execute assembly cleanup
            foreach (TestClassUnit testClass in testClasses)
            {
                if (testClass.AssemblyCleanupMethod != null)
                {
                    testClass.AssemblyCleanupMethod.Invoke(null, new object[] { });
                }
            }

            Console.WriteLine("===Result Summary===");
            Console.WriteLine("=Total Run= : {0}", Test.TestCount); //Total = Pass+Fail+Skip, total not include exclude
            Console.WriteLine("=Pass= : {0}", Test.TestCount - Test.FailCount - Test.SkipCount);
            Console.WriteLine("=Fail= : {0}", Test.FailCount);
            Console.WriteLine("=Skip= : {0}", Test.SkipCount);
            Console.WriteLine("=Exclude= : {0}", ExcludedCases.Count);
            if (Test.FailedCases.Count > 0)
            {
                Console.WriteLine("===Failed Cases===");
                foreach (var c in Test.FailedCases)
                {
                    Console.WriteLine("FailedCaseName: {0}", c);
                }
                Console.WriteLine("==================");
            }
            if (Test.SkippedCases.Count > 0)
            {
                Console.WriteLine("===Skipped Cases==="); //Skipped cases means the cases expected to run but not executed since Test class or test case init fail.
                foreach (var c in Test.SkippedCases)
                {
                    Console.WriteLine("SkippedCaseName: {0}", c);
                }
                Console.WriteLine("==================");
            }
            if (ExcludedCases.Count > 0)
            {
                Console.WriteLine("===Excluded Cases==="); //Exclude cases means the cases are expected to be excluded with -extag flag. Exclude cases not count in Total.
                foreach (var c in ExcludedCases)
                {
                    Console.WriteLine("ExcludedCaseName: {0}", c);
                }
                Console.WriteLine("==================");
            }

            // Write failed and skipped cases into answerfile, will overwrite the previous file
            if (Test.FailedCases.Count + Test.SkippedCases.Count > 0)
            { 
                List<string> failedOrSkippedCases = new List<string>();
                failedOrSkippedCases.AddRange(Test.FailedCases);
                failedOrSkippedCases.AddRange(Test.SkippedCases);

                File.WriteAllLines(DefaultAnswerFileName, failedOrSkippedCases);
            }

            return Test.FailCount + Test.SkipCount;
        }

        static bool QualifyWithDupTags(IList<string> testCategories, TestMethodUnit testMethod)
        {
            bool qualify = true;

            foreach (string categories in testCategories)
            {
                List<string> categoriesList = new List<string>(categories.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                if (categoriesList.Intersect<string>(testMethod.Tag).Count<string>() == 0)
                {
                    qualify = false;
                    break;
                }
            }

            return qualify;
        }

        static bool QualifyWithWildcard(IList<string> testMethodNames, TestMethodUnit testMethod)
        {
            foreach (string name in testMethodNames)
            {
                string regex = WildcardToRegex(name);
                Regex rg = new Regex(regex);
                if (rg.Match(testMethod.Name).Success)
                {
                    return true;
                }
            }

            return false;
        }

        public static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", ".") + "$";
        }

        static void PrintHelp()
        {
            Console.WriteLine("MSTest2.exe /lib:[testlib.dll] /group:[FullTestClassName] /case:[TestMethodName] /tag:[TestCategoryNameToInclude] /extag:[TestCategoryNameToExclude] /offset:[TestCaseIndexToStartFrom] /length:[TestCaseCount] /config:[TestDataFile] /rerun:[CaseReRunCount] [/list] /timeout:[seconds] /answerfile:[AnswerFileName]");
            Console.WriteLine("MSTest2.exe -lib [testlib.dll] -group [FullTestClassName] -case [TestMethodName] -tag [TestCategoryNameToInclude] -extag [TestCategoryNameToExclude] -offset [TestCaseIndexToStartFrom] -length [TestCaseCount] -config [TestDataFile] -rerun [CaseReRunCount] [-list] -timeout seconds -answerfile [AnswerFileName]");
            Console.WriteLine("Notice: there can be multiple tag options.");
        }
    }

    public class TestThreadArgs
    {
        private Exception innerException;

        public Exception InnerException
        {
            get { return innerException; }
            set { innerException = value; }
        }

        private AutoResetEvent testExecutionDone;

        public AutoResetEvent TestExecutionDone
        {
            get { return testExecutionDone; }
            set { testExecutionDone = value; }
        }

        private MethodInfo method;

        public MethodInfo Method
        {
            get { return method; }
            set { method = value; }
        }

        private object testObject;

        public object TestObject
        {
            get { return testObject; }
            set { testObject = value; }
        }

        public void TestCase()
        {
            try
            {
                Method.Invoke(TestObject, new object[] { });
            }
            catch (Exception e)
            {
                InnerException = e.InnerException;
            }
            finally
            {
                TestExecutionDone.Set();
            }
        }
    }
}
