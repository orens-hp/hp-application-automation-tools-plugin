// (c) Copyright 2012 Hewlett-Packard Development Company, L.P. 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using Microsoft.Win32;
using System.Xml.Linq;
using System.Collections.Specialized;
using HpToolsLauncher.Properties;
using System.Runtime.InteropServices;

namespace HpToolsLauncher
{
    public enum TestType
    {
        QTP,
        ST,
    }

    public enum TestState
    {
        Waiting,
        Running,
        NoRun,
        Passed,
        Failed,
        Error,
        Warning,
        Unknown,
    }

    public enum TestResult
    {
        Passed,
        Failed,
        Done,
    }

    public static class Helper
    {
        #region Constants
        public const string FT_REG_ROOT = @"SOFTWARE\Mercury Interactive\QuickTest Professional\CurrentVersion";
        internal const string FT_REG_ROOT_64_BIT = @"SOFTWARE\Wow6432Node\Mercury Interactive\QuickTest Professional\CurrentVersion";
        public const string FT_ROOT_PATH_KEY = "QuickTest Professional";
        private const string QTP_ROOT_ENV_VAR_NAME = "QTP_TESTS_ROOT";


        public const string ServiceTestRegistryKey = @"SOFTWARE\Hewlett-Packard\HP Service Test";
        public const string ServiceTesCurrentVersionRegistryKey = ServiceTestRegistryKey + @"\CurrentVersion";

        public const string ServiceTesWOW64RegistryKey = @"SOFTWARE\Wow6432Node\Hewlett-Packard\HP Service Test";
        public const string ServiceTesCurrentVersionWOW64RegistryKey = ServiceTesWOW64RegistryKey + @"\CurrentVersion";

        public const string LoadRunnerRegistryKey = @"SOFTWARE\Mercury Interactive\LoadRunner";
        public const string LoadRunner64RegisryKey = @"SOFTWARE\Wo6432Node\Mercury Interactive\LoadRunner";

        public const string LoadRunnerControllerRegistryKey = @"\CustComponent\Controller\CurrentVersion";

        public const string LoadRunnerControllerDirRegistryKey = @"\CurrentVersion";

        public const string LoadRunnerControllerDirRegistryValue = @"\Controller";


        public const string InstalltionFolderValue = "LOCAL_MLROOT";

        public const string UftViewerInstalltionFolderRegistryKey =
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{E86D56AE-6660-4357-890F-8B79AB7A8F7B}";
        public const string UftViewerInstalltionFolderRegistryKey64Bit =
            @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{E86D56AE-6660-4357-890F-8B79AB7A8F7B}";


        public const string ResultsFileName = "Results.xml";
        public const string QTPReportProcessPath = @"bin\reportviewer.exe";

        #endregion
        public static string GetRootDirectoryPath()
        {
            String directoryPath;
            RegistryKey regkey = Registry.LocalMachine.OpenSubKey(FT_REG_ROOT);

            if (regkey != null)
                directoryPath = (string)regkey.GetValue(FT_ROOT_PATH_KEY);
            else
            {//TRY 64 bit REG path
                regkey = Registry.LocalMachine.OpenSubKey(FT_REG_ROOT_64_BIT);
                if (regkey != null)
                    directoryPath = (string)regkey.GetValue(FT_ROOT_PATH_KEY);
                else
                    directoryPath = GetRootFromEnvironment();
            }

            return directoryPath;
        }
        public static bool IsTestingToolsInstalled(TestStorageType type)
        {
            //we want to check if we have Service Test, QTP installed on a machine
            RegistryKey regkey;
            string value = null;
            switch (type)
            {

                case TestStorageType.FileSystem:
                    //search for QTP
                    if (IsQtpInstalled()) return true;

                    //search for Service Test
                    if (IsServiceTestInstalled()) return true;
                    break;
                case TestStorageType.LoadRunner:

                    //search for LoadRunner
                    regkey = Registry.LocalMachine.OpenSubKey(LoadRunnerRegistryKey);
                    if (regkey == null)
                    {
                        //try 64 bit
                        regkey = Registry.LocalMachine.OpenSubKey(LoadRunner64RegisryKey);
                    }

                    if (regkey != null)
                    {
                        //LoadRunner Exist.
                        //check if Controller is installed (not SA version)
                        value = (string)regkey.GetValue(LoadRunnerControllerRegistryKey);
                        if (!string.IsNullOrEmpty(value))
                        {
                            return true;
                        }
                    }
                    break;

                default:
                    return false;

            }
            return false;

        }

        public static bool IsQtpInstalled()
        {
            RegistryKey regkey;
            string value;
            regkey = Registry.LocalMachine.OpenSubKey(FT_REG_ROOT);
            if (regkey == null)
            {
                //try 64 bit
                regkey = Registry.LocalMachine.OpenSubKey(FT_REG_ROOT_64_BIT);
            }

            if (regkey != null)
            {
                value = (string)regkey.GetValue(FT_ROOT_PATH_KEY);
                if (!string.IsNullOrEmpty(value))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsServiceTestInstalled()
        {
            RegistryKey regkey;
            string value;
            regkey = Registry.LocalMachine.OpenSubKey(ServiceTesCurrentVersionRegistryKey);
            if (regkey == null)
            {
                //try 64 bit
                regkey = Registry.LocalMachine.OpenSubKey(ServiceTesCurrentVersionWOW64RegistryKey);
            }

            if (regkey != null)
            {
                value = (string)regkey.GetValue(InstalltionFolderValue);
                if (!string.IsNullOrEmpty(value))
                {
                    return true;
                }
            }
            return false;
        }

        private static string GetRootFromEnvironment()
        {
            string qtpRoot = Environment.GetEnvironmentVariable(QTP_ROOT_ENV_VAR_NAME, EnvironmentVariableTarget.Process);

            if (string.IsNullOrEmpty(qtpRoot))
            {
                qtpRoot = Environment.GetEnvironmentVariable(QTP_ROOT_ENV_VAR_NAME, EnvironmentVariableTarget.User);

                if (string.IsNullOrEmpty(qtpRoot))
                {
                    qtpRoot = Environment.GetEnvironmentVariable(QTP_ROOT_ENV_VAR_NAME, EnvironmentVariableTarget.Machine);

                    if (string.IsNullOrEmpty(qtpRoot))
                    {
                        qtpRoot = Environment.CurrentDirectory;
                    }
                }
            }

            return qtpRoot;
        }

        public static string GetInstallPath()
        {
            string ret = String.Empty;
            var regKey = Registry.LocalMachine.OpenSubKey(ServiceTesCurrentVersionRegistryKey);
            if (regKey != null)
            {
                var val = regKey.GetValue(InstalltionFolderValue);
                if (null != val)
                {
                    ret = val.ToString();
                }
            }
            else
            {
                regKey = Registry.LocalMachine.OpenSubKey(ServiceTesCurrentVersionWOW64RegistryKey);
                if (regKey != null)
                {
                    var val = regKey.GetValue(InstalltionFolderValue);
                    if (null != val)
                    {
                        ret = val.ToString();
                    }
                }
                else
                {
                    ret = GetRootDirectoryPath() ?? string.Empty;
                }
            }

            if (!String.IsNullOrEmpty(ret))
            {
                ret = ret.EndsWith("\\") ? ret : (ret + "\\");
                if (ret.EndsWith("\\bin\\"))
                {
                    int endIndex = ret.LastIndexOf("\\bin\\");
                    if (endIndex != -1)
                    {
                        ret = ret.Substring(0, endIndex) + "\\";
                    }
                }
            }

            return ret;
        }

        public static List<string> GetTestsLocations(string baseDir)
        {
            var testsLocations = new List<string>();
            if (string.IsNullOrEmpty(baseDir) || !Directory.Exists(baseDir))
            {
                return testsLocations;
            }

            WalkDirectoryTree(new DirectoryInfo(baseDir), ref testsLocations);
            return testsLocations;
        }

        public static TestType GetTestType(string path)
        {
            var stFiles = Directory.GetFiles(path,
                               @"*.st?",
                               SearchOption.TopDirectoryOnly);

            return (stFiles.Count() > 0) ? TestType.ST : TestType.QTP;
        }

        public static bool IsDirectory(string path)
        {
            var fa = File.GetAttributes(path);
            var isDirectory = false;
            if ((fa & FileAttributes.Directory) != 0)
            {
                isDirectory = true;
            }
            return isDirectory;
        }

        static void WalkDirectoryTree(System.IO.DirectoryInfo root, ref List<string> results)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try
            {
                files = root.GetFiles("*.st");
                files = files.Union(root.GetFiles("*.tsp")).ToArray();
            }
            catch (Exception e)
            {
                // This code just writes out the message and continues to recurse.
                // You may decide to do something different here. For example, you
                // can try to elevate your privileges and access the file again.
                //log.Add(e.Message);
            }

            if (files != null)
            {
                foreach (System.IO.FileInfo fi in files)
                {
                    results.Add(fi.Directory.FullName);
                    // In this example, we only access the existing FileInfo object. If we
                    // want to open, delete or modify the file, then
                    // a try-catch block is required here to handle the case
                    // where the file has been deleted since the call to TraverseTree().
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    // Resursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo, ref results);
                }
            }
        }

        public static string GetTempDir()
        {
            string baseTemp = Path.GetTempPath();

            string dirName = Guid.NewGuid().ToString().Replace("-", string.Empty).Substring(0, 6);
            string tempDirPath = Path.Combine(baseTemp, dirName);

            return tempDirPath;
        }

        public static string CreateTempDir()
        {
            string tempDirPath = GetTempDir();

            Directory.CreateDirectory(tempDirPath);

            return tempDirPath;
        }

        public static bool IsNetworkPath(String path)
        {
            if (path.StartsWith(@"\\"))
                return true;
            var dir = new DirectoryInfo(path);
            var drive = new DriveInfo(dir.Root.ToString());
            return drive.DriveType == DriveType.Network;
        }


        #region Report Related

        public static string GetUftViewerInstallPath()
        {
            string ret = String.Empty;
            var regKey = Registry.LocalMachine.OpenSubKey(UftViewerInstalltionFolderRegistryKey) ??
                         Registry.LocalMachine.OpenSubKey(UftViewerInstalltionFolderRegistryKey64Bit);

            if (regKey != null)
            {
                var val = regKey.GetValue("InstallLocation");
                if (null != val)
                {
                    ret = val.ToString();
                }
            }

            if (!String.IsNullOrEmpty(ret))
            {
                ret = ret.EndsWith("\\") ? ret : (ret + "\\");
            }

            return ret;
        }

        public static string GetLastRunFromReport(string reportPath)
        {
            if (!Directory.Exists(reportPath))
                return null;
            string resultsFileFullPath = reportPath + @"\" + ResultsFileName;
            if (!File.Exists(resultsFileFullPath))
                return null;
            try
            {
                var root = XElement.Load(resultsFileFullPath);

                var element = root.XPathSelectElement("//Doc/Summary");
                var lastStartRun = element.Attribute("sTime");
                return lastStartRun.Value.Split(new char[] { ' ' })[0];

            }
            catch (Exception)
            {
                return null;
            }
        }

        public static TestState GetTestStateFromReport(TestRunResults runDesc)
        {
            try
            {
                if (!Directory.Exists(runDesc.ReportLocation))
                {
                    runDesc.ErrorDesc = string.Format(Resources.DirectoryNotExistError, runDesc.ReportLocation);

                    runDesc.TestState = TestState.Error;
                    return runDesc.TestState;
                }

                string[] resultFiles = Directory.GetFiles(runDesc.ReportLocation, "Results.xml", SearchOption.AllDirectories);
                TestState finalState = TestState.Unknown;

                if (resultFiles == null || resultFiles.Length == 0)
                {
                    runDesc.ErrorDesc = string.Format("no results file found for " + runDesc.TestName);
                    runDesc.TestState = TestState.Error;
                    return runDesc.TestState;
                }

                foreach (string resultsFileFullPath in resultFiles)
                {
                    finalState = TestState.Unknown;
                    string desc = "";
                    TestState state = GetStateFromResultsFile(resultsFileFullPath, out desc);
                    if (finalState == TestState.Unknown || finalState == TestState.Passed)
                    {
                        finalState = state;
                        if (!string.IsNullOrWhiteSpace(desc))
                        {
                            if (finalState == TestState.Error)
                            {
                                runDesc.ErrorDesc = desc;
                            }
                            if (finalState == TestState.Failed)
                            {
                                runDesc.FailureDesc = desc;
                            }
                        }
                    }
                }

                if (finalState == TestState.Unknown)
                    finalState = TestState.Passed;

                if (finalState == TestState.Failed && string.IsNullOrWhiteSpace(runDesc.FailureDesc))
                    runDesc.FailureDesc = "Test failed";

                runDesc.TestState = finalState;

                return runDesc.TestState;
            }
            catch (Exception e)
            {
                return TestState.Unknown;
            }

        }

        private static TestState GetStateFromResultsFile(string resultsFileFullPath, out string desc)
        {
            TestState finalState = TestState.Unknown;
            desc = "";

            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(resultsFileFullPath);
            var testStatusPathNode = doc.SelectSingleNode("//Report/Doc/NodeArgs");
            if (testStatusPathNode == null)
            {
                desc = string.Format(Resources.XmlNodeNotExistError, "//Report/Doc/NodeArgs");
                finalState = TestState.Error;
            }

            if (!testStatusPathNode.Attributes["status"].Specified)
                finalState = TestState.Unknown;

            var status = testStatusPathNode.Attributes["status"].Value;

            var result = (TestResult)Enum.Parse(typeof(TestResult), status);
            if (result == TestResult.Passed || result == TestResult.Done)
            {
                finalState = TestState.Passed;
                //return runDesc.TestState;
            }
            else
                finalState = TestState.Failed;

            return finalState;
        }

        public static string GetUnknownStateReason(string reportPath)
        {
            if (!Directory.Exists(reportPath))
            {
                return string.Format("Directory '{0}' doesn't exist", reportPath);
            }
            string resultsFileFullPath = reportPath + @"\" + ResultsFileName;
            if (!File.Exists(resultsFileFullPath))
            {
                return string.Format("Could not find results file '{0}'", resultsFileFullPath);
            }

            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.Load(resultsFileFullPath);
            var testStatusPathNode = doc.SelectSingleNode("//Report/Doc/NodeArgs");
            if (testStatusPathNode == null)
            {
                return string.Format("XML node '{0}' could not be found", "//Report/Doc/NodeArgs");
            }
            return string.Empty;
        }

        public static void OpenReport(string reportDirectory, ref string optionalReportViewerPath)
        {
            Process p = null;
            try
            {
                string viewerPath = optionalReportViewerPath;
                string reportPath = reportDirectory;
                string resultsFilePath = reportPath + "\\" + ResultsFileName;

                if (!File.Exists(resultsFilePath))
                {
                    return;
                }

                var si = new ProcessStartInfo();
                if (string.IsNullOrEmpty(viewerPath))
                {
                    viewerPath = GetUftViewerInstallPath();
                    optionalReportViewerPath = viewerPath;
                }
                si.Arguments = " -r \"" + reportPath + "\"";

                si.FileName = Path.Combine(viewerPath, QTPReportProcessPath);
                si.WorkingDirectory = Path.Combine(viewerPath, @"bin" + @"\");

                p = Process.Start(si);
                if (p != null)
                {
                }
                return;
            }
            catch (Exception ex)
            {
            }
            finally
            {
                if (p != null)
                {
                    p.Close();
                }
            }
            return;
        }

        #endregion

        #region Export Related

        /// <summary>
        /// Copy directories from source to target
        /// </summary>
        /// <param name="sourceDir">full path source directory</param>
        /// <param name="targetDir">full path target directory</param>
        /// <param name="includeSubDirectories">if true, all subdirectories and contents will be copied</param>
        /// <param name="includeRoot">if true, the source directory will be created too</param>
        public static void CopyDirectories(string sourceDir, string targetDir,
                                    bool includeSubDirectories = false, bool includeRoot = false)
        {
            var source = new DirectoryInfo(sourceDir);
            var target = new DirectoryInfo(targetDir);
            DirectoryInfo workingTarget = target;

            if (includeRoot)
                workingTarget = Directory.CreateDirectory(target.FullName);

            CopyContents(source, workingTarget, includeSubDirectories);
        }

        private static void CopyContents(DirectoryInfo source, DirectoryInfo target, bool includeSubDirectories)
        {
            if (!Directory.Exists(target.FullName))
            {
                Directory.CreateDirectory(target.FullName);
            }


            foreach (FileInfo fi in source.GetFiles())
            {
                string targetFile = Path.Combine(target.ToString(), fi.Name);

                fi.CopyTo(targetFile, true);
            }

            if (includeSubDirectories)
            {
                DirectoryInfo[] subDirectories = source.GetDirectories();
                foreach (DirectoryInfo diSourceSubDir in subDirectories)
                {
                    DirectoryInfo nextTargetSubDir =
                        target.CreateSubdirectory(diSourceSubDir.Name);
                    CopyContents(diSourceSubDir, nextTargetSubDir, true);
                }
            }
        }

        public static void CopyFilesFromFolder(string sourceFolder, IEnumerable<string> fileNames, string targetFolder)
        {
            foreach (var fileName in fileNames)
            {
                var sourceFullPath = Path.Combine(sourceFolder, fileName);
                var targetFullPath = Path.Combine(targetFolder, fileName);
                File.Copy(sourceFullPath, targetFullPath, true);
            }
        }

        /// <summary>
        /// Create html file according to a matching xml
        /// </summary>
        /// <param name="xmlPath">the values xml</param>
        /// <param name="xslPath">the xml transformation file</param>
        /// <param name="targetFile">the full file name - where to save the product</param>
        public static void CreateHtmlFromXslt(string xmlPath, string xslPath, string targetFile)
        {
            var xslTransform = new XslCompiledTransform();
            //xslTransform.Load(xslPath);
            xslTransform.Load(xslPath, new XsltSettings(false, true), null);

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            var xmlWriter = new XhtmlTextWriter(sw);
            xslTransform.Transform(xmlPath, null, xmlWriter);


            File.WriteAllText(targetFile, sb.ToString());
        }

        #endregion
    }
}