﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Security;
using System.Text;
using Management.Storage.ScenarioTest.Util;
using Microsoft.Azure.Management.Storage.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Commands.Storage.Model.ResourceModel;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Microsoft.WindowsAzure.Storage.Table;
using MS.Test.Common.MsTestLib;
using Microsoft.Azure.Commands.Management.Storage.Models;
using System.IO;

namespace Management.Storage.ScenarioTest
{
    class PowerShellAgent : Agent
    {
        public const string BaseObject = "_baseObject";
        private static bool snapInAdded = false;
        private static string ContextParameterName = "Context";
        private static object AgentContext;
        private static string CmdletLogFormat = "{0} : {1}";
        protected bool _UseContextParam = true;  // decide whether to specify the Context parameter

        private static Hashtable ExpectedErrorMsgTablePS = new Hashtable() {
                {"GetBlobContentWithNotExistsBlob", "Can not find blob '{0}' in container '{1}', or the blob type is unsupported."},
                {"GetBlobContentWithNotExistsContainer", "Can not find blob '{0}' in container '{1}', or the blob type is unsupported."},
                {"SetBlobContentWithInvalidBlobType", "The blob type is invalid for this operation."},
                {"GetNonExistingBlob", "Can not find blob '{0}' in container '{1}', or the blob type is unsupported."},
                {"RemoveBlobWithLease", "The remote server returned an error: (412)"},
                {"SetPageBlobWithInvalidFileSize", "File size {0} bytes is invalid for PageBlob, must be a multiple of 512 bytes"},
                {"CreateExistingContainer", "Container '{0}' already exists."},
                {"CreateInvalidContainer", "Container name '{0}' is invalid."},
                {"RemoveNonExistingContainer", "Can not find the container '{0}'."},
                {"RemoveNonExistingBlob", "Can not find blob '{0}' in container '{1}', or the blob type is unsupported."},
                {"SetBlobContentWithInvalidBlobName", "Blob name '{0}' is invalid."},
                {"SetContainerAclWithInvalidName", "Container name '{0}' is invalid."},
                {"CreateExistingTable", "Table '{0}' already exists."},
                {"CreateInvalidTable", "Table name '{0}' is invalid."},
                {"GetNonExistingTable", "Can not find table '{0}'."},
                {"RemoveNonExistingTable", "Can not find table '{0}'."},
                {"CreateExistingQueue", "Queue '{0}' already exists."},
                {"CreateInvalidQueue", "Queue name '{0}' is invalid."},
                {"GetNonExistingQueue", "Can not find queue '{0}'."},
                {"RemoveNonExistingQueue", "Can not find queue '{0}'."},
        };

        internal delegate void ParseCollectionFunc(Collection<PSObject> Values);

        public static new object Context
        {
            get
            {
                return AgentContext;
            }
        }

        public bool UseContextParam
        {
            set { _UseContextParam = value; }
            get { return _UseContextParam; }
        }

        // a common parameter for multi-task
        public static int? ConcurrentTaskCount { get; set; }

        // add this member for importing module
        private static InitialSessionState _InitState = InitialSessionState.CreateDefault();

        private PowerShell GetPowerShellInstance()
        {
            this.Clear();
            return this.shell;
        }

        public static void RemoveModule(string moduleName)
        {
            PowerShell ps = PowerShell.Create(_InitState);
            //TODO add tests for positional parameter
            ps.AddCommand("Remove-Module");
            ps.BindParameter("Name", moduleName);
            ps.Invoke();

            if (ps.Streams.Error.Count > 0)
            {
                Test.Error("Failed to remove module: {0} due to error {1}", moduleName, ps.Streams.Error[0].Exception.Message);
                return;
            }
        }

        public static void ImportModules(string[] ModuleFilePaths)
        {
            foreach (var moduleFilePath in ModuleFilePaths)
            {
                ImportModule(moduleFilePath);
            }
            PrintModule();
        }

        public static void ImportModule(string ModuleFilePath)
        {
            if (string.IsNullOrEmpty(ModuleFilePath))
            {
                Test.Info("Skip importing powershell module");
                return;
            }

            Test.Info("Import-Module {0}", ModuleFilePath);
            _InitState.ImportPSModule(new string[] { ModuleFilePath });
        }

        public static void InstallAzureModule()
        {
            PowerShell ps = PowerShell.Create(_InitState);
            //TODO add tests for positional parameter
            ps.AddCommand("Install-Module");
            ps.BindParameter("Name", "Azure");
            ps.Invoke();

            if (ps.Streams.Error.Count > 0)
            {
                Test.Error("Failed to install module: {0} due to error {1}", "Azure", ps.Streams.Error[0].Exception.Message);
                return;
            }
        }

        /// <summary>
        /// Import SnapIns.
        /// </summary>
        /// <param name="snapInName">Indicating the name of the SpanIn.</param>
        /// <remarks>
        /// This is used for xSMB PSH support since it comes with a standalone
        /// package as a Snap-in.
        /// </remarks>
        public static void AddSnapIn(string snapInName)
        {
            if (!snapInAdded)
            {
                Test.Info("Add-PSSnapIN {0}", snapInName);

                PSSnapInException ex;
                _InitState.ImportPSSnapIn(snapInName, out ex);
                if (ex != null)
                {
                    Test.Warn(ex.ToString());
                }

                snapInAdded = true;
            }
        }

        /// <summary>
        /// Import azure subscription
        /// </summary>
        /// <param name="filePath">Azure subscription file path</param>
        public static void ImportAzureSubscriptionAndSetStorageAccount(string filePath, string subscriptionName, string storageAccountName)
        {
            PowerShell ps = PowerShell.Create(_InitState);
            //TODO add tests for positional parameter
            ps.AddCommand("Import-AzurePublishSettingsFile");
            ps.BindParameter("PublishSettingsFile", filePath);
            ps.Invoke();

            if (ps.Streams.Error.Count > 0)
            {
                Test.Error("Can't set current storage account to {0} in subscription {1}. Exception: {2}", storageAccountName, subscriptionName, ps.Streams.Error[0].Exception.Message);
                return;
            }

            ps = PowerShell.Create(_InitState);
            ps.AddCommand("Set-AzureSubscription");
            ps.BindParameter("SubscriptionName", subscriptionName);
            ps.BindParameter("CurrentStorageAccount", storageAccountName);
            Test.Info("set current storage account in subscription, Cmdline: {0}", GetCommandLine(ps));
            ps.Invoke();

            if (ps.Streams.Error.Count > 0)
            {
                Test.Error("Can't set current storage account to {0} in subscription {1}. Exception: {2}", storageAccountName, subscriptionName, ps.Streams.Error[0].Exception.Message);
            }
        }

        /// <summary>
        /// Set the Reource Mode Current Storage Account
        /// </summary>
        /// <param name="storageAccountName"></param>
        /// <param name="resourceGroupName"></param>
        public override bool SetRmCurrentStorageAccount(string storageAccountName, string resourceGroupName)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Set-AzureRmCurrentStorageAccount");
            ps.BindParameter("Name", storageAccountName);
            ps.BindParameter("ResourceGroupName", resourceGroupName);

            return InvokePowerShellWithoutContext(ps);
        }

        public static string AddRandomAzureEnvironment(string endpoint, string prefix = "")
        {
            string envName = Utility.GenNameString(prefix);
            PowerShell ps = PowerShell.Create(_InitState);
            ps.AddCommand("Add-AzureEnvironment");
            ps.BindParameter("Name", envName);
            ps.BindParameter("PublishSettingsFileUrl", Utility.GenNameString("PublishSettingsFileUrl"));
            ps.BindParameter("ServiceEndpoint", Utility.GenNameString("ServiceEndpoint"));
            ps.BindParameter("ManagementPortalUrl", Utility.GenNameString("ManagementPortalUrl"));
            ps.BindParameter("StorageEndpoint", endpoint);
            Test.Info("Add Azure Environment, Cmdline: {0}", GetCommandLine(ps));
            ps.Invoke();

            if (ps.Streams.Error.Count > 0)
            {
                Test.Error("Can't add azure envrionment. Exception: {0}", ps.Streams.Error[0].Exception.Message);
            }
            return envName;
        }

        /// <summary>
        /// Remove the current azure subscription
        /// </summary>
        public static void RemoveAzureSubscriptionIfExists()
        {
            PowerShell ps = PowerShell.Create(_InitState);
            ps.AddScript("Get-AzureSubscription | Remove-AzureSubscription -Force");
            ps.Invoke();
        }

        public static void SetStorageContext(string StorageAccountName, string StorageAccountKey,
            bool useHttps = true, string endPoint = "")
        {
            PowerShell ps = PowerShell.Create(_InitState);
            ps.AddCommand("New-AzureStorageContext");
            ps.BindParameter("StorageAccountName", StorageAccountName);
            ps.BindParameter("StorageAccountKey", StorageAccountKey);
            ps.BindParameter("EndPoint", endPoint.Trim());

            if (useHttps)
            {
                //TODO need tests to check whether it's ignore cases.
                ps.BindParameter("Protocol", "https");
            }
            else
            {
                ps.BindParameter("Protocol", "http");
            }

            Test.Info("Set PowerShell Storage Context using name and key, Cmdline: {0}", GetCommandLine(ps));
            SetStorageContext(ps);
        }

        public static void SetStorageContext(string ConnectionString)
        {
            PowerShell ps = PowerShell.Create(_InitState);
            ps.AddCommand("New-AzureStorageContext");
            ps.BindParameter("ConnectionString", ConnectionString);

            Test.Info("Set PowerShell Storage Context using connection string, Cmdline: {0}", GetCommandLine(ps));
            SetStorageContext(ps);
        }

        public static void PrintModule()
        {
            PowerShell ps = PowerShell.Create(_InitState);
            ps.AddCommand("Get-Module");

            var result = ps.Invoke();
            foreach (PSObject po in result)
            {
                Test.Info(((System.Management.Automation.PSModuleInfo) po.BaseObject).ModuleBase);
            }
        }

public static void SetLocalStorageContext()
        {
            PowerShell ps = PowerShell.Create(_InitState);
            ps.AddCommand("New-AzureStorageContext");
            ps.BindParameter("Local");

            Test.Info("Set PowerShell Storage Context using local development storage account, Cmdline: {0}", GetCommandLine(ps));
            SetStorageContext(ps);
        }

        public static void SetAnonymousStorageContext(string StorageAccountName, bool useHttps, string endPoint = "")
        {
            PowerShell ps = PowerShell.Create(_InitState);
            ps.AddCommand("New-AzureStorageContext");
            ps.BindParameter("StorageAccountName", StorageAccountName);
            ps.BindParameter("Anonymous");
            ps.BindParameter("EndPoint", endPoint.Trim());

            if (useHttps)
            {
                //TODO need tests to check whether it's ignore cases.
                ps.BindParameter("Protocol", "https");
            }
            else
            {
                ps.BindParameter("Protocol", "http");
            }

            Test.Info("Set PowerShell Storage Context using Anonymous storage account, Cmdline: {0}", GetCommandLine(ps));
            SetStorageContext(ps);
        }

        /// <summary>
        /// Create a stroage context with Oauth, Must log in with Login-AzureRMAccount  before run this
        /// </summary>
        /// <param name="StorageAccountName"></param>
        /// <param name="useHttps"></param>
        /// <param name="endPoint"></param>
        public static void SetOAuthStorageContext(string StorageAccountName, bool useHttps, string endPoint = "")
        {
            PowerShell ps = PowerShell.Create(_InitState);
            ps.AddCommand("New-AzureStorageContext");
            ps.BindParameter("StorageAccountName", StorageAccountName);
            ps.AddParameter("UseConnectedAccount");
            ps.BindParameter("EndPoint", endPoint.Trim());

            if (useHttps)
            {
                //TODO need tests to check whether it's ignore cases.
                ps.BindParameter("Protocol", "https");
            }
            else
            {
                ps.BindParameter("Protocol", "http");
            }

            Test.Info("Set PowerShell Storage Context using OAuth storage account, Cmdline: {0}", GetCommandLine(ps));
            SetStorageContext(ps);
        }

        public override void SetStorageContextWithSASTokenInConnectionString(CloudStorageAccount StorageAccount, string sasToken)
        {
            throw new NotImplementedException();
        }

        public override void SetStorageContextWithSASToken(string StorageAccountName, string sasToken, bool useHttps = true)
        {
            this.SetStorageContextWithSASToken(StorageAccountName, sasToken, null, useHttps);
        }

        public override void SetStorageContextWithSASToken(string StorageAccountName, string sasToken, string endpoint, bool useHttps = true)
        {
            PowerShell ps = this.GetPowerShellInstance();
            ps.AddCommand("New-AzureStorageContext");
            ps.BindParameter("StorageAccountName", StorageAccountName);
            ps.BindParameter("SasToken", sasToken);

            if (useHttps)
            {
                ps.BindParameter("Protocol", "https");
            }
            else
            {
                ps.BindParameter("Protocol", "http");
            }

            if (!string.IsNullOrEmpty(endpoint))
            {
                ps.BindParameter("Endpoint", endpoint);
            }

            Test.Info("Set PowerShell Storage Context using SasToken, Cmdline: {0}", GetCommandLine(ps));
            SetStorageContext(ps);
        }

        internal static void SetStorageContext(PowerShell ps)
        {
            AgentContext = null;

            foreach (PSObject result in ps.Invoke())
            {
                foreach (PSMemberInfo member in result.Members)
                {
                    if (member.Name.Equals("Context"))
                    {
                        AgentContext = member.Value;
                        Agent.Context = AgentContext;
                        
                        return;
                    }
                }
            }

            // if we cannot find the Context field, we will throw an exception here
            throw new Exception("StorageContext not found!");
        }

        public static void SetStorageContextWithAzureEnvironment(string StorageAccountName, string StorageAccountKey,
            bool useHttps = true, string azureEnvironmentName = "")
        {
            PowerShell ps = PowerShell.Create(_InitState);
            ps.AddCommand("New-AzureStorageContext");
            ps.BindParameter("StorageAccountName", StorageAccountName);
            ps.BindParameter("StorageAccountKey", StorageAccountKey);
            ps.BindParameter("Environment", azureEnvironmentName.Trim());

            if (useHttps)
            {
                ps.BindParameter("Protocol", "https");
            }
            else
            {
                ps.BindParameter("Protocol", "http");
            }

            Test.Info("Set PowerShell Storage Context using name, key and azureEnvironment, Cmdline: {0}", GetCommandLine(ps));
            SetStorageContext(ps);
        }

        /// <summary>
        /// Clean storage context
        /// </summary>
        public static void CleanStorageContext()
        {
            AgentContext = null;
        }

        internal static object GetStorageContext(Collection<PSObject> objects)
        {
            foreach (PSObject result in objects)
            {
                foreach (PSMemberInfo member in result.Members)
                {
                    if (member.Name.Equals("Context"))
                    {
                        return member.Value;
                    }
                }
            }
            return null;
        }

        public override object GetStorageContextWithSASToken(CloudStorageAccount account, string sasToken, string endpoint = null, bool useHttps = false)
        {
            string accountName = account.Credentials.AccountName;
            PowerShell ps = this.GetPowerShellInstance();
            ps.AddCommand("New-AzureStorageContext");
            ps.BindParameter("StorageAccountName", accountName);
            ps.BindParameter("SasToken", sasToken);

            if (useHttps)
            {
                ps.BindParameter("Protocol", "https");
            }
            else
            {
                ps.BindParameter("Protocol", "http");
            }

            if (!string.IsNullOrEmpty(endpoint))
            {
                ps.BindParameter("Endpoint", endpoint);
            }

            Test.Info("{0} Test...\n{1}", MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            return GetStorageContext(ps.Invoke());
        }

        internal static object GetStorageContext(string ConnectionString)
        {
            PowerShell ps = PowerShell.Create(_InitState);
            ps.AddCommand("New-AzureStorageContext");
            ps.BindParameter("ConnectionString", ConnectionString);

            Test.Info("{0} Test...\n{1}", MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            return GetStorageContext(ps.Invoke());
        }

        internal static object GetStorageContext(string StorageAccountName, string StorageAccountKey)
        {
            PowerShell ps = PowerShell.Create(_InitState);
            ps.AddCommand("New-AzureStorageContext");
            ps.BindParameter("StorageAccountName", StorageAccountName);
            ps.BindParameter("StorageAccountKey", StorageAccountKey);

            Test.Info("{0} Test...\n{1}", MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            return GetStorageContext(ps.Invoke());
        }

        public override void ImportAzureSubscription(string settingPath)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Import-AzurePublishSettingsFile");
            ps.BindParameter("PublishSettingsFile", settingPath);

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            ParseContainerCollection(ps.Invoke());
            ParseErrorMessages(ps);

            if (ps.HadErrors)
            {
                throw new InvalidOperationException("Failed to import azure subscription.");
            }
        }

        public override void SetActiveSubscription(string subscriptionId)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Select-AzureSubscription");
            ps.BindParameter("SubscriptionId", subscriptionId);

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            ParseContainerCollection(ps.Invoke());
            ParseErrorMessages(ps);

            if (ps.HadErrors)
            {
                throw new InvalidOperationException("Failed to import azure subscription.");
            }
        }

        public bool NewAzureStorageContext(string StorageAccountName, string StorageAccountKey, string endPoint = "")
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("New-AzureStorageContext");
            ps.BindParameter("StorageAccountName", StorageAccountName);
            ps.BindParameter("StorageAccountKey", StorageAccountKey);

            if (string.IsNullOrEmpty(StorageAccountKey))
            {
                ps.BindParameter("Anonymous", true);
            }

            ps.BindParameter("EndPoint", endPoint);

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            return NewAzureStorageContext(ps);
        }

        public bool NewAzureStorageContext(string ConnectionString)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("New-AzureStorageContext");
            ps.BindParameter("ConnectionString", ConnectionString);

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            return NewAzureStorageContext(ps);
        }

        internal bool NewAzureStorageContext(PowerShell ps)
        {
            ParseContainerCollection(ps.Invoke());
            ParseErrorMessages(ps);

            return !ps.HadErrors;
        }


        public override bool NewAzureStorageContainer(string ContainerName)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("New-AzureStorageContainer");

            ps.BindParameter("Name", ContainerName);

            return InvokeStoragePowerShell(ps, null, ParseContainerCollection);
        }

        public override bool NewAzureStorageContainer(string[] ContainerNames)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddScript(FormatNameList(ContainerNames));
            ps.AddCommand("New-AzureStorageContainer");

            return InvokeStoragePowerShell(ps, null, ParseContainerCollection);
        }

        public override bool NewFileShares(string[] names)
        {
            var ps = GetPowerShellInstance();
            ps.Runspace.SessionStateProxy.SetVariable("context", PowerShellAgent.Context);
            ps.AddCommand("Foreach-Object");
            ps.AddParameter("Process", ScriptBlock.Create("New-AzureStorageShare -Name $_ -Context $context"));

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            ParseContainerCollection(ps.Invoke(names));
            ParseErrorMessages(ps);

            return !ps.HadErrors;
        }

        public override bool GetFileSharesByPrefix(string prefix)
        {
            var ps = GetPowerShellInstance();
            ps.AddCommand("Get-AzureStorageShare");
            ps.AddParameter("Prefix", prefix);
            ps.AddParameter("Context", PowerShellAgent.Context);

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            ParseContainerCollection(ps.Invoke());
            ParseErrorMessages(ps);

            return !ps.HadErrors;
        }

        public override bool RemoveFileShares(string[] names)
        {
            var ps = GetPowerShellInstance();
            ps.Runspace.SessionStateProxy.SetVariable("context", PowerShellAgent.Context);
            ps.AddCommand("Foreach-Object");
            ps.AddParameter("Process", ScriptBlock.Create("Remove-AzureStorageShare -Name $_ -Context $context -Confirm:$false"));

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            ParseErrorMessages(ps);

            return !ps.HadErrors;
        }

        public override bool NewDirectories(string fileShareName, string[] directoryNames)
        {
            var ps = GetPowerShellInstance();
            ps.Runspace.SessionStateProxy.SetVariable("context", PowerShellAgent.Context);
            ps.Runspace.SessionStateProxy.SetVariable("shareName", fileShareName);
            ps.AddCommand("Foreach-Object");
            ps.AddParameter("Process", ScriptBlock.Create("New-AzureStorageDirectory -ShareName $shareName -Path $_ -Context $context"));

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            ParseCollection(ps.Invoke(directoryNames));
            ParseErrorMessages(ps);

            return !ps.HadErrors;
        }

        public override bool ListDirectories(string fileShareName)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Get-AzureStorageFile");
            ps.BindParameter("ShareName", fileShareName);
            ps.BindParameter("Context", PowerShellAgent.Context);

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            ParseCollection(ps.Invoke());
            ParseErrorMessages(ps);

            return !ps.HadErrors;
        }

        public override bool RemoveDirectories(string fileShareName, string[] directoryNames)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.Runspace.SessionStateProxy.SetVariable("context", PowerShellAgent.Context);
            ps.Runspace.SessionStateProxy.SetVariable("shareName", fileShareName);
            ps.AddCommand("Foreach-Object");
            ps.AddParameter("Process", ScriptBlock.Create("Remove-AzureStorageDirectory -ShareName $shareName -Path $_ -Context $context -Confirm:$false"));

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            ParseCollection(ps.Invoke(directoryNames));
            ParseErrorMessages(ps);

            return !ps.HadErrors;
        }

        public override bool GetAzureStorageContainer(string ContainerName)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            if (Utility.GetRandomBool())
            {
                ps.AddCommand("Get-AzureStorageContainer");
            }
            else
            {
                ps.AddCommand("Get-AzureStorageContainerAcl");
            }

            ps.BindParameter("Name", ContainerName);

            return InvokeStoragePowerShell(ps, null, ParseContainerCollection);
        }

        public override bool GetAzureStorageContainerByPrefix(string Prefix)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Get-AzureStorageContainer");
            ps.BindParameter("Prefix", Prefix);

            return InvokeStoragePowerShell(ps, null, ParseContainerCollection);
        }

        public override bool SetAzureStorageContainerACL(string ContainerName, BlobContainerPublicAccessType PublicAccess, string leaseId = null, bool PassThru = true)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Set-AzureStorageContainerACL");
            ps.BindParameter("Name", ContainerName);
            ps.BindParameter("PublicAccess", PublicAccess);
            ps.BindParameter("PassThru", PassThru);

            return InvokeStoragePowerShell(ps, null, ParseContainerCollection);
        }

        public override bool SetAzureStorageContainerACL(string[] ContainerNames, BlobContainerPublicAccessType PublicAccess, bool PassThru = true)
        {
            PowerShell ps = this.GetPowerShellInstance();
            ps.AddScript(FormatNameList(ContainerNames));
            ps.AddCommand("Set-AzureStorageContainerACL");
            ps.AddParameter("PublicAccess", PublicAccess);

            if (PassThru)
            {
                ps.AddParameter("PassThru");
            }

            return InvokeStoragePowerShell(ps, null, ParseContainerCollection);
        }

        public override bool RemoveAzureStorageContainer(string ContainerName, string leaseId = null, bool Force = true)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Remove-AzureStorageContainer");
            ps.BindParameter("Name", ContainerName);

            if (Force)
            {
                ps.AddParameter("Force");
            }

            return InvokeStoragePowerShell(ps);
        }

        public override bool RemoveAzureStorageContainer(string[] ContainerNames, bool Force = true)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddScript(FormatNameList(ContainerNames));
            ps.AddCommand("Remove-AzureStorageContainer");

            if (Force)
            {
                ps.AddParameter("Force");
            }

            return InvokeStoragePowerShell(ps);
        }

        public override bool NewAzureStorageQueue(string QueueName)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("New-AzureStorageQueue");
            ps.BindParameter("Name", QueueName);

            return InvokeStoragePowerShell(ps);
        }

        public override bool NewAzureStorageQueue(string[] QueueNames)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddScript(FormatNameList(QueueNames));
            ps.AddCommand("New-AzureStorageQueue");

            return InvokeStoragePowerShell(ps);
        }

        public override bool GetAzureStorageQueue(string QueueName)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Get-AzureStorageQueue");
            ps.BindParameter("Name", QueueName);

            return InvokeStoragePowerShell(ps);
        }

        public override bool GetAzureStorageQueueByPrefix(string Prefix)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Get-AzureStorageQueue");
            ps.BindParameter("Prefix", Prefix);

            return InvokeStoragePowerShell(ps);
        }

        public override bool RemoveAzureStorageQueue(string QueueName, bool Force = true)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Remove-AzureStorageQueue");
            ps.BindParameter("Name", QueueName);

            if (Force)
            {
                ps.AddParameter("Force");
            }

            return InvokeStoragePowerShell(ps);
        }

        public override bool RemoveAzureStorageQueue(string[] QueueNames, bool Force = true)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddScript(FormatNameList(QueueNames));
            ps.AddCommand("Remove-AzureStorageQueue");

            if (Force)
            {
                ps.AddParameter("Force");
            }

            return InvokeStoragePowerShell(ps);
        }

        public override bool SetAzureStorageBlobContent(string FileName, string ContainerName, BlobType Type, string BlobName = "",
            bool Force = true, int ConcurrentCount = -1, Hashtable properties = null, Hashtable metadata = null, PremiumPageBlobTier? premiumPageBlobTier = null)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Set-AzureStorageBlobContent");
            ps.BindParameter("File", FileName);
            ps.BindParameter("Blob", BlobName);
            ps.BindParameter("Container", ContainerName);
            ps.BindParameter("Properties", properties);
            ps.BindParameter("Metadata", metadata);

            if (Type == BlobType.BlockBlob)
            {
                ps.BindParameter("BlobType", "Block");
            }
            else if (Type == BlobType.PageBlob)
            {
                ps.BindParameter("BlobType", "Page");
            }
            else if (Type == BlobType.AppendBlob)
            {
                ps.BindParameter("BlobType", "Append");
            }

            ps.AddParameter("Force");

            if (ConcurrentCount != -1)
            {
                ps.BindParameter("ConcurrentTaskCount", ConcurrentCount);
            }
            if (premiumPageBlobTier != null)
            {
                ps.BindParameter("PremiumPageBlobTier", premiumPageBlobTier.Value);
            }

            return InvokeStoragePowerShell(ps, null, ParseBlobCollection);
        }

        public override bool UploadLocalFiles(string dirPath, string containerName, BlobType blobType, bool force = true, int concurrentCount = -1)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddScript(String.Format("ls -File -Path '{0}'", dirPath));
            ps.AddCommand("Set-AzureStorageBlobContent");
            ps.BindParameter("Container", containerName);

            if (blobType == BlobType.BlockBlob)
            {
                ps.BindParameter("BlobType", "Block");
            }
            else if (blobType == BlobType.PageBlob)
            {
                ps.BindParameter("BlobType", "Page");
            }
            else if (blobType == BlobType.AppendBlob)
            {
                ps.BindParameter("BlobType", "Append");
            }

            if (concurrentCount != -1)
            {
                ps.BindParameter("ConcurrentTaskCount", concurrentCount);
            }

            return InvokeStoragePowerShell(ps, null, ParseBlobCollection);
        }

        public override bool DownloadBlobFiles(string dirPath, string containerName, bool force = true, int concurrentCount = -1)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Get-AzureStorageContainer");
            ps.BindParameter("Container", containerName);

            AddCommonParameters(ps);

            ps.AddCommand("Get-AzureStorageBlob");
            ps.AddCommand("Get-AzureStorageBlobContent");

            ps.BindParameter("Destination", String.Format("{0}", dirPath));

            if (concurrentCount != -1)
            {
                ps.BindParameter("ConcurrentTaskCount", concurrentCount);
            }

            return InvokeStoragePowerShell(ps, null, ParseBlobCollection);
        }

        public override bool GetAzureStorageBlobContent(string Blob, string Destination, string ContainerName,
            bool Force = true, int ConcurrentCount = -1, bool CheckMd5 = false)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Get-AzureStorageBlobContent");
            ps.BindParameter("Blob", Blob);
            ps.BindParameter("Destination", Destination);
            ps.BindParameter("Container", ContainerName);
            if (Force)
            {
                ps.AddParameter("Force");
            }

            if (ConcurrentCount != -1)
            {
                ps.BindParameter("ConcurrentTaskCount", ConcurrentCount);
            }

            if (CheckMd5)
            {
                ps.AddParameter("CheckMd5");
            }

            return InvokeStoragePowerShell(ps, null, ParseBlobCollection);
        }

        public override bool GetAzureStorageBlob(string BlobName, string ContainerName, bool IncludeDeleted = false)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Get-AzureStorageBlob");
            ps.BindParameter("Blob", BlobName);
            ps.BindParameter("Container", ContainerName);
            ps.BindParameter("IncludeDeleted", IncludeDeleted);

            return InvokeStoragePowerShell(ps, null, ParseBlobCollection);
        }

        public override bool GetAzureStorageBlobByPrefix(string Prefix, string ContainerName)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Get-AzureStorageBlob");
            ps.BindParameter("Prefix", Prefix);
            ps.BindParameter("Container", ContainerName);

            return InvokeStoragePowerShell(ps, null, ParseBlobCollection);
        }

        public override bool RemoveAzureStorageBlob(string BlobName, string ContainerName, string snapshotId = "", string leaseId = null, bool? onlySnapshot = null, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Remove-AzureStorageBlob");
            ps.BindParameter("Blob", BlobName);
            ps.BindParameter("Container", ContainerName);
            ps.BindParameter("DeleteSnapshot", onlySnapshot.HasValue ? onlySnapshot.Value : false);

            if (force)
            {
                ps.AddParameter("Force");
            }

            return InvokeStoragePowerShell(ps);
        }

        public override bool NewAzureStorageTable(string TableName)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("New-AzureStorageTable");
            ps.BindParameter("Name", TableName);

            return InvokeStoragePowerShell(ps, null, ParseContainerCollection);
        }

        public override bool NewAzureStorageTable(string[] TableNames)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddScript(FormatNameList(TableNames));
            ps.AddCommand("New-AzureStorageTable");

            return InvokeStoragePowerShell(ps);
        }

        public override bool GetAzureStorageTable(string TableName)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Get-AzureStorageTable");
            ps.BindParameter("Name", TableName);

            return InvokeStoragePowerShell(ps, null, ParseContainerCollection);
        }

        public override bool GetAzureStorageTableByPrefix(string Prefix)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Get-AzureStorageTable");
            ps.BindParameter("Prefix", Prefix);

            return InvokeStoragePowerShell(ps, null, ParseContainerCollection);
        }

        public override bool RemoveAzureStorageTable(string TableName, bool Force = true)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Remove-AzureStorageTable");
            ps.BindParameter("Name", TableName);

            if (Force)
            {
                ps.AddParameter("Force");
            }

            return InvokeStoragePowerShell(ps);
        }

        public override bool RemoveAzureStorageTable(string[] TableNames, bool Force = true)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddScript(FormatNameList(TableNames));
            ps.AddCommand("Remove-AzureStorageTable");

            if (Force)
            {
                ps.AddParameter("Force");
            }

            return InvokeStoragePowerShell(ps);
        }

        public override bool StartAzureStorageBlobCopy(string sourceUri, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Start-CopyAzureStorageBlob");
            ps.BindParameter("SrcUri", sourceUri);
            ps.BindParameter("DestContainer", destContainerName);
            ps.BindParameter("DestBlob", destBlobName);
            ps.BindParameter("Force", force);
            ps.BindParameter("DestContext", destContext);

            //Don't use context parameter for this cmdlet
            bool savedParameter = UseContextParam;
            UseContextParam = false;
            bool executeState = InvokeStoragePowerShell(ps);
            UseContextParam = savedParameter;
            return executeState;
        }

        public override bool StartAzureStorageBlobCopy(string srcContainerName, string srcBlobName, string destContainerName, string destBlobName, string sourceLease = null, string destLease = null, object destContext = null, bool force = true, PremiumPageBlobTier? premiumPageBlobTier = null)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Start-CopyAzureStorageBlob");
            ps.BindParameter("SrcContainer", srcContainerName);
            ps.BindParameter("SrcBlob", srcBlobName);
            ps.BindParameter("DestContainer", destContainerName);
            ps.BindParameter("Force", force);
            ps.BindParameter("DestBlob", destBlobName);
            ps.BindParameter("DestContext", destContext);
            if (premiumPageBlobTier != null)
            {
                ps.BindParameter("PremiumPageBlobTier", premiumPageBlobTier.Value);
            }

            return InvokeStoragePowerShell(ps);
        }

        public override bool StartAzureStorageBlobCopy(CloudBlob srcBlob, string destContainerName, string destBlobName, object destContext = null, bool force = true, PremiumPageBlobTier? premiumPageBlobTier = null)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Start-CopyAzureStorageBlob");
            ps.BindParameter("CloudBlob", srcBlob);
            ps.BindParameter("DestContainer", destContainerName);
            ps.BindParameter("Force", force);
            ps.BindParameter("DestBlob", destBlobName);
            ps.BindParameter("DestContext", destContext);
            if (premiumPageBlobTier != null)
            {
                ps.BindParameter("PremiumPageBlobTier", premiumPageBlobTier.Value);
            }

            return InvokeStoragePowerShell(ps);
        }

        public override bool StartAzureStorageBlobCopyFromFile(string srcShareName, string srcFilePath, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Start-CopyAzureStorageBlob");
            ps.BindParameter("SrcShareName", srcShareName);
            ps.BindParameter("SrcFilePath", srcFilePath);
            ps.BindParameter("DestContainer", destContainerName);
            ps.BindParameter("Force", force);
            ps.BindParameter("DestBlob", destBlobName);
            ps.BindParameter("DestContext", destContext);

            return InvokeStoragePowerShell(ps);
        }

        public override bool StartAzureStorageBlobCopy(CloudFileShare srcShare, string srcFilePath, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Start-CopyAzureStorageBlob");
            ps.BindParameter("SrcShare", srcShare);
            ps.BindParameter("SrcFilePath", srcFilePath);
            ps.BindParameter("DestContainer", destContainerName);
            ps.BindParameter("Force", force);
            ps.BindParameter("DestBlob", destBlobName);
            ps.BindParameter("DestContext", destContext);

            return InvokeStoragePowerShell(ps);
        }

        public override bool StartAzureStorageBlobCopy(CloudFile srcFile, string destContainerName, string destBlobName, object destContext = null, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);

            ps.AddCommand("Start-CopyAzureStorageBlob");
            ps.BindParameter("SrcFile", srcFile);
            ps.BindParameter("DestContainer", destContainerName);
            ps.BindParameter("DestBlob", destBlobName);
            ps.BindParameter("DestContext", destContext);

            if (force)
            {
                ps.AddParameter("Force");
            }

            return InvokeStoragePowerShell(ps);
        }

        public override bool StartAzureStorageBlobIncrementalCopy(string sourceUri, string destContainerName, string destBlobName, object destContext = null)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Start-AzureStorageBlobIncrementalCopy");
            ps.BindParameter("SrcUri", sourceUri);
            ps.BindParameter("DestContainer", destContainerName);
            ps.BindParameter("DestBlob", destBlobName);
            ps.BindParameter("DestContext", destContext);

            //Don't use context parameter for this cmdlet
            bool savedParameter = UseContextParam;
            UseContextParam = false;
            bool executeState = InvokeStoragePowerShell(ps);
            UseContextParam = savedParameter;
            return executeState;
        }
        public override bool StartAzureStorageBlobIncrementalCopy(string srcContainerName, string srcBlobName, DateTimeOffset? SnapshotTime, string destContainerName, string destBlobName, object destContext = null)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Start-AzureStorageBlobIncrementalCopy");
            ps.BindParameter("SrcContainer", srcContainerName);
            ps.BindParameter("SrcBlob", srcBlobName);
            ps.BindParameter("SrcBlobSnapshotTime", SnapshotTime);
            ps.BindParameter("DestContainer", destContainerName);
            ps.BindParameter("DestBlob", destBlobName);
            ps.BindParameter("DestContext", destContext);

            return InvokeStoragePowerShell(ps);
        }
        public override bool StartAzureStorageBlobIncrementalCopy(CloudBlobContainer srcContainer, string srcBlobName, DateTimeOffset? SnapshotTime, string destContainerName, string destBlobName, object destContext = null)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Start-AzureStorageBlobIncrementalCopy");
            ps.BindParameter("CloudBlobContainer", srcContainer);
            ps.BindParameter("SrcBlob", srcBlobName);
            ps.BindParameter("SrcBlobSnapshotTime", SnapshotTime);
            ps.BindParameter("DestContainer", destContainerName);
            ps.BindParameter("DestBlob", destBlobName);
            ps.BindParameter("DestContext", destContext);

            return InvokeStoragePowerShell(ps);
        }
        public override bool StartAzureStorageBlobIncrementalCopy(CloudPageBlob srcBlob, string destContainerName, string destBlobName, object destContext = null)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Start-AzureStorageBlobIncrementalCopy");
            ps.BindParameter("CloudBlob", srcBlob);
            ps.BindParameter("DestContainer", destContainerName);
            ps.BindParameter("DestBlob", destBlobName);
            ps.BindParameter("DestContext", destContext);

            return InvokeStoragePowerShell(ps);
        }
        public override bool StartAzureStorageBlobIncrementalCopy(CloudPageBlob srcBlob, CloudPageBlob destBlob, object destContext = null)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Start-AzureStorageBlobIncrementalCopy");
            ps.BindParameter("CloudBlob", srcBlob);
            ps.BindParameter("DestCloudBlob", destBlob);
            ps.BindParameter("DestContext", destContext);

            return InvokeStoragePowerShell(ps);
        }

        public override bool GetAzureStorageBlobCopyState(string containerName, string blobName, bool waitForComplete)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);

            ps.AddCommand("Get-AzureStorageBlobCopyState");

            ps.BindParameter("Container", containerName);
            ps.BindParameter("Blob", blobName);
            ps.BindParameter("WaitForComplete", waitForComplete);

            return InvokeStoragePowerShell(ps);
        }

        public override bool GetAzureStorageBlobCopyState(CloudBlob blob, object context, bool waitForComplete)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);

            ps.AddCommand("Get-AzureStorageBlobCopyState");
            ps.BindParameter("CloudBlob", blob);
            ps.BindParameter("WaitForComplete", waitForComplete);

            return InvokeStoragePowerShell(ps, context);
        }

        public override bool StopAzureStorageBlobCopy(string containerName, string blobName, string copyId, bool force)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);

            ps.AddCommand("Stop-CopyAzureStorageBlob");
            ps.BindParameter("Container", containerName);
            ps.BindParameter("Blob", blobName);
            ps.BindParameter("CopyId", copyId);
            ps.BindParameter("Force", force);

            return InvokeStoragePowerShell(ps);
        }

        public override bool SnapshotAzureStorageBlob(string containerName, string blobName, string leaseId = null)
        {
            throw new NotImplementedException();
        }

        public override bool AcquireLease(string containerName, string blobName, string proposedLeaseId = null, int duration = -1)
        {
            throw new NotImplementedException();
        }

        public override bool RenewLease(string containerName, string blobName, string leaseId)
        {
            throw new NotImplementedException();
        }

        public override bool ChangeLease(string containerName, string blobName, string leaseId, string proposedLeaseId)
        {
            throw new NotImplementedException();
        }

        public override bool ReleaseLease(string containerName, string blobName, string leaseId)
        {
            throw new NotImplementedException();
        }

        public override bool BreakLease(string containerName, string blobName, int duration = 0)
        {
            throw new NotImplementedException();
        }

        ///-------------------------------------
        /// Logging & Metrics APIs
        ///-------------------------------------
        public override bool GetAzureStorageServiceLogging(Constants.ServiceType serviceType)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Get-AzureStorageServiceLoggingProperty");
            ps.BindParameter("ServiceType", serviceType.ToString());

            return InvokeStoragePowerShell(ps);
        }

        public override bool GetAzureStorageServiceMetrics(Constants.ServiceType serviceType, Constants.MetricsType metricsType)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Get-AzureStorageServiceMetricsProperty");
            ps.BindParameter("ServiceType", serviceType.ToString());
            ps.BindParameter("MetricsType", metricsType.ToString());

            return InvokeStoragePowerShell(ps);
        }

        public override bool SetAzureStorageServiceLogging(Constants.ServiceType serviceType, string loggingOperations = "", string loggingRetentionDays = "",
            string loggingVersion = "", bool passThru = false)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Set-AzureStorageServiceLoggingProperty");
            ps.BindParameter("ServiceType", serviceType.ToString());

            // set logging properties
            ps.BindParameter("LoggingOperations", loggingOperations);
            ps.BindParameter("Version", loggingVersion);
            ps.BindParameter("RetentionDays", loggingRetentionDays);

            ps.BindParameter("PassThru", passThru);

            return InvokeStoragePowerShell(ps);
        }

        public override bool SetAzureStorageServiceLogging(Constants.ServiceType serviceType, LoggingOperations[] loggingOperations, string loggingRetentionDays = "",
            string loggingVersion = "", bool passThru = false)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Set-AzureStorageServiceLoggingProperty");
            ps.BindParameter("ServiceType", serviceType.ToString());

            // set logging properties
            ps.BindParameter("LoggingOperations", loggingOperations);
            ps.BindParameter("Version", loggingVersion);
            ps.BindParameter("RetentionDays", loggingRetentionDays);

            ps.BindParameter("PassThru", passThru);

            return InvokeStoragePowerShell(ps);
        }

        public override bool SetAzureStorageServiceMetrics(Constants.ServiceType serviceType, Constants.MetricsType metricsType, string metricsLevel = "", string metricsRetentionDays = "",
            string metricsVersion = "", bool passThru = false)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Set-AzureStorageServiceMetricsProperty");
            ps.BindParameter("ServiceType", serviceType.ToString());
            ps.BindParameter("MetricsType", metricsType.ToString());

            // set metrics properties
            ps.BindParameter("MetricsLevel", metricsLevel);
            ps.BindParameter("Version", metricsVersion);
            ps.BindParameter("RetentionDays", metricsRetentionDays);

            ps.BindParameter("PassThru", passThru);

            return InvokeStoragePowerShell(ps);
        }

        public override bool SetAzureStorageCORSRules(Constants.ServiceType serviceType, PSCorsRule[] corsRules)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Set-AzureStorageCORSRule");
            ps.BindParameter("ServiceType", serviceType.ToString());
            ps.BindParameter("CorsRules", corsRules);

            return InvokeStoragePowerShell(ps);
        }

        public override bool GetAzureStorageCORSRules(Constants.ServiceType serviceType)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Get-AzureStorageCORSRule");
            ps.BindParameter("ServiceType", serviceType.ToString());

            return InvokeStoragePowerShell(ps);
        }

        public override bool RemoveAzureStorageCORSRules(Constants.ServiceType serviceType)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Remove-AzureStorageCORSRule");
            ps.BindParameter("ServiceType", serviceType.ToString());

            return InvokeStoragePowerShell(ps);
        }

        public override bool GetAzureStorageServiceProperties(Constants.ServiceType serviceType)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Get-AzureStorageServiceProperty");
            ps.BindParameter("ServiceType", serviceType.ToString());

            return InvokeStoragePowerShell(ps);
        }

        public override bool UpdateAzureStorageServiceProperties(Constants.ServiceType serviceType, string DefaultServiceVersion)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Update-AzureStorageServiceProperty");
            ps.BindParameter("ServiceType", serviceType.ToString());
            ps.BindParameter("DefaultServiceVersion", DefaultServiceVersion);

            return InvokeStoragePowerShell(ps);
        }


        public override bool DisableAzureStorageDeleteRetentionPolicy(bool PassThru = false)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Disable-AzureStorageDeleteRetentionPolicy");
            ps.BindParameter("PassThru", PassThru);

            return InvokeStoragePowerShell(ps);
        }

        public override bool EnableAzureStorageDeleteRetentionPolicy(int RetentionDays, bool PassThru = false)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Enable-AzureStorageDeleteRetentionPolicy");
            ps.BindParameter("RetentionDays", RetentionDays);
            ps.BindParameter("PassThru", PassThru);

            return InvokeStoragePowerShell(ps);
        }

        ///-------------------------------------
        /// SAS token APIs
        ///-------------------------------------
        public override bool NewAzureStorageContainerSAS(string container, string policy, string permission,
                DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("New-AzureStorageContainerSASToken");
            ps.BindParameter("Container", container);
            ps.BindParameter("Policy", policy);
            ps.BindParameter("Permission", permission);
            ps.BindParameter("Protocol", protocol);
            ps.BindParameter("IPAddressOrRange", iPAddressOrRange);
            ps.BindParameter("StartTime", startTime);
            ps.BindParameter("ExpiryTime", expiryTime);
            ps.BindParameter("FullURI", fullUri);

            return InvokeStoragePowerShell(ps, parseFunc: ParseSASCollection);
        }

        public override bool NewAzureStorageBlobSAS(string container, string blob, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("New-AzureStorageBlobSASToken");
            ps.BindParameter("Container", container);
            ps.BindParameter("Blob", blob);
            ps.BindParameter("Policy", policy);
            ps.BindParameter("Permission", permission);
            ps.BindParameter("Protocol", protocol);
            ps.BindParameter("IPAddressOrRange", iPAddressOrRange);
            ps.BindParameter("StartTime", startTime);
            ps.BindParameter("ExpiryTime", expiryTime);
            ps.BindParameter("FullURI", fullUri);

            return InvokeStoragePowerShell(ps, parseFunc: ParseSASCollection);
        }

        public override bool NewAzureStorageTableSAS(string name, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false, string startpk = "", string startrk = "", string endpk = "", string endrk = "", SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("New-AzureStorageTableSASToken");
            ps.BindParameter("Name", name);
            ps.BindParameter("Startpk", startpk);
            ps.BindParameter("Startrk", startrk);
            ps.BindParameter("Endpk", endpk);
            ps.BindParameter("Endrk", endrk);
            ps.BindParameter("Policy", policy);
            ps.BindParameter("Permission", permission);
            ps.BindParameter("Protocol", protocol);
            ps.BindParameter("IPAddressOrRange", iPAddressOrRange);
            ps.BindParameter("StartTime", startTime);
            ps.BindParameter("ExpiryTime", expiryTime);
            ps.BindParameter("FullURI", fullUri);

            return InvokeStoragePowerShell(ps, parseFunc: ParseSASCollection);
        }

        public override bool NewAzureStorageQueueSAS(string name, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fullUri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("New-AzureStorageQueueSASToken");
            ps.BindParameter("Name", name);
            ps.BindParameter("Policy", policy);
            ps.BindParameter("Permission", permission);
            ps.BindParameter("Protocol", protocol);
            ps.BindParameter("IPAddressOrRange", iPAddressOrRange);
            ps.BindParameter("StartTime", startTime);
            ps.BindParameter("ExpiryTime", expiryTime);
            ps.BindParameter("FullURI", fullUri);

            return InvokeStoragePowerShell(ps, parseFunc: ParseSASCollection);
        }

        public override bool NewAzureStorageAccountSAS(SharedAccessAccountServices service, SharedAccessAccountResourceTypes resourceType, string permission, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null,
            DateTime? startTime = null, DateTime? expiryTime = null)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("New-AzureStorageAccountSASToken");
            ps.BindParameter("Service", service);
            ps.BindParameter("ResourceType", resourceType);
            ps.BindParameter("Permission", permission);
            ps.BindParameter("Protocol", protocol);
            ps.BindParameter("IPAddressOrRange", iPAddressOrRange);
            ps.BindParameter("StartTime", startTime);
            ps.BindParameter("ExpiryTime", expiryTime);

            return InvokeStoragePowerShell(ps, parseFunc: ParseSASCollection);
        }

        ///-------------------------------------
        /// Stored Access Policy APIs
        ///-------------------------------------     
        public override bool GetAzureStorageTableStoredAccessPolicy(string tableName, string policyName)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Get-AzureStorageTableStoredAccessPolicy");
            ps.BindParameter("Table", tableName);
            ps.BindParameter("Policy", policyName);

            return InvokeStoragePowerShell(ps);
        }


        public override bool NewAzureStorageTableStoredAccessPolicy(string tableName, string policyName, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("New-AzureStorageTableStoredAccessPolicy");
            ps.BindParameter("Table", tableName);
            ps.BindParameter("Policy", policyName);
            ps.BindParameter("Permission", permission, true);
            ps.BindParameter("StartTime", startTime);
            ps.BindParameter("ExpiryTime", expiryTime);

            return InvokeStoragePowerShell(ps);
        }

        public override bool RemoveAzureStorageTableStoredAccessPolicy(string tableName, string policyName, bool Force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Remove-AzureStorageTableStoredAccessPolicy");
            ps.BindParameter("Table", tableName);
            ps.BindParameter("Policy", policyName);

            return InvokeStoragePowerShell(ps);
        }

        public override bool SetAzureStorageTableStoredAccessPolicy(string tableName, string policyName, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool NoStartTime = false, bool NoExpiryTime = false)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Set-AzureStorageTableStoredAccessPolicy");
            ps.BindParameter("Table", tableName);
            ps.BindParameter("Policy", policyName);
            ps.BindParameter("Permission", permission, true);
            ps.BindParameter("StartTime", startTime);
            ps.BindParameter("ExpiryTime", expiryTime);

            if (NoStartTime)
            {
                ps.BindParameter("NoStartTime");
            }

            if (NoExpiryTime)
            {
                ps.BindParameter("NoExpiryTime");
            }

            return InvokeStoragePowerShell(ps);
        }

        public override bool GetAzureStorageQueueStoredAccessPolicy(string queueName, string policyName)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Get-AzureStorageQueueStoredAccessPolicy");
            ps.BindParameter("Queue", queueName);
            ps.BindParameter("Policy", policyName);

            return InvokeStoragePowerShell(ps);
        }

        public override bool NewAzureStorageQueueStoredAccessPolicy(string queueName, string policyName, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("New-AzureStorageQueueStoredAccessPolicy");
            ps.BindParameter("Queue", queueName);
            ps.BindParameter("Policy", policyName);
            ps.BindParameter("Permission", permission, true);
            ps.BindParameter("StartTime", startTime);
            ps.BindParameter("ExpiryTime", expiryTime);

            return InvokeStoragePowerShell(ps);
        }

        public override bool RemoveAzureStorageQueueStoredAccessPolicy(string queueName, string policyName, bool Force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Remove-AzureStorageQueueStoredAccessPolicy");
            ps.BindParameter("Queue", queueName);
            ps.BindParameter("Policy", policyName);

            return InvokeStoragePowerShell(ps);
        }

        public override bool SetAzureStorageQueueStoredAccessPolicy(string queueName, string policyName, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool NoStartTime = false, bool NoExpiryTime = false)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Set-AzureStorageQueueStoredAccessPolicy");
            ps.BindParameter("Queue", queueName);
            ps.BindParameter("Policy", policyName);
            ps.BindParameter("Permission", permission, true);
            ps.BindParameter("StartTime", startTime);
            ps.BindParameter("ExpiryTime", expiryTime);

            if (NoStartTime)
            {
                ps.BindParameter("NoStartTime");
            }

            if (NoExpiryTime)
            {
                ps.BindParameter("NoExpiryTime");
            }

            return InvokeStoragePowerShell(ps);
        }

        public override bool GetAzureStorageContainerStoredAccessPolicy(string containerName, string policyName)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Get-AzureStorageContainerStoredAccessPolicy");
            ps.BindParameter("Container", containerName);
            ps.BindParameter("Policy", policyName);

            return InvokeStoragePowerShell(ps);
        }

        public override bool NewAzureStorageContainerStoredAccessPolicy(string containerName, string policyName, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("New-AzureStorageContainerStoredAccessPolicy");
            ps.BindParameter("Container", containerName);
            ps.BindParameter("Policy", policyName);
            ps.BindParameter("Permission", permission, true);
            ps.BindParameter("StartTime", startTime);
            ps.BindParameter("ExpiryTime", expiryTime);

            //return InvokeStoragePowerShell(ps);
            return InvokeStoragePowerShell(ps, parseFunc: ParseSASCollection);
        }

        public override bool RemoveAzureStorageContainerStoredAccessPolicy(string containerName, string policyName, bool Force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Remove-AzureStorageContainerStoredAccessPolicy");
            ps.BindParameter("Container", containerName);
            ps.BindParameter("Policy", policyName);

            return InvokeStoragePowerShell(ps);
        }

        public override bool SetAzureStorageContainerStoredAccessPolicy(string containerName, string policyName, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool NoStartTime = false, bool NoExpiryTime = false)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Set-AzureStorageContainerStoredAccessPolicy");
            ps.BindParameter("Container", containerName);
            ps.BindParameter("Policy", policyName);
            ps.BindParameter("Permission", permission, true);
            ps.BindParameter("StartTime", startTime);
            ps.BindParameter("ExpiryTime", expiryTime);

            if (NoStartTime)
            {
                ps.BindParameter("NoStartTime");
            }

            if (NoExpiryTime)
            {
                ps.BindParameter("NoExpiryTime");
            }

            return InvokeStoragePowerShell(ps);
        }

        public override bool StartFileCopyFromFile(string srcShareName, string srcFilePath, string shareName, string filePath, object destContext, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Start-AzureStorageFileCopy");
            ps.BindParameter("SrcShareName", srcShareName);
            ps.BindParameter("SrcFilePath", srcFilePath);
            ps.BindParameter("DestShareName", shareName);

            if (!string.IsNullOrEmpty(filePath))
            {
                ps.BindParameter("DestFilePath", filePath);
            }

            if (null != destContext)
            {
                ps.BindParameter("DestContext", destContext);
            }

            ps.BindParameter("Force", force);

            return InvokeStoragePowerShell(ps);
        }

        public override bool StartFileCopy(CloudFileShare share, string srcFilePath, string shareName, string filePath, object destContext, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Start-AzureStorageFileCopy");
            ps.BindParameter("SrcShare", share);
            ps.BindParameter("SrcFilePath", srcFilePath);
            ps.BindParameter("DestShareName", shareName);

            if (!string.IsNullOrEmpty(filePath))
            {
                ps.BindParameter("DestFilePath", filePath);
            }

            if (null != destContext)
            {
                ps.BindParameter("DestContext", destContext);
            }

            ps.BindParameter("Force", force);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool StartFileCopy(CloudFileDirectory dir, string srcFilePath, string shareName, string filePath, object destContext, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Start-AzureStorageFileCopy");
            ps.BindParameter("SrcDir", dir);
            ps.BindParameter("SrcFilePath", srcFilePath);
            ps.BindParameter("DestShareName", shareName);

            if (!string.IsNullOrEmpty(filePath))
            {
                ps.BindParameter("DestFilePath", filePath);
            }

            if (null != destContext)
            {
                ps.BindParameter("DestContext", destContext);
            }

            ps.BindParameter("Force", force);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool StartFileCopy(CloudFile srcFile, string shareName, string filePath, object destContext, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            AttachPipeline(ps);

            ps.AddCommand("Start-AzureStorageFileCopy");
            ps.BindParameter("SrcFile", srcFile);
            ps.BindParameter("DestShareName", shareName);

            if (!string.IsNullOrEmpty(filePath))
            {
                ps.BindParameter("DestFilePath", filePath);
            }

            if (null != destContext)
            {
                ps.BindParameter("DestContext", destContext);
            }

            ps.BindParameter("Force", force);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool StartFileCopy(CloudFile srcFile, CloudFile destFile, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            AttachPipeline(ps);

            ps.AddCommand("Start-AzureStorageFileCopy");
            ps.BindParameter("SrcFile", srcFile);
            ps.BindParameter("DestFile", destFile);

            ps.BindParameter("Force", force);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool StartFileCopy(CloudBlobContainer container, string blobName, string shareName, string filePath, object destContext, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Start-AzureStorageFileCopy");
            ps.BindParameter("SrcContainer", container);
            ps.BindParameter("SrcBlobName", blobName);
            ps.BindParameter("DestShareName", shareName);

            if (!string.IsNullOrEmpty(filePath))
            {
                ps.BindParameter("DestFilePath", filePath);
            }

            if (null != destContext)
            {
                ps.BindParameter("DestContext", destContext);
            }

            ps.BindParameter("Force", force);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool StartFileCopy(CloudBlob blob, string shareName, string filePath, object destContext, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            AttachPipeline(ps);

            ps.AddCommand("Start-AzureStorageFileCopy");
            ps.BindParameter("SrcBlob", blob);
            ps.BindParameter("DestShareName", shareName);

            if (!string.IsNullOrEmpty(filePath))
            {
                ps.BindParameter("DestFilePath", filePath);
            }

            if (null != destContext)
            {
                ps.BindParameter("DestContext", destContext);
            }

            ps.BindParameter("Force", force);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool StartFileCopy(CloudBlob blob, CloudFile destFile, object destContext, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            AttachPipeline(ps);

            ps.AddCommand("Start-AzureStorageFileCopy");
            ps.BindParameter("SrcBlob", blob);
            ps.BindParameter("DestFile", destFile);

            ps.BindParameter("Force", force);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool StartFileCopyFromBlob(string containerName, string blobName, string shareName, string filePath, object destContext, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Start-AzureStorageFileCopy");
            ps.BindParameter("SrcContainerName", containerName);
            ps.BindParameter("SrcBlobName", blobName);
            ps.BindParameter("DestShareName", shareName);

            if (!string.IsNullOrEmpty(filePath))
            {
                ps.BindParameter("DestFilePath", filePath);
            }

            if (null != destContext)
            {
                ps.BindParameter("DestContext", destContext);
            }

            ps.BindParameter("Force", force);

            return InvokeStoragePowerShell(ps);
        }

        public override bool StartFileCopy(string uri, CloudFile destFile, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Start-AzureStorageFileCopy");
            ps.BindParameter("AbsoluteUri", uri);
            ps.BindParameter("DestFile", destFile);

            ps.BindParameter("Force", force);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool StartFileCopy(string uri, string destShareName, string destFilePath, object destContext, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Start-AzureStorageFileCopy");
            ps.BindParameter("AbsoluteUri", uri);
            ps.BindParameter("DestShareName", destShareName);
            ps.BindParameter("DestFilePath", destFilePath);

            if (null != destContext)
            {
                ps.BindParameter("DestContext", destContext);
            }

            ps.BindParameter("Force", force);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool GetFileCopyState(string shareName, string filePath, object context, bool waitForComplete = false)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Get-AzureStorageFileCopyState");
            ps.BindParameter("ShareName", shareName);
            ps.BindParameter("FilePath", filePath);

            ps.BindParameter("WaitForComplete", waitForComplete);

            return InvokeStoragePowerShell(ps);
        }

        public override bool GetFileCopyState(CloudFile file, object context, bool waitForComplete = false)
        {
            PowerShell ps = GetPowerShellInstance();

            AttachPipeline(ps);

            ps.AddCommand("Get-AzureStorageFileCopyState");
            ps.BindParameter("File", file);

            ps.BindParameter("WaitForComplete", waitForComplete);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool StopFileCopy(string shareName, string filePath, string copyId, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            ps.AddCommand("Stop-AzureStorageFileCopy");
            ps.BindParameter("ShareName", shareName);
            ps.BindParameter("FilePath", filePath);

            if (null != copyId)
            {
                ps.BindParameter("CopyId", copyId);
            }

            ps.BindParameter("Force", force);

            return InvokeStoragePowerShell(ps);
        }

        public override bool StopFileCopy(CloudFile file, string copyId, bool force = true)
        {
            PowerShell ps = GetPowerShellInstance();

            AttachPipeline(ps);

            ps.AddCommand("Stop-AzureStorageFileCopy");
            ps.BindParameter("File", file);

            if (null != copyId)
            {
                ps.BindParameter("CopyId", copyId);
            }

            ps.BindParameter("Force", force);

            return InvokePowerShellWithoutContext(ps);
        }

        public bool StartFileCopyFromContainer(string sourceConnectionString, string destConnectionString, string containerName, string shareName)
        {
            PowerShell ps = GetPowerShellInstance();

            string script = ".\\PSHScripts\\CopyFromContainer.ps1" + " -sourceConnectionString \"" + sourceConnectionString
                + "\" -destConnectionString \"" + destConnectionString + "\" -containerName " + containerName + " -shareName " + shareName;

            ps.AddScript(script, true);

            return InvokePowerShellWithoutContext(ps);
        }

        public bool StartFileCopyFromShare(string sourceConnectionString, string destConnectionString, string sourceShare, string destShare)
        {
            PowerShell ps = GetPowerShellInstance();

            string script = ".\\PSHScripts\\CopyFromShare.ps1" + " -sourceConnectionString \"" + sourceConnectionString
                + "\" -destConnectionString \"" + destConnectionString + "\" -sourceShareName " + sourceShare + " -destShareName " + destShare;

            ps.AddScript(script, true);

            return InvokePowerShellWithoutContext(ps);
        }

        /// <summary>
        /// Compare the output collection data with comp
        /// 
        /// Parameters:
        ///     comp: comparison data
        /// </summary> 
        public override void OutputValidation(Collection<Dictionary<string, object>> comp)
        {
            Test.Info("Validate Dictionary objects");
            Test.Assert(comp.Count == Output.Count, "Comparison size: {0} = {1} Output size", comp.Count, Output.Count);
            if (comp.Count != Output.Count)
                return;

            // first check whether Key exists and then check value if it's not null
            for (int i = 0; i < comp.Count; ++i)
            {
                foreach (string str in comp[i].Keys)
                {
                    Test.Assert(Output[i].ContainsKey(str), "{0} should be in the output columns", str);

                    switch (str)
                    {
                        case "Context":
                            break;

                        case "CloudTable":
                            Test.Assert(CompareEntity((CloudTable)comp[i][str], (CloudTable)Output[i][str]),
                                "CloudTable Column {0}: {1} = {2}", str, comp[i][str], Output[i][str]);
                            break;

                        case "CloudQueue":
                            Test.Assert(CompareEntity((CloudQueue)comp[i][str], (CloudQueue)Output[i][str]),
                                "CloudQueue Column {0}: {1} = {2}", str, comp[i][str], Output[i][str]);
                            break;

                        case "CloudBlobContainer":
                            Test.Assert(CompareEntity((CloudBlobContainer)comp[i][str], (CloudBlobContainer)Output[i][str]),
                                "CloudBlobContainer Column {0}: {1} = {2}", str, comp[i][str], Output[i][str]);
                            break;

                        case "ICloudBlob":
                            Test.Assert(CompareEntity((CloudBlob)comp[i][str], (CloudBlob)Output[i][str]),
                                "ICloudBlob Column {0}: {1} = {2}", str, comp[i][str], Output[i][str]);
                            break;

                        case "Permission":
                            Test.Assert(CompareEntity((BlobContainerPermissions)comp[i][str], (BlobContainerPermissions)Output[i][str]),
                                "Permission Column {0}: {1} = {2}", str, comp[i][str], Output[i][str]);
                            break;

                        default:

                            if (comp[i][str] == null)
                            {
                                Test.Assert(Output[i][str] == null, "Column {0}: {1} = {2}", str, comp[i][str], Output[i][str]);
                            }
                            else
                            {
                                Test.Assert(comp[i][str].Equals(Output[i][str]), "Column {0}: {1} = {2}", str, comp[i][str], Output[i][str]);
                            }

                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Compare the output collection data with containers
        /// 
        /// Parameters:
        ///     containers: comparison data
        /// </summary> 
        public override void OutputValidation(IEnumerable<StorageAccount> accounts)
        {
            Test.Info("Validate StorageAccount objects");
            Test.Assert(accounts.Count() == Output.Count, "Comparison size: {0} = {1} Output size", accounts.Count(), Output.Count);
            if (accounts.Count() != Output.Count)
                return;

            int count = 0;
            foreach (StorageAccount account in accounts)
            {
                Test.Assert(CompareEntity(account, (StorageAccount)Output[count][BaseObject]), "StorageAccount equality checking: {0}", account.Name);
                ++count;
            }
        }

        /// <summary>
        /// Compare the output collection data with containers
        /// 
        /// Parameters:
        ///     containers: comparison data
        /// </summary> 
        public override void OutputValidation(IEnumerable<CloudBlobContainer> containers)
        {
            Test.Info("Validate CloudBlobContainer objects");
            Test.Assert(containers.Count() == Output.Count, "Comparison size: {0} = {1} Output size", containers.Count(), Output.Count);
            if (containers.Count() != Output.Count)
                return;

            int count = 0;
            foreach (CloudBlobContainer container in containers)
            {
                container.FetchAttributes();
                Test.Assert(CompareEntity(container, (CloudBlobContainer)Output[count]["CloudBlobContainer"]), "container equality checking: {0}", container.Name);
                ++count;
            }
        }

        public override void OutputValidation(IEnumerable<CloudFileShare> shares)
        {
            Test.Info("Validate CloudFileShare objects");
            Test.Assert(shares.Count() == Output.Count, "Comparison size: {0} = {1} Output size", shares.Count(), Output.Count);
            if (shares.Count() != Output.Count)
                return;

            //Note: if this fails for empty secondary URI, please check if the storage account has been created with endpoints. 
            int count = 0;
            foreach (var share in shares)
            {
                Test.Assert(CompareEntity(share, (CloudFileShare)Output[count][BaseObject]), "fileshare equality checking: {0}", share.Name);
                ++count;
            }
        }

        public override void OutputValidation(IEnumerable<IListFileItem> items)
        {
            var directories = items.Where(i => i as CloudFileDirectory != null).Select(i => (CloudFileDirectory)i);

            Test.Info("Validate CloudFileShare objects");
            Test.Assert(directories.Count() == Output.Count, "Comparison size: {0} = {1} Output size", directories.Count(), Output.Count);
            if (directories.Count() != Output.Count)
                return;

            //Note: if this fails for empty secondary URI, please check if the storage account has been created with endpoints. 
            int count = 0;
            foreach (var dir in directories)
            {
                Test.Assert(CompareEntity(dir, (CloudFileDirectory)Output[count][BaseObject]), "directory equality checking: {0}", dir.Name);
                ++count;
            }
        }

        /// <summary>
        /// Compare the output collection data with container permissions
        /// </summary> 
        /// <param name="containers">a list of cloudblobcontainer objects</param>
        public override void OutputValidation(IEnumerable<BlobContainerPermissions> permissions)
        {
            Test.Info("Validate BlobContainerPermissions");
            Test.Assert(permissions.Count() == Output.Count, "Comparison size: {0} = {1} Output size", permissions.Count(), Output.Count);
            if (permissions.Count() != Output.Count)
                return;

            int count = 0;
            foreach (BlobContainerPermissions permission in permissions)
            {
                Test.Assert(CompareEntity(permission, (BlobContainerPermissions)Output[count]["Permission"]), "container permision equality checking ");
                ++count;
            }
        }

        /// <summary>
        /// Compare the output collection data with CloudBlob
        /// </summary> 
        /// <param name="containers">a list of cloudblobcontainer objects</param>
        public override void OutputValidation(IEnumerable<CloudBlob> blobs)
        {
            Test.Info("Validate CloudBlob objects");
            Test.Assert(blobs.Count() == Output.Count, "Comparison size: {0} = {1} Output size", blobs.Count(), Output.Count);
            if (blobs.Count() != Output.Count)
                return;

            int count = 0;
            foreach (CloudBlob blob in blobs)
            {
                Test.Assert(CompareEntity(blob, (CloudBlob)Output[count]["ICloudBlob"]), string.Format("CloudBlob equality checking for blob '{0}'", blob.Name));
                ++count;
            }
        }


        /// <summary>
        /// Compare the output collection data with queues
        /// 
        /// Parameters:
        ///     queues: comparison data
        /// </summary> 
        public override void OutputValidation(IEnumerable<CloudQueue> queues)
        {
            Test.Info("Validate CloudQueue objects");
            Test.Assert(queues.Count() == Output.Count, "Comparison size: {0} = {1} Output size", queues.Count(), Output.Count);
            if (queues.Count() != Output.Count)
                return;

            int count = 0;
            foreach (CloudQueue queue in queues)
            {
                queue.FetchAttributes();
                Test.Assert(CompareEntity(queue, (CloudQueue)Output[count]["CloudQueue"]), "queue equality checking: {0}", queue.Name);
                ++count;
            }
        }

        /// <summary>
        /// Compare the output collection data with tables
        /// 
        /// Parameters:
        ///     tables: comparison data
        /// </summary> 
        public override void OutputValidation(IEnumerable<CloudTable> tables)
        {
            Test.Info("Validate CloudTable objects");
            Test.Assert(tables.Count() == Output.Count, "Comparison size: {0} = {1} Output size", tables.Count(), Output.Count);
            if (tables.Count() != Output.Count)
                return;

            int count = 0;
            foreach (CloudTable table in tables)
            {
                Test.Assert(CompareEntity(table, (CloudTable)Output[count]["CloudTable"]), "table equality checking: {0}", table.Name);
                ++count;
            }
        }

        public override void OutputValidation(ServiceProperties serviceProperties, string propertiesType)
        {
            Test.Info("Validate ServiceProperties");
            Test.Assert(1 == Output.Count, "Output count should be {0} = 1", Output.Count);

            if (propertiesType.ToLower() == "logging")
            {
                Test.Assert(serviceProperties.Logging.LoggingOperations.ToString().Equals(Output[0]["LoggingOperations"].ToString()),
                    string.Format("expected LoggingOperations '{0}', actually it's '{1}'", serviceProperties.Logging.LoggingOperations.ToString(),
                    Output[0]["LoggingOperations"].ToString()));

                Test.Assert(serviceProperties.Logging.RetentionDays.Equals((Output[0]["RetentionDays"])),
                    string.Format("expected RetentionDays {0}, actually it's {1}", serviceProperties.Logging.RetentionDays, Output[0]["RetentionDays"]));

                Test.Assert(serviceProperties.Logging.Version.Equals(Output[0]["Version"]),
                    string.Format("expected Version {0}, actually it's {1}", serviceProperties.Logging.Version, Output[0]["Version"]));
            }
            else if (propertiesType.ToLower() == "hourmetrics")
            {
                Test.Assert(serviceProperties.HourMetrics.MetricsLevel.ToString().Equals(Output[0]["MetricsLevel"].ToString()),
                    string.Format("expected MetricsLevel '{0}', actually it's '{1}'", serviceProperties.HourMetrics.MetricsLevel.ToString(),
                    Output[0]["MetricsLevel"].ToString()));

                Test.Assert(serviceProperties.HourMetrics.RetentionDays.Equals((Output[0]["RetentionDays"])),
                    string.Format("expected RetentionDays {0}, actually it's {1}", serviceProperties.HourMetrics.RetentionDays, Output[0]["RetentionDays"]));

                Test.Assert(serviceProperties.HourMetrics.Version.Equals(Output[0]["Version"]),
                    string.Format("expected Version {0}, actually it's {1}", serviceProperties.HourMetrics.Version, Output[0]["Version"]));
            }
            else if (propertiesType.ToLower() == "minutemetrics")
            {
                Test.Assert(serviceProperties.MinuteMetrics.MetricsLevel.ToString().Equals(Output[0]["MetricsLevel"].ToString()),
                    string.Format("expected MetricsLevel '{0}', actually it's '{1}'", serviceProperties.MinuteMetrics.MetricsLevel.ToString(),
                    Output[0]["MetricsLevel"].ToString()));

                Test.Assert(serviceProperties.MinuteMetrics.RetentionDays.Equals((Output[0]["RetentionDays"])),
                    string.Format("expected RetentionDays {0}, actually it's {1}", serviceProperties.MinuteMetrics.RetentionDays, Output[0]["RetentionDays"]));

                Test.Assert(serviceProperties.MinuteMetrics.Version.Equals(Output[0]["Version"]),
                    string.Format("expected Version {0}, actually it's {1}", serviceProperties.MinuteMetrics.Version, Output[0]["Version"]));
            }
            else
            {
                throw new Exception("unknown properties type : " + propertiesType);
            }
        }

        /// <summary>
        /// Get blob sas token from powershell cmdlet
        /// </summary>
        /// <returns></returns>
        public override string GetBlobSasFromCmd(string containerName, string blobName, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            Test.Assert(NewAzureStorageBlobSAS(containerName, blobName, policy, permission, startTime, expiryTime, fulluri, protocol, iPAddressOrRange),
                    "Generate blob sas token should succeed");
            if (Output.Count != 0)
            {
                string sasToken = Output[0][Constants.SASTokenKey].ToString();
                Test.Info("Generated sas token: {0}", sasToken);
                return sasToken;
            }
            else
            {
                throw new ArgumentException("Fail to generate sas token.");
            }
        }

        /// <summary>
        /// Get blob sas token from powershell cmdlet
        /// </summary>
        /// <returns></returns>
        public override string GetBlobSasFromCmd(CloudBlob blob, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            return GetBlobSasFromCmd(blob.Container.Name, blob.Name, policy, permission, startTime, expiryTime, fulluri, protocol, iPAddressOrRange);
        }

        /// <summary>
        /// Get container sas token from powershell cmdlet
        /// </summary>
        /// <returns></returns>
        public override string GetContainerSasFromCmd(string containerName, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            Test.Assert(NewAzureStorageContainerSAS(containerName, policy, permission, startTime, expiryTime, fulluri, protocol, iPAddressOrRange),
                    "Generate container sas token should succeed");
            if (Output.Count != 0)
            {
                string sasToken = Output[0][Constants.SASTokenKey].ToString();
                Test.Info("Generated sas token: {0}", sasToken);
                return sasToken;
            }
            else
            {
                throw new ArgumentException("Fail to generate sas token.");
            }
        }

        /// <summary>
        /// Get queue sas token from powershell cmdlet
        /// </summary>
        /// <returns></returns>
        public override string GetQueueSasFromCmd(string queueName, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            Test.Assert(NewAzureStorageQueueSAS(queueName, policy, permission, startTime, expiryTime, fulluri, protocol, iPAddressOrRange),
                    "Generate queue sas token should succeed");
            if (Output.Count != 0)
            {
                string sasToken = Output[0][Constants.SASTokenKey].ToString();
                Test.Info("Generated sas token: {0}", sasToken);
                return sasToken;
            }
            else
            {
                throw new ArgumentException("Fail to generate sas token.");
            }
        }

        /// <summary>
        /// Get table sas token from powershell cmdlet
        /// </summary>
        /// <returns></returns>
        public override string GetTableSasFromCmd(string tableName, string policy, string permission,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false,
            string startpk = "", string startrk = "", string endpk = "", string endrk = "", SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            Test.Assert(NewAzureStorageTableSAS(tableName, policy, permission, startTime, expiryTime, fulluri,
                startpk, startrk, endpk, endrk, protocol, iPAddressOrRange),
                    "Generate table sas token should succeed");
            if (Output.Count != 0)
            {
                string sasToken = Output[0][Constants.SASTokenKey].ToString();
                Test.Info("Generated sas token: {0}", sasToken);
                return sasToken;
            }
            else
            {
                throw new ArgumentException("Fail to generate sas token.");
            }
        }

        /// <summary>
        /// Get account sas token from powershell cmdlet
        /// </summary>
        /// <returns></returns>
        public override string GetAccountSasFromCmd(SharedAccessAccountServices service, SharedAccessAccountResourceTypes resourceType, string permission, SharedAccessProtocol? protocol, string iPAddressOrRange,
            DateTime? startTime = null, DateTime? expiryTime = null)
        {
            Test.Assert(NewAzureStorageAccountSAS(service, resourceType, permission, protocol, iPAddressOrRange, startTime, expiryTime),
                    "Generate account sas token should succeed");
            if (Output.Count != 0)
            {
                string sasToken = Output[0][Constants.SASTokenKey].ToString();
                Test.Info("Generated sas token: {0}", sasToken);
                return sasToken;
            }
            else
            {
                throw new ArgumentException("Fail to generate sas token.");
            }
        }

        public override string SetContextWithSASToken(string accountName, CloudBlobUtil blobUtil, StorageObjectType objectType,
            string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null)
        {
            return this.SetContextWithSASToken(accountName, blobUtil, objectType, null, policy, permission, startTime, expiryTime);
        }

        public override string SetContextWithSASToken(string accountName, CloudBlobUtil blobUtil, StorageObjectType objectType,
            string endpoint, string policy, string permission, DateTime? startTime = null, DateTime? expiryTime = null)
        {
            string sastoken = string.Empty;
            switch (objectType)
            {
                case StorageObjectType.Blob:
                    sastoken = GetBlobSasFromCmd(blobUtil.Blob, policy, permission);
                    break;
                case StorageObjectType.Container:
                    sastoken = GetContainerSasFromCmd(blobUtil.ContainerName, policy, permission);
                    break;
                default:
                    throw new Exception("unsupported object type : " + objectType.ToString());
            }

            this.SetStorageContextWithSASToken(accountName, sastoken, endpoint: endpoint);
            return sastoken;
        }

        /// <summary>
        /// Invoke PowerShell Script
        /// </summary>
        /// <param name="script">the script to run</param>
        /// <returns>running result</returns>
        public bool InvokePSScript(string script)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddScript(script);

            Test.Info("run PS Script: " + script);

            ps.Invoke();
            
            ParseErrorMessages(ps);

            foreach(string s in ErrorMessages)
            {
                Console.WriteLine(s);
            }
            return !ps.HadErrors;
        }

        /// <summary>
        /// Common function for invoke powershell cmdlet
        /// </summary>
        /// <param name="parseFunc">if it's not null, then it will use this specific function to parse values</param>
        /// <returns></returns>
        private bool InvokeStoragePowerShell(PowerShell ps, object context = null, ParseCollectionFunc parseFunc = null)
        {
            if (context == null)
            {
                AddCommonParameters(ps);
            }
            else
            {
                ps.BindParameter(ContextParameterName, context);
            }

            return this.InvokePowerShellWithoutContext(ps, parseFunc);
        }

        private bool InvokePowerShellWithoutContext(PowerShell ps, ParseCollectionFunc parseFunc = null)
        {
            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            _Output.Clear();

            //TODO We should add a time out for this invoke. Bad news, powershell don't support buildin time out for invoking.
            Exception runtimeException = null;
            try
            {
                if (parseFunc != null)
                {
                    parseFunc(ps.Invoke());
                }
                else
                {
                    ParseCollection(ps.Invoke());
                }
            }
            catch (Exception e)
            {
                Test.Info(e.Message);
                runtimeException = e;
            }

            ParseErrorMessages(ps, runtimeException);

            return !ps.HadErrors;
        }

        /// <summary>
        /// Add the common parameters
        ///     -Context ...
        ///     -Force ...
        /// </summary>        
        internal void AddCommonParameters(PowerShell ps, bool Force)
        {
            AddCommonParameters(ps);

            if (Force)
            {
                ps.BindParameter("Force");
            }
        }

        /// <summary>
        /// Add the common parameters
        ///     -Context ...
        /// </summary>        
        internal void AddCommonParameters(PowerShell ps)
        {
            if (UseContextParam && AgentContext != null)
            {
                ps.BindParameter(ContextParameterName, AgentContext);
            }
        }

        /// <summary>
        /// Get the command line string
        /// </summary>        
        static internal string GetCommandLine(PowerShell ps)
        {
            StringBuilder strCmdLine = new StringBuilder();
            bool bFirst = true;
            foreach (Command command in ps.Commands.Commands)
            {
                if (bFirst)
                {
                    bFirst = false;
                }
                else
                {
                    strCmdLine.Append(" | ");
                }

                strCmdLine.Append(command.CommandText);

                foreach (CommandParameter param in command.Parameters)
                {
                    if (param.Name != null)
                    {
                        strCmdLine.Append(" -" + param.Name);
                    }

                    if (param.Value != null)
                    {
                        strCmdLine.Append(" " + param.Value);
                    }
                }
            }
            return strCmdLine.ToString();
        }

        /// <summary>
        /// Parse the return values in the colletion
        /// </summary>     
        internal void ParseCollection(Collection<PSObject> values)
        {
            _Output.Clear();

            foreach (PSObject result in values)
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic[BaseObject] = result.BaseObject;

                foreach (PSMemberInfo member in result.Members)
                {
                    try
                    {
                        if (member.Value != null)
                        {
                            // skip the PSMethod members
                            if (member.Value.GetType() != typeof(PSMethod))
                            {
                                dic.Add(member.Name, member.Value);
                            }
                        }
                        else
                        {
                            dic.Add(member.Name, null);
                        }
                    }
                    catch (Exception)
                    {
                        // It may report an error when try to get some script properties, here ignore them.
                        continue;
                    }
                }
                _Output.Add(dic);
            }

            //clean pipeline when finished
            CleanPipeline();
        }

        /// <summary>
        /// Parse the return values of container operation
        /// </summary>     
        internal void ParseContainerCollection(Collection<PSObject> Values)
        {
            _Output.Clear();

            foreach (PSObject result in Values)
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic[BaseObject] = result.BaseObject;

                foreach (PSMemberInfo member in result.Members)
                {
                    if (member.Value != null)
                    {
                        // skip the PSMethod members
                        if (member.Value.GetType() != typeof(PSMethod))
                        {
                            dic.Add(member.Name, member.Value);
                        }
                    }
                    else
                    {
                        dic.Add(member.Name, member.Value);
                    }

                    if (member.Name.Equals("Properties"))
                    {
                        var properties = member.Value as BlobContainerProperties;

                        if (properties != null)
                        {
                            dic.Add("LastModified", properties.LastModified);
                            continue;
                        }

                        var shareProperties = member.Value as FileShareProperties;
                        if (shareProperties != null)
                        {
                            dic.Add("LastModified", shareProperties.LastModified);
                            continue;
                        }

                        var dirProperties = member.Value as FileDirectoryProperties;
                        if (dirProperties != null)
                        {
                            dic.Add("LastModified", dirProperties.LastModified);
                            continue;
                        }

                        var fileProperties = member.Value as FileProperties;
                        if (fileProperties != null)
                        {
                            dic.Add("LastModified", fileProperties.LastModified);
                            dic.Add("Length", fileProperties.Length);
                            continue;
                        }
                    }
                }
                _Output.Add(dic);
            }

            //clean pipeline when finished
            CleanPipeline();
        }

        /// <summary>
        /// Parse the return values of blob operation
        /// </summary>     
        internal void ParseBlobCollection(Collection<PSObject> Values)
        {
            _Output.Clear();

            foreach (PSObject result in Values)
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();
                foreach (PSMemberInfo member in result.Members)
                {
                    if (member.Value != null)
                    {
                        // skip the PSMethod members
                        if (member.Value.GetType() != typeof(PSMethod))
                        {
                            dic.Add(member.Name, member.Value);
                        }
                    }
                    else
                    {
                        dic.Add(member.Name, member.Value);
                    }

                    if (member.Name.Equals("Properties"))
                    {
                        BlobProperties properties = (BlobProperties)member.Value;
                        dic.Add("LastModified", properties.LastModified);
                        dic.Add("Length", properties.Length);
                        dic.Add("ContentType", properties.ContentType);
                    }
                }
                _Output.Add(dic);
            }

            //clean pipeline when finished
            CleanPipeline();
        }

        /// <summary>
        /// Parse the return values of SAS token colletion
        /// </summary>     
        internal void ParseSASCollection(Collection<PSObject> Values)
        {
            _Output.Clear();
            foreach (PSObject result in Values)
            {
                Dictionary<string, object> dic = new Dictionary<string, object>();
                dic.Add(Constants.SASTokenKey, result.ToString());
                _Output.Add(dic);
            }

            //clean pipeline when finished
            CleanPipeline();
        }

        /// <summary>
        /// Parse the error messages in PowerShell
        /// </summary>     
        internal void ParseErrorMessages(PowerShell ps, Exception runtimeException = null)
        {
            _ErrorMessages.Clear();
            _RuntimeException = null;
            if (ps.HadErrors)
            {
                foreach (ErrorRecord record in ps.Streams.Error)
                {
                    _ErrorMessages.Add(record.Exception.ToString());
                    Test.Info(record.Exception.Message);

                    //Display the stack trace for storage exception in order to investigate the root cause of errors
                    if (record.Exception.GetType() == typeof(StorageException))
                    {
                        //Display the stack trace from where the exception is thrown
                        //Since we repack the storage exception, the following call stack may be inaccurate
                        Test.Info("[Exception Call Stack Trace]:{0}", record.Exception.StackTrace);

                        if (record.Exception.InnerException != null)
                        {
                            //Display the stack trace of innerException
                            Test.Info("[InnerException Call Stack Trace]:{0}", record.Exception.InnerException.StackTrace);
                        }
                    }
                }

                if (runtimeException != null)
                {
                    CmdletInvocationException invocationException = runtimeException as CmdletInvocationException;
                    _RuntimeException = null == invocationException ? runtimeException : invocationException.InnerException;

                    _ErrorMessages.Add(_RuntimeException.ToString());
                }
            }
        }

        /// <summary>
        /// Convert names to a string type name list 
        /// e.g.
        ///     names = new string[]{ "bbbb", "cccc", "dddd" }
        /// ConvertNameList(names) = "bbbb", "cccc", "dddd"
        /// </summary>   
        internal static string FormatNameList(string[] names)
        {
            StringBuilder builder = new StringBuilder();
            bool bFirst = true;

            foreach (string name in names)
            {
                if (bFirst)
                {
                    bFirst = false;
                }
                else
                {
                    builder.Append(",");
                }

                builder.Append(String.Format("\"{0}\"", name));
            }
            return builder.ToString();
        }

        ///-------------------------------------
        /// The following interface only used for PowerShell Agent, and they are not part of Agent
        ///-------------------------------------
        private List<string> pipeLine = new List<string>();

        public void AddPipelineScript(string cmd)
        {
            if (string.IsNullOrEmpty(cmd))
            {
                return;
            }

            pipeLine.Add(cmd);
        }

        public void CleanPipeline()
        {
            pipeLine.Clear();
        }

        /// <summary>
        /// Attach some script to the current PowerShell instance
        ///     Attach Rule :
        ///         1. If the script is start with "$", we directly add it to the pipeline
        ///         2. If the current script is storage cmdlet, we need to add the current storage context to it.
        ///         3. Otherwise, split the script into [CommandName] and many [-Parameter][Value] pairs, attach them using PowerShell command interface(AddParameter/AddCommand/etc)
        ///         //TODO update the step 3
        /// </summary>
        /// <param name="ps">PowerShell instance</param>
        private void AttachPipeline(PowerShell ps)
        {
            foreach (string cmd in pipeLine)
            {
                if (cmd.Length > 0 && cmd[0] == '$')
                {
                    ps.AddScript(cmd);
                }
                else
                {
                    string[] cmdOpts = cmd.Split(' ');
                    string cmdName = cmdOpts[0];
                    ps.AddCommand(cmdName);

                    string opts = string.Empty;
                    bool skip = false;
                    for (int i = 1; i < cmdOpts.Length; i++)
                    {
                        if (skip)
                        {
                            skip = false;
                            continue;
                        }

                        if (cmdOpts[i].IndexOf("-") != 0)
                        {
                            ps.AddArgument(cmdOpts[i]);
                        }
                        else
                        {
                            if (i + 1 < cmdOpts.Length && cmdOpts[i + 1].IndexOf("-") != 0)
                            {
                                ps.BindParameter(cmdOpts[i].Substring(1), cmdOpts[i + 1]);
                                skip = true;
                            }
                            else
                            {
                                ps.BindParameter(cmdOpts[i].Substring(1));
                                skip = false;
                            }
                        }
                    }

                    //add storage context for azure storage cmdlet 
                    //It make sure all the storage cmdlet in pipeline use the same storage context
                    if (cmdName.ToLower().IndexOf("-azurestorage") != -1)
                    {
                        AddCommonParameters(ps);
                    }
                }
            }
        }

        /// <summary>
        /// Compare two entities, usually one from XSCL, one from PowerShell
        /// </summary> 
        public static bool CompareEntity<T>(T v1, T v2)
        {
            bool bResult = true;
            var isFile = v1 is CloudFile;
            var isFileDirectory = v1 is CloudFileDirectory;

            if (v1 == null || v2 == null)
            {
                if (v1 == null && v2 == null)
                {
                    Test.Info("Skip compare null objects");
                    return true;
                }
                else
                {
                    Test.AssertFail(string.Format("v1 is {0}, but v2 is {1}", v1, v2));
                    return false;
                }
            }

            foreach (var propertyInfo in typeof(T).GetProperties())
            {
                if (propertyInfo.Name.Equals("ServiceClient")
                    || propertyInfo.Name.Equals("Container")
                    || propertyInfo.Name.Equals("Parent")
                    || propertyInfo.Name.Equals("AppendBlobCommittedBlockCount"))
                    continue;

                object o1 = null;
                object o2 = null;

                try
                {
                    o1 = propertyInfo.GetValue(v1, null);
                    o2 = propertyInfo.GetValue(v2, null);
                }
                catch
                {
                    //skip the comparison when throw exception
                    string msg = string.Format("Skip compare '{0}' property in type {1}", propertyInfo.Name, typeof(T));
                    Trace.WriteLine(msg);
                    Test.Warn(msg);
                    continue;
                }

                if (propertyInfo.Name.Equals("Metadata"))
                {
                    if (v1.GetType() == typeof(CloudBlobContainer)
                        || v1.GetType() == typeof(CloudBlockBlob)
                        || v1.GetType() == typeof(CloudPageBlob)
                        || v1.GetType() == typeof(CloudAppendBlob)
                        || v1.GetType() == typeof(CloudQueue)
                        || v1.GetType() == typeof(CloudTable)
                        || v1.GetType() == typeof(CloudFileShare)
                        || v1.GetType() == typeof(CloudFileDirectory)
                        || v1.GetType() == typeof(CloudFile))
                    {
                        bResult = ((IDictionary<string, string>)o1).SequenceEqual((IDictionary<string, string>)o2);
                    }
                    else
                    {
                        bResult = o1.Equals(o2);
                    }
                }
                else if (propertyInfo.Name.Equals("Properties"))
                {
                    if (v1.GetType() == typeof(CloudBlockBlob)
                        || v1.GetType() == typeof(CloudPageBlob)
                        || v1.GetType() == typeof(CloudAppendBlob))
                    {
                        bResult = CompareEntity((BlobProperties)o1, (BlobProperties)o2);
                    }
                    else if (v1.GetType() == typeof(CloudBlobContainer))
                    {
                        bResult = CompareEntity((BlobContainerProperties)o1, (BlobContainerProperties)o2);
                    }
                }
                else if (propertyInfo.Name.Equals("SharedAccessPolicies"))
                {
                    if (v1.GetType() == typeof(BlobContainerPermissions))
                    {
                        bResult = CompareEntity((SharedAccessBlobPolicies)o1, (SharedAccessBlobPolicies)o2);
                    }
                    else
                    {
                        bResult = o1.Equals(o2);
                    }
                }
                else if (propertyInfo.Name.Equals("Parent") && (isFile || isFileDirectory))
                {
                    bResult = CompareEntity(o1, o2);
                }
                else if (propertyInfo.Name.Equals("Share") && (isFile || isFileDirectory))
                {
                    bResult = CompareEntity(o1, o2);
                }
                else if (propertyInfo.Name.Equals("PremiumPageBlobTier"))
                {
                    bResult = CompareEntity(o1, o2);
                    if ((o2 == null && o1 != null && ((PremiumPageBlobTier?)o1).Value == PremiumPageBlobTier.Unknown)
                        || (o1 == null && o2 != null && ((PremiumPageBlobTier?)o2).Value == PremiumPageBlobTier.Unknown))
                    {
                        bResult = true;
                    }
                }
                else if (propertyInfo.Name.Equals("StandardBlobTier"))
                {
                    bResult = CompareEntity(o1, o2);
                    if ((o2 == null && o1 != null && ((StandardBlobTier?)o1).Value == StandardBlobTier.Unknown)
                        || (o1 == null && o2 != null && ((StandardBlobTier?)o2).Value == StandardBlobTier.Unknown))
                    {
                        bResult = true;
                    }
                }
                else if (propertyInfo.Name.Equals("BlobTierInferred"))
                {
                    bResult = CompareEntity(o1, o2);
                    if ((o2 == null && o1 != null && (bool)o1 == false)
                        || (o1 == null && o2 != null && (bool)o2 == false))
                    {
                        bResult = true;
                    }
                }
                else if (propertyInfo.Name.Equals("IsServerEncrypted"))
                {
                    // IsServerEncrypted is control by server, not by user input.
                    bResult = true;
                }                
                else
                {
                    if (o1 == null)
                    {
                        if (o2 != null)
                            bResult = false;
                    }
                    else
                    {
                        //compare according to type
                        if (o1 is ICollection<string>)
                        {
                            bResult = ((ICollection<string>)o1).SequenceEqual((ICollection<string>)o2);
                        }
                        else if (o1 is ICollection<SharedAccessBlobPolicy>)
                        {
                            bResult = CompareEntity((ICollection<SharedAccessBlobPolicy>)o1, (ICollection<SharedAccessBlobPolicy>)o2);
                        }
                        else
                        {
                            bResult = o1.Equals(o2);
                        }
                    }
                }

                if (bResult == false)
                {
                    // TODO:
                    // As PageBlobSequenceNumber is new introduced in XSCL 2.1, there is transformation issue in System.Management.Automation.dll, 
                    // we should ignore this in test case, otherwise by XSCL it would be 0, and in Automation.dll it would return null
                    if (propertyInfo.Name == "PageBlobSequenceNumber" && o2 == null)
                    {
                        bResult = true;
                    }
                    else
                    {
                        Test.Error("Property Mismatch: {0} in type {1}. {2} != {3}", propertyInfo.Name, typeof(T), o1, o2);
                        break;
                    }
                }
                else
                {
                    Test.Verbose("Property {0} in type {1}: {2} == {3}", propertyInfo.Name, typeof(T), o1, o2);
                }
            }
            return bResult;
        }

        #region xSMB operations

        private PowerShell shell;

        public PowerShellAgent()
        {
            //assign the error message table for error validation
            ExpectedErrorMsgTable = ExpectedErrorMsgTablePS;

            var initSessionState = _InitState.Clone();
            initSessionState.LanguageMode = PSLanguageMode.FullLanguage;

            this.shell = PowerShell.Create(initSessionState);
            this.Clear();
        }

        public override bool HadErrors
        {
            get
            {
                return this.shell.HadErrors;
            }
        }

        public PowerShell PowerShellSession
        {
            get
            {
                return this.shell;
            }
        }

        public override object CreateStorageContextObject(string connectionString)
        {
            // FIXME: To walk around Bug in XSCL, we will have to add DefaultEndpointsProtocol section
            // in the connection string.
            if (!connectionString.Contains("DefaultEndpointsProtocol"))
            {
                connectionString = "DefaultEndpointsProtocol=http;" + connectionString;
            }

            Test.Info("Creating storage context object with the provided connection string: {0}", connectionString);
            this.shell.AddCommand("New-AzureStorageContext");
            this.shell.AddParameter("ConnectionString", connectionString);
            var result = (PowerShellExecutionResult)this.Invoke();
            Test.Assert(result.Count() == 1, "Should have only one result when creating new azure storage context object.");
            if (result.Count() != 1)
            {
                throw new AssertFailedException();
            }

            this.Clear();
            return result.First().ImmediateBaseObject;
        }

        public override void SetVariable(string variableName, object value)
        {
            this.shell.AddCommand("Set-Variable");
            this.shell.AddParameter("Name", variableName);
            this.shell.AddParameter("Value", value);
            var result = this.Invoke();
            this.AssertNoError();
            this.Clear();
        }

        public override string GetCurrentLocation()
        {
            this.shell.AddCommand("Get-Location");
            ParseCollection(this.shell.Invoke());
            ParseErrorMessages(this.shell);
            this.Clear();
            return _Output[0][PowerShellAgent.BaseObject].ToString();
        }

        public override void ChangeLocation(string path)
        {
            this.shell.AddCommand("Set-Location");
            this.shell.AddParameter("Path", path);
            var result = this.Invoke();
            this.AssertNoError();
            this.Clear();
        }

        public override void NewFileShare(string fileShareName, object contextObject = null)
        {
            this.shell.AddCommand("New-AzureStorageShare");
            this.shell.AddParameter("Name", fileShareName);
            this.shell.AddParameter("Context", contextObject ?? PowerShellAgent.Context);
        }

        public override void NewFileShareFromPipeline()
        {
            this.shell.Runspace.SessionStateProxy.SetVariable("context", PowerShellAgent.Context);
            this.shell.AddCommand("Foreach-Object");
            this.shell.AddParameter("Process", ScriptBlock.Create("New-AzureStorageShare -Name $_ -Context $context"));
        }

        public override void NewDirectoryFromPipeline(string fileShareName)
        {
            this.shell.Runspace.SessionStateProxy.SetVariable("context", PowerShellAgent.Context);
            this.shell.AddCommand("Foreach-Object");
            this.shell.AddParameter("Process", ScriptBlock.Create(string.Format(CultureInfo.InvariantCulture, "New-AzureStorageDirectory -ShareName {0} -Path $_ -Context $context", fileShareName)));
        }

        public override void UploadFilesFromPipeline(string fileShareName, string localFileName)
        {
            this.shell.Runspace.SessionStateProxy.SetVariable("context", PowerShellAgent.Context);
            this.shell.AddCommand("Foreach-Object");
            this.shell.AddParameter("Process", ScriptBlock.Create(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Set-AzureStorageFileContent -ShareName {0} -Source \"{1}\" -Path $_ -Context $context",
                    fileShareName,
                    localFileName)));
        }

        public override void UploadFilesInFolderFromPipeline(string fileShareName, string folder)
        {
            this.shell.Runspace.SessionStateProxy.SetVariable("context", PowerShellAgent.Context);
            this.shell.AddCommand("Foreach-Object");
            this.shell.AddParameter("Process", ScriptBlock.Create(
                string.Format(
                    CultureInfo.InvariantCulture,
                    @"Set-AzureStorageFileContent -ShareName {0} -Source ""{1}\$_"" -Path $_ -Context $context",
                    fileShareName,
                    folder)));
        }

        public override void RemoveFileShareFromPipeline()
        {
            this.shell.Runspace.SessionStateProxy.SetVariable("context", PowerShellAgent.Context);
            this.shell.AddCommand("Foreach-Object");
            this.shell.AddParameter("Process", ScriptBlock.Create("Remove-AzureStorageShare -Name $_ -Context $context -Confirm:$false"));
        }

        public override void RemoveDirectoriesFromPipeline(string fileShareName)
        {
            this.shell.Runspace.SessionStateProxy.SetVariable("context", PowerShellAgent.Context);
            this.shell.AddCommand("Foreach-Object");
            this.shell.AddParameter("Process", ScriptBlock.Create(string.Format(CultureInfo.InvariantCulture, "Remove-AzureStorageDirectory -ShareName {0} -Path $_ -Context $context -Confirm:$false", fileShareName)));
        }

        public override void RemoveFilesFromPipeline(string fileShareName)
        {
            this.shell.Runspace.SessionStateProxy.SetVariable("context", PowerShellAgent.Context);
            this.shell.AddCommand("Foreach-Object");
            this.shell.AddParameter("Process", ScriptBlock.Create(string.Format(CultureInfo.InvariantCulture, "Remove-AzureStorageFile -ShareName {0} -Path $_ -Context $context -Confirm:$false", fileShareName)));
        }

        public override void GetFileShareByName(string fileShareName, DateTimeOffset? snapshotTime = null)
        {
            this.shell.AddCommand("Get-AzureStorageShare");
            if (!string.IsNullOrEmpty(fileShareName))
            {
                this.shell.AddParameter("Name", fileShareName);
            }
            this.shell.BindParameter("SnapshotTime", snapshotTime);
            this.shell.AddParameter("Context", PowerShellAgent.Context);
        }

        public override void GetFileShareByPrefix(string prefix)
        {
            this.shell.AddCommand("Get-AzureStorageShare");
            this.shell.AddParameter("Prefix", prefix);
            this.shell.AddParameter("Context", PowerShellAgent.Context);
        }

        public override void RemoveFileShareByName(string fileShareName, bool passThru = false, object contextObject = null, bool confirm = false, bool includeAllSnapshot = false)
        {
            this.shell.AddCommand("Remove-AzureStorageShare");
            this.shell.AddParameter("Name", fileShareName);
            if (includeAllSnapshot)
            {
                this.shell.AddParameter("IncludeAllSnapshot");
            }
            this.shell.AddParameter("Context", contextObject ?? PowerShellAgent.Context);

            if (!confirm)
            {
                this.shell.AddParameter("Force");
            }

            if (passThru)
            {
                this.shell.AddParameter("PassThru");
            }
        }

        public override void NewDirectory(CloudFileShare fileShare, string directoryName)
        {
            this.shell.AddCommand("New-AzureStorageDirectory");
            this.shell.AddParameter("Share", fileShare);
            this.shell.AddParameter("Path", directoryName);
        }

        public override void NewDirectory(CloudFileDirectory directory, string directoryName)
        {
            this.shell.AddCommand("New-AzureStorageDirectory");
            this.shell.AddParameter("Directory", directory);
            this.shell.AddParameter("Path", directoryName);
        }

        public override void NewDirectory(string fileShareName, string directoryName, object contextObject = null)
        {
            this.shell.AddCommand("New-AzureStorageDirectory");
            this.shell.AddParameter("ShareName", fileShareName);
            this.shell.AddParameter("Path", directoryName);
            this.shell.AddParameter("Context", contextObject ?? PowerShellAgent.Context);
        }

        public override void RemoveDirectory(CloudFileShare fileShare, string directoryName, bool confirm = false)
        {
            this.shell.AddCommand("Remove-AzureStorageDirectory");
            this.shell.AddParameter("Share", fileShare);
            this.shell.AddParameter("Path", directoryName);

            if (!confirm)
            {
                this.shell.AddParameter("Confirm", false);
            }
        }

        public override void RemoveDirectory(CloudFileDirectory directory, string path, bool confirm = false)
        {
            this.shell.AddCommand("Remove-AzureStorageDirectory");
            this.shell.AddParameter("Directory", directory);
            this.shell.AddParameter("Path", path);

            if (!confirm)
            {
                this.shell.AddParameter("Confirm", false);
            }
        }

        public override void RemoveDirectory(string fileShareName, string directoryName, object contextObject = null, bool confirm = false)
        {
            this.shell.AddCommand("Remove-AzureStorageDirectory");
            this.shell.AddParameter("ShareName", fileShareName);
            this.shell.AddParameter("Path", directoryName);
            this.shell.AddParameter("Context", contextObject ?? PowerShellAgent.Context);

            if (!confirm)
            {
                this.shell.AddParameter("Confirm", false);
            }
        }

        public override void RemoveFile(CloudFileShare fileShare, string fileName, bool confirm = false)
        {
            this.shell.AddCommand("Remove-AzureStorageFile");
            this.shell.AddParameter("Share", fileShare);
            this.shell.AddParameter("Path", fileName);

            if (!confirm)
            {
                this.shell.AddParameter("Confirm", false);
            }
        }

        public override void RemoveFile(CloudFileDirectory directory, string fileName, bool confirm = false)
        {
            this.shell.AddCommand("Remove-AzureStorageFile");
            this.shell.AddParameter("Directory", directory);
            this.shell.AddParameter("Path", fileName);

            if (!confirm)
            {
                this.shell.AddParameter("Confirm", false);
            }
        }

        public override void RemoveFile(CloudFile file, bool confirm = false)
        {
            this.shell.AddCommand("Remove-AzureStorageFile");
            this.shell.AddParameter("File", file);

            if (!confirm)
            {
                this.shell.AddParameter("Confirm", false);
            }
        }

        public override void RemoveFile(string fileShareName, string fileName, object contextObject = null, bool confirm = false)
        {
            this.shell.AddCommand("Remove-AzureStorageFile");
            this.shell.AddParameter("ShareName", fileShareName);
            this.shell.AddParameter("Path", fileName);
            this.shell.AddParameter("Context", contextObject ?? PowerShellAgent.Context);

            if (!confirm)
            {
                this.shell.AddParameter("Confirm", false);
            }
        }

        public override void GetFile(string fileShareName, string path = null)
        {
            this.shell.AddCommand("Get-AzureStorageFile");
            this.shell.AddParameter("ShareName", fileShareName);
            if (path != null)
            {
                this.shell.AddParameter("Path", path);
            }

            this.shell.AddParameter("Context", PowerShellAgent.Context);
        }

        /// <summary>
        /// The is used for pipeline Share or Directory object to it to list file/dir.
        /// </summary>
        public override void GetFile()
        {
            this.shell.AddCommand("Get-AzureStorageFile");

        }

        public override void GetFile(CloudFileShare fileShare, string path = null)
        {
            this.shell.AddCommand("Get-AzureStorageFile");
            this.shell.AddParameter("Share", fileShare);
            if (path != null)
            {
                this.shell.AddParameter("Path", path);
            }
        }

        public override void GetFile(CloudFileDirectory directory, string path = null)
        {
            this.shell.AddCommand("Get-AzureStorageFile");
            this.shell.AddParameter("Directory", directory);
            if (path != null)
            {
                this.shell.AddParameter("Path", path);
            }
        }

        public override void DownloadFile(CloudFile file, string destination, bool overwrite = false)
        {
            this.shell.AddCommand("Get-AzureStorageFileContent");
            this.shell.AddParameter("File", file);
            this.shell.AddParameter("Destination", destination);

            if (overwrite)
            {
                this.shell.AddParameter("Force");
            }
        }

        public override void DownloadFile(CloudFileDirectory directory, string path, string destination, bool overwrite = false)
        {
            this.shell.AddCommand("Get-AzureStorageFileContent");
            this.shell.AddParameter("Directory", directory);
            this.shell.AddParameter("Path", path);
            this.shell.AddParameter("Destination", destination);

            if (overwrite)
            {
                this.shell.AddParameter("Force");
            }
        }

        public override void DownloadFile(CloudFileShare fileShare, string path, string destination, bool overwrite = false)
        {
            this.shell.AddCommand("Get-AzureStorageFileContent");
            this.shell.AddParameter("Share", fileShare);
            this.shell.AddParameter("Path", path);
            this.shell.AddParameter("Destination", destination);

            if (overwrite)
            {
                this.shell.AddParameter("Force");
            }
        }

        public override void DownloadFile(string fileShareName, string path, string destination, bool overwrite = false, object contextObject = null, bool CheckMd5 = false)
        {
            this.shell.AddCommand("Get-AzureStorageFileContent");
            this.shell.AddParameter("ShareName", fileShareName);
            this.shell.AddParameter("Path", path);
            this.shell.AddParameter("Destination", destination);

            if (overwrite)
            {
                this.shell.AddParameter("Force");
            }

            if (CheckMd5)
            {
                this.shell.AddParameter("CheckMd5");
            }

            this.shell.AddParameter("Context", contextObject ?? PowerShellAgent.Context);
        }

        public override void DownloadFiles(string fileShareName, string path, string destination, bool overwrite = false)
        {
            this.shell.AddCommand("Get-AzureStorageFile");
            this.shell.AddParameter("shareName", fileShareName);
            this.shell.AddParameter("context", PowerShellAgent.Context);

            this.shell.AddCommand("Get-AzureStorageFileContent");
            this.shell.AddParameter("Destination", destination);

            if (overwrite)
            {
                this.shell.AddParameter("Force");
            }
        }

        public override void UploadFile(CloudFileShare fileShare, string source, string path, bool overwrite = false, bool passThru = false)
        {
            this.shell.AddCommand("Set-AzureStorageFileContent");
            this.shell.AddParameter("Share", fileShare);
            this.shell.AddParameter("Source", source);
            this.shell.AddParameter("Path", path);

            if (overwrite)
            {
                this.shell.AddParameter("Force");
            }

            if (passThru)
            {
                this.shell.AddParameter("PassThru");
            }
        }

        public override void UploadFile(CloudFileDirectory directory, string source, string path, bool overwrite = false, bool passThru = false)
        {
            this.shell.AddCommand("Set-AzureStorageFileContent");
            this.shell.AddParameter("Directory", directory);
            this.shell.AddParameter("Source", source);
            this.shell.AddParameter("Path", path);

            if (overwrite)
            {
                this.shell.AddParameter("Force");
            }

            if (passThru)
            {
                this.shell.AddParameter("PassThru");
            }
        }

        public override void UploadFile(string fileShareName, string source, string path, bool overwrite = false, bool passThru = false, object contextObject = null)
        {
            this.shell.AddCommand("Set-AzureStorageFileContent");
            this.shell.AddParameter("ShareName", fileShareName);
            this.shell.AddParameter("Source", source);
            this.shell.AddParameter("Path", path);

            if (overwrite)
            {
                this.shell.AddParameter("Force");
            }

            this.shell.AddParameter("Context", contextObject ?? PowerShellAgent.Context);

            if (passThru)
            {
                this.shell.AddParameter("PassThru");
            }
        }

        public override bool NewAzureStorageShareStoredAccessPolicy(string shareName, string policyName, string permissions, DateTime? startTime, DateTime? expiryTime)
        {
            this.Clear();

            this.shell.AddCommand("New-AzureStorageShareStoredAccessPolicy");
            this.shell.BindParameter("ShareName", shareName);
            this.shell.BindParameter("Policy", policyName);
            this.shell.BindParameter("Permission", permissions);
            this.shell.BindParameter("StartTime", startTime);
            this.shell.BindParameter("ExpiryTime", expiryTime);

            return InvokeStoragePowerShell(this.shell);
        }

        public override bool GetAzureStorageShareStoredAccessPolicy(string shareName, string policyName)
        {
            this.Clear();

            this.shell.AddCommand("Get-AzureStorageShareStoredAccessPolicy");
            this.shell.BindParameter("ShareName", shareName);

            if (null != policyName)
            {
                this.shell.BindParameter("Policy", policyName);
            }

            return InvokeStoragePowerShell(this.shell);
        }

        public override bool RemoveAzureStorageShareStoredAccessPolicy(string shareName, string policyName, bool confirm = false)
        {
            this.Clear();

            this.shell.AddCommand("Remove-AzureStorageShareStoredAccessPolicy");
            this.shell.BindParameter("ShareName", shareName);
            this.shell.BindParameter("Policy", policyName);

            return InvokeStoragePowerShell(this.shell);
        }

        public override bool SetAzureStorageShareStoredAccessPolicy(string shareName, string policyName, string permissions,
            DateTime? startTime, DateTime? expiryTime, bool noStartTime = false, bool noExpiryTime = false)
        {
            this.Clear();

            this.shell.AddCommand("Set-AzureStorageShareStoredAccessPolicy");
            this.shell.BindParameter("ShareName", shareName);
            this.shell.BindParameter("Policy", policyName);
            this.shell.BindParameter("Permission", permissions);

            if (startTime.HasValue)
            {
                this.shell.BindParameter("StartTime", startTime.Value);
            }

            if (expiryTime.HasValue)
            {
                this.shell.BindParameter("ExpiryTime", expiryTime.Value);
            }

            this.shell.BindParameter("NoStartTime", noStartTime);
            this.shell.BindParameter("NoExpiryTime", noExpiryTime);

            return InvokeStoragePowerShell(this.shell);
        }

        public override bool NewAzureStorageShareSAS(string shareName, string policyName = null, string permissions = null,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            this.Clear();

            this.shell.AddCommand("New-AzureStorageShareSASToken");
            this.shell.BindParameter("ShareName", shareName);
            this.shell.BindParameter("Protocol", protocol);
            this.shell.BindParameter("IPAddressOrRange", iPAddressOrRange);

            this.AddSASTokenParameter(policyName, permissions, startTime, expiryTime, fulluri);


            return InvokeStoragePowerShell(this.shell);
        }

        public override bool NewAzureStorageFileSAS(string shareName, string filePath, string policyName = null, string permissions = null,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            this.Clear();

            this.shell.AddCommand("New-AzureStorageFileSASToken");
            this.shell.BindParameter("ShareName", shareName);
            this.shell.BindParameter("Protocol", protocol);
            this.shell.BindParameter("IPAddressOrRange", iPAddressOrRange);

            this.shell.BindParameter("Path", filePath);

            this.AddSASTokenParameter(policyName, permissions, startTime, expiryTime, fulluri);

            return InvokeStoragePowerShell(this.shell);
        }

        public override bool NewAzureStorageFileSAS(CloudFile file, string policyName = null, string permissions = null,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            this.Clear();

            this.shell.AddCommand("New-AzureStorageFileSASToken");
            this.shell.BindParameter("File", file);

            this.AddSASTokenParameter(policyName, permissions, startTime, expiryTime, fulluri);

            return InvokePowerShellWithoutContext(this.shell);
        }

        private void AddSASTokenParameter(string policyName, string permissions,
            DateTime? startTime, DateTime? expiryTime, bool fulluri)
        {
            if (null != policyName)
            {
                this.shell.BindParameter("Policy", policyName);
            }

            if (null != permissions)
            {
                this.shell.BindParameter("Permission", permissions);
            }

            if (startTime.HasValue)
            {
                this.shell.BindParameter("StartTime", startTime.Value);
            }

            if (expiryTime.HasValue)
            {
                this.shell.BindParameter("ExpiryTime", expiryTime.Value);
            }

            this.shell.BindParameter("FullUri", fulluri);
        }

        public override string GetAzureStorageFileSasFromCmd(string shareName, string filePath, string policy, string permission = null,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            Test.Assert(NewAzureStorageFileSAS(shareName, filePath, policy, permission, startTime, expiryTime, fulluri, protocol, iPAddressOrRange),
                    "Generate file sas token should succeed");

            return this.PassSASToken();
        }

        public override string GetAzureStorageShareSasFromCmd(string shareName, string policyName, string permissions = null,
            DateTime? startTime = null, DateTime? expiryTime = null, bool fulluri = false, SharedAccessProtocol? protocol = null, string iPAddressOrRange = null)
        {
            Test.Assert(NewAzureStorageShareSAS(shareName, policyName, permissions, startTime, expiryTime, fulluri, protocol, iPAddressOrRange),
                    "Generate share sas token should succeed");

            return this.PassSASToken();
        }

        private string PassSASToken()
        {
            if (Output.Count != 0)
            {
                string sasToken = Output[0][BaseObject].ToString();
                Test.Info("Generated sas token: {0}", sasToken);
                return sasToken;
            }
            else
            {
                throw new InvalidOperationException("Fail to generate sas token.");
            }
        }

        public override bool SetAzureStorageShareQuota(string shareName, int quota)
        {
            this.Clear();

            this.shell.AddCommand("Set-AzureStorageShareQuota");
            this.shell.BindParameter("ShareName", shareName);
            this.shell.BindParameter("Quota", quota);

            return InvokeStoragePowerShell(this.shell);
        }

        public override bool SetAzureStorageShareQuota(CloudFileShare share, int quota)
        {
            this.Clear();

            this.shell.AddCommand("Set-AzureStorageShareQuota");
            this.shell.BindParameter("Share", share);
            this.shell.BindParameter("Quota", quota);

            return InvokePowerShellWithoutContext(this.shell);
        }

        public override IExecutionResult Invoke(IEnumerable input = null, bool traceCommand = true)
        {
            if (traceCommand)
            {
                Test.Info("About to invoke powershell command: {0}", PowerShellAgent.GetCommandLine(this.shell));
            }

            try
            {
                var result = input == null ? this.shell.Invoke() : this.shell.Invoke(input);
                if (this.shell.HadErrors)
                {
                    foreach (var record in this.shell.Streams.Error)
                    {
                        Test.Info(record.ToString());
                    }
                }

                return new PowerShellExecutionResult(result);
            }
            catch (Exception ex)
            {
                ParseErrorMessages(this.shell, ex);
                return null;
            }
        }

        public override void AssertNoError()
        {
            Test.Assert(!this.shell.HadErrors, "Should execute command without error.");
        }

        public override void AssertErrors(Action<IExecutionError> assertErrorAction, int expectedErrorCount = 1)
        {
            if (null != _RuntimeException)
            {
                assertErrorAction(new PowerShellExecutionError(new ErrorRecord(_RuntimeException, _RuntimeException.GetType().ToString(), ErrorCategory.InvalidOperation, null)));
                expectedErrorCount--;
            }

            Test.Assert(this.shell.Streams.Error.Count == expectedErrorCount, "Expected {0} error records while there's {1}.", expectedErrorCount, this.shell.Streams.Error.Count);
            foreach (var errorRecord in this.shell.Streams.Error)
            {
                assertErrorAction(new PowerShellExecutionError(errorRecord));
            }
        }

        public override void Clear()
        {
            this.shell.Streams.ClearStreams();
            this.shell.Commands.Clear();
        }

        protected override void DisposeInternal()
        {
            try
            {
                this.shell.Dispose();
            }
            catch
            {

            }
        }

        #endregion

        public override bool ChangeCLIMode(Constants.Mode mode)
        {
            throw new NotImplementedException();
        }
        public override bool Login()
        {
            string password = Test.Data.Get("AADPassword");
            string subscriptionId = Test.Data.Get("AzureSubscriptionID");

            SecureString securePassword = null;

            unsafe
            {
                fixed (char* chPassword = password.ToCharArray())
                {
                    securePassword = new SecureString(chPassword, password.Length);
                }
            }

            PSCredential psCredential = new PSCredential(Test.Data.Get("AADClient"), securePassword);

            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Login-AzureRmAccount");
            ps.BindParameter("Credential", psCredential);
            ps.BindParameter("ServicePrincipal");
            ps.BindParameter("Tenant", Test.Data.Get("AADRealm"));
            ps.BindParameter("SubscriptionId", subscriptionId);

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            ps.Invoke();
            ParseErrorMessages(ps);

            return !ps.HadErrors;
        }

        public override void Logout()
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddScript("Logout-AzureRmAccount");
            ps.Invoke();
        }

        public override bool ShowAzureStorageAccountConnectionString(string accountName, string resourceGroupName = null)
        {
            throw new NotImplementedException();
        }

        public override bool ShowAzureStorageAccountKeys(string accountName)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Get-AzureStorageKey");
            ps.BindParameter("StorageAccountName", accountName);

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            ParseCollection(ps.Invoke());
            ParseErrorMessages(ps);

            return !ps.HadErrors;
        }

        public override bool RenewAzureStorageAccountKeys(string accountName, Constants.AccountKeyType type)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("New-AzureStorageKey");
            ps.BindParameter("StorageAccountName", accountName);

            if (type == Constants.AccountKeyType.Primary)
            {
                ps.BindParameter("KeyType", "Primary");
            }
            else if (type == Constants.AccountKeyType.Secondary)
            {
                ps.BindParameter("KeyType", "Secondary");
            }
            else
            {
                ps.BindParameter("KeyType", "Invalid");
            }

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            try
            {
                ParseCollection(ps.Invoke());
            }
            catch (System.Management.Automation.ParameterBindingException)
            {
                return false;
            }

            ParseErrorMessages(ps);

            return !ps.HadErrors;
        }

        public override bool CreateAzureStorageAccount(string accountName, string subscription, string label, string description, string location, string affinityGroup, string type, bool? geoReplication = null)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("New-AzureStorageAccount");
            ps.BindParameter("StorageAccountName", accountName);
            ps.BindParameter("Type", type);

            if (null != description)
            {
                ps.BindParameter("Description", description);
            }

            if (null != label)
            {
                ps.BindParameter("Label", label);
            }

            if (null != location)
            {
                ps.BindParameter("Location", location);
            }

            if (null != affinityGroup)
            {
                ps.BindParameter("AffinityGroup", affinityGroup);
            }

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            ParseCollection(ps.Invoke());
            ParseErrorMessages(ps);

            return !ps.HadErrors;
        }

        public override bool SetAzureStorageAccount(string accountName, string label, string description, string type, bool? geoReplication = null)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Set-AzureStorageAccount");
            ps.BindParameter("StorageAccountName", accountName);

            if (null != type)
            {
                ps.BindParameter("Type", type);
            }

            if (null != description)
            {
                ps.BindParameter("Description", description);
            }

            if (null != label)
            {
                ps.BindParameter("Label", label);
            }

            if (null != geoReplication)
            {
                ps.BindParameter("GeoReplicationEnabled", geoReplication.Value.ToString());
            }

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            ParseCollection(ps.Invoke());
            ParseErrorMessages(ps);

            return !ps.HadErrors;
        }

        public override bool DeleteAzureStorageAccount(string accountName)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Remove-AzureStorageAccount");
            ps.BindParameter("StorageAccountName", accountName);

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            ParseCollection(ps.Invoke());
            ParseErrorMessages(ps);

            return !ps.HadErrors;
        }

        public override bool ShowAzureStorageAccount(string accountName)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Get-AzureStorageAccount");

            if (!string.IsNullOrEmpty(accountName))
            {
                ps.BindParameter("StorageAccountName", accountName);
            }

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            ParseCollection(ps.Invoke());
            ParseErrorMessages(ps);

            return !ps.HadErrors;
        }

        public override bool CreateSRPAzureStorageAccount(string resourceGroupName,
            string accountName,
            string skuName,
            string location,
            Hashtable[] tags = null,
            Kind? kind = null,
            Constants.EncryptionSupportServiceEnum? enableEncryptionService = null,
            AccessTier? accessTier = null,
            string customDomain = null,
            bool? useSubdomain = null,
            bool? enableHttpsTrafficOnly = null,
            bool AssignIdentity = false, 
            PSNetworkRuleSet networkAcl = null)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("New-AzureRmStorageAccount");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("Name", accountName);
            if (new Random().Next() % 2 == 0)
            {
                ps.BindParameter("SkuName", skuName);
            }
            else
            {
                ps.BindParameter("Type", skuName);
            }
            ps.BindParameter("Location", location);
            ps.BindParameter("Kind", kind);
            ps.BindParameter("Tags", (tags == null || tags.Length == 0) ? null : tags[0]);
            ps.BindParameter("AccessTier", accessTier);
            //ps.BindParameter("EnableEncryptionService", enableEncryptionService);
            if (enableHttpsTrafficOnly != null)
            {
                ps.BindParameter("EnableHttpsTrafficOnly", enableHttpsTrafficOnly.Value);
            }
            ps.BindParameter("CustomDomainName", customDomain);
            ps.BindParameter("UseSubdomain", useSubdomain);
            if (AssignIdentity)
            {
                ps.AddParameter("AssignIdentity");
            }
            ps.BindParameter("NetworkRuleSet", networkAcl);

            Test.Info(CmdletLogFormat, MethodBase.GetCurrentMethod().Name, GetCommandLine(ps));

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool SetSRPAzureStorageAccount(string resourceGroupName,
            string accountName,
            string skuName = null,
            Hashtable[] tags = null,
            Constants.EncryptionSupportServiceEnum? enableEncryptionService = null,
            Constants.EncryptionSupportServiceEnum? disableEncryptionService = null,
            AccessTier? accessTier = null,
            string customDomain = null,
            bool? useSubdomain = null,
            bool? enableHttpsTrafficOnly = null,
            bool AssignIdentity = false,
            bool StorageEncryption = false,
            PSNetworkRuleSet networkAcl = null,
            Kind? kind = null)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Set-AzureRmStorageAccount");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("Name", accountName);
            if (new Random().Next() % 2 == 0)
            {
                ps.BindParameter("SkuName", skuName);
            }
            else
            {
                ps.BindParameter("Type", skuName);
            }
            ps.BindParameter("AccessTier", accessTier);
            //ps.BindParameter("EnableEncryptionService", enableEncryptionService);
            //ps.BindParameter("DisableEncryptionService", disableEncryptionService);
            ps.BindParameter("Tags", (tags == null || tags.Length == 0) ? null : tags[0]);
            if (customDomain != null)
            {
                ps.AddParameter("CustomDomainName", customDomain);
            }
            ps.BindParameter("UseSubDomain", useSubdomain);
            if (enableHttpsTrafficOnly != null)
            {
                ps.AddParameter("EnableHttpsTrafficOnly", enableHttpsTrafficOnly.Value);
            }
            else
            {
                Test.Info("EnableHttpsTrafficOnly is null.");
            }
            ps.AddParameter("Force");
            if (AssignIdentity)
            {
                ps.AddParameter("AssignIdentity");
            }
            if (StorageEncryption)
            {
                ps.AddParameter("StorageEncryption");
            }
            if (kind == Kind.StorageV2)
            {
                ps.AddParameter("UpgradeToStorageV2");
            }
            ps.BindParameter("NetworkRuleSet", networkAcl);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool SetSRPAzureStorageAccountKeyVault(string resourceGroupName,
            string accountName,
            string skuName = null,
            Hashtable[] tags = null,
            Constants.EncryptionSupportServiceEnum? enableEncryptionService = null,
            Constants.EncryptionSupportServiceEnum? disableEncryptionService = null,
            AccessTier? accessTier = null,
            string customDomain = null,
            bool? useSubdomain = null,
            bool? enableHttpsTrafficOnly = null,
            bool AssignIdentity = false,
            bool keyvaultEncryption = false,
            string keyName = null,
            string keyVersion = null,
            string keyVaultUri = null, 
            PSNetworkRuleSet networkAcl = null)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Set-AzureRmStorageAccount");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("Name", accountName);
            if (new Random().Next() % 2 == 0)
            {
                ps.BindParameter("SkuName", skuName);
            }
            else
            {
                ps.BindParameter("Type", skuName);
            }
            ps.BindParameter("AccessTier", accessTier);
            //ps.BindParameter("EnableEncryptionService", enableEncryptionService);
            //ps.BindParameter("DisableEncryptionService", disableEncryptionService);
            ps.BindParameter("Tags", (tags == null || tags.Length == 0) ? null : tags[0]);
            if (customDomain != null)
            {
                ps.AddParameter("CustomDomainName", customDomain);
            }
            ps.BindParameter("UseSubDomain", useSubdomain);
            if (enableHttpsTrafficOnly != null)
            {
                ps.AddParameter("EnableHttpsTrafficOnly", enableHttpsTrafficOnly.Value);
            }
            else
            {
                Test.Info("EnableHttpsTrafficOnly is null.");
            }
            ps.AddParameter("Force");
            if (AssignIdentity)
            {
                ps.AddParameter("AssignIdentity");
            }
            if (keyvaultEncryption)
            {
                ps.AddParameter("KeyvaultEncryption");
            }
            if (keyName != null)
            {
                ps.BindParameter("KeyName", keyName);
            }
            if (keyVersion != null)
            {
                ps.BindParameter("KeyVersion", keyVersion);
            }
            if (keyVaultUri != null)
            {
                ps.BindParameter("keyVaultUri", keyVaultUri);
            }
            ps.BindParameter("NetworkRuleSet", networkAcl);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool SetSRPAzureStorageAccountTags(string resourceGroupName, string accountName, Hashtable[] tags)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Set-AzureRmStorageAccount");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("Name", accountName);
            ps.BindParameter("Tags", (tags == null || tags.Length == 0) ? null : tags[0]);
            ps.AddParameter("Force");

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool SetSRPAzureStorageAccountCustomDomain(string resourceGroupName, string accountName, string customDomain, bool? useSubdomain)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Set-AzureRmStorageAccount");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("Name", accountName);
            ps.BindParameter("CustomDomainName", customDomain, true);
            ps.BindParameter("UseSubDomain", useSubdomain);
            ps.AddParameter("Force");

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool DeleteSRPAzureStorageAccount(string resourceGroup, string accountName)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Remove-AzureRmStorageAccount");
            ps.BindParameter("ResourceGroupName", resourceGroup);
            ps.BindParameter("Name", accountName);
            ps.AddParameter("Force");

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool ShowSRPAzureStorageAccount(string resourceGroup, string accountName)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Get-AzureRmStorageAccount");
            ps.BindParameter("ResourceGroupName", resourceGroup);
            ps.BindParameter("Name", accountName);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool ShowSRPAzureStorageAccountKeys(string resourceGroup, string accountName)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Get-AzureRmStorageAccountKey");
            ps.BindParameter("ResourceGroupName", resourceGroup);
            ps.BindParameter("Name", accountName);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool RenewSRPAzureStorageAccountKeys(string resourceGroup, string accountName, Constants.AccountKeyType type)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("New-AzureRmStorageAccountKey");
            ps.BindParameter("ResourceGroupName", resourceGroup);
            ps.BindParameter("Name", accountName);

            if (Constants.AccountKeyType.Primary == type)
            {
                ps.BindParameter("KeyName", "key1");
            }
            else if (Constants.AccountKeyType.Secondary == type)
            {
                ps.BindParameter("KeyName", "key2");
            }
            else
            {
                ps.BindParameter("KeyName", "invalid");
            }

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool CheckNameAvailability(string accountName)
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Get-AzureRMStorageAccountNameAvailability");
            ps.BindParameter("Name", accountName, true);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool GetAzureStorageUsage()
        {
            PowerShell ps = GetPowerShellInstance();
            ps.AddCommand("Get-AzureRMStorageUsage");

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool UpdateSRPAzureStorageAccountNetworkAcl(string resourceGroupName, 
            string accountName, 
            PSNetWorkRuleBypassEnum? bypass = null,
            PSNetWorkRuleDefaultActionEnum? defaultAction = null, 
            PSIpRule[] ipRules = null, 
            PSVirtualNetworkRule[] networkRules = null)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Update-AzureRmStorageAccountNetworkRuleSet");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("Name", accountName);

            if (bypass != null)
            {
                ps.BindParameter("Bypass", bypass.Value);
            }
            if (defaultAction != null)
            {
                ps.BindParameter("DefaultAction", defaultAction.Value);
            }
            if (ipRules != null)
            {
                if (ipRules.Length == 0 && new Random().Next()%2 == 0)
                {
                    ps.BindParameter("IpRule", null, true);
                }
                else
                {
                    ps.BindParameter("IpRule", ipRules);
                }
            }
            if (networkRules != null)
            {
                if (networkRules.Length == 0 && new Random().Next() % 2 == 0)
                {
                    ps.BindParameter("VirtualNetworkRule", null, true);
                }
                else
                {
                    ps.BindParameter("VirtualNetworkRule", networkRules);
                }
            }

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool GetSRPAzureStorageAccountNetworkAcl(string resourceGroupName, string accountName)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Get-AzureRmStorageAccountNetworkRuleSet");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("Name", accountName);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool AddSRPAzureStorageAccountNetworkAclRule(string resourceGroupName, string accountName, string[] ruleId, bool isIPRule = true)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Add-AzureRmStorageAccountNetworkRule");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("Name", accountName);

            if (isIPRule)
            {
                ps.BindParameter("IPAddressOrRange", ruleId);
            }
            else
            {
                ps.BindParameter("VirtualNetworkResourceId", ruleId);
            }       

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool AddSRPAzureStorageAccountNetworkAclRule(string resourceGroupName, string accountName, PSIpRule[] iprule)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Add-AzureRmStorageAccountNetworkRule");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("Name", accountName);

            ps.BindParameter("IpRule", iprule);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool AddSRPAzureStorageAccountNetworkAclRule(string resourceGroupName, string accountName, PSVirtualNetworkRule[] networkRule)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Add-AzureRmStorageAccountNetworkRule");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("Name", accountName);

            ps.BindParameter("VirtualNetworkRule", networkRule);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool RemoveSRPAzureStorageAccountNetworkAclRule(string resourceGroupName, string accountName, string[] ruleId, bool isIPRule = true)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Remove-AzureRmStorageAccountNetworkRule");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("Name", accountName);

            if (isIPRule)
            {
                ps.BindParameter("IPAddressOrRange", ruleId);
            }
            else
            {
                ps.BindParameter("VirtualNetworkResourceId", ruleId);
            }

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool RemoveSRPAzureStorageAccountNetworkAclRule(string resourceGroupName, string accountName, PSIpRule[] iprule)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Remove-AzureRmStorageAccountNetworkRule");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("Name", accountName);

            ps.BindParameter("IpRule", iprule);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool RemoveSRPAzureStorageAccountNetworkAclRule(string resourceGroupName, string accountName, PSVirtualNetworkRule[] networkRule)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Remove-AzureRmStorageAccountNetworkRule");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("Name", accountName);

            ps.BindParameter("VirtualNetworkRule", networkRule);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool NewAzureRmStorageContainer(string resourceGroupName, string accountName, string name, Hashtable Metadata = null, PSPublicAccess? PublicAccess = null)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("New-AzureRmStorageContainer");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("StorageAccountName", accountName);
            ps.BindParameter("Name", name);
            ps.BindParameter("Metadata", Metadata);
            ps.BindParameter("PublicAccess", PublicAccess);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool UpdateAzureRmStorageContainer(string resourceGroupName, string accountName, string name, Hashtable Metadata = null, PSPublicAccess? PublicAccess = null)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Update-AzureRmStorageContainer");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("StorageAccountName", accountName);
            ps.BindParameter("Name", name);
            ps.BindParameter("Metadata", Metadata);
            ps.BindParameter("PublicAccess", PublicAccess);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool GetAzureRmStorageContainer(string resourceGroupName, string accountName, string name = null)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Get-AzureRmStorageContainer");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("StorageAccountName", accountName);
            ps.BindParameter("Name", name);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool RemoveAzureRmStorageContainer(string resourceGroupName, string accountName, string name)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Remove-AzureRmStorageContainer");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("StorageAccountName", accountName);
            ps.BindParameter("Name", name);
            ps.AddParameter("Force");

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool AddAzureRmStorageContainerLegalHold(string resourceGroupName, string accountName, string name, string[] tag)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Add-AzureRmStorageContainerLegalHold");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("StorageAccountName", accountName);
            ps.BindParameter("Name", name);
            ps.BindParameter("Tag", tag);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool RemoveAzureRmStorageContainerLegalHold(string resourceGroupName, string accountName, string name, string[] tag)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Remove-AzureRmStorageContainerLegalHold");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("StorageAccountName", accountName);
            ps.BindParameter("Name", name);
            ps.BindParameter("Tag", tag);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool GetAzureRmStorageContainerImmutabilityPolicy(string resourceGroupName, string accountName, string containerName)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Get-AzureRmStorageContainerImmutabilityPolicy");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("StorageAccountName", accountName);
            ps.BindParameter("ContainerName", containerName);

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool SetAzureRmStorageContainerImmutabilityPolicy(string resourceGroupName, string accountName, string containerName, int immutabilityPeriod, bool extendPolicy = false, string Etag = null)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Set-AzureRmStorageContainerImmutabilityPolicy");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("StorageAccountName", accountName);
            ps.BindParameter("ContainerName", containerName);
            ps.BindParameter("ImmutabilityPeriod", immutabilityPeriod);
            ps.BindParameter("Etag", Etag);
            if (extendPolicy)
            {
                ps.BindParameter("ExtendPolicy");
            }

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool LockAzureRmStorageContainerImmutabilityPolicy(string resourceGroupName, string accountName, string containerName, string Etag)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Lock-AzureRmStorageContainerImmutabilityPolicy");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("StorageAccountName", accountName);
            ps.BindParameter("ContainerName", containerName);
            ps.BindParameter("Etag", Etag);
            ps.AddParameter("Force");

            return InvokePowerShellWithoutContext(ps);
        }

        public override bool RemoveAzureRmStorageContainerImmutabilityPolicy(string resourceGroupName, string accountName, string containerName, string Etag)
        {
            PowerShell ps = GetPowerShellInstance();
            AttachPipeline(ps);
            ps.AddCommand("Remove-AzureRmStorageContainerImmutabilityPolicy");
            ps.BindParameter("ResourceGroupName", resourceGroupName);
            ps.BindParameter("StorageAccountName", accountName);
            ps.BindParameter("ContainerName", containerName);
            ps.BindParameter("Etag", Etag);

            return InvokePowerShellWithoutContext(ps);
        }
    }
}
