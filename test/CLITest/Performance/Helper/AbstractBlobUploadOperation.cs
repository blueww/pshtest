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
    public abstract class AbstractBlobUploadOperation : ICLIOperation
    {
        public AbstractBlobUploadOperation(Agent agent, CloudBlobHelper helper)
        {
            this.Agent = agent;
            this.CheckMD5 = true;
            this.Force = true;
            this.BlobHelper = helper;
        }

        public virtual void Before(string containerName, string fileName)
        {
            BlobHelper.CreateContainer(containerName);
            BlobHelper.DeleteBlob(containerName, fileName);
        }

        public virtual void BeforeBatch(string local, string remote)
        {
            BlobHelper.CreateContainer(remote);
            BlobHelper.CleanupContainer(remote);
        }

        public CloudBlobHelper BlobHelper { get; set; }
        public string Name { get; set; }
        public bool CheckMD5 { get; set; }
        public Agent Agent { get; set; }
        public bool Force { get; set; }
        public int MaxSize { get; set; }
        public string Unit { get; set; }

        public abstract bool Go(string containerName, string fileName);
        public abstract bool GoBatch(string local, string remote);

        public virtual bool ValidateBatch(string local, string remote, int fileNum, out string error)
        {
            error = string.Empty;

            //load blobs
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
                var filePath = Path.Combine(local, blob.Name);
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

                if (localMd5 != blob.Properties.ContentMD5)
                {
                    error = string.Format("blob content md5 should be {0}, and actually it's {1}", localMd5, blob.Properties.ContentMD5);
                    return false;
                }
            }

            return true;
        }

        public virtual bool IsUploadTest
        {
            get { return true; }
        }
    }
}
