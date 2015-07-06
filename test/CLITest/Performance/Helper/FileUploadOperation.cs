using Management.Storage.ScenarioTest.Util;
using Microsoft.WindowsAzure.Storage.File;
using StorageTestLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Management.Storage.ScenarioTest.Performance.Helper
{
    public class FileUploadOperation : AbstractBlobUploadOperation
    {
        public FileUploadOperation(Agent agent, CloudFileHelper fileHelper) : base(agent, null)
        {
            this.Name = "Upload-file(s)";
            this.FileHelper = fileHelper;
            this.CheckMD5 = false; //MD5 value is not available yet
            this.MaxSize = Constants.MAX_FILE_SIZE;
            this.Unit = Constants.FILE_UNIT;
        }

        public override bool Go(string containerName, string fileName)
        {
            this.Agent.UploadFile(
                fileShareName: containerName,
                       source: fileName,
                         path: fileName,
                    overwrite: this.Force);

            this.Agent.Invoke();
           
            var result = !this.Agent.HadErrors;
            this.Agent.Clear();

            return result;
        }

        public override void Before(string containerName, string fileName)
        {
            FileHelper.CreateShare(containerName);
            FileHelper.DeleteFiles(containerName, ".", fileName);

        }

        public override void BeforeBatch(string local, string remote)
        {
            FileHelper.CreateShare(remote);
            FileHelper.CleanupShare(remote);
        }

        public override bool GoBatch(string local, string remote)
        {
            var files = from f in Directory.EnumerateFiles(local)
                        select new FileInfo(f).Name;

            this.Agent.UploadFilesInFolderFromPipeline(
                fileShareName: remote,
                       folder: local);

            this.Agent.Invoke(files);

            var result = !this.Agent.HadErrors;
            this.Agent.Clear();
            return result;
        }

        public override bool ValidateBatch(string local, string remote, int fileNum, out string error)
        {
            error = string.Empty;

            //load files
            var files = new List<CloudFile>();
            FileHelper.ListFiles(
                shareName: remote,
                     path: string.Empty,
                    files: out files);


            // check file num first
            if (files.Count != fileNum)
            {
                error = string.Format("The copied file count is {0}, it should be {1}.", files.Count, fileNum);
                return false;
            }

            foreach (var file in files)
            {
                var filePath = Path.Combine(local, file.Name);
                var fi = new FileInfo(filePath);

                //check existence
                if (!fi.Exists)
                {
                    error = string.Format("The file {0} should exist", filePath);
                    return false;
                }

                //check file length (note: MD5 is not available yet)
                if (file.Properties.Length != fi.Length)
                {
                    error = string.Format("File length does not match. Local length: {0}, Remote file length: {1}. File:{2}", fi.Length, file.Properties.Length, filePath);
                    return false;
                }

                //Check MD5 (even CheckMD5 is true, still check if MD5 value is available )
                if (this.CheckMD5 && file.Properties.ContentMD5 != null)
                {
                    var remoteMd5 = file.Properties.ContentMD5;
                    var localMd5 = StorageTestLib.Helper.GetFileContentMD5(filePath);
                    if (remoteMd5 != localMd5)
                    {
                        error = string.Format("{0} MD5 not matched. Remote:{1} Local:{2}", filePath, remoteMd5, localMd5);
                        return false;
                    }
                }
            }

            return true;
        }

        public override bool Validate(string containerName, string filePath, out string error)
        {
            error = string.Empty;
           
            var localFile = Path.GetFileName(filePath);
            var file = FileHelper.QueryFile(containerName, folder: string.Empty, fileName: filePath);

            //check file length
            var localLength = new FileInfo(localFile).Length;
            var remoteLength = file.Properties.Length;
            if (localLength != remoteLength)
            {
                error = string.Format("file length is different. Local is {0}, and remote is {1}", localLength, remoteLength);
                return false;
            }

            //check MD5
            if (this.CheckMD5)
            {
                var localMd5 = FileUtil.GetFileContentMD5(filePath);
                var remoteMd5 = file.Properties.ContentMD5;

                if (localMd5 != remoteMd5)
                {
                    error = string.Format("{0} MD5 not matched. Remote:{1} Local:{2}", filePath, remoteMd5, localMd5);
                    return false;
                }
            }

            return true;
        }

        public CloudFileHelper FileHelper { get; set; }
    }
}
