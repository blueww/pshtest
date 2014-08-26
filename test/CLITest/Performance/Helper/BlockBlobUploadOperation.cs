using Microsoft.WindowsAzure.Storage.Blob;
using StorageTestLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Management.Storage.ScenarioTest.Performance.Helper
{
    public class BlockBlobUploadOperation : AbstractBlobUploadOperation
    {
        public BlockBlobUploadOperation(Agent agent, CloudBlobHelper helper)
            : base(agent, helper)
        {
            this.Name = "Upload-block-blob(s)";
            this.MaxSize = Constants.MAX_BLOCK_BLOB_SIZE;
            this.Unit = Constants.BLOCK_BLOB_UNIT;
        }

        public override bool Go(string containerName, string fileName)
        {
            return this.Agent.SetAzureStorageBlobContent(
                     FileName: fileName, 
                ContainerName: containerName, 
                         Type: Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob, 
                        Force: this.Force);
        }

        public override bool GoBatch(string local, string remote)
        {
            var agent = this.Agent as PowerShellAgent;
            if (agent != null)
            {
                agent.AddPipelineScript(string.Format("ls -File -Path {0}", local));
                return agent.SetAzureStorageBlobContent(string.Empty, remote, Microsoft.WindowsAzure.Storage.Blob.BlobType.BlockBlob);
            }

            return false;
        }
    }
}
