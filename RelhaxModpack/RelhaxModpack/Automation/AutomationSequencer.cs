﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using RelhaxModpack.Utilities;
using RelhaxModpack.Utilities.Enums;
using RelhaxModpack.Xml;
using System.IO;
using RelhaxModpack.Database;
using System.Xml.Linq;
using System.Reflection;
using RelhaxModpack.Common;
using RelhaxModpack.Windows;
using RelhaxModpack.Settings;

namespace RelhaxModpack.Automation
{
    public class AutomationSequencer : IDisposable, IComponentWithID
    {
        /// <summary>
        /// The API URL to return a json format document of the current branches in the automation repository
        /// </summary>
        public const string BranchesURL = "https://api.github.com/repos/Relhax-Modpack-Team/DatabaseAutoUpdateScripts/branches";

        public const string AutomationXmlRepoFilebase = "https://raw.githubusercontent.com/Relhax-Modpack-Team/DatabaseAutoUpdateScripts/{branch}/";

        public const string AutomationXmlRoot = AutomationXmlRepoFilebase + "root.xml";

        public List<AutomationMacro> ApplicationMacros { get; } = new List<AutomationMacro>();

        public List<AutomationMacro> GlobalMacros { get; } = new List<AutomationMacro>();

        public List<AutomationSequence> AutomationSequences { get; } = new List<AutomationSequence>();

        public AutomationRunnerSettings AutomationRunnerSettings { get; set; } = null;

        public int NumErrors { get; set; } = 0;

        public string[] AutomationBranches = null;

        public bool AutomationBranchesLoaded = false;

        public AutomationRunMode AutomationRunMode = AutomationRunMode.Interactive;

        public DatabaseAutomationRunner DatabaseAutomationRunner { get; set; } = null;

        private XmlDocument RootDocument = null;

        private XmlDocument GlobalMacrosDocument = null;

        private WebClient WebClient = null;

        public string WoTClientVersion { get; set; }

        public string WoTModpackOnlineFolderVersion { get; set; }

        private string AutomationXmlRootEscaped { get { return AutomationRunnerSettings == null? string.Empty : AutomationXmlRoot.Replace("{branch}", AutomationRunnerSettings.SelectedBranch); } }

        private string AutomationXmlRepoFilebaseEscaped { get { return AutomationRunnerSettings == null ? string.Empty : AutomationXmlRepoFilebase.Replace("{branch}", AutomationRunnerSettings.SelectedBranch); } }

        public string ComponentInternalName { get; } = "AutomationSequencer";

        public AutomationSequencer()
        {
            WebClient = new WebClient();
        }

        /// <summary>
        /// Load the list of branches from github
        /// </summary>
        /// <returns>A task of the asynchronous operation</returns>
        public async Task LoadBranchesListAsync()
        {
            if (AutomationBranchesLoaded)
            {
                Logging.AutomationRunner(LogOptions.MethodName, "Branches list already loaded, ignoring this call", LogLevel.Warning);
                return;
            }

            List<string> branches = await CommonUtils.GetListOfGithubRepoBranchesAsync(BranchesURL);
            AutomationBranches = branches.ToArray();
            AutomationBranchesLoaded = true;
        }

        public async Task LoadRootDocumentAsync()
        {
            if (string.IsNullOrWhiteSpace(AutomationXmlRootEscaped))
                throw new ArgumentException("AutomationXmlRootEscaped cannot be null or whitespace");

            string xmlString = await WebClient.DownloadStringTaskAsync(AutomationXmlRootEscaped);
            RootDocument = XmlUtils.LoadXmlDocument(xmlString, XmlLoadType.FromString);
        }

        public async Task LoadGlobalMacrosAsync()
        {
            if (RootDocument == null)
                throw new NullReferenceException();

            //get the url to download from
            string globalMacrosUrlFile = XmlUtils.GetXmlStringFromXPath(RootDocument, "/root.xml/GlobalMacros/@path");
            string globalMacrosUrl = string.Format("{0}{1}", AutomationXmlRepoFilebaseEscaped, globalMacrosUrlFile);
            string globalMacrosXml = await WebClient.DownloadStringTaskAsync(globalMacrosUrl);
            GlobalMacrosDocument = XmlUtils.LoadXmlDocument(globalMacrosXml, XmlLoadType.FromString);
        }

        public async Task<bool> ParseRootDocumentAsync()
        {
            if (RootDocument == null)
                throw new NullReferenceException();

            Logging.AutomationRunner(LogOptions.MethodName, "Checking root document for to build automation sequences", LogLevel.Info);

            XmlNodeList sequencesXml = XmlUtils.GetXmlNodesFromXPath(RootDocument, "//root.xml/AutomationSequence");

            foreach (XmlElement result in sequencesXml)
            {
                string sequencePackageName = result.Attributes["packageName"].Value;
                string sequenceUID = result.Attributes["UID"].Value;
                string sequenceUrlPath = result.Attributes["path"].Value;
                Logging.AutomationRunner(LogOptions.MethodName, "Parsing sequence for package {0} (UID {1})", LogLevel.Info, sequencePackageName, sequenceUID);
                AutomationSequences.Add(new AutomationSequence() { AutomationSequencer = this, Package = null, PackageName = sequencePackageName, PackageUID = sequenceUID, SequenceDownloadUrl = AutomationXmlRepoFilebaseEscaped + sequenceUrlPath });
            }

            return true;
        }

        public async Task<bool> ParseAutomationSequencesToRunAsync(List<DatabasePackage> packagesToRun)
        {
            if (RootDocument == null)
                throw new NullReferenceException();
            if (packagesToRun == null)
                throw new NullReferenceException();
            if (packagesToRun.Count == 0)
                throw new BadMemeException("packagesToRun must have at least one package to run automation on");

            Logging.AutomationRunner("Parsing sequences for packages");

            foreach(DatabasePackage package in packagesToRun)
            {
                Logging.AutomationRunner("Parsing sequences for packages for package {0} (UID {1})", LogLevel.Info, package.PackageName, package.UID);

                //TODO: modify code to parse from list of parsed sequences

                /*
                Logging.AutomationRunner(LogOptions.MethodName, "Checking root document for to build urls to download automation sequences", LogLevel.Info);
                foreach (DatabasePackage package in packagesToRun)
                {
                    Logging.AutomationRunner(LogOptions.MethodName, "Parsing path for automation of package {0}", LogLevel.Info, package.PackageName);
                    //sample xpath: /root.xml/AutomationSequence[@UID='123456789ABCD']
                    XmlElement result = XmlUtils.GetXmlNodeFromXPath(RootDocument, string.Format("/root.xml/AutomationSequence[@UID='{0}']", package.UID)) as XmlElement;
                    if (result == null)
                    {
                        Logging.Error(Logfiles.AutomationRunner, "Package not found in automation database");
                        return false;
                    }

                    if (result.Attributes["packageName"].Value != package.PackageName)
                    {
                        Logging.Warning(Logfiles.AutomationRunner, "The packageName property is out of date. From database: {0}. From Package: {1}", result.Attributes["packageName"].Value, package.PackageName);
                    }

                    string pathToFileFromRepoRoot = result.Attributes["path"].Value;
                    if (string.IsNullOrEmpty(pathToFileFromRepoRoot))
                    {
                        Logging.Error(Logfiles.AutomationRunner, "Package path attribute not found from xml node");
                        return false;
                    }
                    if (pathToFileFromRepoRoot[0] == '/')
                    {
                        Logging.Warning(Logfiles.AutomationRunner, "Package path attribute starts with slash, please update entry to remove it!");
                        pathToFileFromRepoRoot = pathToFileFromRepoRoot.Substring(1);
                    }
                    AutomationSequences.Add(new AutomationSequence() { AutomationSequencer = this, Package = package, SequenceDownloadUrl = AutomationXmlRepoFilebaseEscaped + pathToFileFromRepoRoot });
                    Logging.Debug(Logfiles.AutomationRunner, "Added automation sequence URL: {0}", AutomationSequences.Last().SequenceDownloadUrl);
                }

                Logging.Info(Logfiles.AutomationRunner, LogOptions.MethodName, "Parsing each automationSequence xml document from its download URL");
                foreach (AutomationSequence automationSequence in AutomationSequences)
                {
                    Logging.Info(Logfiles.AutomationRunner, "Load automation sequence xml for package {0}", automationSequence.Package.PackageName);
                    await automationSequence.LoadAutomationXmlAsync();
                }
                */
            }

            return false;
        }

        public void ResetApplicationMacros(AutomationSequence sequence)
        {
            Logging.Debug(Logfiles.AutomationRunner, LogOptions.ClassName, "Resetting application macros");
            DatabasePackage package = sequence.Package;
            SelectablePackage selectablePackage = package as SelectablePackage;
            ApplicationMacros.Clear();
            ApplicationMacros.Add(new AutomationMacro() { Name = "date", Value = DateTime.UtcNow.ToString("yyyy-MM-dd"), MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "category.name", Value = (selectablePackage != null)? selectablePackage.ParentCategory.Name : "null", MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "name", Value = (selectablePackage != null)? selectablePackage.NameFormatted : "null", MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "packageName", Value = package.PackageName, MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "zipfile", Value = package.ZipFile, MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "level", Value = (selectablePackage != null)? selectablePackage.Level.ToString() : "null", MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "patchGroup", Value = package.PatchGroup.ToString(), MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "installGroup", Value = package.InstallGroupWithOffset.ToString(), MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "clientVersion", Value = WoTClientVersion, MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "onlineFolderVersion", Value = WoTModpackOnlineFolderVersion, MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "author", Value = package.Author, MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "version", Value = package.Version, MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "applicationPath", Value = ApplicationConstants.ApplicationStartupPath, MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "relhaxTemp", Value = ApplicationConstants.RelhaxTempFolderPath, MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "workDirectory", Value = string.Format("{0}\\{1}", ApplicationConstants.RelhaxTempFolderPath, package.PackageName), MacroType = MacroType.ApplicationDefined });
            ApplicationMacros.Add(new AutomationMacro() { Name = "automationRepoRoot", Value = AutomationXmlRepoFilebaseEscaped, MacroType = MacroType.ApplicationDefined });
        }

        public bool ParseGlobalMacros()
        {
            Logging.Info(Logfiles.AutomationRunner, LogOptions.MethodName, "Parsing global macros from xml document to class objects");
            GlobalMacros.Clear();
            XmlElement result = XmlUtils.GetXmlNodeFromXPath(GlobalMacrosDocument, "/GlobalMacros") as XmlElement;
            XElement globalMacrosXmlHolder = XElement.Parse(result.OuterXml);
            PropertyInfo listPropertyInfo = this.GetType().GetProperty(nameof(GlobalMacros));
            try
            {
                CommonUtils.SetListEntries(this, listPropertyInfo, globalMacrosXmlHolder.Elements());
            }
            catch (Exception ex)
            {
                Logging.AutomationRunner(ex.ToString(), LogLevel.Exception);
                return false;
            }
            return true;
        }

        public bool ParseAutomationSequences()
        {
            Logging.Info(Logfiles.AutomationRunner, LogOptions.MethodName, "Parsing each automationSequence from xml to class objects");
            foreach (AutomationSequence automationSequence in AutomationSequences)
            {
                Logging.Info(Logfiles.AutomationRunner, "Load automation sequence data for package {0}", automationSequence.Package.PackageName);
                if  (!automationSequence.ParseAutomationTasks())
                {
                    Logging.Error("Failed to parse sequence {0}, check the syntax and try again", automationSequence.ComponentInternalName);
                    return false;
                }
            }
            return true;
        }

        public async Task<bool> RunSequencesAsync()
        {
            if (AutomationSequences.Count == 0)
            {
                Logging.Warning(Logfiles.AutomationRunner, LogOptions.MethodName, "No sequences specified in AutomationSequences list, is this intended?");
                return true;
            }

            Logging.Info(Logfiles.AutomationRunner, LogOptions.MethodName, "RUNNING AUTOMATION SEQUENCES. Put in caps so you know it's important");
            NumErrors = 0;
            foreach (AutomationSequence sequence in AutomationSequences)
            {
                Logging.Info(Logfiles.AutomationRunner, LogOptions.MethodName, "Preparing macro lists for sequence run: {0}", sequence.ComponentInternalName);
                ResetApplicationMacros(sequence);
                ParseGlobalMacros();
                if (AutomationRunnerSettings.DumpParsedMacrosPerSequenceRun)
                {
                    DumpApplicationMacros();
                    DumpGlobalMacros();
                }

                Logging.Info(Logfiles.AutomationRunner, LogOptions.MethodName, "Running sequence: {0}", sequence.ComponentInternalName);
                bool SequenceResult = await sequence.RunTasksAsync();
                if (!SequenceResult)
                {
                    Logging.Error(Logfiles.AutomationRunner, LogOptions.MethodName, "Error with sequence '{0}'. Check the log above or enable verbose logging for details.", sequence.ComponentInternalName);
                    NumErrors++;
                    continue;
                }
                Logging.Info(Logfiles.AutomationRunner, LogOptions.MethodName, "Finished sequence '{0}.", sequence.ComponentInternalName);
            }

            Logging.Info(Logfiles.AutomationRunner, LogOptions.MethodName, "Sequence run finished with {0} errors.", NumErrors);
            return true;
        }

        public void DumpApplicationMacros()
        {
            Logging.Info(Logfiles.AutomationRunner, LogOptions.ClassName, "Dumping application parsed macros. Count = {0}", ApplicationMacros.Count);
            foreach (AutomationMacro macro in ApplicationMacros)
            {
                Logging.Info(Logfiles.AutomationRunner, LogOptions.None, "Macro: Name = {0}, Value = {1}", macro.Name, macro.Value);
            }
        }

        public void DumpGlobalMacros()
        {
            Logging.Info(Logfiles.AutomationRunner, LogOptions.ClassName, "Dumping global parsed macros. Count = {0}", GlobalMacros.Count);
            foreach (AutomationMacro macro in GlobalMacros)
            {
                Logging.Info(Logfiles.AutomationRunner, LogOptions.None, "Macro: Name = {0}, Value = {1}", macro.Name, macro.Value);
            }
        }

        public void Dispose()
        {
            ApplicationMacros.Clear();
            GlobalMacros.Clear();
            AutomationSequences.Clear();
            ((IDisposable)WebClient).Dispose();
        }
    }
}
