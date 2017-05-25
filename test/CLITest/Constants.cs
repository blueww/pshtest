using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MS.Test.Common.MsTestLib;

namespace Management.Storage.ScenarioTest
{
    public class Constants
    {
        /// <summary>
        /// CLI Modes: ASM, ARM
        /// </summary>
        public enum Mode
        {
            asm,
            arm
        };

        /// <summary>
        /// Used for retrieve SAS token value in agent.Output
        /// </summary>
        public const string SASTokenKey = "sastoken";
        public const string SASTokenKeyNode = "sas";
        public const string SASTokenURLNode = "url";

        public struct AccountType
        {
            public const string Standard_LRS = "Standard_LRS";
            public const string Standard_ZRS = "Standard_ZRS";
            public const string Standard_GRS = "Standard_GRS";
            public const string Standard_RAGRS = "Standard_RAGRS";
            public const string Premium_LRS = "Premium_LRS";
        };

        public static readonly string[] AccountTypes = { AccountType.Standard_LRS, 
                                                           AccountType.Standard_GRS,
                                                           AccountType.Standard_RAGRS, 
                                                           AccountType.Standard_ZRS,  
                                                           AccountType.Premium_LRS };

        public struct Location
        {
            public const string WestUS = "West US";
            public const string SouthCentralUS = "South Central US";
            public const string EastUS = "East US";
            public const string EastUS2 = "East US 2";
            public const string EastUS2EUAP = "EastUS2EUAP";
            public const string EastUS2Stage = "East US 2 (Stage)";
            public const string CentralUS = "Central US";
            public const string NorthEurope = "North Europe";
            public const string WestEurope = "West Europe";
            public const string SoutheastAsia = "Southeast Asia";
            public const string EastAsia = "East Asia";
        };

        public struct MCLocation
        {
            public const string ChinaEast = "China East";
            public const string ChinaNorth = "China North";
        };

        public static readonly string[] Locations = { Location.WestUS,
                                                        Location.SouthCentralUS,
                                                        Location.EastUS,
                                                        Location.EastUS2,
                                                        Location.CentralUS,
                                                        Location.NorthEurope,
                                                        Location.WestEurope,
                                                        Location.SoutheastAsia,
                                                        Location.EastAsia };

        public static readonly string[] SRPLocations = { //Location.WestUS,
                                                        //Location.EastUS,
                                                        //Location.SoutheastAsia,
                                                        Location.EastAsia }; //File E@R only enabled on eastasia now, will switch it back when it's available on all regions
                                                        //Location.WestEurope,
                                                        //Location.NorthEurope };
                                                        

                                                        //{ Location.EastUS2Stage};
                                                        //E@R and XCool is already enabled in product, switch it back to PROD locations

        public static readonly string[] MCLocations = { MCLocation.ChinaEast,
                                                        MCLocation.ChinaNorth };

        /// <summary>
        /// used for Set/Get Service types
        /// </summary>
        public enum ServiceType
        {
            Blob,
            Queue,
            Table,
            File,
            InvalidService
        };

        /// <summary>
        /// used for Set/Get resource types
        /// </summary>
        public enum ResourceType
        {
            Account,
            Container,
            Blob,
            Queue,
            Message,
            Table,
            Entity,
            Share,
            Directory,
            File,
            InvalidResource
        };

        [Flags]
        public enum EncryptionSupportServiceEnum
        {
            None = 0,
            Blob = 1,
            File = 2
        }

        /// <summary>
        /// used for Set/Get Metrics Properties
        /// </summary>
        public enum MetricsType
        {
            Hour,
            Minute
        };

        /// <summary>
        /// Storage account Keys types: primary, secondary
        /// </summary>
        public enum AccountKeyType
        {
            Primary,
            Secondary,
            Invalid
        };

        public const int MAX_BLOCK_BLOB_SIZE = 195;   //GB
        public const int MAX_PAGE_BLOB_SIZE = 1024;   //GB
        public const int MAX_FILE_SIZE = 1024;   //GB
        public const string BLOCK_BLOB_UNIT = "G_BLOCK";
        public const string PAGE_BLOB_UNIT = "G_PAGE";
        public const string FILE_UNIT = "G_FILE";

        public readonly static string[] ResourceModulePaths;

        public readonly static string[] ServiceModulePaths;

        public const int Iterations = 5; 
        
        public const int DefaultMaxWaitingTime = 900000;  // in miliseconds, increased from 600s to 900s due to AppendBlob. It should be less than the default timeout value of mstest2, which is 3600s for now.

        static Constants()
        {
            string isPowerShellGet = Test.Data.Get("IsPowerShellGet");

            if (string.IsNullOrEmpty(isPowerShellGet) || bool.Parse(isPowerShellGet))
            {
                ResourceModulePaths = new string[]
                    {
                        "AzureRM.Profile",
                        "Azure.Storage",
                        "AzureRM.Storage"
                    };

                ServiceModulePaths = new string[]
                    {
                        "AzureRM.Profile",
                        "Azure.Storage",
                        "Azure"
                    };
            }
            else
            {
                string moduleFileFolder = Test.Data.Get("ModuleFileFolder");
                if (null == moduleFileFolder)
                {
                    moduleFileFolder = string.Empty;
                }

                ResourceModulePaths = new string[] {
                        Path.Combine(moduleFileFolder, "ResourceManager\\AzureResourceManager\\AzureRM.Profile\\AzureRM.Profile.psd1"),
                        Path.Combine(moduleFileFolder, "Storage\\Azure.Storage\\Azure.Storage.psd1"),
                        Path.Combine(moduleFileFolder, "ResourceManager\\AzureResourceManager\\AzureRM.Storage\\AzureRM.Storage.psd1") };

                ServiceModulePaths = new string[] {
                        Path.Combine(moduleFileFolder, "ResourceManager\\AzureResourceManager\\AzureRM.Profile\\AzureRM.Profile.psd1"),
                        Path.Combine(moduleFileFolder, "Storage\\Azure.Storage\\Azure.Storage.psd1"),
                        Path.Combine(moduleFileFolder, "ServiceManagement\\Azure\\Azure.psd1") };
            }
        }
    }
}
