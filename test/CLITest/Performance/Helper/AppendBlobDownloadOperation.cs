using StorageTestLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Management.Storage.ScenarioTest.Performance.Helper
{
    public class AppendBlobDownloadOperation : BlockBlobDownloadOperation
    {
        public AppendBlobDownloadOperation(Agent agent, CloudBlobHelper blobHelper)
            : base(agent, blobHelper)
        {
            this.Name = "Download-append-blob(s)";
        }
    }
}
