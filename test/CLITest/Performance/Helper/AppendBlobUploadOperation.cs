using StorageTestLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Management.Storage.ScenarioTest.Performance.Helper
{
    public class AppendBlobUploadOperation : BlockBlobUploadOperation
    {

        public AppendBlobUploadOperation(Agent agent, CloudBlobHelper helper)
            : base(agent, helper)
        {
            this.Name = "Upload-append-blob(s)";
        }

        public override bool Go(string containerName, string fileName)
        {
            return this.Agent.SetAzureStorageBlobContent(
                     FileName: fileName, 
                ContainerName: containerName, 
                         Type: Microsoft.WindowsAzure.Storage.Blob.BlobType.AppendBlob, 
                        Force: this.Force);
        }

        public override bool GoBatch(string local, string remote)
        {
            var agent = this.Agent as PowerShellAgent;
            if (agent != null)
            {
                agent.AddPipelineScript(string.Format("ls -File -Path {0}", local));
                return agent.SetAzureStorageBlobContent(string.Empty, remote, Microsoft.WindowsAzure.Storage.Blob.BlobType.AppendBlob);
            }

            return false;
        }
    }
}
