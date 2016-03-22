﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public static readonly string[] SRPLocations = { Location.EastUS2Stage};
                                                        //Will add them back when E@R and XCool is enabled in product
                                                        //{ Location.WestUS,
                                                        //Location.EastUS,
                                                        //Location.SoutheastAsia,
                                                        //Location.EastAsia,
                                                        //Location.WestEurope,
                                                        //Location.NorthEurope };

        public static readonly string[] MCLocations = { MCLocation.ChinaEast,
                                                        MCLocation.ChinaNorth };

        /// <summary>
        /// used for Set/Get Service Properties
        /// </summary>
        public enum ServiceType
        {
            Blob,
            Queue,
            Table,
            File,
            InvalidService
        };

        public enum EncryptionSupportServiceEnum
        {
            Blob = 1
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

        public readonly static string[] ResourceModulePaths = {"ResourceManager\\AzureResourceManager\\AzureRM.Profile\\AzureRM.Profile.psd1",
                                                    "ResourceManager\\AzureResourceManager\\Azure.Storage\\Azure.Storage.psd1",
                                                    "ResourceManager\\AzureResourceManager\\AzureRM.Storage\\AzureRM.Storage.psd1"};

        public const string ServiceModulePath = "ServiceManagement\\Azure\\Azure.psd1";

        public const int Iterations = 5; 
        
        public const int DefaultMaxWaitingTime = 900000;  // in miliseconds, increased from 600s to 900s due to AppendBlob. It should be less than the default timeout value of mstest2, which is 3600s for now.
    }
}
