using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Management.Storage.ScenarioTest
{
    public class Constants
    {
        /// <summary>
        /// Used for retrieve SAS token value in agent.Output
        /// </summary>
        public const string SASTokenKey = "";
        public const string SASTokenKeyNode = "sas";

        /// <summary>
        /// used for Set/Get Service Properties
        /// </summary>
        public enum ServiceType
        {
            Blob,
            Queue,
            Table
        };

        /// <summary>
        /// used for Set/Get Metrics Properties
        /// </summary>
        public enum MetricsType
        {
            Hour,
            Minute
        };

        public const int MAX_BLOCK_BLOB_SIZE = 195;   //GB
        public const int MAX_PAGE_BLOB_SIZE = 1024;   //GB
        public const int MAX_FILE_SIZE = 1024;   //GB
        public const string BLOCK_BLOB_UNIT = "G_BLOCK";
        public const string PAGE_BLOB_UNIT = "G_PAGE";
        public const string FILE_UNIT = "G_FILE";

        public const int Iterations = 5;
    }
}
