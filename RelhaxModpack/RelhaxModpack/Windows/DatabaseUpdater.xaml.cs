﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Net;
using System.IO;
using Microsoft.Win32;
using System.Xml;
using RelhaxModpack.Utilities;
using RelhaxModpack.Xml;
using RelhaxModpack.UI;
using RelhaxModpack.Database;

namespace RelhaxModpack.Windows
{
    /// <summary>
    /// Interaction logic for DatabaseUpdater.xaml
    /// </summary>
    public partial class DatabaseUpdater : RelhaxWindow
    {
        #region Constants and statics
        private const string DatabaseUpdateFilename = "databaseUpdate.txt";
        private const string TrashXML = "trash.xml";
        private const string DatabaseXml = "database.xml";
        private const string MissingPackagesTxt = "missingPackages.txt";
        private const string RepoResourcesFolder = "resources";
        private const string RepoLatestDatabaseFolder = "latest_database";
        private const string UpdaterErrorExceptionCatcherLogfile = "UpdaterErrorCatcher.log";
        private const string InstallStatisticsXml = "install_statistics.xml";

        /// <summary>
        /// The current path for Willster419's database repository
        /// </summary>
        /// <remarks>
        /// This was done because the database repository is different then the application repository.
        /// During debug, this can be set to have the updater (in the repository path) assume that it's in the database repository.
        /// </remarks>
        public const string HardCodeRepoPath = "C:\\Users\\Willster419\\Tanks Stuff\\RelhaxModpackDatabase";
        /// <summary>
        /// Flag to use the 
        /// </summary>
        public static bool UseHardCodePath = false;
        #endregion

        #region Properties
        private string DatabaseUpdatePath
        {
            get { return Path.Combine(UseHardCodePath ? HardCodeRepoPath : Settings.ApplicationStartupPath, RepoResourcesFolder, DatabaseUpdateFilename); }
        }

        private string SupportedClientsPath
        {
            get { return Path.Combine(UseHardCodePath ? HardCodeRepoPath : Settings.ApplicationStartupPath, RepoResourcesFolder, Settings.SupportedClients); }
        }

        private string ManagerVersionPath
        {
            get { return Path.Combine(UseHardCodePath ? HardCodeRepoPath : Settings.ApplicationStartupPath, RepoResourcesFolder, Settings.ManagerVersion); }
        }

        private string RepoLatestDatabaseFolderPath
        {
            get { return Path.Combine(UseHardCodePath ? HardCodeRepoPath : Settings.ApplicationStartupPath, RepoLatestDatabaseFolder); }
        }
        #endregion

        #region Variables
        private string KeyFilename = "key.txt";//can be overridden by command line argument
        private WebClient client;
        private bool authorized = false;
        //open
        private OpenFileDialog SelectModInfo = new OpenFileDialog() { Filter = "*.xml|*.xml" };
        private OpenFileDialog SelectV2Application = new OpenFileDialog() { Title = "Find V2 application to upload", Filter = "*.exe|*.exe" };
        private OpenFileDialog SelectManagerInfoXml = new OpenFileDialog() { Title = "Find " + Settings.ManagerVersion, Filter = Settings.ManagerVersion + "|" + Settings.ManagerVersion };
        private OpenFileDialog SelectSupportedClientsXml = new OpenFileDialog() { Title = "Find " + Settings.SupportedClients, Filter = Settings.SupportedClients + "|" + Settings.SupportedClients};
        //save
        private SaveFileDialog SelectModInfoSave = new SaveFileDialog() { Filter = "*.xml|*.xml" };
        #endregion

        #region Password auth stuff
        private void RelhaxWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //check if key filename was changed from command line
            if (!string.IsNullOrWhiteSpace(CommandLineSettings.UpdateKeyFileName))
            {
                Logging.Updater("User specified from command line new key filename to use: {0}", LogLevel.Info, CommandLineSettings.UpdateKeyFileName);
                KeyFilename = CommandLineSettings.UpdateKeyFileName;
            }
            if(File.Exists(KeyFilename))
            {
                Logging.Updater("File for auth exists, attempting authorization");
                Logging.Updater(KeyFilename);
                AttemptAuthFromFile(KeyFilename);
            }
            else
            {
                Logging.Updater("Loading without pre-file authorization");
            }
            loading = false;
        }

        private void PasswordButton_Click(object sender, RoutedEventArgs e)
        {
            AttemptAuthFromString(PaswordTextbox.Text);
        }

        private async Task<bool> AttemptAuthFromFile(string filepath)
        {
            Logging.Updater("attempting authorization", LogLevel.Info, filepath);
            return await AttemptAuthFromString(File.ReadAllText(filepath));
        }

        private async Task<bool> AttemptAuthFromString(string key)
        {
            AuthStatusTextblock.Text = "Current status: Checking...";
            AuthStatusTextblock.Foreground = new SolidColorBrush(Colors.Yellow);
            //compare local password to online version
            using (client = new WebClient() { Credentials = PrivateStuff.BigmodsNetworkCredentialPrivate })
            {
                string onlinePassword = await client.DownloadStringTaskAsync(PrivateStuff.BigmodsModpackUpdaterKey);
                if (onlinePassword.Equals(key))
                {
                    Logging.Updater("Authorized, keys match");
                    AuthStatusTextblock.Text = "Current status: Authorized";
                    AuthStatusTextblock.Foreground = new SolidColorBrush(Colors.Green);
                    authorized = true;
                    return true;
                }
                else
                {
                    Logging.Updater("Not authorized, keys do not match");
                    AuthStatusTextblock.Text = "Current status: Denied";
                    AuthStatusTextblock.Foreground = new SolidColorBrush(Colors.Red);
                    authorized = false;
                    return false;
                }
            }
        }
        #endregion

        #region Standard class stuff
        private bool loading = true;
        /// <summary>
        /// Create an instance of the DatabaseUpdater window
        /// </summary>
        public DatabaseUpdater()
        {
            InitializeComponent();
        }

        private void OnApplicationClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //if strings are not empty and file exists, delete them
            //for all the class level strings
            Logging.Updater("Deleting trash files...");
            string[] filesToDelete = new string[]
            {
                DatabaseXml,
                TrashXML,
                MissingPackagesTxt
            };
            foreach (string s in filesToDelete)
            {
                if (!string.IsNullOrWhiteSpace(s) && File.Exists(s))
                    File.Delete(s);
            }
        }
        #endregion

        #region UI Interaction methods

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!authorized && !loading)
            {
                //only MD5 and database output are allowed
                if(GenerateHashesTab.IsSelected || DatabaseOutputTab.IsSelected)
                {
                    //don't do anything
                }
                else
                {
                    ReportProgress("You are not authorized to use this tab");
                    AuthStatusTab.IsSelected = true;
                    AuthStatusTab.Focus();
                }
            }
        }

        private void LogOutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            LogOutput.ScrollToEnd();
        }

        private void CancelDownloadButon_Click(object sender, RoutedEventArgs e)
        {
            client.CancelAsync();
        }

        private void PaswordTextbox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                PasswordButton_Click(null, null);
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            ClearUILog();
        }

        private async void LoadPasswordFromFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openPassword = new OpenFileDialog()
            {
                Title = "Locate password text file",
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                //Office Files|*.doc;*.xls;*.ppt
                Filter = "Text Files|*.txt",
                Multiselect = false
            };
            if ((bool)openPassword.ShowDialog())
            {
                await AttemptAuthFromFile(openPassword.FileName);
            }
        }
        #endregion

        #region Util methods

        private async Task<XmlDocument> ParseVersionInfoXmlDoc(string pathToSupportedClients)
        {
            XmlDocument doc = null;
            if (string.IsNullOrEmpty(pathToSupportedClients))
            {
                ReportProgress("Loading supported clients from online");
                using (client = new WebClient() { Credentials = PrivateStuff.BigmodsNetworkCredential })
                {
                    string xml = await client.DownloadStringTaskAsync(PrivateStuff.BigmodsFTPModpackManager + Settings.SupportedClients);
                    doc = XmlUtils.LoadXmlDocument(xml, XmlLoadType.FromString);
                }
            }
            else
            {
                ReportProgress("Loading supported clients document from " + pathToSupportedClients);
                doc = XmlUtils.LoadXmlDocument(SelectSupportedClientsXml.FileName, XmlLoadType.FromFile);
            }

            return doc;
        }

        private async Task<List<VersionInfos>> ParseVersionInfoXml(string pathToSupportedClients)
        {
            List<VersionInfos> versionInfosList = new List<VersionInfos>();
            ReportProgress("Loading and parsing " + Settings.SupportedClients);

            //load xml document
            XmlDocument doc = await ParseVersionInfoXmlDoc(pathToSupportedClients);

            //parse each online folder to list type string
            ReportProgress("Parsing " + Settings.SupportedClients);
            XmlNodeList supportedClients = XmlUtils.GetXmlNodesFromXPath(doc, "//versions/version");
            foreach (XmlNode node in supportedClients)
            {
                VersionInfos newVersionInfo = new VersionInfos()
                {
                    WoTOnlineFolderVersion = node.Attributes["folder"].Value,
                    WoTClientVersion = node.InnerText
                };
                versionInfosList.Add(newVersionInfo);
            }
            return versionInfosList;
        }

        private void OnLoadModInfo(object sender, RoutedEventArgs e)
        {
            if ((bool)SelectModInfo.ShowDialog())
            {
                LogOutput.Text = "Loading database...";
                //for the onlineFolder version: //modInfoAlpha.xml/@onlineFolder
                //for the folder version: //modInfoAlpha.xml/@version
                Settings.WoTModpackOnlineFolderVersion = XmlUtils.GetXmlStringFromXPath(SelectModInfo.FileName, Settings.DatabaseOnlineFolderXpath);
                Settings.WoTClientVersion = XmlUtils.GetXmlStringFromXPath(SelectModInfo.FileName, Settings.DatabaseOnlineVersionXpath);
                string versionInfo = string.Format("{0} = {1},  {2} = {3}", nameof(Settings.WoTModpackOnlineFolderVersion)
                    , Settings.WoTModpackOnlineFolderVersion, nameof(Settings.WoTClientVersion), Settings.WoTClientVersion);
                ReportProgress(versionInfo);
                ReportProgress("Database loaded");
            }
            else
                ReportProgress("Canceled loading database");
        }

        private void ReportProgress(string message)
        {
            //reports to the log file and the console otuptu
            Logging.Updater(message);
            LogOutput.AppendText(message + "\n");
        }

        private async Task RunPhpScript(NetworkCredential credentials, string URL, int timeoutMilliseconds)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            using (client = new PatientWebClient()
            { Credentials = credentials, Timeout = timeoutMilliseconds })
            {
                try
                {
                    string result = await client.DownloadStringTaskAsync(URL);
                    ReportProgress(result.Replace("<br />", "\n"));
                    ToggleUI((TabController.SelectedItem as TabItem), true);
                }
                catch (WebException wex)
                {
                    ReportProgress("failed to run script");
                    ReportProgress(wex.ToString());
                    ToggleUI((TabController.SelectedItem as TabItem), true);
                }
            }
        }

        private async void GenerateMD5Button_Click(object sender, RoutedEventArgs e)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Starting hashing");
            OpenFileDialog zipsToHash = new OpenFileDialog()
            {
                DefaultExt = "zip",
                Filter = "*.zip|*.zip",
                Multiselect = true,
                Title = "Load zip files to hash"
            };
            if (!(bool)zipsToHash.ShowDialog())
            {
                ReportProgress("Hashing Aborted");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }
            foreach (string s in zipsToHash.FileNames)
            {
                ReportProgress(string.Format("hash of {0}:", Path.GetFileName(s)));
                ReportProgress(await FileUtils.CreateMD5HashAsync(s));
            }
            ReportProgress("Done");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private void ClearUILog()
        {
            LogOutput.Clear();
            ReportProgress("Log Cleared");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private void ToggleUI(TabItem tab, bool toggle)
        {
            foreach (FrameworkElement control in UiUtils.GetAllWindowComponentsLogical(tab, false))
            {
                if (control is Button butt)
                    butt.IsEnabled = toggle;
            }
            SetProgress(JobProgressBar.Minimum);
            UiUtils.AllowUIToUpdate();
        }

        private void SetProgress(double prog)
        {
            JobProgressBar.Value = prog;
            UiUtils.AllowUIToUpdate();
        }

        private async Task<bool> LoadDatabase1V1FromBigmods(string lastWoTClientVersion, List<DatabasePackage> globalDependencies, List<Dependency> dependencies, List<Category> parsedCategoryList)
        {
            using (client = new WebClient() { Credentials = PrivateStuff.BigmodsNetworkCredential })
            {
                string databaseFtpPath = string.Format("{0}{1}/", PrivateStuff.BigmodsFTPModpackDatabase, lastWoTClientVersion);
                ReportProgress(string.Format("FTP path parsed as {0}", databaseFtpPath));
                ReportProgress("Downloading documents");
                ReportProgress("Download root document");
                string rootDatabase = await client.DownloadStringTaskAsync(databaseFtpPath + "database.xml");
                XmlDocument root1V1Document = XmlUtils.LoadXmlDocument(rootDatabase, XmlLoadType.FromString);

                ReportProgress("Downloading globalDependencies document");
                string globalDependencies1V1 = await client.DownloadStringTaskAsync(databaseFtpPath + XmlUtils.GetXmlStringFromXPath(root1V1Document, "/modInfoAlpha.xml/globalDependencies/@file"));

                ReportProgress("Downloading dependencies document");
                string dependnecies1V1 = await client.DownloadStringTaskAsync(databaseFtpPath + XmlUtils.GetXmlStringFromXPath(root1V1Document, "/modInfoAlpha.xml/dependencies/@file"));

                List<string> categoriesStrings1V1 = new List<string>();
                foreach (XmlNode categoryNode in XmlUtils.GetXmlNodesFromXPath(root1V1Document, "//modInfoAlpha.xml/categories/category"))
                {
                    ReportProgress(string.Format("Downloading category {0}", categoryNode.Attributes["file"].Value));
                    categoriesStrings1V1.Add(await client.DownloadStringTaskAsync(databaseFtpPath + categoryNode.Attributes["file"].Value));
                }

                ReportProgress("Parsing to lists");
                return XmlUtils.ParseDatabase1V1FromStrings(globalDependencies1V1, dependnecies1V1, categoriesStrings1V1, globalDependencies, dependencies, parsedCategoryList);
            }
        }

        private async void RunPhpScriptCreateModInfo(object sender, RoutedEventArgs e)
        {
            ReportProgress("Starting update database step 5");
            ReportProgress("Running script to create mod info data file(s)");
            await RunPhpScript(PrivateStuff.BigmodsNetworkCredentialScripts, PrivateStuff.BigmodsCreateModInfoPHP, 100000);
        }

        private async void RunPhpScriptCreateManagerInfo(object sender, RoutedEventArgs e)
        {
            ReportProgress("Starting Update database step 6");
            ReportProgress("Running script to create manager info data file");
            await RunPhpScript(PrivateStuff.BigmodsNetworkCredentialScripts, PrivateStuff.BigmodsCreateManagerInfoPHP, 100000);
        }
        #endregion

        #region Database output
        private void SaveDatabaseText(bool @internal)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            //true = internal, false = user
            string notApplicable = "n/a";
            //list creation and parsing
            List<Category> parsecCateogryList = new List<Category>();
            List<DatabasePackage> globalDependencies = new List<DatabasePackage>();
            List<Dependency> dependencies = new List<Dependency>();
            XmlDocument doc = new XmlDocument();
            doc.Load(SelectModInfo.FileName);
            XmlUtils.ParseDatabase(doc, globalDependencies, dependencies, parsecCateogryList, Path.GetDirectoryName(SelectModInfo.FileName));
            //link stuff in memory or something
            DatabaseUtils.BuildLinksRefrence(parsecCateogryList, false);
            //create variables
            StringBuilder sb = new StringBuilder();
            string saveLocation = @internal ? System.IO.Path.Combine(Settings.ApplicationStartupPath, "database_internal.csv") :
                Path.Combine(Settings.ApplicationStartupPath, "database_user.csv");
            //global dependencies
            string header = @internal ? "PackageName\tCategory\tPackage\tLevel\tZip\tDevURL\tEnabled\tVisible\tVersion" : "Category\tMod\tDevURL";
            sb.AppendLine(header);
            if(@internal)
            {
                foreach (DatabasePackage dp in globalDependencies)
                {
                    sb.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                        dp.PackageName,
                        "GlobalDependencies",
                        string.Empty,
                        "0",
                        string.IsNullOrWhiteSpace(dp.ZipFile) ? notApplicable : dp.ZipFile,
                        string.IsNullOrWhiteSpace(dp.DevURLList[0].Trim()) ? "" : "=HYPERLINK(\"" + dp.DevURLList[0].Trim() + "\",\"link\")",
                        dp.Enabled,
                        string.Empty,
                        dp.Version
                        ));
                }
                foreach (Dependency dep in dependencies)
                {
                    sb.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                        dep.PackageName,
                        "Dependencies",
                        string.Empty,
                        "0",
                        string.IsNullOrWhiteSpace(dep.ZipFile) ? notApplicable : dep.ZipFile,
                        string.IsNullOrWhiteSpace(dep.DevURLList[0].Trim()) ? "" : "=HYPERLINK(\"" + dep.DevURLList[0].Trim() + "\",\"link\")",
                        dep.Enabled,
                        string.Empty,
                        dep.Version));
                }
            }
            foreach (Category cat in parsecCateogryList)
            {
                List<SelectablePackage> flatlist = cat.GetFlatPackageList();
                foreach (SelectablePackage sp in flatlist)
                {
                    
                    if (!@internal)
                    {
                        string nameIndneted = sp.NameFormatted;
                        for (int i = 0; i < sp.Level; i++)
                        {
                            nameIndneted = "--" + nameIndneted;
                        }
                        sb.AppendLine(string.Format("{0}\t{1}\t{2}", sp.ParentCategory.Name, nameIndneted,
                            string.IsNullOrWhiteSpace(sp.DevURLList[0].Trim()) ? "" : "=HYPERLINK(\"" + sp.DevURLList[0].Trim() + "\",\"link\")"));
                    }
                    else
                    {
                        sb.AppendLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                            sp.PackageName,
                            sp.ParentCategory.Name,
                            sp.NameFormatted,
                            sp.Level,
                            string.IsNullOrWhiteSpace(sp.ZipFile) ? notApplicable : sp.ZipFile,
                            string.IsNullOrWhiteSpace(sp.DevURLList[0].Trim()) ? "" : "=HYPERLINK(\"" + sp.DevURLList[0].Trim() + "\",\"link\")",
                            sp.Enabled,
                            sp.Visible,
                            sp.Version));
                    }
                }
            }
            try
            {
                File.WriteAllText(saveLocation, sb.ToString());
                ReportProgress("Saved in " + saveLocation);
                ToggleUI((TabController.SelectedItem as TabItem), true);
            }
            catch (IOException)
            {
                ReportProgress("Failed to save in " + saveLocation + " (IOException, probably file open in another window)");
                ToggleUI((TabController.SelectedItem as TabItem), true);
            }
        }
        private void DatabaseOutputStep2a_Click(object sender, RoutedEventArgs e)
        {
            ReportProgress("Generation of internal csv...");
            //check
            if (string.IsNullOrEmpty(Settings.WoTClientVersion) || string.IsNullOrEmpty(Settings.WoTModpackOnlineFolderVersion))
            {
                ReportProgress("Database not loaded");
                return;
            }
            SaveDatabaseText(true);
        }

        private void DatabaseOutputStep2b_Click(object sender, RoutedEventArgs e)
        {
            ReportProgress("Generation of user csv...");
            //check
            if (string.IsNullOrEmpty(Settings.WoTClientVersion) || string.IsNullOrEmpty(Settings.WoTModpackOnlineFolderVersion))
            {
                ReportProgress("Database not loaded");
                return;
            }
            SaveDatabaseText(false);
        }
        #endregion
        
        #region Application update V2
        private async void UpdateApplicationV2UploadApplicationStable(object sender, RoutedEventArgs e)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Locate stable V2 application");
            if (!(bool)SelectV2Application.ShowDialog())
            {
                ToggleUI((TabController.SelectedItem as TabItem), true);
                ReportProgress("Canceled");
                return;
            }
            ReportProgress("Located");

            //check if it's the correct file to upload
            if (!Path.GetFileName(SelectV2Application.FileName).Equals(Settings.ApplicationFilenameStable))
            {
                if (MessageBox.Show(string.Format("The file selected is not {0}, are you sure you selected the correct file?",
                    Settings.ApplicationFilenameStable), "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    ToggleUI((TabController.SelectedItem as TabItem), true);
                    ReportProgress("Canceled");
                    return;
                }
            }

            ReportProgress("Uploading stable V2 application to bigmods...");
            using (client = new WebClient() { Credentials = PrivateStuff.BigmodsNetworkCredential })
            {
                await client.UploadFileTaskAsync(PrivateStuff.BigmodsFTPModpackRelhaxModpack + Settings.ApplicationFilenameStable, SelectV2Application.FileName);
            }
            ReportProgress("Done");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private async void UpdateApplicationV2UploadApplicationBeta(object sender, RoutedEventArgs e)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Locate beta V2 application");
            if (!(bool)SelectV2Application.ShowDialog())
            {
                ToggleUI((TabController.SelectedItem as TabItem), true);
                ReportProgress("Canceled");
                return;
            }
            ReportProgress("Located");

            //check if it's the correct file to upload
            if (!Path.GetFileName(SelectV2Application.FileName).Equals(Settings.ApplicationFilenameBeta))
            {
                if (MessageBox.Show(string.Format("The file selected is not {0}, are you sure you selected the correct file?",
                    Settings.ApplicationFilenameBeta), "Confirmation", MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    ToggleUI((TabController.SelectedItem as TabItem), true);
                    ReportProgress("Canceled");
                    return;
                }
            }

            ReportProgress("Uploading beta V2 application to bigmods...");
            using (client = new WebClient() { Credentials = PrivateStuff.BigmodsNetworkCredential })
            {
                await client.UploadFileTaskAsync(PrivateStuff.BigmodsFTPModpackRelhaxModpack + Settings.ApplicationFilenameBeta, SelectV2Application.FileName);
            }
            ReportProgress("Done");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private async void UpdateApplicationV2UploadManagerInfo(object sender, RoutedEventArgs e)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Running upload manager_info.xml to bigmods");
            if (!(bool)SelectManagerInfoXml.ShowDialog())
            {
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            ReportProgress("Upload manager_info.xml");
            using (client = new WebClient() { Credentials = PrivateStuff.BigmodsNetworkCredential })
            {
                await client.UploadFileTaskAsync(PrivateStuff.BigmodsFTPModpackManager + Path.GetFileName(SelectManagerInfoXml.FileName), SelectManagerInfoXml.FileName);
            }
            ReportProgress("Done");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private async void UpdateApplicationV2CreateUpdatePackages(object sender, RoutedEventArgs e)
        {
            ReportProgress("Running script to create update packages (bigmods)...");
            await RunPhpScript(PrivateStuff.BigmodsNetworkCredentialScripts, PrivateStuff.BigmodsCreateUpdatePackagesPHP, 100000);
        }

        private async void UpdateApplicationV2CreateManagerInfoBigmods(object sender, RoutedEventArgs e)
        {
            ReportProgress("Running script to create manager info (bigmods)...");
            await RunPhpScript(PrivateStuff.BigmodsNetworkCredentialScripts, PrivateStuff.BigmodsCreateManagerInfoPHP, 30 * CommonUtils.TO_SECONDS);
        }
        #endregion

        #region Cleaning online folders
        List<VersionInfos> VersionInfosListClean;
        VersionInfos selectedVersionInfos;
        bool cancelDelete = false;

        private async void CleanFoldersOnlineStep1_Click(object sender, RoutedEventArgs e)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Running Clean online folders step 1");

            VersionInfosListClean = await ParseVersionInfoXml(string.Empty);

            CleanFoldersOnlineStep2b.Items.Clear();
            foreach(VersionInfos vi in VersionInfosListClean)
                CleanFoldersOnlineStep2b.Items.Add(vi);

            ReportProgress("Done");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private async void CleanFoldersOnlineStep2_Click(object sender, RoutedEventArgs e)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Running Clean Folders online step 2");
            if(CleanFoldersOnlineStep2b.Items.Count == 0)
            {
                ReportProgress("Combobox items count = 0");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }
            if(VersionInfosListClean == null)
            {
                ReportProgress("VersionsInfoList == null");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }
            if(CleanFoldersOnlineStep2b.SelectedItem == null)
            {
                ReportProgress("Item not selected");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }
            if(VersionInfosListClean.Count == 0)
            {
                ReportProgress("VersionsInfosList count = 0");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            selectedVersionInfos = (VersionInfos)CleanFoldersOnlineStep2b.SelectedItem;
            ReportProgress("Getting all trash files in online folder " + selectedVersionInfos.WoTOnlineFolderVersion);

            //make a new list where it only has versions who's online folder match the selected one from the combobox
            List<VersionInfos> specificVersions = VersionInfosListClean.Where(info => info.WoTOnlineFolderVersion.Equals(selectedVersionInfos.WoTOnlineFolderVersion)).ToList();
            List<string> allUsedZipFiles = new List<string>();

            //could be multiple branches on github database
            ReportProgress("Getting list of branches");
            UiUtils.AllowUIToUpdate();

            List<string> branches = await CommonUtils.GetListOfGithubRepoBranchesAsync();

            ReportProgress(string.Join(",", branches));
            UiUtils.AllowUIToUpdate();

            foreach (string branch in branches)
            {
                specificVersions.Add(new VersionInfos { WoTClientVersion = "GITHUB," + branch });
            }

            foreach(VersionInfos infos in specificVersions)
            {
                ReportProgress("Adding zip files from WoTClientVersion " + infos.WoTClientVersion);
                UiUtils.AllowUIToUpdate();

                XmlDocument doc = new XmlDocument();
                List<DatabasePackage> flatList = new List<DatabasePackage>();
                List<DatabasePackage> globalDependencies = new List<DatabasePackage>();
                List<Dependency> dependencies = new List<Dependency>();
                List<Category> parsedCategoryList = new List<Category>();
                string globalDependencyXmlString = string.Empty;
                string dependenicesXmlString = string.Empty;
                List<string> categoriesXml = new List<string>();

                //download and parse database to flat list
                if (infos.WoTClientVersion.Contains("GITHUB"))
                {
                    //get branch name
                    string branchName = infos.WoTClientVersion.Split(',')[1].Trim();

                    //create root database xml download URL
                    string branchDownloadUrl = Settings.BetaDatabaseV2FolderURLEscaped.Replace(@"{branch}", branchName);
                    string branchDownloadUrlDbRoot = branchDownloadUrl + Settings.BetaDatabaseV2RootFilename;

                    //download and parse document
                    using (WebClient client = new WebClient())
                    {
                        //download the xml string into "modInfoXml"
                        client.Headers.Add("user-agent", "Mozilla / 4.0(compatible; MSIE 6.0; Windows NT 5.2;)");
                        string xmlString = await client.DownloadStringTaskAsync(branchDownloadUrlDbRoot);
                        doc = XmlUtils.LoadXmlDocument(xmlString, XmlLoadType.FromString);
                    }

                    //parse xml document for online folder version
                    string betaDatabaseOnlineFolderVersion = XmlUtils.GetXmlStringFromXPath(doc, Settings.DatabaseOnlineFolderXpath);

                    ReportProgress(string.Format("GITHUB branch = {0}, online folder={1}, selected online folder to clean version={2}", branchName, betaDatabaseOnlineFolderVersion, selectedVersionInfos.WoTOnlineFolderVersion));
                    UiUtils.AllowUIToUpdate();

                    if (!betaDatabaseOnlineFolderVersion.Equals(selectedVersionInfos.WoTOnlineFolderVersion))
                    {
                        ReportProgress("Skipping (online folders are not equal)");
                        UiUtils.AllowUIToUpdate();
                        continue;
                    }
                    else
                    {
                        ReportProgress("Including (online folders are equal)");
                        UiUtils.AllowUIToUpdate();

                        //parse beta database to lists
                        List<string> downloadURLs = new List<string>()
                        {
                            branchDownloadUrl + XmlUtils.GetXmlStringFromXPath(doc, "/modInfoAlpha.xml/globalDependencies/@file"),
                            branchDownloadUrl + XmlUtils.GetXmlStringFromXPath(doc, "/modInfoAlpha.xml/dependencies/@file")
                        };

                        //categories
                        foreach (XmlNode categoryNode in XmlUtils.GetXmlNodesFromXPath(doc, "//modInfoAlpha.xml/categories/category"))
                        {
                            string categoryFileName = categoryNode.Attributes["file"].Value;
                            downloadURLs.Add(branchDownloadUrl + categoryFileName);
                        }

                        downloadURLs = downloadURLs.Select(name => name.Replace(".Xml", ".xml")).ToList();

                        string[] xmlDownloadStrings = await CommonUtils.DownloadStringsFromUrlsAsync(downloadURLs);

                        globalDependencyXmlString = xmlDownloadStrings[0];
                        dependenicesXmlString = xmlDownloadStrings[1];

                        categoriesXml = new List<string>();
                        for (int i = 2; i < downloadURLs.Count; i++)
                        {
                            categoriesXml.Add(xmlDownloadStrings[i]);
                        }
                    }
                }
                else
                {
                    string modInfoxmlURL = Settings.BigmodsDatabaseRootEscaped.Replace(@"{dbVersion}", infos.WoTClientVersion) + "modInfo.dat";
                    ReportProgress("Downloading database " + modInfoxmlURL);
                    UiUtils.AllowUIToUpdate();

                    //download latest modInfo xml
                    byte[] zip;
                    using (WebClient client = new WebClient())
                    {
                        zip = await client.DownloadDataTaskAsync(modInfoxmlURL);
                    }

                    //parse downloaded zip file
                    using (Ionic.Zip.ZipFile zipfile = Ionic.Zip.ZipFile.Read(new MemoryStream(zip)))
                    {
                        //extract modinfo xml string
                        string modInfoXml = FileUtils.GetStringFromZip(zipfile, "database.xml");
                        doc = XmlUtils.LoadXmlDocument(modInfoXml, XmlLoadType.FromString);

                        string globalDependencyFilename = XmlUtils.GetXmlStringFromXPath(doc, "/modInfoAlpha.xml/globalDependencies/@file");
                        globalDependencyXmlString = FileUtils.GetStringFromZip(zipfile, globalDependencyFilename);

                        string dependencyFilename = XmlUtils.GetXmlStringFromXPath(doc, "/modInfoAlpha.xml/dependencies/@file");
                        dependenicesXmlString = FileUtils.GetStringFromZip(zipfile, dependencyFilename);

                        foreach (XmlNode categoryNode in XmlUtils.GetXmlNodesFromXPath(doc, "//modInfoAlpha.xml/categories/category"))
                        {
                            string categoryFilename = categoryNode.Attributes["file"].Value;
                            categoriesXml.Add(FileUtils.GetStringFromZip(zipfile, categoryFilename));
                        }
                    }
                }
                
                //parse into lists
                if (!XmlUtils.ParseDatabase1V1FromStrings(globalDependencyXmlString, dependenicesXmlString, categoriesXml, globalDependencies, dependencies, parsedCategoryList))
                {
                    ReportProgress("Failed to parse modInfo to lists");
                    ToggleUI((TabController.SelectedItem as TabItem), true);
                    return;
                }

                DatabaseUtils.BuildLinksRefrence(parsedCategoryList, false);
                DatabaseUtils.BuildLevelPerPackage(parsedCategoryList);

                //if the list of zip files does not already have it, then add it
                flatList = DatabaseUtils.GetFlatList(globalDependencies, dependencies, null, parsedCategoryList);
                foreach (DatabasePackage package in flatList)
                    if(!string.IsNullOrWhiteSpace(package.ZipFile) && !allUsedZipFiles.Contains(package.ZipFile))
                            allUsedZipFiles.Add(package.ZipFile);
            }

            //just a check
            allUsedZipFiles = allUsedZipFiles.Distinct().ToList();

            //have a list of ALL used zip files in the folder, now get the list of all zip files in the onlineFolder
            ReportProgress("Complete, getting database.xml in onlineFolder " + selectedVersionInfos.WoTOnlineFolderVersion);
            List<string> notUsedFiles = new List<string>();
            List<string> missingFiles = new List<string>();
            List<string> filesFromDatabaseXml = new List<string>();
            using (client = new WebClient())
            {
                XmlDocument filesInOnlineFolder = new XmlDocument();
                string downlaodUrlString = string.Format("http://bigmods.relhaxmodpack.com/WoT/{0}/{1}", selectedVersionInfos.WoTOnlineFolderVersion, DatabaseXml);
                ReportProgress("Downloading from " + downlaodUrlString);
                filesInOnlineFolder.LoadXml(await client.DownloadStringTaskAsync(downlaodUrlString));
                foreach(XmlNode node in filesInOnlineFolder.SelectNodes("//database/file"))
                {
                    filesFromDatabaseXml.Add(node.Attributes["name"].Value);
                }
            }
            filesFromDatabaseXml = filesFromDatabaseXml.Distinct().ToList();
            ReportProgress("Complete, building lists");
            notUsedFiles = filesFromDatabaseXml.Except(allUsedZipFiles).ToList();
            missingFiles = allUsedZipFiles.Except(filesFromDatabaseXml).ToList();
            CleanZipFoldersTextbox.Clear();
            if (missingFiles.Count > 0)
            {
                ReportProgress("ERROR: missing files on the server! (Saved in textbox)");
                CleanZipFoldersTextbox.AppendText(string.Join("\n", missingFiles));
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }
            else if(notUsedFiles.Count == 0)
            {
                ReportProgress("No files to clean!");
                ToggleUI((TabController.SelectedItem as TabItem), true);
            }
            else
            {
                CleanZipFoldersTextbox.AppendText(string.Join("\n", notUsedFiles));
                ReportProgress("Complete");
                ToggleUI((TabController.SelectedItem as TabItem), true);
            }
        }

        private void CleanFoldersOnlineCancel_Click(object sender, RoutedEventArgs e)
        {
            cancelDelete = true;
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private async void CleanFoldersOnlineStep3_Click(object sender, RoutedEventArgs e)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            if (string.IsNullOrWhiteSpace(CleanZipFoldersTextbox.Text))
            {
                ReportProgress("textbox is empty");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }
            if(string.IsNullOrWhiteSpace(selectedVersionInfos.WoTOnlineFolderVersion))
            {
                ReportProgress("selectedVersionInfos is null");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }
            cancelDelete = false;
            CleanFoldersOnlineCancelStep3.Visibility = Visibility.Visible;
            CleanFoldersOnlineCancelStep3.IsEnabled = true;
            List<string> filesToDelete;
            filesToDelete = CleanZipFoldersTextbox.Text.Split('\n').ToList();
            string[] filesActuallyInFolder = await FtpUtils.FtpListFilesFoldersAsync(
                PrivateStuff.BigmodsFTPRootWoT + selectedVersionInfos.WoTOnlineFolderVersion,PrivateStuff.BigmodsNetworkCredential);
            int count = 1;
            foreach(string s in filesToDelete)
            {
                if(cancelDelete)
                {
                    ReportProgress("Cancel Requested");
                    ToggleUI((TabController.SelectedItem as TabItem), true);
                    return;
                }
                if(!filesActuallyInFolder.Contains(s))
                {
                    ReportProgress(string.Format("skipping file {0}, does not exist", s));
                    count++;
                    continue;
                }
                ReportProgress(string.Format("Deleting file {0} of {1}, {2}", count++, filesToDelete.Count, s));
                await FtpUtils.FtpDeleteFileAsync(string.Format("{0}{1}/{2}",
                    PrivateStuff.BigmodsFTPRootWoT, selectedVersionInfos.WoTOnlineFolderVersion, s), PrivateStuff.BigmodsNetworkCredential);
            }
            CleanZipFoldersTextbox.Clear();
            CleanFoldersOnlineCancelStep3.Visibility = Visibility.Hidden;
            ReportProgress("Done");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }
        #endregion

        #region Database Updating V2
        //the version number of the last supported WoT client, used for making backup online folder
        private string LastSupportedTanksVersion = "";

        private async void UpdateDatabaseV2Step2_Click(object sender, RoutedEventArgs e)
        {
            ReportProgress("Starting Update database step 2...");
            ReportProgress("Running script to update online hash database...");
            //a PatientWebClient should allow a timeout value of 5 mins (or more)
            await RunPhpScript(PrivateStuff.BigmodsNetworkCredentialScripts, PrivateStuff.BigmodsCreateDatabasePHP, 30 * CommonUtils.TO_SECONDS * CommonUtils.TO_MINUETS);
        }

        private async void UpdateDatabaseV2Step3_Click(object sender, RoutedEventArgs e)
        {
            //init UI
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Starting DatabaseUpdate step 3");
            ReportProgress("Preparing database update");

            //checks
            if (string.IsNullOrEmpty(Settings.WoTModpackOnlineFolderVersion))
            {
                ReportProgress("WoTModpackOnlineFolderVersion is empty");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }
            if (!File.Exists(SelectModInfo.FileName))
            {
                ReportProgress("SelectModInfo file selected does not exist:" + SelectModInfo.FileName);
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            //initialize the application main logile if possible for logging potential util level exception issues
            ReportProgress("Attempting to init the main log file for exception logging");
            if(Logging.Init(Logfiles.Application, UpdaterErrorExceptionCatcherLogfile))
            {
                Logging.Info(Settings.LogSpacingLineup);
                Logging.Info("Log file initialized for database V2 step 3 error and exception catching");
            }
            else
            {
                ReportProgress("Failed to initialize the custom application logfile. Error and exception catching will be disabled");
            }

            //init stringBuilders
            StringBuilder filesNotFoundSB = new StringBuilder();
            StringBuilder databaseUpdateText = new StringBuilder();
            filesNotFoundSB.Append("FILES NOT FOUND:\n");

            //init lists
            List<DatabasePackage> globalDependencies = new List<DatabasePackage>();
            List<Dependency> dependencies = new List<Dependency>();
            List<Category> parsedCategoryList = new List<Category>();
            List<DatabasePackage> addedPackages = new List<DatabasePackage>();
            List<DatabasePackage> updatedPackages = new List<DatabasePackage>();
            List<DatabasePackage> disabledPackages = new List<DatabasePackage>();
            List<DatabasePackage> removedPackages = new List<DatabasePackage>();
            List<DatabasePackage> missingPackages = new List<DatabasePackage>();
            List<DatabaseBeforeAfter> renamedPackages = new List<DatabaseBeforeAfter>();
            List<DatabaseBeforeAfter> internallyRenamed = new List<DatabaseBeforeAfter>();
            List<DatabaseBeforeAfter> movedPackages = new List<DatabaseBeforeAfter>();

            //init strings
            LastSupportedTanksVersion = string.Empty;
            SetProgress(10);

            ReportProgress("Loading Root database 1.1 document");
            //load root document
            XmlDocument rootDocument = XmlUtils.LoadXmlDocument(SelectModInfo.FileName, XmlLoadType.FromFile);
            if(rootDocument == null)
            {
                ReportProgress("Failed to parse root database file. Invalid XML document");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            ReportProgress("Parsing database 1.1 document");
            //parse main database
            if (!XmlUtils.ParseDatabase1V1FromFiles(Path.GetDirectoryName(SelectModInfo.FileName), rootDocument,
                globalDependencies, dependencies, parsedCategoryList))
            {
                ReportProgress("Failed to parse database");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            SetProgress(20);

            //bulid link refrences (parent/child, levels, etc)
            DatabaseUtils.BuildLinksRefrence(parsedCategoryList, true);
            DatabaseUtils.BuildLevelPerPackage(parsedCategoryList);
            List<DatabasePackage> flatListCurrent = DatabaseUtils.GetFlatList(globalDependencies, dependencies, null, parsedCategoryList);

            //check for duplicates
            ReportProgress("Checking for duplicate database packageName entries");
            List<string> duplicates = DatabaseUtils.CheckForDuplicates(globalDependencies, dependencies, parsedCategoryList);
            if (duplicates.Count > 0)
            {
                ReportProgress("ERROR: Duplicates found!");
                foreach (string s in duplicates)
                    ReportProgress(s);
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }
            ReportProgress("No duplicates found");

            SetProgress(30);

            //make the name of the new database update version
            ReportProgress("Making new database version string");
            string dateTimeFormat = string.Format("{0:yyyy-MM-dd}", DateTime.Now);

            //load manager version xml file
            XmlDocument doc = XmlUtils.LoadXmlDocument(ManagerVersionPath, XmlLoadType.FromFile);
            XmlNode database_version_text = doc.SelectSingleNode("//version/database");

            //database update text is like this: <WoTVersion>_<Date>_<itteration>
            //only update the iteration if the WoT version and date match
            string lastWoTClientVersion = database_version_text.InnerText.Split('_')[0];
            string lastDate = database_version_text.InnerText.Split('_')[1];

            ReportProgress(string.Format("lastWoTClientVersion    = {0}", lastWoTClientVersion));
            ReportProgress(string.Format("currentWoTClientVersion = {0}", Settings.WoTClientVersion));
            ReportProgress(string.Format("lastDate                = {0}", lastDate));
            ReportProgress(string.Format("currentDate             = {0}", dateTimeFormat));

            string databaseVersionTag = string.Empty;

            if (lastWoTClientVersion.Equals(Settings.WoTClientVersion) && lastDate.Equals(dateTimeFormat))
            {
                ReportProgress("WoTVersion and date match, so incrementing the itteration");
                int lastItteration = int.Parse(database_version_text.InnerText.Split('_')[2]);
                databaseVersionTag = string.Format("{0}_{1}_{2}", Settings.WoTClientVersion, dateTimeFormat, ++lastItteration);
            }
            else
            {
                ReportProgress("lastWoTVersion and/or date NOT match, not incrementing the version (starts at 1)");
                databaseVersionTag = string.Format("{0}_{1}_1", Settings.WoTClientVersion, dateTimeFormat);
            }

            ReportProgress(string.Format("databaseVersionTag = {0}", databaseVersionTag));

            //update and save the manager_version.xml file
            database_version_text.InnerText = databaseVersionTag;
            doc.Save(ManagerVersionPath);

            SetProgress(40);

            //make a flat list of old (last supported modInfoxml) for comparison
            List<DatabasePackage> globalDependenciesOld = new List<DatabasePackage>();
            List<Dependency> dependenciesOld = new List<Dependency>();
            List<Category> parsedCateogryListOld = new List<Category>();

            //get last supported wot version for comparison
            ReportProgress("Getting last supported database files");
            LastSupportedTanksVersion = lastWoTClientVersion;

            //get strings for 1v1 parsing
            //BigmodsFTPModpackDatabase -> .../database/
            if (!await LoadDatabase1V1FromBigmods(lastWoTClientVersion, globalDependenciesOld, dependenciesOld, parsedCateogryListOld))
            {
                ReportProgress("Failed to parse modInfo to lists");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            //build link references of old document
            DatabaseUtils.BuildLinksRefrence(parsedCateogryListOld, true);
            DatabaseUtils.BuildLevelPerPackage(parsedCateogryListOld);
            List<DatabasePackage> flatListOld = DatabaseUtils.GetFlatList(globalDependenciesOld, dependenciesOld, null, parsedCateogryListOld);

            //check if any packages had a UID change, because this is not allowed
            //check based on packageName, loop through new
            //if it exists in old, make sure the UID did not change, else abort
            List<DatabaseBeforeAfter2> packagesWithChangedUIDs = new List<DatabaseBeforeAfter2>();
            foreach(DatabasePackage currentPackage in flatListCurrent)
            {
                DatabasePackage oldPackage = flatListOld.Find(pac => pac.PackageName.Equals(currentPackage.PackageName));
                if(oldPackage != null)
                {
                    if(!oldPackage.UID.Equals(currentPackage.UID))
                    {
                        packagesWithChangedUIDs.Add(new DatabaseBeforeAfter2() { Before = oldPackage, After = currentPackage });
                    }
                }
            }

            if(packagesWithChangedUIDs.Count > 0)
            {
                ReportProgress("ERROR: The following packages have UIDs changed! This is not allowed!");
                foreach (DatabaseBeforeAfter2 beforeAfter in packagesWithChangedUIDs)
                {
                    ReportProgress(string.Format("Before package: PackageName = {0}, UID = {1}",beforeAfter.Before.PackageName, beforeAfter.Before.UID));
                    ReportProgress(string.Format("After package:  PackageName = {0}, UID = {1}", beforeAfter.After.PackageName, beforeAfter.After.UID));
                }
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            //check if any packages are missing UIDs
            List<DatabasePackage> packagesMissingUids = flatListCurrent.FindAll(pak => string.IsNullOrWhiteSpace(pak.UID));
            if(packagesMissingUids.Count > 0)
            {
                ReportProgress("ERROR: The following packages don't have UIDs and need to be added!");
                foreach (DatabasePackage package in packagesMissingUids)
                {
                    ReportProgress(string.Format("Package missing UID: {0}", package.PackageName));
                }
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            //download and load latest zip file server database
            ReportProgress("Downloading database.xml of current WoT onlineFolder version from server");
            XmlDocument databaseXml = null;
            using (client = new WebClient())
            {
                string databaseXmlString = await client.DownloadStringTaskAsync(string.Format("http://bigmods.relhaxmodpack.com/WoT/{0}/{1}",
                    Settings.WoTModpackOnlineFolderVersion, DatabaseXml));
                databaseXml = XmlUtils.LoadXmlDocument(databaseXmlString, XmlLoadType.FromString);
            }

            SetProgress(50);

            //update the crc values, also makes list of updated mods
            ReportProgress("Downloaded, comparing crc values for list of updated mods");
            UiUtils.AllowUIToUpdate();
            foreach (DatabasePackage package in flatListCurrent)
            {
                if (string.IsNullOrEmpty(package.ZipFile))
                    continue;
                //"//database/file[@name="Sounds_HRMOD_Gun_Sounds_by_Zorgane_v2.01_1.2.0_2018-10-12.zip"]"
                string xpathText = string.Format("//database/file[@name=\"{0}\"]", package.ZipFile);
                XmlNode databaseEntry = databaseXml.SelectSingleNode(xpathText);
                if (databaseEntry != null)
                {
                    string newCRC = databaseEntry.Attributes["md5"].Value;
                    if (string.IsNullOrWhiteSpace(newCRC))
                        throw new BadMemeException("newCRC string is null or whitespace");
                    if (!package.CRC.Equals(newCRC))
                    {
                        package.CRC = newCRC;
                        updatedPackages.Add(package);
                    }
                    //legacy compatibility check: size parameters need to be updated
                    ulong fakeSize = CommonUtils.ParseuLong(databaseEntry.Attributes["size"].Value, 0);
                    if (package.Size == 0 || fakeSize == 0 || package.Size != fakeSize)
                    {
                        //update the current size of the package
                        package.Size = fakeSize;
                        if (package.Size == 0)
                        {
                            Logging.Updater("Zip file {0} is 0 bytes (empty file)", LogLevel.Info, package.ZipFile);
                            return;
                        }
                    }
                }
                else if (package.CRC.Equals("f") || string.IsNullOrWhiteSpace(package.CRC))
                {
                    missingPackages.Add(package);
                }
            }

            SetProgress(80);

            //do list magic to get all added, removed, disabled, etc package lists
            //used for disabled, removed, added mods
            ReportProgress("Getting list of added and removed packages");
            UiUtils.AllowUIToUpdate();
            PackageComparerByUID pc = new PackageComparerByUID();

            //if in before but not after = removed
            removedPackages = flatListOld.Except(flatListCurrent, pc).ToList();

            //if not in before but after = added
            addedPackages = flatListCurrent.Except(flatListOld, pc).ToList();

            ReportProgress("Getting list of packages old and new minus removed and added");
            UiUtils.AllowUIToUpdate();

            //first start by getting the list of all current packages, then filter out removed and added packages
            //make a copy of the current flat list
            List<DatabasePackage> packagesNotRemovedOrAdded = new List<DatabasePackage>(flatListCurrent);

            //remove any added or removed packages from the new list to check for other stuff
            packagesNotRemovedOrAdded = packagesNotRemovedOrAdded.Except(removedPackages, pc).Except(addedPackages, pc).ToList();

            //we only want of type SelectablePackage 
            //https://stackoverflow.com/questions/3842714/linq-selection-by-type-of-an-object
            List<SelectablePackage> selectablePackagesNotRemovedOrAdded = packagesNotRemovedOrAdded.OfType<SelectablePackage>().ToList();

            //remove any added or removed packages from the new list to check for other stuff
            List<DatabasePackage> oldPackagesNotRemovedOrAdded = new List<DatabasePackage>(flatListOld);
            oldPackagesNotRemovedOrAdded = oldPackagesNotRemovedOrAdded.Except(removedPackages, pc).Except(addedPackages, pc).ToList();
            List<SelectablePackage> selectablePackagesOld = oldPackagesNotRemovedOrAdded.OfType<SelectablePackage>().ToList();

            //get the list of renamed packages
            //a renamed package will have the same internal name, but a different display name
            ReportProgress("Getting list of renamed packages");
            UiUtils.AllowUIToUpdate();
            foreach (SelectablePackage selectablePackage in selectablePackagesNotRemovedOrAdded)
            {
                SelectablePackage oldPackageWithMatchingUID = selectablePackagesOld.Find(pack => pack.UID.Equals(selectablePackage.UID));
                if (oldPackageWithMatchingUID == null)
                    continue;

                if (!selectablePackage.NameFormatted.Equals(oldPackageWithMatchingUID.NameFormatted))
                {
                    Logging.Updater("Package rename-> old:{0}, new:{1}", LogLevel.Info, oldPackageWithMatchingUID.PackageName, selectablePackage.PackageName);
                    renamedPackages.Add(new DatabaseBeforeAfter() { Before = oldPackageWithMatchingUID, After = selectablePackage });
                }
            }

            //list of moved packages
            //a moved package will have a different UIDPath (the UID's don't change, so any change detected would imply a structure level change)
            ReportProgress("Getting list of moved packages");
            UiUtils.AllowUIToUpdate();
            foreach (SelectablePackage selectablePackage in selectablePackagesNotRemovedOrAdded)
            {
                SelectablePackage oldPackageWithMatchingUID = selectablePackagesOld.Find(pack => pack.UID.Equals(selectablePackage.UID));
                if (oldPackageWithMatchingUID == null)
                    continue;

                if (!selectablePackage.CompleteUIDPath.Equals(oldPackageWithMatchingUID.CompleteUIDPath))
                {
                    Logging.Updater("Package moved: {0}", LogLevel.Info, selectablePackage.PackageName);
                    movedPackages.Add(new DatabaseBeforeAfter { Before = oldPackageWithMatchingUID, After = selectablePackage });
                }
            }

            SetProgress(85);

            //if a package was internally renamed, the packageName won't match
            ReportProgress("Getting list of internal renamed packages");
            UiUtils.AllowUIToUpdate();
            foreach (SelectablePackage selectablePackage in selectablePackagesNotRemovedOrAdded)
            {
                SelectablePackage oldPackageWithMatchingUID = selectablePackagesOld.Find(pack => pack.UID.Equals(selectablePackage.UID));
                if (oldPackageWithMatchingUID == null)
                    continue;

                if (!selectablePackage.PackageName.Equals(oldPackageWithMatchingUID.PackageName))
                {
                    Logging.Updater("Package internal renamed: {0}", LogLevel.Info, selectablePackage.PackageName);
                    movedPackages.Add(new DatabaseBeforeAfter { Before = oldPackageWithMatchingUID, After = selectablePackage });
                }
            }
            
            SetProgress(90);

            ReportProgress("Getting list of disabled packages");
            //list of disabled packages before
            List<DatabasePackage> disabledBefore = oldPackagesNotRemovedOrAdded.Where(p => !p.Enabled).ToList();

            //list of disabled packages after
            List<DatabasePackage> disabledAfter = packagesNotRemovedOrAdded.Where(p => !p.Enabled).ToList();

            //compare except with after -> before
            disabledPackages = disabledAfter.Except(disabledBefore, pc).ToList();

            //any final list processing
            //also need to remove and removed and added and disabled from updated
            updatedPackages = updatedPackages.Except(removedPackages, pc).ToList();
            updatedPackages = updatedPackages.Except(disabledPackages, pc).ToList();
            updatedPackages = updatedPackages.Except(addedPackages, pc).ToList();
            //should remove any packages from the added list that were actually moved or renamed
            addedPackages = addedPackages.Except(movedPackages.Select(pack => pack.After)).ToList();
            addedPackages = addedPackages.Except(renamedPackages.Select(pack => pack.After)).ToList();
            //don't included any added packages that are disabled
            List<DatabasePackage> addedPackagesTemp = new List<DatabasePackage>();
            foreach(DatabasePackage package in addedPackages)
            {
                if (package is SelectablePackage sPack)
                {
                    if (sPack.Visible)
                        addedPackagesTemp.Add(package);
                }
                else
                    addedPackagesTemp.Add(package);
            }
            addedPackages = addedPackagesTemp;

            SetProgress(95);

            //put them to stringBuilder and write text to disk
            ReportProgress("Building databaseUpdate.txt");
            StringBuilder numberBuilder = new StringBuilder();
            numberBuilder.AppendLine(string.Format("Number of Added packages: {0}", addedPackages.Count));
            numberBuilder.AppendLine(string.Format("Number of Updated packages: {0}", updatedPackages.Count));
            numberBuilder.AppendLine(string.Format("Number of Disabled packages: {0}", disabledPackages.Count));
            numberBuilder.AppendLine(string.Format("Number of Removed packages: {0}", removedPackages.Count));
            numberBuilder.AppendLine(string.Format("Number of Moved packages: {0}", movedPackages.Count));
            numberBuilder.AppendLine(string.Format("Number of Renamed packages: {0}", renamedPackages.Count));
            numberBuilder.AppendLine(string.Format("Number of Internally renamed packages: {0}", internallyRenamed.Count));

            ReportProgress(numberBuilder.ToString());

            SetProgress(100);

            //abort if missing files
            if (missingPackages.Count > 0)
            {
                if (File.Exists(MissingPackagesTxt))
                    File.Delete(MissingPackagesTxt);
                filesNotFoundSB.Clear();
                foreach (DatabasePackage package in missingPackages)
                    filesNotFoundSB.AppendLine(package.ZipFile);
                File.WriteAllText(MissingPackagesTxt, filesNotFoundSB.ToString());
                ReportProgress("ERROR: " + missingPackages.Count + " packages missing files! (saved to missingPackages.txt)");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            //make stringBuilder of databaseUpdate.text
            databaseUpdateText.Clear();
            databaseUpdateText.AppendLine("Database Update!\n");
            databaseUpdateText.AppendLine("New version tag: " + databaseVersionTag);

            databaseUpdateText.Append("\n" + numberBuilder.ToString());

            databaseUpdateText.AppendLine("\nAdded:");
            foreach (DatabasePackage dp in addedPackages)
                databaseUpdateText.AppendLine(" - " + dp.CompletePath);

            databaseUpdateText.AppendLine("\nUpdated:");
            foreach (DatabasePackage dp in updatedPackages)
                databaseUpdateText.AppendLine(" - " + dp.CompletePath);

            databaseUpdateText.AppendLine("\nRenamed:");
            foreach (DatabaseBeforeAfter dp in renamedPackages)
                databaseUpdateText.AppendFormat(" - \"{0}\" was renamed to \"{1}\"\r\n", dp.Before.NameFormatted, dp.After.NameFormatted);

            databaseUpdateText.AppendLine("\nMoved:");
            foreach (DatabaseBeforeAfter dp in movedPackages)
                databaseUpdateText.AppendFormat(" - \"{0}\" was moved to \"{1}\"\r\n", dp.Before.CompletePath, dp.After.CompletePath);

            databaseUpdateText.AppendLine("\nDisabled:");
            foreach (DatabasePackage dp in disabledPackages)
                databaseUpdateText.AppendLine(" - " + dp.CompletePath);

            databaseUpdateText.AppendLine("\nRemoved:");
            foreach (DatabasePackage dp in removedPackages)
                databaseUpdateText.AppendLine(" - " + dp.CompletePath);

            databaseUpdateText.AppendLine("\nNotes:\n - \n\n-----------------------------------------------------" +
                "---------------------------------------------------------------------------------------");

            //fix line endings
            string fixedDatabaseUpdate = databaseUpdateText.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n");

            //save databaseUpdate.txt
            if (File.Exists(DatabaseUpdatePath))
                File.Delete(DatabaseUpdatePath);
            File.WriteAllText(DatabaseUpdatePath, fixedDatabaseUpdate);
            ReportProgress("Database text processed and written to disk");

            //save new modInfo.xml
            ReportProgress("Updating database");
            File.Delete(SelectModInfo.FileName);
            XmlUtils.SaveDatabase(SelectModInfo.FileName, Settings.WoTClientVersion, Settings.WoTModpackOnlineFolderVersion,
                globalDependencies, dependencies, parsedCategoryList, DatabaseXmlVersion.OnePointOne);

            ReportProgress("Done");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private async void UpdateDatabaseV2Step4_Click(object sender, RoutedEventArgs e)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            //check for stuff
            ReportProgress("Starting DatabaseUpdate step 4");
            ReportProgress("Uploading changed files");

            //checks
            if (string.IsNullOrEmpty(Settings.WoTModpackOnlineFolderVersion))
            {
                ReportProgress("WoTModpackOnlineFolderVersion is empty");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }
            if (!File.Exists(SelectModInfo.FileName))
            {
                ReportProgress("SelectModInfo file selected does not exist:" + SelectModInfo.FileName);
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }
            if (!File.Exists(ManagerVersionPath))
            {
                ReportProgress("manager_version.xml does not exist");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            SetProgress(10);

            //database xmls
            ReportProgress("Uploading new database files to bigmods");
            using (client = new WebClient() { Credentials = PrivateStuff.BigmodsNetworkCredential })
            {
                string databaseFtpPath = string.Format("{0}{1}/", PrivateStuff.BigmodsFTPModpackDatabase, Settings.WoTClientVersion);
                ReportProgress(string.Format("FTP upload path parsed as {0}", databaseFtpPath));

                //check if ftp folder exists
                ReportProgress(string.Format("Checking if FTP folder '{0}' exists", Settings.WoTClientVersion));
                string[] folders = await FtpUtils.FtpListFilesFoldersAsync(PrivateStuff.BigmodsFTPModpackDatabase, PrivateStuff.BigmodsNetworkCredential);
                if (!folders.Contains(Settings.WoTClientVersion))
                {
                    ReportProgress("Does not exist, making");
                    await FtpUtils.FtpMakeFolderAsync(databaseFtpPath, PrivateStuff.BigmodsNetworkCredential);
                }
                else
                {
                    ReportProgress("Yes, yes it does");
                }

                //RepoLatestDatabaseFolderPath
                ReportProgress("Uploading root");
                string rootDatabasePath = Path.Combine(RepoLatestDatabaseFolderPath, "database.xml");
                XmlDocument root1V1Document = XmlUtils.LoadXmlDocument(rootDatabasePath, XmlLoadType.FromFile);
                await client.UploadFileTaskAsync(databaseFtpPath + "database.xml", rootDatabasePath);

                //list of files by xml name to upload (because they all sit in the same folder, ftp and local
                ReportProgress("Creating list of database files to upload");
                List<string> xmlFilesNames = new List<string>()
                {
                    XmlUtils.GetXmlStringFromXPath(root1V1Document, "/modInfoAlpha.xml/globalDependencies/@file"),
                    XmlUtils.GetXmlStringFromXPath(root1V1Document, "/modInfoAlpha.xml/dependencies/@file")
                };
                foreach (XmlNode categoryNode in XmlUtils.GetXmlNodesFromXPath(root1V1Document, "//modInfoAlpha.xml/categories/category"))
                {
                    xmlFilesNames.Add(categoryNode.Attributes["file"].Value);
                }

                //upload the files
                foreach(string xmlFilename in xmlFilesNames)
                {
                    ReportProgress(string.Format("Uploading {0}", xmlFilename));
                    string localPath = Path.Combine(RepoLatestDatabaseFolderPath, xmlFilename);
                    string ftpPath = databaseFtpPath + xmlFilename;
                    Logging.Updater("localPath = {0}", LogLevel.Debug, localPath);
                    Logging.Updater("ftpPath   = {0}", LogLevel.Debug, ftpPath);
                    await client.UploadFileTaskAsync(ftpPath, localPath);
                }
            }
            ReportProgress("Database uploads complete");

            SetProgress(85);

            //manager_version.xml
            ReportProgress("Uploading new manager_version.xml to bigmods");
            using (client = new WebClient() { Credentials = PrivateStuff.BigmodsNetworkCredential })
            {
                string completeURL = PrivateStuff.BigmodsFTPModpackManager + Settings.ManagerVersion;
                await client.UploadFileTaskAsync(completeURL, ManagerVersionPath);
            }

            SetProgress(90);

            //check if supported_clients.xml needs to be updated for a new version
            ReportProgress("Checking if supported_clients.xml needs to be updated for new WoT version");

            ReportProgress("Checking if latest WoT version is the same as this database supports");
            ReportProgress("Old version = " + LastSupportedTanksVersion + ", new version = " + Settings.WoTClientVersion);
            if (!LastSupportedTanksVersion.Equals(Settings.WoTClientVersion))
            {
                ReportProgress("Last supported version does not match");
                MessageBox.Show("Old database client version != new client version.\nPlease update the " + Settings.SupportedClients + " document after publishing the database");
            }
            else
            {
                ReportProgress("DOES NOT need to be updated/uploaded");
            }

            SetProgress(100);
            ReportProgress("Done");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private void UpdateDatabasestep8_NA_ENG_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://forum.worldoftanks.com/index.php?/topic/535868-");
        }

        private void UpdateDatabaseStep8_EU_ENG_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://forum.worldoftanks.eu/index.php?/topic/623269-");
        }

        private void UpdateDatabaseStep8_EU_GER_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://forum.worldoftanks.eu/index.php?/topic/624499-");
        }
        #endregion

        #region Statistics
        private async void DatabaseStatsSave_Click(object sender, RoutedEventArgs e)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            //BigmodsFTPModpackManager
            ReportProgress("Starting stats saving");

            using (client = new WebClient { Credentials = PrivateStuff.BigmodsNetworkCredential })
            {
                //download manager_version.xml
                ReportProgress("Downloading manager_version.xml from bigmods for app version");
                string managerVersionXml = await client.DownloadStringTaskAsync(PrivateStuff.BigmodsFTPModpackManager + Settings.ManagerVersion);
                string managerVersionXpath = @"/version/relhax_v2_stable";
                string managerVersion = XmlUtils.GetXmlStringFromXPath(managerVersionXml, managerVersionXpath, Settings.ManagerVersion);
                ReportProgress("Done, parsed as " + managerVersion);

                //download supported_clients.xml
                ReportProgress("Downloading supported_clients.xml from bigmods for db version");
                string supportedClientsXml = await client.DownloadStringTaskAsync(PrivateStuff.BigmodsFTPModpackManager + Settings.SupportedClients);
                //https://stackoverflow.com/questions/1459132/xslt-getting-last-element
                string supportedClientsXpath = @"(//version)[last()]";
                string supportedClientLast = XmlUtils.GetXmlStringFromXPath(supportedClientsXml, supportedClientsXpath, Settings.SupportedClients);
                ReportProgress("Done, parsed as " + supportedClientLast);

                //create new name
                string dateTimeFormat = string.Format("{0:yyyy_MM_dd}", DateTime.Now);
                string newFileName = string.Format("{0}_{1}_{2}_{3}.xml",dateTimeFormat, Path.GetFileNameWithoutExtension(InstallStatisticsXml),managerVersion,supportedClientLast);
                ReportProgress(string.Format("New filename parsed as '{0}', checking it doesn't exist on server", newFileName));

                //make sure name isn't on server already
                string[] filesOnServer = await FtpUtils.FtpListFilesFoldersAsync(PrivateStuff.BigmodsFTPModpackInstallStats, PrivateStuff.BigmodsNetworkCredential);
                if(filesOnServer.Contains(newFileName))
                {
                    ReportProgress("Already exists, abort");
                    return;
                }

                //upload to server
                ReportProgress("Does not exist, copying to new name");
                string instalStatsXmlText = await client.DownloadStringTaskAsync(PrivateStuff.BigmodsFTPModpackInstallStats + InstallStatisticsXml);
                await client.UploadStringTaskAsync(PrivateStuff.BigmodsFTPModpackInstallStats + newFileName, instalStatsXmlText);
                ReportProgress("Done");
            }
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private async void DatabaseStatsUpdateCurrent_Click(object sender, RoutedEventArgs e)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            List<DatabasePackage> globalDependencies = new List<DatabasePackage>();
            List<Dependency> dependencies = new List<Dependency>();
            List<Category> parsedCategoryList = new List<Category>();
            string currentInstallStatsXml = string.Empty;

            using (client = new WebClient { Credentials = PrivateStuff.BigmodsNetworkCredential })
            {
                //download supported_clients.xml
                ReportProgress("Downloading supported_clients.xml from bigmods for db version");
                string supportedClientsXml = await client.DownloadStringTaskAsync(PrivateStuff.BigmodsFTPModpackManager + Settings.SupportedClients);
                string supportedClientsXpath = @"(//version)[last()]";
                string supportedClientLast = XmlUtils.GetXmlStringFromXPath(supportedClientsXml, supportedClientsXpath, Settings.SupportedClients);
                ReportProgress("Done, parsed as " + supportedClientLast);

                ReportProgress("Loading current database from bigmods");
                if (!await LoadDatabase1V1FromBigmods(supportedClientLast, globalDependencies, dependencies, parsedCategoryList))
                {
                    ReportProgress("Failed to parse modInfo to lists");
                    ToggleUI((TabController.SelectedItem as TabItem), true);
                    return;
                }

                //download current install statistics
                ReportProgress("Downloading current install statistics");
                currentInstallStatsXml = await client.DownloadStringTaskAsync(PrivateStuff.BigmodsFTPModpackInstallStats + InstallStatisticsXml);
            }

            ReportProgress("Preparing lists for merge");
            XmlDocument installStats = XmlUtils.LoadXmlDocument(currentInstallStatsXml, XmlLoadType.FromString);
            List<DatabasePackage> flatList = DatabaseUtils.GetFlatList(globalDependencies, dependencies, null, parsedCategoryList);

            //replace any non existent entries in installStats with empty entries where installCount = 0
            foreach(DatabasePackage package in flatList)
            {
                //@"//package[@name='Dependency_global_WoT_xml_Creation']"
                string xPath = string.Format(@"//package[@name='{0}']", package.PackageName);
                XmlNode node = installStats.SelectSingleNode(xPath);
                if(node == null)
                {
                    ReportProgress(string.Format("Package '{0}' does not exist, adding to install stats",package.PackageName));
                    UiUtils.AllowUIToUpdate();
                    XmlElement element = installStats.CreateElement("package");
                    XmlAttribute nameAttribute = installStats.CreateAttribute("name");
                    nameAttribute.Value = package.PackageName;
                    XmlAttribute instalCountAttribute = installStats.CreateAttribute("installCount");
                    instalCountAttribute.Value = 0.ToString();
                    element.Attributes.Append(nameAttribute);
                    element.Attributes.Append(instalCountAttribute);
                    installStats.DocumentElement.AppendChild(element);
                }
            }

            //upload document back to server
            ReportProgress("Merge complete, uploading statistics back to server");
            using (client = new WebClient { Credentials = PrivateStuff.BigmodsNetworkCredential })
            {
                string xml = installStats.InnerXml;
                await client.UploadStringTaskAsync(PrivateStuff.BigmodsFTPModpackInstallStats + InstallStatisticsXml, xml);
            }
            ReportProgress("Done");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private async void DatabaseStatsMakeNew_Click(object sender, RoutedEventArgs e)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            List<DatabasePackage> globalDependencies = new List<DatabasePackage>();
            List<Dependency> dependencies = new List<Dependency>();
            List<Category> parsedCategoryList = new List<Category>();

            using (client = new WebClient { Credentials = PrivateStuff.BigmodsNetworkCredential })
            {
                //download supported_clients.xml
                ReportProgress("Downloading supported_clients.xml from bigmods for db version");
                string supportedClientsXml = await client.DownloadStringTaskAsync(PrivateStuff.BigmodsFTPModpackManager + Settings.SupportedClients);
                string supportedClientsXpath = @"(//version)[last()]";
                string supportedClientLast = XmlUtils.GetXmlStringFromXPath(supportedClientsXml, supportedClientsXpath, Settings.SupportedClients);
                ReportProgress("Done, parsed as " + supportedClientLast);

                ReportProgress("Loading current database from bigmods");
                if (!await LoadDatabase1V1FromBigmods(supportedClientLast, globalDependencies, dependencies, parsedCategoryList))
                {
                    ReportProgress("Failed to parse modInfo to lists");
                    ToggleUI((TabController.SelectedItem as TabItem), true);
                    return;
                }
            }

            ReportProgress("Creating new xml document");
            List<DatabasePackage> flatList = DatabaseUtils.GetFlatList(globalDependencies, dependencies, null, parsedCategoryList);
            XmlDocument doc = new XmlDocument();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            doc.AppendChild(xmlDeclaration);
            XmlElement root = doc.CreateElement(InstallStatisticsXml);
            doc.AppendChild(root);

            foreach (DatabasePackage package in flatList)
            {
                XmlElement element = doc.CreateElement("package");
                XmlAttribute nameAttribute = doc.CreateAttribute("name");
                nameAttribute.Value = package.PackageName;
                XmlAttribute instalCountAttribute = doc.CreateAttribute("installCount");
                instalCountAttribute.Value = 0.ToString();
                element.Attributes.Append(nameAttribute);
                element.Attributes.Append(instalCountAttribute);
                root.AppendChild(element);
            }

            ReportProgress("Document created, uploading to server");
            using (client = new WebClient { Credentials = PrivateStuff.BigmodsNetworkCredential })
            {
                string xml = doc.InnerXml;
                await client.UploadStringTaskAsync(PrivateStuff.BigmodsFTPModpackInstallStats + InstallStatisticsXml, xml);
            }
            ReportProgress("Done");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }
        #endregion

        #region DatabasePackage and UIDs checks
        List<Category> parsedCategoryListDuplicateCheck;
        List<DatabasePackage> globalDependenciesDuplicateCheck;
        List<Dependency> dependenciesDuplicateCheck;
        XmlDocument docDuplicateCheck;

        private void AnotherLoadDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            //init UI
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Loading database");

            OnLoadModInfo(null, null);

            //list creation and parsing
            parsedCategoryListDuplicateCheck = new List<Category>();
            globalDependenciesDuplicateCheck = new List<DatabasePackage>();
            dependenciesDuplicateCheck = new List<Dependency>();
            docDuplicateCheck = XmlUtils.LoadXmlDocument(SelectModInfo.FileName, XmlLoadType.FromFile);
            XmlUtils.ParseDatabase(docDuplicateCheck, globalDependenciesDuplicateCheck, dependenciesDuplicateCheck, parsedCategoryListDuplicateCheck, Path.GetDirectoryName(SelectModInfo.FileName));

            //link stuff in memory
            DatabaseUtils.BuildLinksRefrence(parsedCategoryListDuplicateCheck, false);
            DatabaseUtils.BuildLevelPerPackage(parsedCategoryListDuplicateCheck);
            DatabaseUtils.BuildDependencyPackageRefrences(parsedCategoryListDuplicateCheck, dependenciesDuplicateCheck);

            ReportProgress("Database loaded");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private void DatabaseDuplicatePNsCheck_Click(object sender, RoutedEventArgs e)
        {
            //init UI
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Checking for duplicate packageNames");

            //checks
            if (string.IsNullOrEmpty(Settings.WoTModpackOnlineFolderVersion))
            {
                ReportProgress("WoTModpackOnlineFolderVersion is empty");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }
            if ((docDuplicateCheck == null) || (dependenciesDuplicateCheck == null) || (globalDependenciesDuplicateCheck == null) || (parsedCategoryListDuplicateCheck == null))
            {
                ReportProgress("Lists not loaded yet for duplicate checks or adds");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            List<string> duplicatesList = DatabaseUtils.CheckForDuplicates(globalDependenciesDuplicateCheck, dependenciesDuplicateCheck, parsedCategoryListDuplicateCheck);

            if(duplicatesList == null || duplicatesList.Count == 0)
            {
                ReportProgress("No duplicates");
            }
            else
            {
                ReportProgress("The following packages are duplicate packageNames:");
                foreach (string s in duplicatesList)
                    ReportProgress(s);
            }

            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private void DatabaseDuplicateUIDsCheck_Click(object sender, RoutedEventArgs e)
        {
            //init UI
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Checking for duplicate UIDs");

            //checks
            if (string.IsNullOrEmpty(Settings.WoTModpackOnlineFolderVersion))
            {
                ReportProgress("WoTModpackOnlineFolderVersion is empty");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }
            if ((docDuplicateCheck == null) || (dependenciesDuplicateCheck == null) || (globalDependenciesDuplicateCheck == null) || (parsedCategoryListDuplicateCheck == null))
            {
                ReportProgress("Lists not loaded yet for duplicate checks or adds");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            List<DatabasePackage> duplicatesList = DatabaseUtils.CheckForDuplicateUIDsPackageList(globalDependenciesDuplicateCheck, dependenciesDuplicateCheck, parsedCategoryListDuplicateCheck);

            if (duplicatesList.Count == 0)
            {
                ReportProgress("No duplicates");
            }
            else
            {
                ReportProgress("The following packages are duplicate UIDs:");
                foreach (DatabasePackage package in duplicatesList)
                    ReportProgress(string.Format("PackageName: {0}, UID: {1}",package.PackageName,package.UID));
            }

            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private async void AddMissingUIDs_Click(object sender, RoutedEventArgs e)
        {
            //init UI
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Checking for missing UIDs and adding");

            //checks
            if (string.IsNullOrEmpty(Settings.WoTModpackOnlineFolderVersion))
            {
                ReportProgress("WoTModpackOnlineFolderVersion is empty");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }
            if ((docDuplicateCheck == null) || (dependenciesDuplicateCheck == null) || (globalDependenciesDuplicateCheck == null) || (parsedCategoryListDuplicateCheck == null))
            {
                ReportProgress("Lists not loaded yet for duplicate checks or adds");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            //create a flat list
            List<DatabasePackage> allPackages = DatabaseUtils.GetFlatList(globalDependenciesDuplicateCheck, dependenciesDuplicateCheck, null, parsedCategoryListDuplicateCheck);

            foreach (DatabasePackage packageToAddUID in allPackages)
            {
                if(string.IsNullOrWhiteSpace(packageToAddUID.UID))
                {
                    await Task.Run(() =>
                    {
                        packageToAddUID.UID = CommonUtils.GenerateUID(allPackages);
                    });
                    ReportProgress(string.Format("Package {0} got generated UID {1}", packageToAddUID.PackageName, packageToAddUID.UID));
                }
            }

            ReportProgress("Done");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private void SaveDatabaseDuplicateCheckButton_Click(object sender, RoutedEventArgs e)
        {
            //init UI
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Saving Database");

            //checks
            if ((docDuplicateCheck == null) || (dependenciesDuplicateCheck == null) || (globalDependenciesDuplicateCheck == null) || (parsedCategoryListDuplicateCheck == null))
            {
                ReportProgress("Lists not loaded yet for duplicate checks or adds");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            if(SelectModInfoSave.ShowDialog() == false)
            {
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            string fullDatabasePath = Path.Combine(Path.GetDirectoryName(SelectModInfoSave.FileName), Settings.BetaDatabaseV2RootFilename);
            XmlUtils.SaveDatabase(fullDatabasePath, Settings.WoTClientVersion, Settings.WoTModpackOnlineFolderVersion, globalDependenciesDuplicateCheck, dependenciesDuplicateCheck, parsedCategoryListDuplicateCheck, DatabaseXmlVersion.OnePointOne);

            ReportProgress("Database saved");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }
        #endregion

        #region Supported_Clients updating

        private void LoadDatabaseUpdateSupportedClientsButton_Click(object sender, RoutedEventArgs e)
        {
            //init UI
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Loading database");

            OnLoadModInfo(null, null);
            
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private void LoadSupportedClientsDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress(string.Format("Loading {0}", Settings.SupportedClients));

            if(!(bool)SelectSupportedClientsXml.ShowDialog())
            {
                ReportProgress("Canceled");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            ReportProgress("Done");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private async void CheckClientsToRemoveFromDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Remove clients from xml and server");

            //checks
            if (string.IsNullOrEmpty(Settings.WoTModpackOnlineFolderVersion))
            {
                ReportProgress("WoTModpackOnlineFolderVersion is empty");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            if (string.IsNullOrEmpty(SelectModInfo.FileName))
            {
                ReportProgress("SelectModInfo filename is empty");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            if (string.IsNullOrEmpty(SelectSupportedClientsXml.FileName))
            {
                ReportProgress("SelectSupportedClientsXml Filename is empty");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            //parse supported_clients.xml
            XmlDocument supportedClients = await ParseVersionInfoXmlDoc(SelectSupportedClientsXml.FileName);

            //get list of WoT online folders (ftp)
            ReportProgress("Getting a list of supported WoT clients by major version from the ftp server's folder list");
            string[] foldersList = await FtpUtils.FtpListFilesFoldersAsync(PrivateStuff.BigmodsFTPRootWoT, PrivateStuff.BigmodsNetworkCredential);

            //check any online folder clients where no matching server folder name and remove
            bool anyEntriesRemoved = false;
            XmlNodeList supportedClientsXmlList = XmlUtils.GetXmlNodesFromXPath(supportedClients, "//versions/version");
            for(int i = 0; i < supportedClientsXmlList.Count; i++)
            {
                XmlElement version = supportedClientsXmlList[i] as XmlElement;
                string onlineFolderVersion = version.Attributes["folder"].InnerText;

                if(!foldersList.Contains(onlineFolderVersion))
                {
                    ReportProgress(string.Format("Version {0} (online folder {1}) is not supported and will be removed", version.InnerText, onlineFolderVersion));
                    version.ParentNode.RemoveChild(version);
                    ReportProgress("Also removing the folder on the server if exists");
                    string[] databaseVersions = (await FtpUtils.FtpListFilesFoldersAsync(PrivateStuff.BigmodsFTPModpackDatabase, PrivateStuff.BigmodsNetworkCredential));
                    if (databaseVersions.Contains(version.InnerText))
                    {
                        string folderUrl = PrivateStuff.BigmodsFTPModpackDatabase + version.InnerText + "/";
                        await FtpUtils.FtpDeleteFolderAsync(folderUrl, PrivateStuff.BigmodsNetworkCredential);
                    }
                    anyEntriesRemoved = true;
                }
                else
                {
                    ReportProgress(string.Format("Version {0} (online folder {1}) exists", version.InnerText, onlineFolderVersion));
                }
            }

            if (anyEntriesRemoved)
            {
                ReportProgress("Entries removed, saving file back to disk and upload");
                supportedClients.Save(SelectSupportedClientsXml.FileName);

                using (client = new WebClient() { Credentials = PrivateStuff.BigmodsNetworkCredential })
                {
                    await client.UploadFileTaskAsync(PrivateStuff.BigmodsFTPModpackManager + Settings.SupportedClients, SelectSupportedClientsXml.FileName);
                }
            }
            else
            {
                ReportProgress("No entries removed");
            }

            ReportProgress("Done");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }

        private async void CheckClientsToAddToDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleUI((TabController.SelectedItem as TabItem), false);
            ReportProgress("Add clients to and upload supported_clients.xml");
            
            //checks
            if (string.IsNullOrEmpty(Settings.WoTModpackOnlineFolderVersion))
            {
                ReportProgress("WoTModpackOnlineFolderVersion is empty");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            if (string.IsNullOrEmpty(SelectModInfo.FileName))
            {
                ReportProgress("SelectModInfo filename is empty");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            if (string.IsNullOrEmpty(SelectSupportedClientsXml.FileName))
            {
                ReportProgress("SelectSupportedClientsXml Filename is empty");
                ToggleUI((TabController.SelectedItem as TabItem), true);
                return;
            }

            //parse supported_clients.xml
            XmlDocument supportedClients = await ParseVersionInfoXmlDoc(SelectSupportedClientsXml.FileName);

            //if loaded database's wot version is new, then add it to the document
            // "/versions/version[text()='1.8.0.1']"
            string xpathString = string.Format(@"/versions/version[text()='{0}']",Settings.WoTClientVersion);
            XmlNode selectedVersion = XmlUtils.GetXmlNodeFromXPath(supportedClients, xpathString);
            if(selectedVersion == null)
            {
                ReportProgress("Does not exist in the document, adding");
                //select the document root node
                XmlNode versionRoot = supportedClients.SelectSingleNode("/versions");

                //create the version element and set attributes and text
                XmlElement supported_client = supportedClients.CreateElement("version");
                supported_client.InnerText = Settings.WoTClientVersion;
                supported_client.SetAttribute("folder", Settings.WoTModpackOnlineFolderVersion);

                //add element to document at the end
                versionRoot.AppendChild(supported_client);

                ReportProgress("Entry added, saving file back to disk and upload");
                supportedClients.Save(SelectSupportedClientsXml.FileName);

                using (client = new WebClient() { Credentials = PrivateStuff.BigmodsNetworkCredential })
                {
                    await client.UploadFileTaskAsync(PrivateStuff.BigmodsFTPModpackManager + Settings.SupportedClients, SelectSupportedClientsXml.FileName);
                }
            }
            else
            {
                //does exist, no further action needed
                ReportProgress("Already exists");
            }

            ReportProgress("Done");
            ToggleUI((TabController.SelectedItem as TabItem), true);
        }
        #endregion
    }
}
