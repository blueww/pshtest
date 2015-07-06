using Management.Storage.ScenarioTest.Util;
using Microsoft.WindowsAzure.Storage.Blob;
using StorageTestLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Management.Storage.ScenarioTest.Performance.Helper
{
    public abstract class AbstractBlobDownloadOperation : ICLIOperation
    {
        public AbstractBlobDownloadOperation(Agent agent, CloudBlobHelper helper)
        {
            this.Agent = agent;
            this.BlobHelper = helper;
            this.CheckMD5 = true;
            this.Force = true;
        }

        public virtual void Before(string containerName, string fileName)
        {
            //do nothing
        }

        public virtual void BeforeBatch(string local, string remote)
        {
            StorageTestLib.Helper.CreateNewFolder(local);
        }

        public virtual bool Go(string containerName, string fileName)
        {
            return this.Agent.GetAzureStorageBlobContent(
                         Blob: fileName,
                     FileName: fileName,
                ContainerName: containerName,
                        Force: true);
        }

        public virtual bool GoBatch(string local, string remote)
        {
            var agent = this.Agent as PowerShellAgent;
            if (agent != null)
            {
                agent.AddPipelineScript(string.Format("Get-AzureStorageContainer {0}", remote));
                agent.AddPipelineScript("Get-AzureStorageBlob");
                return agent.GetAzureStorageBlobContent(
                                     Blob: string.Empty, 
                              Destination: local, 
                            ContainerName: string.Empty, 
                                    Force: this.Force);
            }

            return false;
        }

        public CloudBlobHelper BlobHelper { get; set; }
        public string Name { get; set; }
        public bool CheckMD5 { get; set; }
        public Agent Agent { get; set; }
        public bool Force { get; set; }
        public int MaxSize { get; set; }
        public string Unit { get; set; }

        public virtual bool ValidateBatch(string local, string remote, int fileNum, out string error)
        {
            error = string.Empty;

            //check blob
            var folderName = local;
            List<CloudBlob> bloblist;
            BlobHelper.ListBlobs(remote, out bloblist);

            // check file num first
            if (bloblist.Count != fileNum)
            {
                error = string.Format("The copied file count is {0}, it should be {1}.", bloblist.Count, fileNum);
                return false;
            }

            foreach (var blob in bloblist)
            {
                var filePath = folderName + "\\" + blob.Name;
                if (File.Exists(filePath))
                {
                    blob.FetchAttributes();
                    var remoteMd5 = blob.Properties.ContentMD5;
                    var localMd5 = StorageTestLib.Helper.GetFileContentMD5(filePath);
                    if (remoteMd5 != localMd5)
                    {
                        error = string.Format("{0} MD5 not matched. Remote:{1} Local:{2}", filePath, remoteMd5, localMd5);
                        return false;
                    }
                }
                else
                {
                    error = string.Format("The file {0} should exist", filePath);
                    return false;
                }
            }

            return true;
        }

        public virtual bool Validate(string containerName, string filePath, out string error)
        {
            Console.WriteLine("ContainerName:{0}, filePath:{1}", containerName, filePath);
            error = string.Empty;

            var blobName = Path.GetFileName(filePath);
            var blob = BlobHelper.QueryBlob(containerName, blobName);

            //check blob length
            var localLength = new FileInfo(blobName).Length;
            var remoteLength = blob.Properties.Length;
            if (localLength != remoteLength)
            {
                error = string.Format("blob length is different. Local is {0}, and remote is {1}", localLength, remoteLength);
                return false;
            }

            //check MD5
            if (this.CheckMD5)
            {
                var localMd5 = FileUtil.GetFileContentMD5(filePath);
                blob.FetchAttributes();

                if (localMd5 != blob.Properties.ContentMD5)
                {
                    error = string.Format("blob content md5 should be {1}, and actually it's {0}", localMd5, blob.Properties.ContentMD5);
                    return false;
                }
            }

            return true;
        }


        public virtual bool IsUploadTest
        {
            get { return false; }
        }
    }
}
