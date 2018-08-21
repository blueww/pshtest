using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MS.Test.Common.MsTestLib
{
    public interface ILogger
    {
        void WriteError(
            string msg,
            params object[] objToLog);

        void WriteWarning(
            string msg,
            params object[] objToLog);

        void WriteInfo(
            string msg,
            params object[] objToLog);

        void WriteVerbose(
            string msg,
            params object[] objToLog);

        void StartTest(
            string testId);

        void EndTest(
            string testId,
            TimeSpan executionTime,
            TestResult result);

        object GetLogger();

        void Close();

    }

    public enum TestResult
    {
        PASS,
        FAIL,
        SKIP
    }


}
