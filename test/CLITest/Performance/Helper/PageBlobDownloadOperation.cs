using StorageTestLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Management.Storage.ScenarioTest.Performance.Helper
{
    public class PageBlobDownloadOperation : AbstractBlobDownloadOperation
    {
        public PageBlobDownloadOperation(Agent agent, CloudBlobHelper helper)
            : base(agent, helper)
        {
            this.Name = "Download-page-blob(s)";
            this.MaxSize = Constants.MAX_PAGE_BLOB_SIZE;
            this.Unit = Constants.PAGE_BLOB_UNIT;
        }
    }
}
