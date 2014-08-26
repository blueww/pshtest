using StorageTestLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Management.Storage.ScenarioTest.Performance.Helper
{
    public class BlockBlobDownloadOperation : AbstractBlobDownloadOperation
    {
        public BlockBlobDownloadOperation(Agent agent, CloudBlobHelper blobHelper)
            : base(agent, blobHelper)
        {
            this.Name = "Download-block-blob(s)";
            this.MaxSize = Constants.MAX_BLOCK_BLOB_SIZE;
            this.Unit = Constants.BLOCK_BLOB_UNIT;
        }
    }
}
