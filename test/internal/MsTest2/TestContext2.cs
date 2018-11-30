using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Collections;
using System.Data.Common;


namespace MS.Test.Common.MsTestLib
{
    public class TestContext2 : TestContext
    {
#if DOTNET5_4
        Dictionary<string, object> properties = new Dictionary<string, object>();
        public override IDictionary<string, object> Properties { get { return properties; } }
        public override void WriteLine(string message)
        {
        }
#else
        private DataRow dataRow = null;
        public override DataRow DataRow { get { return dataRow; } }
        Dictionary<object, object> properties = new Dictionary<object, object>();
        public override IDictionary Properties { get { return properties; } }
        public override void AddResultFile(string fileName) { }
        public override void BeginTimer(string timerName) { }

        public override void EndTimer(string timerName) { }

        public override DbConnection DataConnection { get { return null; } }
#endif

        public override void WriteLine(string format, params object[] args) { }

        public string fullyQualifiedTestClassName = string.Empty;

        public override string FullyQualifiedTestClassName { get { return fullyQualifiedTestClassName; } }


        public string testName = string.Empty;
        public override string TestName { get { return testName; } }
    }
    
    
}
