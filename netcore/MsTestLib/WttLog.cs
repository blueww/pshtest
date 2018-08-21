using System;
using System.Collections.Generic;
using System.Text;

namespace MS.Test.Common.MsTestLib
{
    enum Level
    {
        Info,
        Warn,
        Error
    };

    class WttLog : IDisposable
    {
        public WttLog(string initString)
        {
        }

        public void StartTest(string testId)
        {
        }

        public void EndTest(string testId)
        {
        }

        public void Write(Level logLevel, string logInfo)
        {
        }

        public void Dispose()
        {
        }
    }
}
