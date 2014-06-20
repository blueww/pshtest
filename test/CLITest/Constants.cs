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
    }
}
