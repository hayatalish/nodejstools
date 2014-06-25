﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.NodejsTools.Npm;
using Microsoft.NodejsTools.Npm.SPI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudioTools.Project;
using SR = Microsoft.NodejsTools.Project.SR;

namespace Microsoft.NodejsTools.Repl {
#if INTERACTIVE_WINDOW
    using IReplCommand = IInteractiveWindowCommand;
    using IReplWindow = IInteractiveWindow;    
#endif

    [Export(typeof(IReplCommand))]
    class NpmReplCommand : IReplCommand {
        private string _npmPath;

        #region IReplCommand Members

        public async Task<ExecutionResult> Execute(IReplWindow window, string arguments) {
            string projectPath = string.Empty;
            string npmArguments = arguments.TrimStart(' ', '\t');

            // Parse project name/directory in square brackets
            if (npmArguments.StartsWith("["))
            {
                var match = Regex.Match(npmArguments, @"(?:[[]\s*\""?\s*)(.*?)(?:\s*\""?\s*[]]\s*)");
                projectPath = match.Groups[1].Value;
                npmArguments = npmArguments.Substring(match.Length);
            }

            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            IEnumerable<IVsProject> loadedProjects = solution.EnumerateLoadedProjects(onlyNodeProjects: true);

            var projectNameToDirectoryDictionary = new Dictionary<string, string>();
            object projectName, projectDirectory;
            foreach (var project in loadedProjects) {
                var hierarchy = (IVsHierarchy)project;
                var nameResult = hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_Name, out projectName);
                var projectResult = hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectDir, out projectDirectory);
                var projectNameString = projectName as string;
                var projectDirectoryString = projectDirectory as string;

                if (ErrorHandler.Succeeded(nameResult) && ErrorHandler.Succeeded(projectResult) &&
                    projectNameString != null && projectDirectoryString != null) {
                    projectNameToDirectoryDictionary.Add((string)projectName, (string)projectDirectory);
                }
            }

            if (string.IsNullOrEmpty(projectPath) && projectNameToDirectoryDictionary.Count == 1) {
                projectPath = projectNameToDirectoryDictionary.Values.First();
            } else {
                string directoryFromName;
                if (projectNameToDirectoryDictionary.TryGetValue(projectPath, out directoryFromName)) {
                    projectPath = directoryFromName;
                }
            }

            if (!(Directory.Exists(projectPath) && File.Exists(Path.Combine(projectPath, "package.json")))){
                window.WriteError("Please specify a valid Node.js project or project directory in solution. If solution contains multiple projects, specify target project using .npm [ProjectName or ProjectDir] <npm arguments>");
                return ExecutionResult.Failure;
            }

            if (null == _npmPath || !File.Exists(_npmPath)) {
                _npmPath = NpmHelpers.GetPathToNpm();
            }
            var npmReplRedirector = new NpmReplRedirector(window);
            using (var process = ProcessOutput.Run(
                    _npmPath,
                    new[] { npmArguments },
                    projectPath,
                    null,
                    false,
                    npmReplRedirector,
                    quoteArgs: false)){
                        await process;
            }

            if (npmReplRedirector.HasErrors) {
                window.WriteError(SR.GetString(SR.NpmCompletedWithErrors, arguments));
            }
            else {
                window.WriteLine(SR.GetString(SR.NpmSuccessfullyCompleted, arguments));
            }

            return ExecutionResult.Success;
        }

        public string Description {
            get { return "Executes npm command. If solution contains multiple projects, specify target project using .npm [ProjectName] <npm arguments>"; }
        }

        public string Command {
            get { return "npm"; }
        }

        public object ButtonContent {
            get { return null; }
        }

        #endregion

        internal class NpmReplRedirector : Redirector {
            
            internal const string ErrorAnsiColor = "\x1b[31;1m";
            internal const string WarnAnsiColor = "\x1b[33;22m";
            internal const string NormalAnsiColor = "\x1b[39;49m";

            private const string ErrorText = "npm ERR!";
            private const string WarningText = "npm WARN";

            private IReplWindow _window;

            public NpmReplRedirector(IReplWindow window) {
                _window = window;
                HasErrors = false;
            }
            public bool HasErrors { get; set; }

            public override void WriteLine(string line) {
                string decodedString = Encoding.UTF8.GetString(Console.OutputEncoding.GetBytes(line));
                var substring = string.Empty;
                string outputString = string.Empty;

                if (decodedString.StartsWith(ErrorText)) {
                    outputString += ErrorAnsiColor + decodedString.Substring(0, ErrorText.Length);
                    substring = decodedString.Length > ErrorText.Length ? decodedString.Substring(ErrorText.Length) : string.Empty;
                    this.HasErrors = true;
                } else if (decodedString.StartsWith(WarningText)) {
                    outputString += WarnAnsiColor + decodedString.Substring(0, WarningText.Length);
                    substring = decodedString.Length > WarningText.Length ? decodedString.Substring(WarningText.Length) : string.Empty;
                } else {
                    substring = decodedString;
                }

                outputString += NormalAnsiColor + substring;

                _window.WriteLine(outputString);
                Debug.WriteLine(decodedString, "REPL npm");
            }

            public override void WriteErrorLine(string line) {
                _window.WriteError(line);
            }
        }
    }
}