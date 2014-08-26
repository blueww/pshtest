// ----------------------------------------------------------------------------------
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

namespace Management.Storage.ScenarioTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;
    using System.Text;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage;
    using MS.Test.Common.MsTestLib;

    public sealed class PowerShellExecutionError : IExecutionError
    {
        public PowerShellExecutionError(ErrorRecord error)
        {
            this.ErrorRecord = error;
        }

        public ErrorRecord ErrorRecord
        {
            get;
            private set;
        }

        public void AssertError(params string[] errorIds)
        {
            var record = this.ErrorRecord;
            string errorCode = record.FullyQualifiedErrorId;
            if (record.FullyQualifiedErrorId.StartsWith("StorageException"))
            {
                var exception = (StorageException)record.Exception;
                if (exception.RequestInformation != null && exception.RequestInformation.ExtendedErrorInformation != null)
                {
                    errorCode = exception.RequestInformation.ExtendedErrorInformation.ErrorCode;
                }
            }
            else if (record.FullyQualifiedErrorId.StartsWith("DirectoryNotFoundException") ||
                     record.FullyQualifiedErrorId.StartsWith("FileNotFoundException"))
            {
                errorCode = AssertUtil.PathNotFoundFullQualifiedErrorId;
            }
            else if (record.FullyQualifiedErrorId.StartsWith("ArgumentException"))
            {
                errorCode = AssertUtil.InvalidArgumentFullQualifiedErrorId;
            }

            foreach (var errorId in errorIds)
            {
                foreach (var err in errorId.Split('+'))
                {
                    bool assertResult = errorCode.StartsWith(err, StringComparison.Ordinal);
                    if (assertResult)
                    {
                        return;
                    }
                }
            }

            Test.Assert(false, "Expecting error id {0} while getting {1}.", string.Join(",", errorIds), errorCode);
            throw new AssertFailedException();
        }
    }
}
