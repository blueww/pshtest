using Management.Storage.ScenarioTest.Common;
using MS.Test.Common.MsTestLib;
using StorageTestLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Management.Storage.ScenarioTest
{
    public abstract class CLIPerfBase : TestBase
    {

        public CLIPerfBase()
        {
            UploadContainerPrefix = Test.Data.Get("UploadPerfContainerPrefix");
            DownloadContainerPrefix = Test.Data.Get("DownloadPerfContainerPrefix");
            try
            {
                GenerateDataBeforeDownload = Convert.ToBoolean(Test.Data.Get("GenerateDataBeforeDownload").ToString());
            }
            catch (FormatException e)
            {
                throw new Exception(
                    string.Format("Cannot format GenerateDataBeforeDownload value ({0}) to a boolean value", 
                        Test.Data.Get("GenerateDataBeforeDownload").ToString()),
                    e);
            }
            catch (ArgumentException)
            {
                //argument is not specified, default to False
                this.GenerateDataBeforeDownload = false;
            }
        }

        public string DownloadContainerPrefix { get; set; }
        public string UploadContainerPrefix { get; set; }
        public bool GenerateDataBeforeDownload { get; set; }
    }
}
