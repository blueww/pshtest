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
    using System.Text.RegularExpressions;
    using Management.Storage.ScenarioTest.Util;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage;
    using MS.Test.Common.MsTestLib;

    public sealed class NodeJSExecutionError : IExecutionError
    {
        private static readonly Dictionary<string, string> ErrorCodeTranslateDictionary = new Dictionary<string, string>()
        {
            {"ShareBeingDeleted","The specified share is being deleted."},
            {"InvalidArgument","Share name format is incorrect+Share name must be between 3 and 63 characters long.+The specifed resource name contains invalid characters.+BadRequest"},
            {"ShareAlreadyExists","The specified share already exists."},
            {"NameResolutionFailure","getaddrinfo ENOTFOUND"},
            {"AuthenticationFailed","Server failed to authenticate the request.+Forbidden"},
            {"ShareNotFound","The specified share does not exist.+@Share \\w{1,} doesn't exist"},
            {"PathNotFound","@File '[^']{1,}' in share \\w{1,} does not exist+@Local file .{1,} doesn't exist"},
            {"TransferException","ENOENT"},
            {"ResourceAlreadyExists","The specified resource already exists."},
            {"ResourceNotFound","Can not find directory+Can not find file"},
            {"DirectoryNotEmpty","The specified directory is not empty."},
            {"ParentNotFound","@Path '[^']{1,}' is neither an existing file nor under an existing directory+The specified parent path does not exist"},
            {"InvalidResource","Cannot delete root directory. A path to a subdirectory is mandatory"}
        };

        public NodeJSExecutionError(string errorMessage)
        {
            this.ErrorMessage = errorMessage;
        }

        public string ErrorMessage
        {
            get;
            private set;
        }

        public void AssertError(params string[] errorIds)
        {
            foreach (var errorIdCombination in errorIds)
            {
                foreach (var errorId in errorIdCombination.Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string errorMessage;
                    if (!ErrorCodeTranslateDictionary.TryGetValue(errorId, out errorMessage))
                    {
                        errorMessage = errorId;
                    }

                    foreach (var err in errorMessage.Split('+'))
                    {
                        if (err.Length == 0)
                        {
                            continue;
                        }

                        if (err[0] == '@')
                        {
                            if (Regex.Match(this.ErrorMessage, err.Substring(1)).Success)
                            {
                                return;
                            }
                        }
                        else
                        {
                            if (this.ErrorMessage.StartsWith(err, StringComparison.Ordinal))
                            {
                                return;
                            }
                        }
                    }
                }
            }

            Test.Assert(false, "Expecting error id {0} while getting {1}.", string.Join(",", errorIds), this.ErrorMessage);
            throw new AssertFailedException();
        }
    }
}
