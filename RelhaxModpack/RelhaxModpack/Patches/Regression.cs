﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using RelhaxModpack.Utilities;

namespace RelhaxModpack.Patches
{
    /// <summary>
    /// The types of patch regression tests that can be performed
    /// </summary>
    public enum PatchRegressionTypes
    {
        /// <summary>
        /// Json regression (standard, non-XVM)
        /// </summary>
        json,

        /// <summary>
        /// Xml regression
        /// </summary>
        xml,

        /// <summary>
        /// Regex regression
        /// </summary>
        regex,

        /// <summary>
        /// Json regression (XVM style)
        /// </summary>
        followPath
    }

    /// <summary>
    /// Represents a patch operation with a description and desired assertion condition
    /// </summary>
    public class UnitTest
    {
        /// <summary>
        /// The patch operation object
        /// </summary>
        public Patch Patch;

        /// <summary>
        /// A description of what the patch should do
        /// </summary>
        public string Description;

        /// <summary>
        /// Determines if the patch should pass or fail the test
        /// </summary>
        public bool ShouldPass;
    }

    /// <summary>
    /// A regression object is an entire regression test suite. Manages the unit tests and runs the test assertions
    /// </summary>
    /// <remarks>A regression test is designed to only test one type of patch i.e. a series of XML patches.
    /// The patching system works by having a starting file and making changes at each unit test. It then loads the files and compares
    /// the results. Results are logged to a new logfile each time a regression run is started</remarks>
    public class Regression : IDisposable
    {

        private Logfile RegressionLogfile;
        private List<UnitTest> UnitTests;
        private int NumPassed = 0;
        private string Startfile = "startfile";
        private string CheckFilenamePrefix = "check_";
        private string RegressionFolderPath;
        private string RegressionTypeString = "";
        private string RegressionExtension = "";

        /// <summary>
        /// Make a regression object
        /// </summary>
        /// <param name="regressionType">The type of regressions to run</param>
        /// <param name="unitTestsToRun">The list of unit tests to run</param>
        public Regression(PatchRegressionTypes regressionType, List<UnitTest> unitTestsToRun)
        {
            UnitTests = unitTestsToRun;
            switch (regressionType)
            {
                case PatchRegressionTypes.json:
                    RegressionTypeString = "json";
                    RegressionExtension = string.Format(".{0}",RegressionTypeString);
                    Startfile = string.Format("{0}{1}", Startfile, RegressionExtension);
                    RegressionFolderPath = Path.Combine("patch_regressions", RegressionTypeString);
                    RegressionLogfile = new Logfile(Path.Combine("patch_regressions", "logs", string.Format("{0}_{1}{2}", RegressionTypeString,
                        DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") , ".log")),Logging.ApplicationLogfileTimestamp);
                    break;
                case PatchRegressionTypes.regex:
                    RegressionTypeString = "regex";
                    RegressionExtension = string.Format(".{0}", "txt");
                    Startfile = string.Format("{0}{1}", Startfile, RegressionExtension);
                    RegressionFolderPath = Path.Combine("patch_regressions", RegressionTypeString);
                    RegressionLogfile = new Logfile(Path.Combine("patch_regressions", "logs", string.Format("{0}_{1}{2}", RegressionTypeString,
                        DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"), ".log")), Logging.ApplicationLogfileTimestamp);
                    break;
                case PatchRegressionTypes.xml:
                    RegressionTypeString = "xml";
                    RegressionExtension = string.Format(".{0}", RegressionTypeString);
                    Startfile = string.Format("{0}{1}", Startfile, RegressionExtension);
                    RegressionFolderPath = Path.Combine("patch_regressions", RegressionTypeString);
                    RegressionLogfile = new Logfile(Path.Combine("patch_regressions", "logs", string.Format("{0}_{1}{2}", RegressionTypeString,
                        DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"), ".log")), Logging.ApplicationLogfileTimestamp);
                    break;
                case PatchRegressionTypes.followPath:
                    RegressionTypeString = "json";
                    RegressionExtension = string.Format(".{0}", "xc");
                    Startfile = string.Format("{0}{1}", @"@xvm", RegressionExtension);
                    RegressionFolderPath = Path.Combine("patch_regressions", "followPath");
                    RegressionLogfile = new Logfile(Path.Combine("patch_regressions", "logs", string.Format("{0}_{1}{2}", "followPath",
                        DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss"), ".log")), Logging.ApplicationLogfileTimestamp);
                    break;
            }
        }

        /// <summary>
        /// Run a complete regression test based on the list of unit tests
        /// </summary>
        /// <returns>Returns false if a setup error occurred, true otherwise</returns>
        /// <remarks>The return value of the method does NOT related to the success of the Unit Tests</remarks>
        public bool RunRegressions()
        {
            //if from the editor, enable verbose logging (allows it to get debug log statements)
            bool tempVerboseLoggingSetting = ModpackSettings.VerboseLogging;
            if (!ModpackSettings.VerboseLogging)
            {
                Logging.Info("p.FromEditor=true and ModpackSettings.VerboseLogging=false, setting to true for duration of patch method");
                ModpackSettings.VerboseLogging = true;
            }

            //init the logfile for regressions
            if(File.Exists(RegressionLogfile.Filepath))
            {
                Logging.Warning("regression log file previously exists, deleting...");
                File.Delete(RegressionLogfile.Filepath);
            }

            if(!RegressionLogfile.Init())
            {
                Logging.Error("failed to initialize logfile");
                return false;
            }

            //make sure the files to test against exist first
            //and the start file
            if(!File.Exists(Path.Combine(RegressionFolderPath,Startfile)))
            {
                Logging.Error("regressions start file does not exist!");
                Logging.Error(Path.Combine(RegressionFolderPath, Startfile));
                return false;
            }
            for(int i = 1; i < UnitTests.Count+1; i++)
            {
                string checkfile = Path.Combine(RegressionFolderPath, string.Format("{0}{1}{2}", CheckFilenamePrefix, i.ToString("D2"), RegressionExtension));
                if (!File.Exists(checkfile))
                {
                    Logging.Error("checkfile does not exist!");
                    Logging.Error(checkfile);
                    return false;
                }
            }

            //make a new file to be the one to make changes to
            //path get extension gets the dot
            string filenameToTest = "testfile" + RegressionExtension;
            string filenameToTestPath = Path.Combine(RegressionFolderPath, filenameToTest);
            if (File.Exists(filenameToTestPath))
                File.Delete(filenameToTestPath);
            File.Copy(Path.Combine(RegressionFolderPath, Startfile), filenameToTestPath);

            WriteToLogfiles("----- Unit tests start -----");

            bool breakOutEarly = false;
            foreach (UnitTest unitTest in UnitTests)
            {
                unitTest.Patch.CompletePath = filenameToTestPath;
                unitTest.Patch.File = filenameToTestPath;
                unitTest.Patch.Type = RegressionTypeString;
                WriteToLogfiles("Running test {0} of {1}: {2}", ++NumPassed, UnitTests.Count, unitTest.Description);
                unitTest.Patch.FromEditor = true;
                if(unitTest.Patch.FollowPath)
                {
                    //delete testfile
                    if (File.Exists(filenameToTestPath))
                        File.Delete(filenameToTestPath);
                    if (NumPassed >= 5)
                    {
                        File.Copy(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"check_04", RegressionExtension)), filenameToTestPath);
                        if (NumPassed == 6)
                        {
                            //backup currentPlayersPanel and copy over new one
                            if(File.Exists(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"playersPanelBackup", RegressionExtension))))
                                File.Delete(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"playersPanelBackup", RegressionExtension)));
                            File.Copy(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"playersPanel", RegressionExtension)),
                                Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"playersPanelBackup", RegressionExtension)));

                            if(File.Exists(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"playersPanel", RegressionExtension))))
                                File.Delete(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"playersPanel", RegressionExtension)));
                            File.Copy(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"check_05", RegressionExtension)),
                                Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"playersPanel", RegressionExtension)));
                        }
                        else if (NumPassed == 7)
                        {
                            if (File.Exists(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"playersPanel", RegressionExtension))))
                                File.Delete(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"playersPanel", RegressionExtension)));
                            File.Copy(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"check_06", RegressionExtension)),
                                Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"playersPanel", RegressionExtension)));
                        }
                    }
                    else
                    {
                        File.Copy(Path.Combine(RegressionFolderPath, Startfile), filenameToTestPath);
                    }
                }
                PatchUtils.RunPatch(unitTest.Patch);
                string checkfile = Path.Combine(RegressionFolderPath, string.Format("{0}{1}{2}", CheckFilenamePrefix, NumPassed.ToString("D2"), Path.GetExtension(Startfile)));
                WriteToLogfiles("Checking results against check file {0}...",Path.GetFileName(checkfile));
                string patchRun = File.ReadAllText(filenameToTestPath);
                string patchTestAgainst = File.ReadAllText(checkfile);
                if (patchTestAgainst.Equals(patchRun))
                {
                    WriteToLogfiles("Success!");
                }
                else
                {
                    WriteToLogfiles("Failed!");
                    breakOutEarly = true;
                    break;
                }
            }

            if (breakOutEarly)
            {
                WriteToLogfiles("----- Unit tests finish (fail)-----");
            }
            else
            {
                WriteToLogfiles("----- Unit tests finish (pass)-----");
                //delete the test file, we don't need it. (it's the same text as the last check file anyways)
                if (File.Exists(filenameToTestPath))
                    File.Delete(filenameToTestPath);
                if(UnitTests[0].Patch.FollowPath)
                {
                    //delete not needed "escaped" files and put playersPanelBackup back
                    if (File.Exists(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"playersPanel", RegressionExtension))))
                        File.Delete(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"playersPanel", RegressionExtension)));
                    File.Copy(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"playersPanelBackup", RegressionExtension)),
                        Path.Combine(RegressionFolderPath, string.Format("{0}{1}", @"playersPanel", RegressionExtension)));
                    foreach (string file in new string[] { "battleLabelsTemplates_escaped", "battleLabels_escaped",
                        "damageLog_escaped", "playersPanel_escaped", "testfile_escaped", "playersPanelBackup" })
                    {
                        if (File.Exists(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", file, RegressionExtension))))
                            File.Delete(Path.Combine(RegressionFolderPath, string.Format("{0}{1}", file, RegressionExtension)));
                    }
                }
            }
            //dispose log file
            RegressionLogfile.Dispose();

            //set the verbose setting back
            Logging.Debug("temp logging setting={0}, ModpackSettings.VerboseLogging={1}, setting logging back to temp");
            ModpackSettings.VerboseLogging = tempVerboseLoggingSetting;
            return true;
        }

        private void WriteToLogfiles(string message, params object[] paramss)
        {
            WriteToLogfiles(string.Format(message, paramss));
        }

        private void WriteToLogfiles(string message)
        {
            Logging.Debug(message);
            RegressionLogfile.Write(message);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Dispose of managed resources
        /// </summary>
        /// <param name="disposing">For redundant calls</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if(RegressionLogfile != null)
                    {
                        RegressionLogfile.Dispose();
                        RegressionLogfile = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Regression()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        /// <summary>
        /// Dispose of managed resources
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
