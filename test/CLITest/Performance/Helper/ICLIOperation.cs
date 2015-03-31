using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Management.Storage.ScenarioTest.Performance.Helper
{
    /// <summary>
    /// This interface is to abstract all operations in different performance tests, e.g. download one blob, upload one file.
    /// It contains two sets of functions: 
    ///     one for OneBlob/OneFile test: before/Go/Validate, 
    ///     the other for multiple file test: beforeBatch/GoBatch/ValidateBatch
    /// </summary>
    public interface ICLIOperation
    {
        #region OneBlob tests
        /// <summary>
        /// Do stuff before each iteration
        /// </summary>
        void Before(string containerName, string fileName);

        /// <summary>
        /// Do the real job
        /// </summary>
        /// <returns>execution result: true: completed w/o errors, false: w/ errors</returns>
        bool Go(string containerName, string fileName);

        /// <summary>
        /// Validate the result
        /// </summary>
        /// <returns>validation result: true: pass, false: failed</returns>
        bool Validate(string containerName, string fileName, out string error);
        #endregion

        #region 64Mbig / 2G_N tests
        /// <summary>
        /// Do stuff before each iteration for 64M/2G_N tests
        /// </summary>
        /// <param name="local">local folder</param>
        /// <param name="remote">remote target: can be container name or file share</param>
        void BeforeBatch(string local, string remote);

        /// <summary>
        /// Do the real job
        /// </summary>
        /// <param name="local">local folder</param>
        /// <param name="remote">remote target: can be container name or file share</param>
        bool GoBatch(string local, string remote);

        /// <summary>
        /// Validate the result
        /// </summary>
        /// <param name="local">local folder</param>
        /// <param name="remote">remote target: can be container name or file share</param>
        /// <param name="fileNum">file number</param>
        /// <param name="error">error message</param>
        bool ValidateBatch(string local, string remote, int fileNum, out string error);
        #endregion

        TimeSpan GetReportedTransferringTime();

        /// <summary>
        /// This operation name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Switch to check MD5 value
        /// </summary>
        bool CheckMD5 { get; set; }

        /// <summary>
        /// Agent to do the job. Can be PowerShellAgent or NodeJSAgent
        /// </summary>
        Agent Agent { get; set; }

        /// <summary>
        /// Switch to overwrite remote/local file in job
        /// </summary>
        bool Force { get; set; }

        /// <summary>
        /// Supported max file size
        /// </summary>
        int MaxSize { get; set; }

        /// <summary>
        /// Unit of supported max file size. Defined in Constants.*_UNIT
        /// </summary>
        string Unit { get; set; }

        /// <summary>
        /// Switch to specify if data preparation is done before the tests
        /// </summary>
        bool NeedDataPreparation { get; }
    }
}
