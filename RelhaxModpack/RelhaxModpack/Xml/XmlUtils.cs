using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Windows;
using System.Reflection;
using System.Text;
using Ionic.Zip;
using RelhaxModpack.Xml;
using RelhaxModpack.Database;
using System.Net;
using System.Threading.Tasks;
using RelhaxModpack.Utilities;
using RelhaxModpack.Atlases;
using RelhaxModpack.Patches;
using RelhaxModpack.Shortcuts;

namespace RelhaxModpack.Xml
{
    /// <summary>
    /// Xml document load type enumeration
    /// </summary>
    public enum XmlLoadType
    {
        /// <summary>
        /// loading Xml from a file on disk
        /// </summary>
        FromFile,
        /// <summary>
        /// loading Xml from a text string
        /// </summary>
        FromString
    }

    /// <summary>
    /// The enumeration representations of the Xml database saving format
    /// </summary>
    public enum DatabaseXmlVersion
    {
        /// <summary>
        /// The Legacy format. All in one document
        /// </summary>
        Legacy,

        /// <summary>
        /// The 1.1 format. A root file, a file for the global and standard dependencies, and a file for each categories
        /// </summary>
        OnePointOne
    }

    /// <summary>
    /// Utility class for dealing with Xml features and functions
    /// </summary>
    public static class XmlUtils
    {
        #region Xml Validating
        /// <summary>
        /// Check to make sure an Xml file is valid
        /// </summary>
        /// <param name="filePath">The path to the Xml file</param>
        /// <returns>True if valid Xml, false otherwise</returns>
        public static bool IsValidXml(string filePath)
        {
            return IsValidXml(File.ReadAllText(filePath), Path.GetFileName(filePath));
        }

        /// <summary>
        /// Check to make sure an Xml file is valid
        /// </summary>
        /// <param name="xmlString">The Xml text string</param>
        /// <param name="fileName">the name of the file, used for debugging purposes</param>
        /// <returns>True if valid Xml, false otherwise</returns>
        public static bool IsValidXml(string xmlString, string fileName)
        {
            XmlTextReader read = new XmlTextReader(xmlString);
            try
            {
                //continue to read the entire document
                while (read.Read()) ;
                read.Close();
                read.Dispose();
                return true;
            }
            catch (Exception e)
            {
                Logging.Error("Invalid Xml file: {0}\n{1}",fileName,e.Message);
                read.Close();
                read.Dispose();
                return false;
            }
        }
        #endregion

        #region Xpath stuff
        /// <summary>
        /// Get an Xml element attribute given an Xml path
        /// </summary>
        /// <param name="file">The path to the Xml file</param>
        /// <param name="xpath">The xpath search string</param>
        /// <returns>The value from the xpath search, otherwise null</returns>
        public static string GetXmlStringFromXPath(string file, string xpath)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(file);
            }
            catch (Exception e)
            {
                Logging.WriteToLog(string.Format("Failed to read Xml file {0}\n{1}", Path.GetFileName(file),e.Message), Logfiles.Application, LogLevel.Error);
                return null;
            }
            return GetXmlStringFromXPath(doc, xpath);
        }

        /// <summary>
        /// Get an Xml element attribute given an Xml path
        /// </summary>
        /// <param name="xmlString">The Xml text string</param>
        /// <param name="xpath">The xpath search string</param>
        /// <param name="filename"></param>
        /// <returns>The value from the xpath search, otherwise null</returns>
        public static string GetXmlStringFromXPath(string xmlString, string xpath, string filename)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(xmlString);
            }
            catch (Exception e)
            {
                Logging.WriteToLog(string.Format("Failed to read Xml file {0}\n{1}", Path.GetFileName(filename), e.Message), Logfiles.Application, LogLevel.Error);
                return null;
            }
            return GetXmlStringFromXPath(doc, xpath);
        }
        
        /// <summary>
        /// Get an Xml element attribute given an Xml path
        /// </summary>
        /// <param name="doc">The Xml document object to check</param>
        /// <param name="xpath">The xpath search string</param>
        /// <returns>The value from the xpath search, otherwise null</returns>
        /// <remarks>
        /// The following are Xml attribute examples
        /// element example: "//root/element"
        /// attribute example: "//root/element/@attribute"
        /// for the onlineFolder version: //modInfoAlpha.xml/@onlineFolder
        /// for the folder version: //modInfoAlpha.xml/@version
        /// </remarks>
        public static string GetXmlStringFromXPath(XmlDocument doc, string xpath)
        {
            //set to something dumb for temporary purposes
            XmlNode result;
            try
            {
                result = doc.SelectSingleNode(xpath);
            }
            catch
            {
                return null;
            }
            if (result == null)
                return null;
            return result.InnerText;
        }

        /// <summary>
        /// Get an Xml node value given an Xml path
        /// </summary>
        /// <param name="file">The path to the Xml file</param>
        /// <param name="xpath">The xpath search string</param>
        /// <returns>The Xml node object of the search result, or null</returns>
        public static XmlNode GetXmlNodeFromXPath(string file, string xpath)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(file);
            }
            catch (Exception e)
            {
                Logging.WriteToLog(string.Format("Failed to read Xml file {0}\n{1}", Path.GetFileName(file), e.Message), Logfiles.Application, LogLevel.Error);
                return null;
            }
            return GetXmlNodeFromXPath(doc, xpath);
        }

        /// <summary>
        /// Get an Xml node value given an Xml path
        /// </summary>
        /// <param name="xmlString">The Xml string to parse</param>
        /// <param name="xpath">The xpath search string</param>
        /// <param name="filename">The name of the file, used for logging purposes</param>
        /// <returns>The Xml node object of the search result, or null</returns>
        public static XmlNode GetXmlNodeFromXPath(string xmlString, string xpath, string filename)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(xmlString);
            }
            catch (Exception e)
            {
                Logging.WriteToLog(string.Format("Failed to read Xml file {0}\n{1}", Path.GetFileName(filename), e.Message), Logfiles.Application, LogLevel.Error);
                return null;
            }
            return GetXmlNodeFromXPath(doc, xpath);
        }

        /// <summary>
        /// Get an Xml node value given an Xml path
        /// </summary>
        /// <param name="doc">The XmlDocument object to search</param>
        /// <param name="xpath">The xpath string</param>
        /// <returns>The Xml node object of the search result, or null</returns>
        public static XmlNode GetXmlNodeFromXPath(XmlDocument doc, string xpath)
        {
          XmlNode result = doc.SelectSingleNode(xpath);
          if (result == null)
              return null;
          return result;
        }

        /// <summary>
        /// Get a List of Xml nodes that match given an Xml path
        /// </summary>
        /// <param name="file">The path to the Xml file</param>
        /// <param name="xpath">The xpath string</param>
        /// <returns>The node list of matching results, or null</returns>
        public static XmlNodeList GetXmlNodesFromXPath(string file, string xpath)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(file);
            }
            catch (Exception e)
            {
                Logging.WriteToLog(string.Format("Failed to read Xml file {0}\n{1}", Path.GetFileName(file), e.Message), Logfiles.Application, LogLevel.Error);
                return null;
            }
            return GetXmlNodesFromXPath(doc, xpath);
        }

        /// <summary>
        /// Get a List of Xml nodes that match given an Xml path
        /// </summary>
        /// <param name="xmlString">The xml document in a string</param>
        /// <param name="filename">The name of the document for logging purposes</param>
        /// <param name="xpath">The xpath string</param>
        /// <returns>The node list of matching results, or null</returns>
        public static XmlNodeList GetXmlNodesFromXPath(string xmlString, string xpath, string filename)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(xmlString);
            }
            catch (Exception e)
            {
                Logging.WriteToLog(string.Format("Failed to read Xml file {0}\n{1}", Path.GetFileName(filename), e.Message), Logfiles.Application, LogLevel.Error);
                return null;
            }
            return GetXmlNodesFromXPath(doc, xpath);
        }


        /// <summary>
        /// Get a List of Xml nodes that match given an Xml path
        /// </summary>
        /// <param name="doc">The XmlDocument to search</param>
        /// <param name="xpath">The xml path string</param>
        /// <returns>The node list of matching results, or null</returns>
        /// <remarks>XmlElement and XmlAttribute inherit from XmlNode</remarks> 
        public static XmlNodeList GetXmlNodesFromXPath(XmlDocument doc, string xpath)
        {
            XmlNodeList results = doc.SelectNodes(xpath);
          if (results == null || results.Count == 0)
          {
            return null;
          }
          return results;
        }
        #endregion

        #region Database Loading
        /// <summary>
        /// Downloads the root 'database.xml' file from github using the selected branch and loads it to an XmlDocument object
        /// </summary>
        /// <returns>the XmlDocument object of the root database file</returns>
        public static XmlDocument GetBetaDatabaseRoot1V1Document()
        {
            XmlDocument rootDocument = null;
            using (WebClient client = new WebClient())
            {
                //load string constant url from manager info xml
                string rootXml = Settings.BetaDatabaseV2FolderURL + Settings.BetaDatabaseV2RootFilename;

                //download the xml string into "modInfoXml"
                client.Headers.Add("user-agent", "Mozilla / 4.0(compatible; MSIE 6.0; Windows NT 5.2;)");
                rootDocument = LoadXmlDocument(client.DownloadString(rootXml), XmlLoadType.FromString);
            }
            return rootDocument;
        }

        /// <summary>
        /// Get a list of all URLs to each xml document file of the database
        /// </summary>
        /// <param name="rootDocument">The root 'database.xml' document object</param>
        /// <returns>The list of URLs</returns>
        public static List<string> GetBetaDatabase1V1FilesList(XmlDocument rootDocument)
        {
            List<string> databaseFiles = new List<string>()
            {
                Settings.BetaDatabaseV2FolderURL + GetXmlStringFromXPath(rootDocument, "/modInfoAlpha.xml/globalDependencies/@file"),
                Settings.BetaDatabaseV2FolderURL + GetXmlStringFromXPath(rootDocument, "/modInfoAlpha.xml/dependencies/@file")
            };

            //categories
            foreach (XmlNode categoryNode in GetXmlNodesFromXPath(rootDocument, "//modInfoAlpha.xml/categories/category"))
            {
                string categoryFileName = categoryNode.Attributes["file"].Value;
                databaseFiles.Add(Settings.BetaDatabaseV2FolderURL + categoryFileName);
            }

            return databaseFiles.Select(name => name.Replace(".Xml", ".xml")).ToList();
        }

        /// <summary>
        /// Get a list of all URLs to each xml document file of the database using the selected branch and loads it to an XmlDocument object
        /// </summary>
        /// <returns>The list of URLs</returns>
        public static List<string> GetBetaDatabase1V1FilesList()
        {
            XmlDocument rootDocument = GetBetaDatabaseRoot1V1Document();

            List<string> databaseFiles = new List<string>()
            {
                Settings.BetaDatabaseV2FolderURL + GetXmlStringFromXPath(rootDocument, "/modInfoAlpha.xml/globalDependencies/@file"),
                Settings.BetaDatabaseV2FolderURL + GetXmlStringFromXPath(rootDocument, "/modInfoAlpha.xml/dependencies/@file")
            };

            //categories
            foreach (XmlNode categoryNode in GetXmlNodesFromXPath(rootDocument, "//modInfoAlpha.xml/categories/category"))
            {
                string categoryFileName = categoryNode.Attributes["file"].Value;
                databaseFiles.Add(Settings.BetaDatabaseV2FolderURL + categoryFileName);
            }

            return databaseFiles.Select(name => name.Replace(".Xml",".xml")).ToList();
        }

        /// <summary>
        /// Get a list of all URLs to each xml document file of the database using the selected branch and loads it to an XmlDocument object
        /// </summary>
        /// <returns>The list of URLs</returns>
        public async static Task<List<string>> GetBetaDatabase1V1FilesListAsync()
        {
            return await Task<List<string>>.Run(() => GetBetaDatabase1V1FilesList());
        }

        /// <summary>
        /// Parse the Xml database of any type into lists in memory
        /// </summary>
        /// <param name="modInfoDocument">The root document. Can either be the entire document (Legacy) or the file with database header information only (OnePointOne)</param>
        /// <param name="globalDependencies">The global dependencies list</param>
        /// <param name="dependencies">The dependencies list</param>
        /// <param name="parsedCategoryList">The category list</param>
        /// <param name="location">The folder path to the OnePointOne+ additional xml files as part of the database</param>
        /// <returns>True if database parsing was a success, false otherwise</returns>
        public static bool ParseDatabase(XmlDocument modInfoDocument, List<DatabasePackage> globalDependencies,
            List<Dependency> dependencies, List<Category> parsedCategoryList, string location = null)
        {
            Logging.Debug("[ParseDatabase]: Determining database version for parsing");
            //check all input parameters
            if (modInfoDocument == null)
                throw new ArgumentNullException("modInfoDocument is null");
            if (globalDependencies == null)
                throw new ArgumentNullException("lists cannot be null");
            else
                globalDependencies.Clear();
            if (dependencies == null)
                throw new ArgumentNullException("lists cannot be null");
            else
                dependencies.Clear();
            if (parsedCategoryList == null)
                throw new ArgumentNullException("lists cannot be null");
            else
                parsedCategoryList.Clear();

            //determine which version of the document we are loading. allows for loading of different versions if structure change
            //a blank value is assumed to be pre 2.0 version of the database
            string versionString = GetXmlStringFromXPath(modInfoDocument, "//modInfoAlpha.xml/@documentVersion");
            Logging.Info("[ParseDatabase]: VersionString parsed as '{0}'", versionString);
            if (string.IsNullOrEmpty(versionString))
                Logging.Info("[ParseDatabase]: VersionString is null or empty, treating as legacy");

            switch(versionString)
            {
                case "1.1":
                    return ParseDatabase1V1FromFiles(location, modInfoDocument, globalDependencies, dependencies, parsedCategoryList);
                default:
                    //parse legacy database
                    List<Dependency> logicalDependencies = new List<Dependency>();
#pragma warning disable CS0612
                    ParseDatabaseLegacy(DocumentToXDocument(modInfoDocument), globalDependencies, dependencies, logicalDependencies,
                        parsedCategoryList, true);
#pragma warning restore CS0612
                    dependencies.AddRange(logicalDependencies);
                    return true;
            }
        }

        /// <summary>
        /// Parse a database into the version 1.1 format from files on the disk
        /// </summary>
        /// <param name="rootPath">The path to the folder that contains all the xml database files</param>
        /// <param name="rootDocument">The root document object</param>
        /// <param name="globalDependencies">The list of global dependencies</param>
        /// <param name="dependencies">The list of dependencies</param>
        /// <param name="parsedCategoryList">The list of categories</param>
        /// <returns></returns>
        public static bool ParseDatabase1V1FromFiles(string rootPath, XmlDocument rootDocument, List<DatabasePackage> globalDependencies,
            List<Dependency> dependencies, List<Category> parsedCategoryList)
        {
            //load each document to make sure they all exist first
            if(string.IsNullOrWhiteSpace(rootPath))
            {
                Logging.Error("location string is empty in ParseDatabase1V1");
                return false;
            }

            //document for global dependencies
            string completeFilepath = Path.Combine(rootPath, GetXmlStringFromXPath(rootDocument, "/modInfoAlpha.xml/globalDependencies/@file"));
            if (!File.Exists(completeFilepath))
            {
                Logging.Error("{0} file does not exist at {1}", "Global Dependency", completeFilepath);
                return false;
            }
            XDocument globalDepsDoc = LoadXDocument(completeFilepath, XmlLoadType.FromFile);
            if (globalDepsDoc == null)
                throw new BadMemeException("this should not be null");

            //document for dependencies
            completeFilepath = Path.Combine(rootPath, GetXmlStringFromXPath(rootDocument, "/modInfoAlpha.xml/dependencies/@file"));
            if (!File.Exists(completeFilepath))
            {
                Logging.Error("{0} file does not exist at {1}", "Dependency", completeFilepath);
                return false;
            }
            XDocument depsDoc = LoadXDocument(completeFilepath, XmlLoadType.FromFile);
            if (depsDoc == null)
                throw new BadMemeException("this should not be null");

            //list of documents for categories
            List<XDocument> categoryDocuments = new List<XDocument>();
            foreach(XmlNode categoryNode in GetXmlNodesFromXPath(rootDocument, "//modInfoAlpha.xml/categories/category"))
            {
                //make string path
                completeFilepath = Path.Combine(rootPath, categoryNode.Attributes["file"].Value);

                //check if file exists
                if (!File.Exists(completeFilepath))
                {
                    Logging.Error("{0} file does not exist at {1}", "Category", completeFilepath);
                    return false;
                }

                //load xdocument of category from category file
                XDocument catDoc = LoadXDocument(completeFilepath, XmlLoadType.FromFile);
                if (catDoc == null)
                    throw new BadMemeException("this should not be null");

                //add Xml cat to list
                categoryDocuments.Add(catDoc);
            }
            //run the loading method
            return ParseDatabase1V1(globalDepsDoc, globalDependencies, depsDoc, dependencies, categoryDocuments, parsedCategoryList);
        }

        /// <summary>
        /// Parse a database into version 1.1 from string representations of the Xml files
        /// </summary>
        /// <param name="globalDependenciesXml">The Xml string of the global dependencies document</param>
        /// <param name="dependneciesXml">The Xml string of the dependencies document</param>
        /// <param name="categoriesXml">The list of Xml strings of the categories document</param>
        /// <param name="globalDependencies">The list of global dependencies</param>
        /// <param name="dependencies">The list of dependencies</param>
        /// <param name="parsedCategoryList">The list of categories</param>
        /// <returns></returns>
        public static bool ParseDatabase1V1FromStrings(string globalDependenciesXml, string dependneciesXml, List<string> categoriesXml,
            List<DatabasePackage> globalDependencies, List<Dependency> dependencies, List<Category> parsedCategoryList)
        {
            Logging.Debug(LogOptions.MethodName, "Parsing global dependencies");
            XDocument globalDependenciesdoc = LoadXDocument(globalDependenciesXml, XmlLoadType.FromString);

            Logging.Debug(LogOptions.MethodName, "Parsing dependencies");
            XDocument dependenciesdoc = LoadXDocument(dependneciesXml, XmlLoadType.FromString);

            Logging.Debug(LogOptions.MethodName, "Parsing categories");
            List<XDocument> categoryDocuments = new List<XDocument>();
            foreach(string category in categoriesXml)
            {
                categoryDocuments.Add(LoadXDocument(category, XmlLoadType.FromString));
            }

            //check if any of the databases failed to parse
            if(globalDependenciesdoc == null)
            {
                Logging.Error("Failed to parse global dependencies xml");
                return false;
            }

            if(dependenciesdoc == null)
            {
                Logging.Error("Failed to parse dependencies xml");
                return false;
            }

            for(int i = 0; i < categoryDocuments.Count; i++)
            {
                if(categoryDocuments[i] == null)
                {
                    Logging.Error("Category document index {0} failed to parse", i);
                    return false;
                }
            }

            return ParseDatabase1V1(globalDependenciesdoc, globalDependencies, dependenciesdoc, dependencies, categoryDocuments, parsedCategoryList);
        }

        /// <summary>
        /// Parse a database into version 1.1 from XDocument objects
        /// </summary>
        /// <param name="globalDependenciesDoc">The Xml document of global dependencies</param>
        /// <param name="globalDependenciesList">The list of global dependencies</param>
        /// <param name="dependenciesDoc">The Xml document of dependencies</param>
        /// <param name="dependenciesList">The list of dependencies</param>
        /// <param name="categoryDocuments">The list of xml documents of the category Xml documents</param>
        /// <param name="parsedCategoryList">The list of categories</param>
        /// <returns></returns>
        private static bool ParseDatabase1V1(XDocument globalDependenciesDoc, List<DatabasePackage> globalDependenciesList, XDocument dependenciesDoc,
            List<Dependency> dependenciesList, List<XDocument> categoryDocuments, List<Category> parsedCategoryList)
        {
            //parsing the global dependencies
            Logging.Debug("[ParseDatabase1V1]: Parsing GlobalDependencies");
            bool globalParsed = ParseDatabase1V1Packages(globalDependenciesDoc.XPathSelectElements("/GlobalDependencies/GlobalDependency").ToList(), globalDependenciesList);

            //parsing the logical dependnecies
            Logging.Debug("[ParseDatabase1V1]: Parsing Dependencies");
            bool depsParsed = ParseDatabase1V1Packages(dependenciesDoc.XPathSelectElements("/Dependencies/Dependency").ToList(), dependenciesList);

            //parsing the categories
            bool categoriesParsed = true;
            for (int i = 0; i < categoryDocuments.Count; i++)
            {
                Category cat = new Category()
                {
                    //https://stackoverflow.com/questions/18887061/getting-attribute-value-using-xelement
                    Name = categoryDocuments[i].Root.Attribute("Name").Value
                };
                XElement result = categoryDocuments[i].XPathSelectElement(@"/Category/Maintainers");
                if (result != null)
                    cat.Maintainers = result.Value.ToString();

                //parse the list of dependencies from Xml for the categories into the category in list
                Logging.Debug("[ParseDatabase1V1]: Parsing Dependency references for category {0}", cat.Name);
                IEnumerable<XElement> listOfDependencies = categoryDocuments[i].XPathSelectElements("//Category/Dependencies/Dependency");
                CommonUtils.SetListEntries(cat, cat.GetType().GetProperty(nameof(cat.Dependencies)), listOfDependencies);

                //parse the list of packages
                Logging.Debug("[ParseDatabase1V1]: Parsing Packages for category {0}", cat.Name);
                if (!ParseDatabase1V1Packages(categoryDocuments[i].XPathSelectElements("/Category/Package").ToList(), cat.Packages))
                {
                    categoriesParsed = false;
                }
                parsedCategoryList.Add(cat);
            }
            return globalParsed && depsParsed && categoriesParsed;
        }

        private static bool ParseDatabase1V1Packages(List<XElement> xmlPackageNodesList, IList genericPackageList)
        {
            //get the type of element in the list
            Type listObjectType = genericPackageList.GetType().GetInterfaces().Where(i => i.IsGenericType && i.GenericTypeArguments.Length == 1)
                .FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IEnumerable<>)).GenericTypeArguments[0];

            //for each Xml package entry
            foreach (XElement xmlPackageNode in xmlPackageNodesList)
            {
                //make sure object type is properly implemented into serialization system
                if (!(Activator.CreateInstance(listObjectType) is IXmlSerializable listEntry))
                    throw new BadMemeException("Type of this list is not of IXmlSerializable");

                IDatabaseComponent componentWithID = (IDatabaseComponent)listEntry;

                //create attribute and element unknown and missing lists
                List<string> unknownAttributes = new List<string>();
                List<string> missingAttributes = new List<string>(listEntry.PropertiesForSerializationAttributes());
                List<string> unknownElements = new List<string>();

                //first deal with the Xml attributes in the entry
                foreach (XAttribute attribute in xmlPackageNode.Attributes())
                {
                    string attributeName = attribute.Name.LocalName;

                    //check if the whitelist contains it
                    if (!listEntry.PropertiesForSerializationAttributes().Contains(attributeName))
                    {
                        Logging.Debug("Member {0} from Xml attribute does not exist in fieldInfo", attributeName);
                        unknownAttributes.Add(attributeName);
                        continue;
                    }

                    //get the propertyInfo object representing the same name corresponding field or property in the memory database entry
                    PropertyInfo property = listObjectType.GetProperty(attributeName);

                    //check if attribute exists in class object
                    if (property == null)
                    {
                        Logging.Error("Property (xml attribute) {0} exists in array for serialization, but not in class design!, ", attributeName);
                        Logging.Error("Package: {0}, line: {1}", componentWithID.ComponentInternalName, ((IXmlLineInfo)xmlPackageNode).LineNumber);
                        continue;
                    }

                    missingAttributes.Remove(attributeName);

                    if(!CommonUtils.SetObjectProperty(listEntry,property,attribute.Value))
                    {
                        Logging.Error("Failed to set member {0}, default (if exists) was used instead, PackageName: {1}, LineNumber {2}",
                            attributeName, componentWithID.ComponentInternalName, ((IXmlLineInfo)xmlPackageNode).LineNumber);
                    }
                }

                //list any unknown or missing attributes
                foreach(string unknownAttribute in unknownAttributes)
                {
                    Logging.Error("Unknown Attribute from Xml node not in whitelist or memberInfo: {0}, PackageName: {1}, LineNumber {2}",
                        unknownAttribute, componentWithID.ComponentInternalName, ((IXmlLineInfo)xmlPackageNode).LineNumber);
                }
                foreach(string missingAttribute in missingAttributes)
                {
                    Logging.Error("Missing required attribute not in xmlInfo: {0}, PackageName: {1}, LineNumber {2}",
                        missingAttribute, componentWithID.ComponentInternalName, ((IXmlLineInfo)xmlPackageNode).LineNumber);
                }

                //now deal with element values. no need to log what isn't set (elements are optional)
                foreach(XElement element in xmlPackageNode.Elements())
                {
                    string elementName = element.Name.LocalName;

                    //check if the whitelist contains it
                    if (!listEntry.PropertiesForSerializationElements().Contains(elementName))
                    {
                        Logging.Debug("member {0} from Xml attribute does not exist in fieldInfo", elementName);
                        unknownElements.Add(elementName);
                        continue;
                    }

                    //get the propertyInfo object representing the same name corresponding field or property in the memory database entry
                    PropertyInfo property = listObjectType.GetProperty(elementName);

                    //check if attribute exists in class object
                    if (property == null)
                    {
                        Logging.Error("Property (xml attribute) {0} exists in array for serialization, but not in class design!, ", elementName);
                        Logging.Error("Package: {0}, line: {1}", componentWithID.ComponentInternalName, ((IXmlLineInfo)element).LineNumber);
                        continue;
                    }

                    //if it's a package entry, we need to recursivly procsses it
                    if (componentWithID is SelectablePackage throwAwayPackage && elementName.Equals(nameof(throwAwayPackage.Packages)))
                    {
                        //need hard code special case for Packages
                        ParseDatabase1V1Packages(element.Elements().ToList(), throwAwayPackage.Packages);
                    }

                    //if the object is a list type, we need to parse the list first
                    //https://stackoverflow.com/questions/4115968/how-to-tell-whether-a-type-is-a-list-or-array-or-ienumerable-or
                    else if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && !property.PropertyType.Equals(typeof(string)))
                    {
                        CommonUtils.SetListEntries(componentWithID, property, xmlPackageNode.Element(element.Name).Elements());
                    }
                    else if (!CommonUtils.SetObjectProperty(componentWithID,property,element.Value))
                    {
                        Logging.Error("Failed to set member {0}, default (if exists) was used instead, PackageName: {1}, LineNumber {2}",
                            element.Name.LocalName, componentWithID.ComponentInternalName, ((IXmlLineInfo)element).LineNumber);
                    }
                }

                //list any unknown attributes here
                foreach (string unknownelement in unknownElements)
                {
                    //log it here
                    Logging.Error("Unknown Element from Xml node not in whitelist or memberInfo: {0}, PackageName: {1}, LineNumber {2}",
                        unknownelement, componentWithID.ComponentInternalName, ((IXmlLineInfo)xmlPackageNode).LineNumber);
                }

                //add it to the internal memory list
                genericPackageList.Add(componentWithID);
            }
            return true;
        }
        #endregion

        #region Other Xml stuffs
        /// <summary>
        /// Convert an XmlDocument to an XDocument
        /// </summary>
        /// <param name="doc">The XmlDocument to convert</param>
        /// <returns>The converted XDocument</returns>
        /// <remarks>See https://blogs.msdn.microsoft.com/xmlteam/2009/03/31/converting-from-xmldocument-to-xdocument/ </remarks>
        public static XDocument DocumentToXDocument(XmlDocument doc)
        {
            if (doc == null)
                return null;
            return XDocument.Parse(doc.OuterXml,LoadOptions.SetLineInfo);
        }

        /// <summary>
        /// Load an Xml document to an XDocument object
        /// </summary>
        /// <param name="fileOrXml">The filepath or string representation of the Xml document</param>
        /// <param name="type">The type to define if fileOrXml is a file path or the Xml string</param>
        /// <returns>A parsed XDocument of the Xml document</returns>
        public static XDocument LoadXDocument(string fileOrXml, XmlLoadType type)
        {
            if(type == XmlLoadType.FromFile && !File.Exists(fileOrXml))
            {
                Logging.Error("XmlLoadType set to file and file does not exist at {0}", fileOrXml);
                return null;
            }
            try
            {
                switch(type)
                {
                    case XmlLoadType.FromFile:
                        return XDocument.Load(fileOrXml, LoadOptions.SetLineInfo);
                    case XmlLoadType.FromString:
                        return XDocument.Parse(fileOrXml, LoadOptions.SetLineInfo);
                }
            }
            catch(XmlException ex)
            {
                Logging.Exception(ex.ToString());
                return null;
            }
            return null;
        }

        /// <summary>
        /// Load an Xml document to an XmlDocument object
        /// </summary>
        /// <param name="fileOrXml">The filepath or string representation of the Xml document</param>
        /// <param name="type">The type to define if fileOrXml is a file path or the Xml string</param>
        /// <returns>A parsed XmlDocument of the Xml document</returns>
        public static XmlDocument LoadXmlDocument(string fileOrXml, XmlLoadType type)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                switch(type)
                {
                    case XmlLoadType.FromFile:
                        doc.Load(fileOrXml);
                        break;
                    case XmlLoadType.FromString:
                        doc.LoadXml(fileOrXml);
                        break;
                }
            }
            catch (XmlException xmlEx)
            {
                if (File.Exists(fileOrXml))
                    Logging.Exception("Failed to load Xml file: {0}\n{1}", Path.GetFileName(fileOrXml), xmlEx.ToString());
                else
                    Logging.Exception("failed to load Xml string:\n" + xmlEx.ToString());
                return null;
            }
            return doc;
        }

        /// <summary>
        /// Copies an xml file from an archive or directory path and unpacks it from binary Xml to human-readable Xml
        /// </summary>
        /// <param name="xmlUnpack">The Xml unpack instructions object</param>
        /// <param name="unpackBuilder">The stringBuilder to log the generated files location for the install log</param>
        public static bool UnpackXmlFile(XmlUnpack xmlUnpack, StringBuilder unpackBuilder)
        {
            //log info for debugging if need be
            Logging.Info(xmlUnpack.DumpInfoToLog);

            //check if new destination name for replacing
            string destinationFilename = string.IsNullOrWhiteSpace(xmlUnpack.NewFileName) ? xmlUnpack.FileName : xmlUnpack.NewFileName;
            string destinationCompletePath = Path.Combine(xmlUnpack.ExtractDirectory, destinationFilename);
            string sourceCompletePath = Path.Combine(xmlUnpack.DirectoryInArchive, xmlUnpack.FileName);

            //if the destination file already exists, then don't copy it over
            if(File.Exists(destinationCompletePath))
            {
                Logging.Info("Replacement file already exists, skipping");
                return true;
            }

            FileUtils.Unpack(xmlUnpack.Pkg, sourceCompletePath, destinationCompletePath);
            unpackBuilder.AppendLine(destinationCompletePath);

            Logging.Info("unpacking Xml binary file (if binary)");
            try
            {
                XmlBinaryHandler binaryHandler = new XmlBinaryHandler();
                binaryHandler.UnpackXmlFile(destinationCompletePath);
                return true;
            }
            catch (Exception xmlUnpackExceptino)
            {
                Logging.Exception(xmlUnpackExceptino.ToString());
                return false;
            }
        }
        #endregion

        #region Legacy methods
        /// <summary>
        /// Parses the database Xml document from the legacy format into memory
        /// </summary>
        /// <param name="doc">The document to parse from</param>
        /// <param name="globalDependencies">The list of global dependencies</param>
        /// <param name="dependencies">The list of dependencies</param>
        /// <param name="logicalDependencies">The list of logical dependencies</param>
        /// <param name="parsedCatagoryList">The list of categories</param>
        /// <param name="buildRefrences">Flag for if the list references (like level, parent, topParent) should be built as well</param>
        [Obsolete]
        public static void ParseDatabaseLegacy(XDocument doc, List<DatabasePackage> globalDependencies, List<Dependency> dependencies,
            List<Dependency> logicalDependencies, List<Category> parsedCatagoryList, bool buildRefrences)
        {
            //LEGACY CONVERSION:
            //remove all the file loading stuff
            //add the global dependencies
            foreach (XElement dependencyNode in doc.XPathSelectElements("/modInfoAlpha.xml/globaldependencies/globaldependency"))
            {
                List<string> depNodeList = new List<string>() { "zipFile", "crc", "enabled", "packageName", "appendExtraction" };
                List<string> optionalDepNodList = new List<string>() { "startAddress", "endAddress", "devURL", "timestamp", "logAtInstall" };
                List<string> unknownNodeList = new List<string>() { };
                DatabasePackage d = new DatabasePackage();
                foreach (XElement globs in dependencyNode.Elements())
                {
                    switch (globs.Name.ToString())
                    {
                        case "zipFile":
                            d.ZipFile = globs.Value;
                            break;
                        case "timestamp":
                            d.Timestamp = long.Parse("0" + globs.Value);
                            break;
                        case "crc":
                            d.CRC = globs.Value;
                            break;
                        case "startAddress":
                            d.StartAddress = globs.Value;
                            break;
                        case "endAddress":
                            d.EndAddress = globs.Value;
                            break;
                        case "logAtInstall":
                            d.LogAtInstall = CommonUtils.ParseBool(globs.Value,true);
                            break;
                        case "devURL":
                            d.DevURL = globs.Value;
                            break;
                        case "enabled":
                            d.Enabled = CommonUtils.ParseBool(globs.Value, false);
                            break;
                        case "appendExtraction":
                            d.AppendExtraction = CommonUtils.ParseBool(globs.Value.Trim(), false);
                            break;
                        case "packageName":
                            d.PackageName = globs.Value.Trim();
                            if (string.IsNullOrEmpty(d.PackageName))
                            {
                                Logging.Warning(string.Format("modInfo.Xml: PackageName not defined. node \"{0}\" => (line {1})",
                                    globs.Name.ToString(), ((IXmlLineInfo)globs).LineNumber));
                                if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                    MessageBox.Show(string.Format("modInfo.Xml: PackageName not defined.\nnode \"{0}\" => globsPend {1}\n\nmore informations, see logfile",
                                        globs.Name.ToString(), d.ZipFile), "Warning",MessageBoxButton.OK);
                            }
                            break;
                        default:
                            Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => (line {1})",
                                globs.Name.ToString(), ((IXmlLineInfo)globs).LineNumber));
                            if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: ZipFile, CRC, StartAddress, EndAddress, enabled, PackageName\n\nNode found: {0}\n\nmore informations, see logfile",
                                    globs.Name.ToString()), "Warning", MessageBoxButton.OK);
                            break;
                    }
                    //mandatory component
                    if (depNodeList.Contains(globs.Name.ToString()))
                        depNodeList.Remove(globs.Name.ToString());
                    //optional component
                    else if (optionalDepNodList.Contains(globs.Name.ToString()))
                        optionalDepNodList.Remove(globs.Name.ToString());
                    //unknown component
                    else
                        unknownNodeList.Add(globs.Name.ToString());
                }
                if (depNodeList.Count > 0)
                    Logging.Warning(string.Format("modInfo.Xml nodes not used: {0} => globsPend {1} (line {2})",
                        string.Join(",", depNodeList), d.PackageName, ((IXmlLineInfo)dependencyNode).LineNumber));
                if (unknownNodeList.Count > 0)
                    Logging.Warning(string.Format("modInfo.Xml unknown nodes: {0} => globsPend {1} (line {2})",
                        string.Join(",", unknownNodeList), d.PackageName, ((IXmlLineInfo)dependencyNode).LineNumber));
                if (string.IsNullOrWhiteSpace(d.PackageName))
                {
                    throw new BadMemeException("packagename is blank!!!!!1");
                }
                globalDependencies.Add(d);
            }
            //add the dependencies
            foreach (XElement dependencyNode in doc.XPathSelectElements("/modInfoAlpha.xml/dependencies/dependency"))
            {
                List<string> depNodeList = new List<string>() { "zipFile", "crc", "enabled", "packageName", "appendExtraction" };
                List<string> optionalDepNodList = new List<string>() { "startAddress", "endAddress", "devURL", "timestamp" , "logicalDependencies", "logAtInstall" };
                List<string> unknownNodeList = new List<string>() { };
                Dependency d = new Dependency
                {
                    wasLogicalDependencyLegacy = false
                };
                foreach (XElement globs in dependencyNode.Elements())
                {
                    switch (globs.Name.ToString())
                    {
                        case "zipFile":
                            d.ZipFile = globs.Value;
                            break;
                        case "timestamp":
                            d.Timestamp = long.Parse("0" + globs.Value);
                            break;
                        case "crc":
                            d.CRC = globs.Value;
                            break;
                        case "startAddress":
                            d.StartAddress = globs.Value;
                            break;
                        case "endAddress":
                            d.EndAddress = globs.Value;
                            break;
                        case "logAtInstall":
                            d.LogAtInstall = CommonUtils.ParseBool(globs.Value, true);
                            break;
                        case "devURL":
                            d.DevURL = globs.Value;
                            break;
                        case "enabled":
                            d.Enabled = CommonUtils.ParseBool(globs.Value, false);
                            break;
                        case "appendExtraction":
                            d.AppendExtraction = CommonUtils.ParseBool(globs.Value.Trim(), false);
                            break;
                        case "packageName":
                            d.PackageName = globs.Value.Trim();
                            if (string.IsNullOrEmpty(d.PackageName))
                            {
                                Logging.Warning(string.Format("modInfo.Xml: PackageName not defined. node \"{0}\" => (line {1})",
                                    globs.Name.ToString(), ((IXmlLineInfo)globs).LineNumber));
                                if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                    MessageBox.Show(string.Format("modInfo.Xml: PackageName not defined.\nnode \"{0}\" => globsPend {1}\n\nmore informations, see logfile",
                                    globs.Name.ToString(), d.ZipFile), "Warning", MessageBoxButton.OK);
                            }
                            break;
                        case "logicalDependencies":
                            //parse all dependencies
                            foreach (XElement logDependencyHolder in globs.Elements())
                            {
                                string[] logDepNodeList = new string[] { "packageName", "negateFlag" };
                                DatabaseLogic ld = new DatabaseLogic
                                {
                                    Logic = Logic.AND
                                };
                                foreach (XElement logDependencyNode in logDependencyHolder.Elements())
                                {
                                    logDepNodeList = logDepNodeList.Except(new string[] { logDependencyNode.Name.ToString() }).ToArray();
                                    switch (logDependencyNode.Name.ToString())
                                    {
                                        case "packageName":
                                            ld.PackageName = logDependencyNode.Value.Trim();
                                            if (ld.PackageName.Equals(""))
                                            {
                                                Logging.Warning(string.Format("modInfo.Xml: PackageName not defined. node \"{0}\" => (line {1})",
                                                    logDependencyNode.Name.ToString(), ((IXmlLineInfo)logDependencyNode).LineNumber));
                                                if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                    MessageBox.Show(string.Format("modInfo.Xml: PackageName not defined.\nnode \"{0}\"  => dep {1}",
                                                    logDependencyNode.Name.ToString(), d.ZipFile), "Warning", MessageBoxButton.OK);
                                            }
                                            break;
                                        case "negateFlag":
                                            ld.NotFlag = CommonUtils.ParseBool(logDependencyNode.Value, true);
                                            break;
                                        default:
                                            Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => (line {1})",
                                                logDependencyNode.Name.ToString(), ((IXmlLineInfo)logDependencyNode).LineNumber));
                                            if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: PackageName\n\nNode found: {0}\n\nmore informations, see logfile",
                                                    logDependencyNode.Name.ToString()));
                                            break;
                                    }
                                }
                                if (logDepNodeList.Length > 0)
                                    Logging.Warning(string.Format("modInfo.Xml nodes not used: {0} => logDep (line {1})",
                                    string.Join(",", logDepNodeList), ((IXmlLineInfo)logDependencyHolder).LineNumber));
                                if (ld.PackageName.Equals(""))
                                {
                                    ld.PackageName = CommonUtils.RandomString(30);
                                    Logging.Info("PackageName is random generated: " + ld.PackageName);              // to avoid exceptions
                                }
                                d.Dependencies.Add(ld);
                            }
                            break;
                        default:
                            Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => (line {1})",
                                globs.Name.ToString(), ((IXmlLineInfo)globs).LineNumber));
                            if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: ZipFile, CRC, StartAddress, EndAddress, enabled, PackageName\n\nNode found: {0}\n\nmore informations, see logfile",
                                    globs.Name.ToString()), "Warning", MessageBoxButton.OK);
                            break;
                    }
                    //mandatory component
                    if (depNodeList.Contains(globs.Name.ToString()))
                        depNodeList.Remove(globs.Name.ToString());
                    //optional component
                    else if (optionalDepNodList.Contains(globs.Name.ToString()))
                        optionalDepNodList.Remove(globs.Name.ToString());
                    //unknown component
                    else
                        unknownNodeList.Add(globs.Name.ToString());
                }
                if (depNodeList.Count > 0)
                    Logging.Warning(string.Format("modInfo.Xml nodes not used: {0} => (line {1})",
                        string.Join(",", depNodeList), ((IXmlLineInfo)dependencyNode).LineNumber));
                if (unknownNodeList.Count > 0)
                    Logging.Warning(string.Format("modInfo.Xml unknown nodes: {0} => globsPend {1} (line {2})",
                        string.Join(",", unknownNodeList), d.PackageName, ((IXmlLineInfo)dependencyNode).LineNumber));
                if (string.IsNullOrWhiteSpace(d.PackageName))
                {
                    throw new BadMemeException("packagename is blank!!!!!1");
                }
                dependencies.Add(d);
            }
            //add the logicalDependencies
            foreach (XElement dependencyNode in doc.XPathSelectElements("/modInfoAlpha.xml/logicalDependencies/logicalDependency"))
            {
                List<string> depNodeList = new List<string>() { "zipFile", "crc", "enabled", "packageName", "logic" };
                List<string> optionalDepNodList = new List<string>() { "startAddress", "endAddress", "devURL", "timestamp", "logAtInstall" };
                List<string> unknownNodeList = new List<string>() { };
                Dependency d = new Dependency
                {
                    wasLogicalDependencyLegacy = true
                };
                foreach (XElement globs in dependencyNode.Elements())
                {
                    switch (globs.Name.ToString())
                    {
                        case "zipFile":
                            d.ZipFile = globs.Value;
                            break;
                        case "timestamp":
                            d.Timestamp = long.Parse("0" + globs.Value);
                            break;
                        case "crc":
                            d.CRC = globs.Value;
                            break;
                        case "startAddress":
                            d.StartAddress = globs.Value;
                            break;
                        case "endAddress":
                            d.EndAddress = globs.Value;
                            break;
                        case "logAtInstall":
                            d.LogAtInstall = CommonUtils.ParseBool(globs.Value, true);
                            break;
                        case "devURL":
                            d.DevURL = globs.Value;
                            break;
                        case "enabled":
                            d.Enabled = CommonUtils.ParseBool(globs.Value, false);
                            break;
                        case "packageName":
                            d.PackageName = globs.Value.Trim();
                            if (d.PackageName.Equals(""))
                            {
                                Logging.Warning(string.Format("modInfo.Xml: PackageName not defined. node \"{0}\" => (line {1})",
                                    globs.Name.ToString(), ((IXmlLineInfo)globs).LineNumber));
                                if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                    MessageBox.Show(string.Format("modInfo.Xml: PackageName not defined.\nnode \"{0}\" => logDep {1}\n\nmore informations, see logfile",
                                        globs.Name.ToString(), d.ZipFile), "Warning", MessageBoxButton.OK);
                            }
                            break;
                        default:
                            Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => (line {1})",
                                globs.Name.ToString(), ((IXmlLineInfo)globs).LineNumber));
                            if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: ZipFile, CRC, StartAddress, EndAddress, enabled, PackageName\n\nNode found: {0}\n\nmore informations, see logfile",
                                    globs.Name.ToString()), "Warning", MessageBoxButton.OK);
                            break;
                    }
                    //mandatory component
                    if (depNodeList.Contains(globs.Name.ToString()))
                        depNodeList.Remove(globs.Name.ToString());
                    //optional component
                    else if (optionalDepNodList.Contains(globs.Name.ToString()))
                        optionalDepNodList.Remove(globs.Name.ToString());
                    //unknown component
                    else
                        unknownNodeList.Add(globs.Name.ToString());
                }
                if (depNodeList.Count > 0)
                    Logging.Warning(string.Format("modInfo.Xml nodes not used: {0} => (line {1})",
                        string.Join(",", depNodeList), ((IXmlLineInfo)dependencyNode).LineNumber));
                if (unknownNodeList.Count > 0)
                    Logging.Warning(string.Format("modInfo.Xml unknown nodes: {0} => globsPend {1} (line {2})",
                        string.Join(",", unknownNodeList), d.PackageName, ((IXmlLineInfo)dependencyNode).LineNumber));
                if (string.IsNullOrWhiteSpace(d.PackageName))
                {
                    throw new BadMemeException("packagename is blank!!!!!1");
                }
                logicalDependencies.Add(d);
            }
            foreach (XElement catagoryHolder in doc.XPathSelectElements("/modInfoAlpha.xml/catagories/catagory"))
            {
                Category cat = new Category();
                string[] catNodeList = new string[] { "name", "installGroup", "packages", "dependencies" };
                foreach (XElement catagoryNode in catagoryHolder.Elements())
                {
                    catNodeList = catNodeList.Except(new string[] { catagoryNode.Name.ToString() }).ToArray();
                    switch (catagoryNode.Name.ToString())
                    {
                        case "name":
                            cat.Name = catagoryNode.Value;
                            break;
                        case "installGroup":
                            cat.InstallGroup = CommonUtils.ParseInt(catagoryNode.Value.Trim(), 0);
                            break;
                        case "packages":
                            foreach (XElement modHolder in catagoryNode.Elements())
                            {
                                switch (modHolder.Name.ToString())
                                {
                                    case "package":
                                        List<string> packageNodeList = new List<string>() { "name", "zipFile", "crc", "enabled", "visible", "packageName", "type" };
                                        List<string> optionalPackageNodeList = new List<string>() { "version", "timestamp", "startAddress", "endAddress", "size",
                                            "description", "updateComment", "devURL", "userDatas", "medias", "dependencies", "logicalDependencies", "packages", "logAtInstall" };
                                        List<string> unknownNodeList = new List<string>() { };
                                        SelectablePackage m = new SelectablePackage()
                                        {
                                            Level = 0
                                        };
                                        foreach (XElement modNode in modHolder.Elements())
                                        {
                                            switch (modNode.Name.ToString())
                                            {
                                                case "name":
                                                    m.Name = modNode.Value;
                                                    break;
                                                case "version":
                                                    m.Version = modNode.Value;
                                                    break;
                                                case "zipFile":
                                                    m.ZipFile = modNode.Value;
                                                    break;
                                                case "timestamp":
                                                    m.Timestamp = long.Parse("0" + modNode.Value);
                                                    break;
                                                case "startAddress":
                                                    m.StartAddress = modNode.Value;
                                                    break;
                                                case "endAddress":
                                                    m.EndAddress = modNode.Value;
                                                    break;
                                                case "logAtInstall":
                                                    m.LogAtInstall = CommonUtils.ParseBool(modNode.Value, true);
                                                    break;
                                                case "crc":
                                                    m.CRC = modNode.Value;
                                                    break;
                                                case "type":
                                                    switch (modNode.Value)
                                                    {
                                                        case "multi":
                                                            m.Type = SelectionTypes.multi;
                                                            break;
                                                        case "single":
                                                            m.Type = SelectionTypes.single1;
                                                            break;
                                                        case "single1":
                                                            m.Type = SelectionTypes.single1;
                                                            break;
                                                        case "single_dropdown":
                                                            m.Type = SelectionTypes.single_dropdown1;
                                                            break;
                                                        case "single_dropdown1":
                                                            m.Type = SelectionTypes.single_dropdown1;
                                                            break;
                                                        case "single_dropdown2":
                                                            m.Type = SelectionTypes.single_dropdown2;
                                                            break;
                                                    }
                                                    break;
                                                case "enabled":
                                                    m.Enabled = CommonUtils.ParseBool(modNode.Value, false);
                                                    break;
                                                case "visible":
                                                    m.Visible = CommonUtils.ParseBool(modNode.Value, true);
                                                    break;
                                                case "packageName":
                                                    m.PackageName = modNode.Value.Trim();
                                                    if (m.PackageName.Equals(""))
                                                    {
                                                        Logging.Warning(string.Format("modInfo.Xml: PackageName not defined. node \"{0}\" => mod {1} ({2}) (line {3})",
                                                            modNode.Name.ToString(), m.Name, m.ZipFile, ((IXmlLineInfo)modNode).LineNumber));
                                                        if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                            MessageBox.Show(string.Format("modInfo.Xml: PackageName not defined.\nnode \"{0}\" => mod {1} ({2})",
                                                                modNode.Name.ToString(), m.Name, m.ZipFile), "Warning", MessageBoxButton.OK);
                                                    }
                                                    break;
                                                case "size":
                                                    m.Size = CommonUtils.ParseuLong(modNode.Value, 0);
                                                    break;
                                                case "description":
                                                    m.Description = ConvertFromXmlSaveFormat(modNode.Value);
                                                    break;
                                                case "updateComment":
                                                    m.UpdateComment = ConvertFromXmlSaveFormat(modNode.Value);
                                                    break;
                                                case "devURL":
                                                    m.DevURL = modNode.Value;
                                                    break;
                                                case "userDatas":
                                                    foreach (XElement userDataNode in modNode.Elements())
                                                    {
                                                        switch (userDataNode.Name.ToString())
                                                        {
                                                            case "userData":
                                                                string innerText = userDataNode.Value;
                                                                if (innerText == null)
                                                                    continue;
                                                                if (innerText.Equals(""))
                                                                    continue;
                                                                UserFile uf = new UserFile
                                                                {
                                                                    Pattern = innerText
                                                                };
                                                                if (userDataNode.Attribute("before") != null)
                                                                    uf.PlaceBeforeExtraction = CommonUtils.ParseBool(userDataNode.Attribute("before").Value, false);
                                                                if (userDataNode.Attribute("pre") != null)
                                                                    uf.PlaceBeforeExtraction = CommonUtils.ParseBool(userDataNode.Attribute("pre").Value, false);
                                                                if (userDataNode.Attribute("system") != null)
                                                                    uf.SystemInitiated = CommonUtils.ParseBool(userDataNode.Attribute("system").Value, false);
                                                                m.UserFiles.Add(uf);
                                                                break;
                                                            default:
                                                                Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => mod {1} ({2}) => userDatas => expected node: userData (line {3})",
                                                                    userDataNode.Name.ToString(), m.Name, m.ZipFile, ((IXmlLineInfo)userDataNode).LineNumber));
                                                                if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                                    MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected node: userData\n\nNode found: {0}\n\nmore informations, see logfile",
                                                                        userDataNode.Name.ToString()), "Warning", MessageBoxButton.OK);
                                                                break;
                                                        }
                                                    }
                                                    break;
                                                case "medias":
                                                    //parse every picture
                                                    foreach (XElement pictureHolder in modNode.Elements())
                                                    {
                                                        Media med = new Media();
                                                        switch (pictureHolder.Name.ToString())
                                                        {
                                                            case "media":
                                                                foreach (XElement pictureNode in pictureHolder.Elements())
                                                                {
                                                                    switch (pictureNode.Name.ToString())
                                                                    {
                                                                        case "URL":
                                                                            string innerText = pictureNode.Value;
                                                                            if (innerText == null)
                                                                                continue;
                                                                            if (innerText.Equals(""))
                                                                                continue;
                                                                            med.URL = innerText;
                                                                            break;
                                                                        case "type":
                                                                            int innerValue = CommonUtils.ParseInt(pictureNode.Value, 1);
                                                                            switch (innerValue)
                                                                            {
                                                                                case 1:
                                                                            med.MediaType = MediaType.Picture;
                                                                            break;
                                                                        case 2:
                                                                            med.MediaType = MediaType.Webpage;
                                                                            break;
                                                                        case 3:
                                                                            med.MediaType = MediaType.MediaFile;
                                                                            break;
                                                                        case 4:
                                                                            med.MediaType = MediaType.HTML;
                                                                            break;
                                                                            }
                                                                            break;
                                                                        default:
                                                                            Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => mod {1} ({2}) => pictures => picture =>expected nodes: URL (line {3})",
                                                                                pictureNode.Name.ToString(), m.Name, m.ZipFile, ((IXmlLineInfo)pictureNode).LineNumber));
                                                                            if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                                                MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: URL\n\nNode found: {0}\n\nmore informations, see logfile",
                                                                                    pictureNode.Name.ToString()), "Warning", MessageBoxButton.OK);
                                                                            break;
                                                                    }
                                                                }
                                                                break;
                                                            default:
                                                                Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => mod {1} ({2}) => pictures => expected node: picture (line {3})",
                                                                    pictureHolder.Name, m.Name, m.ZipFile, ((IXmlLineInfo)pictureHolder).LineNumber));
                                                                if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                                    MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected node: picture\n\nNode found: {0}\n\nmore informations, see logfile",
                                                                        pictureHolder.Name.ToString()), "Warning", MessageBoxButton.OK);
                                                                break;
                                                        }
                                                        m.Medias.Add(med);
                                                    }
                                                    break;
                                                case "dependencies":
                                                    //parse all dependencies
                                                    foreach (XElement dependencyHolder in modNode.Elements())
                                                    {
                                                        string[] depNodeList = new string[] { "packageName" };
                                                        DatabaseLogic d = new DatabaseLogic() { Logic = Logic.OR };
                                                        foreach (XElement dependencyNode in dependencyHolder.Elements())
                                                        {
                                                            depNodeList = depNodeList.Except(new string[] { dependencyNode.Name.ToString() }).ToArray();
                                                            switch (dependencyNode.Name.ToString())
                                                            {
                                                                case "packageName":
                                                                    d.PackageName = dependencyNode.Value.Trim();
                                                                    if (d.PackageName.Equals(""))
                                                                    {
                                                                        Logging.Warning(string.Format("modInfo.Xml: PackageName not defined. node \"{0}\" => mod {1} ({2}) => (line {3})",
                                                                            dependencyNode.Name.ToString(), m.Name, m.ZipFile, ((IXmlLineInfo)dependencyNode).LineNumber));
                                                                        if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                                        {
                                                                            MessageBox.Show(string.Format("modInfo.Xml: PackageName not defined.\nnode \"{0}\" => mod {1} ({2})",
                                                                                dependencyNode.Name.ToString(), m.Name, m.ZipFile), "Warning", MessageBoxButton.OK);
                                                                        }
                                                                    }
                                                                    break;
                                                                default:
                                                                    Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => mod {1} ({2}) => (line {3})",
                                                                        dependencyNode.Name.ToString(), m.Name, m.ZipFile, ((IXmlLineInfo)dependencyNode).LineNumber));
                                                                    if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                                        MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: PackageName\n\nNode found: {0}\n\nmore informations, see logfile",
                                                                            dependencyNode.Name.ToString()), "Warning", MessageBoxButton.OK);
                                                                    break;
                                                            }
                                                        }
                                                        if (depNodeList.Length > 0)
                                                        {
                                                            Logging.Warning(string.Format("modInfo.Xml nodes not used: {0} => mod {1} ({2}) => (line {3})",
                                                                string.Join(",", depNodeList), m.Name, m.ZipFile, ((IXmlLineInfo)dependencyHolder).LineNumber));
                                                        }
                                                        if (string.IsNullOrWhiteSpace(d.PackageName))
                                                        {
                                                            throw new BadMemeException("packagename is blank!!!!!1");
                                                        }
                                                        m.Dependencies.Add(d);
                                                    }
                                                    break;
                                                case "logicalDependencies":
                                                    //parse all dependencies
                                                    foreach (XElement dependencyHolder in modNode.Elements())
                                                    {
                                                        string[] depNodeList = new string[] { "packageName", "negateFlag" };
                                                        DatabaseLogic d = new DatabaseLogic() { Logic = Logic.AND };
                                                        foreach (XElement dependencyNode in dependencyHolder.Elements())
                                                        {
                                                            depNodeList = depNodeList.Except(new string[] { dependencyNode.Name.ToString() }).ToArray();
                                                            switch (dependencyNode.Name.ToString())
                                                            {
                                                                case "packageName":
                                                                    d.PackageName = dependencyNode.Value.Trim();
                                                                    if (d.PackageName.Equals(""))
                                                                    {
                                                                        Logging.Warning(string.Format("modInfo.Xml: PackageName not defined. node \"{0}\" => config {1} ({2}) => (line {3})",
                                                                            dependencyNode.Name.ToString(), m.Name, m.ZipFile, ((IXmlLineInfo)dependencyNode).LineNumber));
                                                                        if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                                            MessageBox.Show(string.Format("modInfo.Xml: PackageName not defined.\nnode \"{0}\" => config {1} ({2})",
                                                                                dependencyNode.Name.ToString(), m.Name, m.ZipFile), "Warning", MessageBoxButton.OK);
                                                                    }
                                                                    break;
                                                                case "negateFlag":
                                                                    //d.NegateFlag = Utils.ParseBool(dependencyNode.Value, true);
                                                                    break;
                                                                default:
                                                                    Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => config {1} ({2}) => (line {3})",
                                                                        dependencyNode.Name.ToString(), m.Name, m.ZipFile, ((IXmlLineInfo)dependencyNode).LineNumber));
                                                                    if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                                    {
                                                                        MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: PackageName\n\nNode found: {0}\n\nmore informations, see logfile",
                                                                            dependencyNode.Name.ToString()));
                                                                    };
                                                                    break;
                                                            }
                                                        }
                                                        if (depNodeList.Length > 0)
                                                        {
                                                            Logging.Warning(string.Format("modInfo.Xml nodes not used: {0} => config {1} ({2}) => (line {3})",
                                                                string.Join(",", depNodeList), m.Name, m.ZipFile, ((IXmlLineInfo)dependencyHolder).LineNumber));
                                                        }
                                                        if (string.IsNullOrWhiteSpace(d.PackageName))
                                                        {
                                                            throw new BadMemeException("packagename is blank!!!!!1");
                                                        }
                                                        m.Dependencies.Add(d);
                                                    }
                                                    break;
                                                case "packages":
                                                    //run the process configs method
                                                    ProcessConfigs(modNode, m, true,m.Level+1);
                                                    break;
                                                default:
                                                    Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => mod {1} ({2}) (line {3})",
                                                        modNode.Name.ToString(), m.Name, m.ZipFile, ((IXmlLineInfo)modNode).LineNumber));
                                                    if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                        MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: name, version, ZipFile," +
                                                            "StartAddress, EndAddress, CRC, Enabled, PackageName, size, description, updateComment, devURL, userDatas," +
                                                            " pictures, dependencies, configs\n\nNode found: {0}\n\nmore informations, see logfile",
                                                            modNode.Name.ToString()), "Warning", MessageBoxButton.OK);
                                                    break;
                                            }
                                            //mandatory component
                                            if (packageNodeList.Contains(modNode.Name.ToString()))
                                                packageNodeList.Remove(modNode.Name.ToString());
                                            //optional component
                                            else if (optionalPackageNodeList.Contains(modNode.Name.ToString()))
                                                optionalPackageNodeList.Remove(modNode.Name.ToString());
                                            //unknown component
                                            else
                                                unknownNodeList.Add(modNode.Name.ToString());
                                        }
                                        if (packageNodeList.Count > 0)
                                        Logging.Warning(string.Format("modInfo.Xml nodes not used: {0} => package {1} (line {2})",
                                            string.Join(",", packageNodeList), m.Name, ((IXmlLineInfo)modHolder).LineNumber));
                                        if (unknownNodeList.Count > 0)
                                            Logging.Warning(string.Format("modInfo.Xml unknown nodes: {0} => package {1} (line {2})",
                                                string.Join(",", unknownNodeList), m.PackageName, ((IXmlLineInfo)modHolder).LineNumber));
                                        if (string.IsNullOrWhiteSpace(m.PackageName))
                                        {
                                            throw new BadMemeException("packagename is blank!!!!!1");
                                        }
                                        cat.Packages.Add(m);
                                        break;
                                    default:
                                        Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => cat {1} (line {2})",
                                            modHolder.Name.ToString(), cat.Name, ((IXmlLineInfo)modHolder).LineNumber));
                                        if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                            MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: mod\n\nNode found: {0}\n\nmore informations, see logfile",
                                                modHolder.Name.ToString()), "Warning", MessageBoxButton.OK);
                                        break;
                                }
                            }
                            break;
                        case "dependencies":
                            //parse every dependency for that mod
                            foreach (XElement dependencyHolder in catagoryNode.Elements())
                            {
                                string[] depNodeList = new string[] { "packageName" };
                                DatabaseLogic d = new DatabaseLogic() { Logic = Logic.OR };
                                foreach (XElement dependencyNode in dependencyHolder.Elements())
                                {
                                    depNodeList = depNodeList.Except(new string[] { dependencyNode.Name.ToString() }).ToArray();
                                    switch (dependencyNode.Name.ToString())
                                    {
                                        case "packageName":
                                            d.PackageName = dependencyNode.Value.Trim();
                                            if (d.PackageName.Equals(""))
                                            {
                                                Logging.Warning(string.Format("modInfo.Xml: PackageName not defined. node \"{0}\" => cat {1} => (line {2})",
                                                    dependencyNode.Name.ToString(), cat.Name, ((IXmlLineInfo)dependencyNode).LineNumber));
                                                if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                {
                                                    MessageBox.Show(string.Format("modInfo.Xml: PackageName not defined.\nnode \"{0}\" => cat {1}",
                                                        dependencyNode.Name.ToString(), cat.Name), "Warning", MessageBoxButton.OK);
                                                }
                                            }
                                            break;
                                        default:
                                            Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => cat {1} => (line {2})",
                                                dependencyNode.Name, cat.Name, ((IXmlLineInfo)dependencyNode).LineNumber));
                                            if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                            {
                                                MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: PackageName\n\nNode found: {0}\n\nmore informations, see logfile",
                                                    dependencyNode.Name), "Warning", MessageBoxButton.OK);
                                            }
                                            break;
                                    }
                                }
                                if (depNodeList.Length > 0)
                                {
                                    Logging.Warning(string.Format("modInfo.Xml nodes not used: {0} => cat {1} => (line {2})",
                                        string.Join(",", depNodeList), cat.Name, ((IXmlLineInfo)dependencyHolder).LineNumber));
                                };
                                if (string.IsNullOrWhiteSpace(d.PackageName))
                                {
                                    throw new BadMemeException("packagename is blank!!!!!1");
                                }
                                cat.Dependencies.Add(d);
                            }
                            break;
                        default:
                            Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => cat {1} (line {2})",
                                catagoryNode.Name.ToString(), cat.Name, ((IXmlLineInfo)catagoryNode).LineNumber));
                            if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                            {
                                MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: name, selectionType, mods, dependencies\n\nNode found: {0}\n\nmore informations, see logfile",
                                    catagoryNode.Name.ToString()), "Warning", MessageBoxButton.OK);
                            }
                            break;
                    }
                }
                if (catNodeList.Length > 0)
                {
                    Logging.Warning(string.Format("modInfo.Xml nodes not used: {0} => cat {1} (line {2})",
                        string.Join(",", catNodeList), cat.Name, ((IXmlLineInfo)catagoryHolder).LineNumber));
                }
                parsedCatagoryList.Add(cat);
            }
        }

        //recursively processes the configs
        [Obsolete]
        private static void ProcessConfigs(XElement holder, SelectablePackage m, bool parentIsMod, int level, SelectablePackage con = null)
        {
            //parse every config for that mod
            foreach (XElement configHolder in holder.Elements())
            {
                switch (configHolder.Name.ToString())
                {
                    case "package":
                        List<string> packageNodeList = new List<string>() { "name", "zipFile", "crc", "enabled", "visible", "packageName", "type" };
                        List<string> optionalPackageNodeList = new List<string>() { "version", "timestamp", "startAddress", "endAddress", "size",
                            "description", "updateComment", "devURL", "userDatas", "medias", "dependencies", "logicalDependencies", "packages", "logAtInstall" };
                        List<string> unknownNodeList = new List<string>() { };
                        SelectablePackage c = new SelectablePackage()
                        {
                            Level = level
                        };
                        foreach (XElement configNode in configHolder.Elements())
                        {
                            switch (configNode.Name.ToString())
                            {
                                case "name":
                                    c.Name = configNode.Value;
                                    break;
                                case "version":
                                    c.Version = configNode.Value;
                                    break;
                                case "zipFile":
                                    c.ZipFile = configNode.Value;
                                    break;
                                case "timestamp":
                                    c.Timestamp = long.Parse("0" + configNode.Value);
                                    break;
                                case "startAddress":
                                    c.StartAddress = configNode.Value;
                                    break;
                                case "endAddress":
                                    c.EndAddress = configNode.Value;
                                    break;
                                case "logAtInstall":
                                    c.LogAtInstall = CommonUtils.ParseBool(configNode.Value, true);
                                    break;
                                case "crc":
                                    c.CRC = configNode.Value;
                                    break;
                                case "enabled":
                                    c.Enabled = CommonUtils.ParseBool(configNode.Value, false);
                                    break;
                                case "visible":
                                    c.Visible = CommonUtils.ParseBool(configNode.Value, true);
                                    break;
                                case "packageName":
                                    c.PackageName = configNode.Value.Trim();
                                    if (c.PackageName.Equals(""))
                                    {
                                        Logging.Warning(string.Format("modInfo.Xml: PackageName not defined. node \"{0}\" => config {1} ({2}) (line {3})",
                                            configNode.Name.ToString(), c.Name, c.ZipFile, ((IXmlLineInfo)configNode).LineNumber));
                                        if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                            MessageBox.Show(string.Format("modInfo.Xml: PackageName not defined.\nnode \"{0}\" => config {1} ({2})",
                                                configNode.Name.ToString(), c.Name, c.ZipFile), "Warning", MessageBoxButton.OK);
                                    }
                                    break;
                                case "size":
                                    c.Size = CommonUtils.ParseuLong(configNode.Value, 0);
                                    break;
                                case "updateComment":
                                    c.UpdateComment = ConvertFromXmlSaveFormat(configNode.Value);
                                    break;
                                case "description":
                                    c.Description = ConvertFromXmlSaveFormat(configNode.Value);
                                    break;
                                case "devURL":
                                    c.DevURL = configNode.Value;
                                    break;
                                case "type":
                                    switch (configNode.Value)
                                    {
                                        case "multi":
                                            c.Type = SelectionTypes.multi;
                                            break;
                                        case "single":
                                            c.Type = SelectionTypes.single1;
                                            break;
                                        case "single1":
                                            c.Type = SelectionTypes.single1;
                                            break;
                                        case "single_dropdown":
                                            c.Type = SelectionTypes.single_dropdown1;
                                            break;
                                        case "single_dropdown1":
                                            c.Type = SelectionTypes.single_dropdown1;
                                            break;
                                        case "single_dropdown2":
                                            c.Type = SelectionTypes.single_dropdown2;
                                            break;
                                    }
                                    break;
                                case "packages":
                                    ProcessConfigs(configNode, m, false, c.Level+1 ,c);
                                    break;
                                case "userDatas":
                                    foreach (XElement userDataNode in configNode.Elements())
                                    {
                                        switch (userDataNode.Name.ToString())
                                        {
                                            case "userData":
                                                string innerText = userDataNode.Value;
                                                if (innerText == null)
                                                    continue;
                                                if (innerText.Equals(""))
                                                    continue;
                                                UserFile uf = new UserFile
                                                {
                                                    Pattern = innerText
                                                };
                                                if (userDataNode.Attribute("before") != null)
                                                    uf.PlaceBeforeExtraction = CommonUtils.ParseBool(userDataNode.Attribute("before").Value, false);
                                                if (userDataNode.Attribute("pre") != null)
                                                    uf.PlaceBeforeExtraction = CommonUtils.ParseBool(userDataNode.Attribute("pre").Value, false);
                                                if (userDataNode.Attribute("system") != null)
                                                    uf.SystemInitiated = CommonUtils.ParseBool(userDataNode.Attribute("system").Value, false);
                                                c.UserFiles.Add(uf);
                                                break;
                                            default:
                                                Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => config {1} ({2}) => userDatas => expected nodes: userData (line {3})",
                                                    userDataNode.Name.ToString(), c.Name, c.ZipFile, ((IXmlLineInfo)configNode).LineNumber));
                                                if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                    MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: userData\n\nNode found: {0}\n\nmore informations, see logfile",
                                                        userDataNode.Name.ToString()));
                                                break;
                                        }
                                    }
                                    break;
                                case "medias":
                                    //parse every picture
                                    foreach (XElement pictureHolder in configNode.Elements())
                                    {
                                        Media med = new Media();
                                        switch (pictureHolder.Name.ToString())
                                        {
                                            case "media":
                                                foreach (XElement pictureNode in pictureHolder.Elements())
                                                {
                                                    switch (pictureNode.Name.ToString())
                                                    {
                                                        case "URL":
                                                            string innerText = pictureNode.Value;
                                                            if (innerText == null)
                                                                continue;
                                                            if (innerText.Equals(""))
                                                                continue;
                                                            med.URL = innerText;
                                                            break;
                                                        case "type":
                                                            switch (CommonUtils.ParseInt(pictureNode.Value, 1))
                                                            {
                                                                case 1:
                                                                    med.MediaType = MediaType.Picture;
                                                                    break;
                                                                case 2:
                                                                    med.MediaType = MediaType.Webpage;
                                                                    break;
                                                                case 3:
                                                                    med.MediaType = MediaType.MediaFile;
                                                                    break;
                                                                case 4:
                                                                    med.MediaType = MediaType.HTML;
                                                                    break;
                                                            }
                                                            break;
                                                        default:
                                                            Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => mod {1} ({2}) => pictures => picture =>expected nodes: URL (line {3})",
                                                                pictureNode.Name.ToString(), m.Name, m.ZipFile, ((IXmlLineInfo)pictureNode).LineNumber));
                                                            if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                                MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: URL\n\nNode found: {0}\n\nmore informations, see logfile",
                                                                    pictureNode.Name.ToString()), "Warning", MessageBoxButton.OK);
                                                            break;
                                                    }
                                                }
                                                break;
                                            default:
                                                Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => mod {1} ({2}) => pictures => expected node: picture (line {3})",
                                                    pictureHolder.Name, m.Name, m.ZipFile, ((IXmlLineInfo)pictureHolder).LineNumber));
                                                if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                    MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected node: picture\n\nNode found: {0}\n\nmore informations, see logfile",
                                                        pictureHolder.Name.ToString()), "Warning", MessageBoxButton.OK);
                                                break;
                                        }
                                        c.Medias.Add(med);
                                    }
                                    break;
                                case "dependencies":
                                    //parse all dependencies
                                    foreach (XElement dependencyHolder in configNode.Elements())
                                    {
                                        string[] depNodeList = new string[] { "packageName" };
                                        DatabaseLogic d = new DatabaseLogic() { Logic = Logic.OR };
                                        foreach (XElement dependencyNode in dependencyHolder.Elements())
                                        {
                                            depNodeList = depNodeList.Except(new string[] { dependencyNode.Name.ToString() }).ToArray();
                                            switch (dependencyNode.Name.ToString())
                                            {
                                                case "packageName":
                                                    d.PackageName = dependencyNode.Value.Trim();
                                                    if (d.PackageName.Equals(""))
                                                    {
                                                        Logging.Warning(string.Format("modInfo.Xml: PackageName not defined. node \"{0}\" => config {1} ({2}) => (line {3})",
                                                            dependencyNode.Name.ToString(), c.Name, c.ZipFile, ((IXmlLineInfo)dependencyNode).LineNumber));
                                                        if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                            MessageBox.Show(string.Format("modInfo.Xml: PackageName not defined.\nnode \"{0}\" => config {1} ({2})",
                                                                dependencyNode.Name.ToString(), c.Name, c.ZipFile), "Warning", MessageBoxButton.OK);
                                                    }
                                                    break;
                                                default:
                                                    Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => config {1} ({2}) => (line {3})",
                                                        dependencyNode.Name.ToString(), c.Name, c.ZipFile, ((IXmlLineInfo)dependencyNode).LineNumber));
                                                    if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                        MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: PackageName\n\nNode found: {0}\n\nmore informations, see logfile",
                                                            dependencyNode.Name.ToString()));
                                                    break;
                                            }
                                        }
                                        if (depNodeList.Length > 0)
                                            Logging.Warning(string.Format("modInfo.Xml nodes not used: {0} => config {1} ({2}) => (line {3})",
                                                string.Join(",", depNodeList), c.Name, c.ZipFile, ((IXmlLineInfo)dependencyHolder).LineNumber));
                                        if (d.PackageName.Equals(""))
                                        {
                                            d.PackageName = CommonUtils.RandomString(30);
                                            Logging.Info("PackageName is random generated: " + d.PackageName);
                                        }              // to avoid exceptions
                                        c.Dependencies.Add(d);
                                    }
                                    break;
                                case "logicalDependencies":
                                    //parse all dependencies
                                    foreach (XElement dependencyHolder in configNode.Elements())
                                    {
                                        string[] depNodeList = new string[] { "packageName", "negateFlag" };
                                        DatabaseLogic d = new DatabaseLogic() { Logic = Logic.AND };
                                        foreach (XElement dependencyNode in dependencyHolder.Elements())
                                        {
                                            depNodeList = depNodeList.Except(new string[] { dependencyNode.Name.ToString() }).ToArray();
                                            switch (dependencyNode.Name.ToString())
                                            {
                                                case "packageName":
                                                    d.PackageName = dependencyNode.Value.Trim();
                                                    if (d.PackageName.Equals(""))
                                                    {
                                                        Logging.Warning(string.Format("modInfo.Xml: PackageName not defined. node \"{0}\" => config {1} ({2}) => (line {3})",
                                                            dependencyNode.Name.ToString(), c.Name, c.ZipFile, ((IXmlLineInfo)dependencyNode).LineNumber));
                                                        if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                            MessageBox.Show(string.Format("modInfo.Xml: PackageName not defined.\nnode \"{0}\" => config {1} ({2})",
                                                                dependencyNode.Name.ToString(), c.Name, c.ZipFile), "Warning", MessageBoxButton.OK);
                                                    }
                                                    break;
                                                case "negateFlag":
                                                    //d.NegateFlag = Utils.ParseBool(dependencyNode.Value, true);
                                                    break;
                                                default:
                                                    Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => config {1} ({2}) => (line {3})",
                                                        dependencyNode.Name.ToString(), c.Name, c.ZipFile, ((IXmlLineInfo)dependencyNode).LineNumber));
                                                    if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                                        MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: PackageName\n\nNode found: {0}\n\nmore informations, see logfile",
                                                            dependencyNode.Name.ToString()));
                                                    break;
                                            }
                                        }
                                        if (depNodeList.Length > 0)
                                            Logging.Warning(string.Format("modInfo.Xml nodes not used: {0} => config {1} ({2}) => (line {3})",
                                                string.Join(",", depNodeList), c.Name, c.ZipFile, ((IXmlLineInfo)dependencyHolder).LineNumber));
                                        if (d.PackageName.Equals(""))
                                        {
                                            d.PackageName = CommonUtils.RandomString(30);
                                            Logging.Info("PackageName is random generated: " + d.PackageName);
                                        }              // to avoid exceptions
                                        c.Dependencies.Add(d);
                                    }
                                    break;
                                default:
                                    Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => config {1} ({2}) (line {3})",
                                        configNode.Name.ToString(), c.Name, c.ZipFile, ((IXmlLineInfo)configNode).LineNumber));
                                    if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                                        MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: name, version, ZipFile, StartAddress, " +
                                            "EndAddress, CRC, Enabled, PackageName, size, updateComment, description, devURL, type, configs, userDatas, pictures, " +
                                            "dependencies\n\nNode found: {0}\n\nmore informations, see logfile",
                                            configNode.Name.ToString()));
                                    break;
                            }
                            //mandatory component
                            if (packageNodeList.Contains(configNode.Name.ToString()))
                                packageNodeList.Remove(configNode.Name.ToString());
                            //optional component
                            else if (optionalPackageNodeList.Contains(configNode.Name.ToString()))
                                optionalPackageNodeList.Remove(configNode.Name.ToString());
                            //unknown component
                            else
                                unknownNodeList.Add(configNode.Name.ToString());
                        }
                        if (packageNodeList.Count > 0)
                            Logging.Warning(string.Format("modInfo.Xml nodes not used: {0} => package {1} (line {2})",
                                string.Join(",", packageNodeList), m.Name, ((IXmlLineInfo)configHolder).LineNumber));
                        if (unknownNodeList.Count > 0)
                            Logging.Warning(string.Format("modInfo.Xml unknown nodes: {0} => package {1} (line {2})",
                                string.Join(",", unknownNodeList), m.PackageName, ((IXmlLineInfo)configHolder).LineNumber));
                        if (string.IsNullOrWhiteSpace(c.PackageName))
                        {
                            throw new BadMemeException("packagename is blank!!!!!1");
                        }
                        //attach it to eithor the config of correct level or the mod
                        if (parentIsMod)
                            m.Packages.Add(c);
                        else
                            con.Packages.Add(c);
                        break;
                    default:
                        Logging.Warning(string.Format("modInfo.Xml incomprehensible node \"{0}\" => mod {1} ({2}) => expected nodes: config (line {3})",
                            configHolder.Name.ToString(), m.Name, m.ZipFile, ((IXmlLineInfo)configHolder).LineNumber));
                        if (ModpackSettings.DatabaseDistroVersion == DatabaseVersions.Test)
                            MessageBox.Show(string.Format("modInfo.Xml file is incomprehensible.\nexpected nodes: config\n\nNode found: {0}\n\nmore informations, see logfile",
                                configHolder.Name));
                        break;
                }
            }
        }
        
        //convert string with CR and/or LF from Xml save format
        [Obsolete]
        private static string ConvertFromXmlSaveFormat(string s)
        {
            return s.TrimEnd().Replace("@", "\n").Replace(@"\r", "\r").Replace(@"\t", "\t").Replace(@"\n", "\n").Replace(@"&#92;", @"\");
        }

        //convert string with CR and/or LF to Xml save format
        [Obsolete]
        private static string ConvertToXmlSaveFormat(string s)
        {
            return s.TrimEnd().Replace(@"\", @"&#92;").Replace("\r", @"\r").Replace("\t", @"\t").Replace("\n", @"\n");
        }


        /// <summary>
        /// Saves the current mod database into the legacy document format
        /// </summary>
        /// <param name="saveLocation">The file save location</param>
        /// <param name="doc">The XmlDocument to save into</param>
        /// <param name="globalDependencies">The list of global dependencies</param>
        /// <param name="dependencies">The list of dependencies</param>
        /// <param name="parsedCatagoryList">The list of categories</param>
        [Obsolete]
        public static void SaveDatabaseLegacy(string saveLocation, XmlDocument doc, List<DatabasePackage> globalDependencies,
            List<Dependency> dependencies, List<Category> parsedCatagoryList)
        {
            //V2 compatibility: dependencies need to be sorted into logical and regular for legacy database
            List<Dependency> logicalDependencies = dependencies.Where(dep => dep.wasLogicalDependencyLegacy).ToList();
            dependencies = dependencies.Where(dep => !dep.wasLogicalDependencyLegacy).ToList();

            // Create an Xml declaration. (except it already has it, so don't)
            //XmlDeclaration xmldecl;
            //xmldecl = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");

            // Add the new node to the document.
            //XmlElement cDoc = doc.DocumentElement;
            //doc.InsertBefore(xmldecl, cDoc);

            //database root modInfo.Xml
            XmlElement root = doc.DocumentElement;

            //global dependencies
            XmlElement globalDependenciesXml = doc.CreateElement("globaldependencies");
            foreach (DatabasePackage d in globalDependencies)
            {
                //declare dependency root
                XmlElement globalDependencyRoot = doc.CreateElement("globaldependency");
                //make dependency elements
                //mandatory
                XmlElement globalDepZipFile = doc.CreateElement("zipFile");
                if(!string.IsNullOrWhiteSpace(d.ZipFile))
                    globalDepZipFile.InnerText = d.ZipFile.Trim();
                globalDependencyRoot.AppendChild(globalDepZipFile);

                XmlElement globalDepCRC = doc.CreateElement("crc");
                if (!string.IsNullOrWhiteSpace(d.CRC))
                    globalDepCRC.InnerText = d.CRC.Trim();
                globalDependencyRoot.AppendChild(globalDepCRC);

                XmlElement globalDepEnabled = doc.CreateElement("enabled");
                globalDepEnabled.InnerText = "" + d.Enabled;
                globalDependencyRoot.AppendChild(globalDepEnabled);

                XmlElement globalDepAppendExtraction = doc.CreateElement("appendExtraction");
                globalDepAppendExtraction.InnerText = "" + d.AppendExtraction;
                globalDependencyRoot.AppendChild(globalDepAppendExtraction);

                XmlElement globalDepPackageName = doc.CreateElement("packageName");
                if (!string.IsNullOrWhiteSpace(d.PackageName))
                    globalDepPackageName.InnerText = d.PackageName.Trim();
                globalDependencyRoot.AppendChild(globalDepPackageName);

                //optional elements
                if (d.Timestamp > 0)
                {
                    XmlElement globalDepTimestamp = doc.CreateElement("timestamp");
                    globalDepTimestamp.InnerText = d.Timestamp.ToString();
                    globalDependencyRoot.AppendChild(globalDepTimestamp);
                }
                if (!d.StartAddress.Trim().Equals(Settings.DefaultStartAddress) && !string.IsNullOrWhiteSpace(d.StartAddress))
                {
                    XmlElement globalDepStartAddress = doc.CreateElement("startAddress");
                    globalDepStartAddress.InnerText = d.StartAddress.Trim();
                    globalDependencyRoot.AppendChild(globalDepStartAddress);
                }
                if (!d.EndAddress.Trim().Equals(Settings.DefaultEndAddress) && !string.IsNullOrWhiteSpace(d.EndAddress))
                {
                    XmlElement globalDepEndAddress = doc.CreateElement("endAddress");
                    globalDepEndAddress.InnerText = d.EndAddress.Trim();
                    globalDependencyRoot.AppendChild(globalDepEndAddress);
                }
                if (!string.IsNullOrWhiteSpace(d.DevURL))
                {
                    XmlElement globalDepURL = doc.CreateElement("devURL");
                    globalDepURL.InnerText = d.DevURL.Trim();
                    globalDependencyRoot.AppendChild(globalDepURL);
                }
                if (!d.LogAtInstall)
                {
                    XmlElement globalDepLogAtInstall = doc.CreateElement("logAtInstall");
                    globalDepLogAtInstall.InnerText = d.LogAtInstall.ToString();
                    globalDependencyRoot.AppendChild(globalDepLogAtInstall);
                }
                //attach dependency root
                globalDependenciesXml.AppendChild(globalDependencyRoot);
            }
            root.AppendChild(globalDependenciesXml);

            //dependencies
            XmlElement DependenciesXml = doc.CreateElement("dependencies");
            foreach (Dependency d in dependencies)
            {
                //declare dependency root
                XmlElement dependencyRoot = doc.CreateElement("dependency");
                //make dependency
                //mandatory
                XmlElement depZipFile = doc.CreateElement("zipFile");
                if (!string.IsNullOrWhiteSpace(d.ZipFile))
                    depZipFile.InnerText = d.ZipFile.Trim();
                dependencyRoot.AppendChild(depZipFile);

                XmlElement depCRC = doc.CreateElement("crc");
                if (!string.IsNullOrWhiteSpace(d.CRC))
                    depCRC.InnerText = d.CRC.Trim();
                dependencyRoot.AppendChild(depCRC);

                XmlElement depEnabled = doc.CreateElement("enabled");
                depEnabled.InnerText = "" + d.Enabled;
                dependencyRoot.AppendChild(depEnabled);

                XmlElement depAppendExtraction = doc.CreateElement("appendExtraction");
                depAppendExtraction.InnerText = "" + d.AppendExtraction;
                dependencyRoot.AppendChild(depAppendExtraction);

                XmlElement depPackageName = doc.CreateElement("packageName");
                if (!string.IsNullOrWhiteSpace(d.PackageName))
                    depPackageName.InnerText = d.PackageName.Trim();
                dependencyRoot.AppendChild(depPackageName);
                //optional
                if(d.Timestamp > 0)
                {
                    XmlElement depTimestamp = doc.CreateElement("timestamp");
                    depTimestamp.InnerText = d.Timestamp.ToString();
                    dependencyRoot.AppendChild(depTimestamp);
                }
                if(!d.StartAddress.Trim().Equals(Settings.DefaultStartAddress) && !string.IsNullOrWhiteSpace(d.StartAddress))
                {
                    XmlElement depStartAddress = doc.CreateElement("startAddress");
                    depStartAddress.InnerText = d.StartAddress.Trim();
                    dependencyRoot.AppendChild(depStartAddress);
                }
                if(!d.EndAddress.Trim().Equals(Settings.DefaultEndAddress) && !string.IsNullOrWhiteSpace(d.EndAddress))
                {
                    XmlElement depEndAddress = doc.CreateElement("endAddress");
                    depEndAddress.InnerText = d.EndAddress.Trim();
                    dependencyRoot.AppendChild(depEndAddress);
                }
                if(!string.IsNullOrWhiteSpace(d.DevURL.Trim()))
                {
                    XmlElement depdevURL = doc.CreateElement("devURL");
                    depdevURL.InnerText = d.DevURL.Trim();
                    dependencyRoot.AppendChild(depdevURL);
                }
                if (!d.LogAtInstall)
                {
                    XmlElement depLogAtInstall = doc.CreateElement("logAtInstall");
                    depLogAtInstall.InnerText = d.LogAtInstall.ToString();
                    dependencyRoot.AppendChild(depLogAtInstall);
                }

                //logicalDependencies for the configs
                if (d.Dependencies.Count > 0)
                {
                    XmlElement depLogicalDependencies = doc.CreateElement("logicalDependencies");
                    foreach (DatabaseLogic ld in d.Dependencies)
                    {
                        //declare logicalDependency root
                        XmlElement DependencyRoot = doc.CreateElement("logicalDependency");
                        //make logicalDependency
                        XmlElement DependencyPackageName = doc.CreateElement("packageName");
                        DependencyPackageName.InnerText = ld.PackageName.Trim();
                        DependencyRoot.AppendChild(DependencyPackageName);
                        XmlElement DependencyNegateFlag = doc.CreateElement("negateFlag");
                        DependencyNegateFlag.InnerText = "" + ld.NotFlag;
                        DependencyRoot.AppendChild(DependencyNegateFlag);
                        //attach logicalDependency root
                        depLogicalDependencies.AppendChild(DependencyRoot);
                    }
                    dependencyRoot.AppendChild(depLogicalDependencies);
                }
                DependenciesXml.AppendChild(dependencyRoot);
            }
            root.AppendChild(DependenciesXml);
            //dependencies
            XmlElement logicalDependenciesXml = doc.CreateElement("logicalDependencies");
            foreach (Dependency d in logicalDependencies)
            {
                //declare dependency root
                XmlElement logicalDependencyRoot = doc.CreateElement("logicalDependency");
                //make dependency
                //mandatory
                XmlElement logicalDepZipFile = doc.CreateElement("zipFile");
                if (!d.ZipFile.Trim().Equals(""))
                    logicalDepZipFile.InnerText = d.ZipFile.Trim();
                logicalDependencyRoot.AppendChild(logicalDepZipFile);

                XmlElement logicalDepCRC = doc.CreateElement("crc");
                if (!d.ZipFile.Trim().Equals(""))
                    logicalDepCRC.InnerText = d.CRC.Trim();
                logicalDependencyRoot.AppendChild(logicalDepCRC);

                XmlElement logicalDepEnabled = doc.CreateElement("enabled");
                logicalDepEnabled.InnerText = "" + d.Enabled;
                logicalDependencyRoot.AppendChild(logicalDepEnabled);

                XmlElement logicalDepPackageName = doc.CreateElement("packageName");
                if (!d.ZipFile.Trim().Equals(""))
                    logicalDepPackageName.InnerText = d.PackageName.Trim();
                logicalDependencyRoot.AppendChild(logicalDepPackageName);

                XmlElement logicalDepLogic = doc.CreateElement("logic");
                logicalDepLogic.InnerText = "AND";//hard-coded for legacy
                logicalDependencyRoot.AppendChild(logicalDepLogic);

                //optional
                if (d.Timestamp > 0)
                {
                    XmlElement logicalDepTimestamp = doc.CreateElement("timestamp");
                    logicalDepTimestamp.InnerText = d.Timestamp.ToString();
                    logicalDependencyRoot.AppendChild(logicalDepTimestamp);
                }
                if (!d.StartAddress.Trim().Equals(Settings.DefaultStartAddress) && !string.IsNullOrWhiteSpace(d.StartAddress))
                {
                    XmlElement logicalDepStartAddress = doc.CreateElement("startAddress");
                    logicalDepStartAddress.InnerText = d.StartAddress.Trim();
                    logicalDependencyRoot.AppendChild(logicalDepStartAddress);
                }
                if (!d.StartAddress.Trim().Equals(Settings.DefaultEndAddress) && !string.IsNullOrWhiteSpace(d.EndAddress))
                {
                    XmlElement logicalDepEndAddress = doc.CreateElement("endAddress");
                    logicalDepEndAddress.InnerText = d.EndAddress.Trim();
                    logicalDependencyRoot.AppendChild(logicalDepEndAddress);
                }
                if(!string.IsNullOrWhiteSpace(d.DevURL))
                {
                    XmlElement logicalDepdevURL = doc.CreateElement("devURL");
                    logicalDepdevURL.InnerText = d.DevURL.Trim();
                    logicalDependencyRoot.AppendChild(logicalDepdevURL);
                }
                if (!d.LogAtInstall)
                {
                    XmlElement logicalDepLogAtInstall = doc.CreateElement("logAtInstall");
                    logicalDepLogAtInstall.InnerText = d.LogAtInstall.ToString();
                    logicalDependencyRoot.AppendChild(logicalDepLogAtInstall);
                }
                //attach dependency root
                logicalDependenciesXml.AppendChild(logicalDependencyRoot);
            }
            root.AppendChild(logicalDependenciesXml);
            //catagories
            XmlElement catagoriesHolder = doc.CreateElement("catagories");
            foreach (Category c in parsedCatagoryList)
            {
                //catagory root
                XmlElement catagoryRoot = doc.CreateElement("catagory");
                //make catagory
                XmlElement catagoryName = doc.CreateElement("name");
                catagoryName.InnerText = c.Name.Trim();
                catagoryRoot.AppendChild(catagoryName);

                XmlElement categoryInstallGroup = doc.CreateElement("installGroup");
                categoryInstallGroup.InnerText = "" + c.InstallGroup;
                catagoryRoot.AppendChild(categoryInstallGroup);

                //dependencies for catagory
                XmlElement catagoryDependencies = doc.CreateElement("dependencies");
                foreach (DatabaseLogic d in c.Dependencies)
                {
                    //declare dependency root
                    XmlElement DependencyRoot = doc.CreateElement("dependency");
                    XmlElement DepPackageName = doc.CreateElement("packageName");
                    DepPackageName.InnerText = d.PackageName.Trim();
                    DependencyRoot.AppendChild(DepPackageName);
                    //attach dependency root
                    catagoryDependencies.AppendChild(DependencyRoot);
                }
                catagoryRoot.AppendChild(catagoryDependencies);
                //mods for catagory
                XmlElement modsHolder = doc.CreateElement("packages");
                foreach (SelectablePackage m in c.Packages)
                {
                    //add it to the list
                    XmlElement modRoot = doc.CreateElement("package");
                    //mandatory
                    XmlElement modName = doc.CreateElement("name");
                    if (!string.IsNullOrWhiteSpace(m.Name))
                        modName.InnerText = m.Name.Trim();
                    modRoot.AppendChild(modName);
                    XmlElement modZipFile = doc.CreateElement("zipFile");
                    if (!string.IsNullOrWhiteSpace(m.ZipFile))
                        modZipFile.InnerText = m.ZipFile.Trim();
                    modRoot.AppendChild(modZipFile);
                    XmlElement modZipCRC = doc.CreateElement("crc");
                    if (!string.IsNullOrWhiteSpace(m.CRC))
                        modZipCRC.InnerText = m.CRC.Trim();
                    modRoot.AppendChild(modZipCRC);
                    XmlElement modEnabled = doc.CreateElement("enabled");
                    modEnabled.InnerText = "" + m.Enabled;
                    modRoot.AppendChild(modEnabled);
                    XmlElement modVisible = doc.CreateElement("visible");
                    modVisible.InnerText = "" + m.Visible;
                    modRoot.AppendChild(modVisible);
                    XmlElement modPackageName = doc.CreateElement("packageName");
                    if (!string.IsNullOrWhiteSpace(m.PackageName))
                        modPackageName.InnerText = m.PackageName.Trim();
                    modRoot.AppendChild(modPackageName);
                    XmlElement modType = doc.CreateElement("type");
                    if (m.Type != SelectionTypes.none)
                        modType.InnerText = m.Type.ToString().Trim();
                    modRoot.AppendChild(modType);
                    //optional
                    if (!string.IsNullOrWhiteSpace(m.Version))
                    {
                        XmlElement modVersion = doc.CreateElement("version");
                        modVersion.InnerText = m.Version.Trim();
                        modRoot.AppendChild(modVersion);
                    }
                    if(m.Timestamp > 0)
                    {
                        XmlElement modTimestamp = doc.CreateElement("timestamp");
                        modTimestamp.InnerText = m.Timestamp.ToString();
                        modRoot.AppendChild(modTimestamp);
                    }
                    if(!m.StartAddress.Trim().Equals(Settings.DefaultStartAddress) && !string.IsNullOrWhiteSpace(m.StartAddress))
                    {
                        XmlElement modStartAddress = doc.CreateElement("startAddress");
                        modStartAddress.InnerText = m.StartAddress.Trim();
                        modRoot.AppendChild(modStartAddress);
                    }
                    if(!m.EndAddress.Trim().Equals(Settings.DefaultEndAddress) && !string.IsNullOrWhiteSpace(m.StartAddress))
                    {
                        XmlElement modEndAddress = doc.CreateElement("endAddress");
                        modEndAddress.InnerText = m.EndAddress.Trim();
                        modRoot.AppendChild(modEndAddress);
                    }
                    if(m.Size > 0)
                    {
                        XmlElement modZipSize = doc.CreateElement("size");
                        modZipSize.InnerText = "" + m.Size;
                        modRoot.AppendChild(modZipSize);
                    }
                    if (!m.LogAtInstall)
                    {
                        XmlElement modLogAtInstall = doc.CreateElement("logAtInstall");
                        modLogAtInstall.InnerText = m.LogAtInstall.ToString();
                        modRoot.AppendChild(modLogAtInstall);
                    }
                    if (!string.IsNullOrWhiteSpace(m.UpdateComment))
                    {
                        XmlElement modUpdateComment = doc.CreateElement("updateComment");
                        modUpdateComment.InnerText = ConvertToXmlSaveFormat(m.UpdateComment);
                        modRoot.AppendChild(modUpdateComment);
                    }
                    if(!string.IsNullOrWhiteSpace(m.Description))
                    {
                        XmlElement modDescription = doc.CreateElement("description");
                        modDescription.InnerText = ConvertToXmlSaveFormat(m.Description);
                        modRoot.AppendChild(modDescription);
                    }
                    if(!string.IsNullOrWhiteSpace(m.DevURL))
                    {
                        XmlElement modDevURL = doc.CreateElement("devURL");
                        modDevURL.InnerText = m.DevURL.Trim();
                        modRoot.AppendChild(modDevURL);
                    }
                    //datas for the mods
                    if(m.UserFiles.Count > 0)
                    {
                        XmlElement modDatas = doc.CreateElement("userDatas");
                        foreach (UserFile us in m.UserFiles)
                        {
                            XmlElement userData = doc.CreateElement("userData");
                            userData.InnerText = us.Pattern.Trim();
                            if (us.PlaceBeforeExtraction)
                                userData.SetAttribute("pre", "" + us.PlaceBeforeExtraction);
                            if (us.SystemInitiated)
                                userData.SetAttribute("system", "" + us.SystemInitiated);
                            modDatas.AppendChild(userData);
                        }
                        modRoot.AppendChild(modDatas);
                    }
                    //pictures for the mods
                    if(m.Medias.Count > 0)
                    {
                        XmlElement modPictures = doc.CreateElement("medias");
                        foreach (Media p in m.Medias)
                        {
                            XmlElement pictureRoot = doc.CreateElement("media");
                            XmlElement pictureType = doc.CreateElement("type");
                            XmlElement pictureURL = doc.CreateElement("URL");
                            pictureURL.InnerText = p.URL.Trim();
                            pictureType.InnerText = "" + (int)p.MediaType;
                            pictureRoot.AppendChild(pictureType);
                            pictureRoot.AppendChild(pictureURL);
                            modPictures.AppendChild(pictureRoot);
                        }
                        modRoot.AppendChild(modPictures);
                    }
                    if (m.Packages.Count > 0)
                    {
                        //configs for the mods
                        XmlElement configsHolder = doc.CreateElement("packages");
                        SaveDatabaseConfigLegacy(doc, configsHolder, m.Packages);
                        modRoot.AppendChild(configsHolder);
                    }
                    if(m.Dependencies.Count > 0)
                    {
                        XmlElement modDependencies = doc.CreateElement("dependencies");
                        foreach (DatabaseLogic d in m.Dependencies)
                        {
                            //declare dependency root
                            XmlElement DependencyRoot = doc.CreateElement("dependency");
                            //make dependency
                            XmlElement DepPackageName = doc.CreateElement("packageName");
                            DepPackageName.InnerText = d.PackageName.Trim();
                            DependencyRoot.AppendChild(DepPackageName);
                            //attach dependency root
                            modDependencies.AppendChild(DependencyRoot);
                        }
                        modRoot.AppendChild(modDependencies);
                    }
                    modsHolder.AppendChild(modRoot);
                }
                catagoryRoot.AppendChild(modsHolder);
                //append catagory
                catagoriesHolder.AppendChild(catagoryRoot);
            }
            root.AppendChild(catagoriesHolder);

            // save database file
            doc.Save(saveLocation);
        }

        [Obsolete]
        private static void SaveDatabaseConfigLegacy(XmlDocument doc, XmlElement configsHolder, List<SelectablePackage> configsList)
        {
            foreach (SelectablePackage cc in configsList)
            {
                //add the config to the list
                XmlElement configRoot = doc.CreateElement("package");
                configsHolder.AppendChild(configRoot);
                //mandatory
                XmlElement configName = doc.CreateElement("name");
                if (!string.IsNullOrWhiteSpace(cc.Name))
                    configName.InnerText = cc.Name.Trim();
                configRoot.AppendChild(configName);
                XmlElement configZipFile = doc.CreateElement("zipFile");
                if (!string.IsNullOrWhiteSpace(cc.ZipFile))
                    configZipFile.InnerText = cc.ZipFile.Trim();
                configRoot.AppendChild(configZipFile);
                XmlElement configZipCRC = doc.CreateElement("crc");
                if (!string.IsNullOrWhiteSpace(cc.CRC))
                    configZipCRC.InnerText = cc.CRC.Trim();
                configRoot.AppendChild(configZipCRC);
                XmlElement configEnabled = doc.CreateElement("enabled");
                configEnabled.InnerText = "" + cc.Enabled;
                configRoot.AppendChild(configEnabled);
                XmlElement configVisible = doc.CreateElement("visible");
                configVisible.InnerText = "" + cc.Visible;
                configRoot.AppendChild(configVisible);
                XmlElement configPackageName = doc.CreateElement("packageName");
                if (!string.IsNullOrWhiteSpace(cc.PackageName))
                    configPackageName.InnerText = cc.PackageName.Trim();
                configRoot.AppendChild(configPackageName);
                XmlElement configType = doc.CreateElement("type");
                if (cc.Type != SelectionTypes.none)
                    configType.InnerText = cc.Type.ToString().Trim();
                configRoot.AppendChild(configType);
                //optional
                if(!string.IsNullOrWhiteSpace(cc.Version))
                {
                    XmlElement configVersion = doc.CreateElement("version");
                    configVersion.InnerText = cc.Version.Trim();
                    configRoot.AppendChild(configVersion);
                }
                if(cc.Timestamp > 0)
                {
                    XmlElement configTimestamp = doc.CreateElement("timestamp");
                    configTimestamp.InnerText = cc.Timestamp.ToString();
                    configRoot.AppendChild(configTimestamp);
                }
                if(!cc.StartAddress.Trim().Equals(Settings.DefaultStartAddress) && !string.IsNullOrWhiteSpace(cc.StartAddress))
                {
                    XmlElement configStartAddress = doc.CreateElement("startAddress");
                    configStartAddress.InnerText = cc.StartAddress.Trim();
                    configRoot.AppendChild(configStartAddress);
                }
                if(!cc.EndAddress.Trim().Equals(Settings.DefaultEndAddress) && !string.IsNullOrWhiteSpace(cc.StartAddress))
                {
                    XmlElement configEndAddress = doc.CreateElement("endAddress");
                    configEndAddress.InnerText = cc.EndAddress.Trim();
                    configRoot.AppendChild(configEndAddress);
                }
                if(cc.Size > 0)
                {
                    XmlElement configSize = doc.CreateElement("size");
                    configSize.InnerText = "" + cc.Size;
                    configRoot.AppendChild(configSize);
                }
                if (!cc.LogAtInstall)
                {
                    XmlElement configLogAtInstall = doc.CreateElement("logAtInstall");
                    configLogAtInstall.InnerText = cc.LogAtInstall.ToString();
                    configRoot.AppendChild(configLogAtInstall);
                }
                if (!string.IsNullOrWhiteSpace(cc.UpdateComment))
                {
                    XmlElement configComment = doc.CreateElement("updateComment");
                    configComment.InnerText = ConvertToXmlSaveFormat(cc.UpdateComment);
                    configRoot.AppendChild(configComment);
                }
                if(!string.IsNullOrWhiteSpace(cc.Description))
                {
                    XmlElement configDescription = doc.CreateElement("description");
                    configDescription.InnerText = ConvertToXmlSaveFormat(cc.Description);
                    configRoot.AppendChild(configDescription);
                }
                if(!string.IsNullOrWhiteSpace(cc.DevURL))
                {
                    XmlElement configDevURL = doc.CreateElement("devURL");
                    configDevURL.InnerText = cc.DevURL.Trim();
                    configRoot.AppendChild(configDevURL);
                }
                //datas for the mods
                if(cc.UserFiles.Count > 0)
                {
                    XmlElement configDatas = doc.CreateElement("userDatas");
                    foreach (UserFile us in cc.UserFiles)
                    {
                        XmlElement userData = doc.CreateElement("userData");
                        userData.InnerText = us.Pattern.Trim();
                        if (us.PlaceBeforeExtraction)
                            userData.SetAttribute("pre", "" + us.PlaceBeforeExtraction);
                        if (us.SystemInitiated)
                            userData.SetAttribute("system", "" + us.SystemInitiated);
                        configDatas.AppendChild(userData);
                    }
                    configRoot.AppendChild(configDatas);
                }
                //pictures for the configs
                if(cc.Medias.Count > 0)
                {
                    XmlElement configPictures = doc.CreateElement("medias");
                    foreach (Media p in cc.Medias)
                    {
                        XmlElement pictureRoot = doc.CreateElement("media");
                        XmlElement pictureType = doc.CreateElement("type");
                        XmlElement pictureURL = doc.CreateElement("URL");
                        pictureURL.InnerText = p.URL.Trim();
                        pictureType.InnerText = "" + (int)p.MediaType;
                        pictureRoot.AppendChild(pictureType);
                        pictureRoot.AppendChild(pictureURL);
                        configPictures.AppendChild(pictureRoot);
                    }
                    configRoot.AppendChild(configPictures);
                }
                //packages for the packages (meta)
                if(cc.Packages.Count > 0)
                {
                    XmlElement configsHolderSub = doc.CreateElement("packages");
                    //if statement here
                    SaveDatabaseConfigLegacy(doc, configsHolderSub, cc.Packages);
                    configRoot.AppendChild(configsHolderSub);
                }
                //dependencies for the packages
                if(cc.Dependencies.Count > 0)
                {
                    XmlElement catDependencies = doc.CreateElement("dependencies");
                    foreach (DatabaseLogic d in cc.Dependencies)
                    {
                        //declare dependency root
                        XmlElement DependencyRoot = doc.CreateElement("dependency");
                        //make dependency
                        XmlElement DepPackageName = doc.CreateElement("packageName");
                        DepPackageName.InnerText = d.PackageName.Trim();
                        DependencyRoot.AppendChild(DepPackageName);
                        //attach dependency root
                        catDependencies.AppendChild(DependencyRoot);
                    }
                    configRoot.AppendChild(catDependencies);
                }
                configsHolder.AppendChild(configRoot);
            }
        }
        #endregion

        #region Database Saving
        /// <summary>
        /// Save the database to an Xml version
        /// </summary>
        /// <param name="saveLocation">The folder path to save the files into</param>
        /// <param name="gameVersion">The version of the game that this database supports</param>
        /// <param name="onlineFolderVersion">The online folder for the zip file location of this database</param>
        /// <param name="globalDependencies">The list of global dependencies</param>
        /// <param name="dependencies">The list of dependencies</param>
        /// <param name="parsedCatagoryList">The list of categories</param>
        /// <param name="versionToSaveAs">The Xml version of the database to save as</param>
        public static void SaveDatabase(string saveLocation, string gameVersion, string onlineFolderVersion, List<DatabasePackage> globalDependencies,
        List<Dependency> dependencies, List<Category> parsedCatagoryList, DatabaseXmlVersion versionToSaveAs)
        {
            //make root of document
            XmlDocument doc = new XmlDocument();

            //add declaration
            //https://stackoverflow.com/questions/334256/how-do-i-add-a-custom-xmldeclaration-with-xmldocument-xmldeclaration
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            doc.AppendChild(xmlDeclaration);

            //database root modInfo.Xml
            XmlElement root = doc.CreateElement("modInfoAlpha.xml");

            //add version and onlineFolder attributes to modInfoAlpha.xml element
            root.SetAttribute("version", gameVersion.Trim());
            root.SetAttribute("onlineFolder", onlineFolderVersion.Trim());

            //put root element into document
            doc.AppendChild(root);

            //
            switch (versionToSaveAs)
            {
                case DatabaseXmlVersion.Legacy:
                    //when legacy, saveLocation is a single file
                    if(Path.HasExtension(saveLocation))
#pragma warning disable CS0612
                        SaveDatabaseLegacy(saveLocation, doc, globalDependencies, dependencies, parsedCatagoryList);
                    else
                        SaveDatabaseLegacy(Path.Combine(saveLocation, "modInfoAlpha.xml"), doc, globalDependencies, dependencies, parsedCatagoryList);
                    break;
#pragma warning restore CS0612
                case DatabaseXmlVersion.OnePointOne:
                    //in 1.1, saveLocation is a document path
                    if (Path.HasExtension(saveLocation))
                        SaveDatabase1V1(Path.GetDirectoryName(saveLocation), doc, globalDependencies, dependencies, parsedCatagoryList);
                    else
                        SaveDatabase1V1(saveLocation, doc, globalDependencies, dependencies, parsedCatagoryList);
                    break;
            }            
        }

        /// <summary>
        /// Save the database to the Xml version 1.1 standard
        /// </summary>
        /// <param name="savePath">The path to save all the xml files to</param>
        /// <param name="doc">The root XmlDocument to save the header information to</param>
        /// <param name="globalDependencies">The list of global dependencies</param>
        /// <param name="dependencies">The list of dependencies</param>
        /// <param name="parsedCatagoryList">The list of categories</param>
        public static void SaveDatabase1V1(string savePath, XmlDocument doc, List<DatabasePackage> globalDependencies,
        List<Dependency> dependencies, List<Category> parsedCatagoryList)
        {
            //create root document (contains filenames for all other xml documents)
            XmlElement root = doc.DocumentElement;
            root.SetAttribute("documentVersion", "1.1");

            //create and append globalDependencies
            XmlElement xmlGlobalDependencies = doc.CreateElement("globalDependencies");
            xmlGlobalDependencies.SetAttribute("file", "globalDependencies.xml");
            root.AppendChild(xmlGlobalDependencies);

            //create and append dependencies
            XmlElement xmlDependencies = doc.CreateElement("dependencies");
            xmlDependencies.SetAttribute("file", "dependencies.xml");
            root.AppendChild(xmlDependencies);

            //create and append categories
            XmlElement xmlCategories = doc.CreateElement("categories");
            foreach(Category cat in parsedCatagoryList)
            {
                XmlElement xmlCategory = doc.CreateElement("category");
                if (string.IsNullOrWhiteSpace(cat.XmlFilename))
                {
                    cat.XmlFilename = cat.Name.Replace(" ", string.Empty);
                    cat.XmlFilename = cat.XmlFilename.Replace("/", "_") + ".xml";
                }
                xmlCategory.SetAttribute("file", cat.XmlFilename);
                xmlCategories.AppendChild(xmlCategory);
            }
            root.AppendChild(xmlCategories);
            doc.Save(Path.Combine(savePath, "database.xml"));

            //save the actual xml files for database entries
            //globalDependency
            XmlDocument xmlGlobalDependenciesFile = new XmlDocument();
            XmlDeclaration xmlDeclaration = xmlGlobalDependenciesFile.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            xmlGlobalDependenciesFile.AppendChild(xmlDeclaration);
            XmlElement xmlGlobalDependenciesFileRoot = xmlGlobalDependenciesFile.CreateElement("GlobalDependencies");
            SaveDatabaseList1V1(globalDependencies, xmlGlobalDependenciesFileRoot, xmlGlobalDependenciesFile, "GlobalDependency");
            xmlGlobalDependenciesFile.AppendChild(xmlGlobalDependenciesFileRoot);
            xmlGlobalDependenciesFile.Save(Path.Combine(savePath, "globalDependencies.xml"));

            //dependency
            XmlDocument xmlDependenciesFile = new XmlDocument();
            xmlDeclaration = xmlDependenciesFile.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            xmlDependenciesFile.AppendChild(xmlDeclaration);
            XmlElement xmlDependenciesFileRoot = xmlDependenciesFile.CreateElement("Dependencies");
            SaveDatabaseList1V1(dependencies, xmlDependenciesFileRoot, xmlDependenciesFile, "Dependency");
            xmlDependenciesFile.AppendChild(xmlDependenciesFileRoot);
            xmlDependenciesFile.Save(Path.Combine(savePath, "dependencies.xml"));

            //for each category do the same thing
            foreach (Category cat in parsedCatagoryList)
            {
                XmlDocument xmlCategoryFile = new XmlDocument();
                xmlDeclaration = xmlCategoryFile.CreateXmlDeclaration("1.0", "UTF-8", "yes");
                xmlCategoryFile.AppendChild(xmlDeclaration);

                XmlElement xmlCategoryFileRoot = xmlCategoryFile.CreateElement("Category");

                //save attributes for category
                SavePropertiesToXmlAttributes(cat, xmlCategoryFileRoot, cat);

                //create and savae maintainers element if not default
                SavePropertiesToXmlElements(new Category(), nameof(cat.Maintainers), xmlCategoryFileRoot, cat, null, xmlCategoryFile);

                //need to incorporate the fact that categories have dependencies
                if (cat.Dependencies.Count > 0)
                {
                    XmlElement xmlCategoryDependencies = xmlCategoryFile.CreateElement("Dependencies");
                    foreach(DatabaseLogic logic in cat.Dependencies)
                    {
                        XmlElement xmlCategoryDependency = xmlCategoryFile.CreateElement("Dependency");
                        foreach(string attributeToSave in logic.PropertiesForSerializationAttributes())
                        {
                            PropertyInfo propertyInfo = logic.GetType().GetProperty(attributeToSave);
                            xmlCategoryDependency.SetAttribute(propertyInfo.Name, propertyInfo.GetValue(logic).ToString());
                        }
                        xmlCategoryDependencies.AppendChild(xmlCategoryDependency);
                    }
                    xmlCategoryFileRoot.AppendChild(xmlCategoryDependencies);
                }

                //then save packages
                SaveDatabaseList1V1(cat.Packages, xmlCategoryFileRoot, xmlCategoryFile, "Package");
                xmlCategoryFile.AppendChild(xmlCategoryFileRoot);
                xmlCategoryFile.Save(Path.Combine(savePath, cat.XmlFilename));
            }

        }

        /// <summary>
        /// Saves a list of packages to a document
        /// </summary>
        /// <param name="packagesToSave">The generic list of packages to save</param>
        /// <param name="documentRootElement">The element that will be holding this list</param>
        /// <param name="docToMakeElementsFrom">The document needed to create xml elements and attributes</param>
        /// <param name="nameToSaveElementsBy">The string name to save the xml element name by</param>
        private static void SaveDatabaseList1V1(IList packagesToSave, XmlElement documentRootElement, XmlDocument docToMakeElementsFrom, string nameToSaveElementsBy)
        {
            //based on list type, get list of elements and attributes to save as
            //get the type of element in the list
            Type listObjectType = packagesToSave.GetType().GetInterfaces().Where(i => i.IsGenericType && i.GenericTypeArguments.Length == 1)
                .FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IEnumerable<>)).GenericTypeArguments[0];

            if (!(Activator.CreateInstance(listObjectType) is IXmlSerializable listEntryWithDefaultValues))
                throw new BadMemeException("Type of this list is not of IXmlSerializable");

            //parse for each package type
            for (int i = 0; i < packagesToSave.Count; i++)
            {
                //it's at least a databasePackage
                DatabasePackage packageToSaveOfAnyType = (DatabasePackage)packagesToSave[i];
                SelectablePackage packageOnlyUsedForNames = packageToSaveOfAnyType as SelectablePackage;

                //make the element to save to (Package, Dependency, etc.)
                XmlElement PackageHolder = docToMakeElementsFrom.CreateElement(nameToSaveElementsBy);

                //iterate through each of the attributes and nodes in the arrays to allow for listing in custom order
                SavePropertiesToXmlAttributes(listEntryWithDefaultValues, PackageHolder, packageToSaveOfAnyType);

                foreach (string elementToSave in listEntryWithDefaultValues.PropertiesForSerializationElements())
                {
                    PropertyInfo propertyOfPackage = listEntryWithDefaultValues.GetType().GetProperty(elementToSave);

                    //check if it's a package list of packages
                    if (packageOnlyUsedForNames != null && propertyOfPackage.Name.Equals(nameof(packageOnlyUsedForNames.Packages)) && packageOnlyUsedForNames.Packages.Count > 0)
                    {
                        XmlElement packagesHolder = docToMakeElementsFrom.CreateElement(nameof(packageOnlyUsedForNames.Packages));
                        SaveDatabaseList1V1(packageOnlyUsedForNames.Packages, packagesHolder, docToMakeElementsFrom, nameToSaveElementsBy);
                        PackageHolder.AppendChild(packagesHolder);
                    }
                    //else handle as standard element/element container
                    else
                    {
                        SavePropertiesToXmlElements(listEntryWithDefaultValues, elementToSave, PackageHolder, packageToSaveOfAnyType, propertyOfPackage, docToMakeElementsFrom);
                    }
                }
                //save them to the holder
                documentRootElement.AppendChild(PackageHolder);
            }
        }

        private static void SavePropertiesToXmlElements(IXmlSerializable databaseComponentWithDefaultValues, string elementToSave, XmlElement elementContainer, IDatabaseComponent componentToSave, PropertyInfo propertyOfComponentToSave, XmlDocument docToMakeElementsFrom)
        {
            if(propertyOfComponentToSave == null)
            {
                propertyOfComponentToSave = databaseComponentWithDefaultValues.GetType().GetProperty(elementToSave);
            }
            if (typeof(IEnumerable).IsAssignableFrom(propertyOfComponentToSave.PropertyType) && !propertyOfComponentToSave.PropertyType.Equals(typeof(string)))
            {
                //get the list type to allow for itterate
                IList list = (IList)propertyOfComponentToSave.GetValue(componentToSave);

                //if there's no items, then don't bother
                if (list.Count == 0)
                    return;

                //get the types of objects stored in this list
                Type objectTypeInList = list[0].GetType();

                if (!(Activator.CreateInstance(objectTypeInList) is IXmlSerializable subListEntry))
                    throw new BadMemeException("Type of this list is not of IXmlSerializable");

                //elementFieldHolder is holder for list type like "Medias" (the property name)
                XmlElement elementFieldHolder = docToMakeElementsFrom.CreateElement(propertyOfComponentToSave.Name);
                for (int k = 0; k < list.Count; k++)
                {
                    //list element value like "Media"
                    string objectInListName = objectTypeInList.Name;

                    //hard code compatibility "DatabaseLogic" -> "Dependency"
                    if (objectInListName.Equals(nameof(DatabaseLogic)))
                        objectInListName = nameof(Dependency);

                    //create the xml holder based on object name like "Media"
                    XmlElement elementFieldValue = docToMakeElementsFrom.CreateElement(objectInListName);

                    //if it's a single value type (like string)
                    if (objectTypeInList.IsValueType)
                    {
                        elementFieldValue.InnerText = list[k].ToString();
                    }
                    else
                    {
                        //custom type like media
                        //store attributes
                        foreach (string attributeToSave in subListEntry.PropertiesForSerializationAttributes())
                        {
                            PropertyInfo attributeProperty = objectTypeInList.GetProperty(attributeToSave);
                            elementFieldValue.SetAttribute(attributeProperty.Name, attributeProperty.GetValue(list[k]).ToString());
                        }

                        //store elements
                        foreach (string elementsToSave in subListEntry.PropertiesForSerializationElements())
                        {
                            PropertyInfo elementProperty = objectTypeInList.GetProperty(elementToSave);
                            string defaultValue = elementProperty.GetValue(subListEntry).ToString();
                            string currentValue = elementProperty.GetValue(list[k]).ToString();
                            if (currentValue != defaultValue)
                            {
                                XmlElement elementOfCustomType = docToMakeElementsFrom.CreateElement(elementToSave);
                                elementOfCustomType.InnerText = currentValue;
                                elementFieldValue.AppendChild(elementOfCustomType);
                            }
                        }
                    }

                    //add the "Media" to the "Medias"
                    elementFieldHolder.AppendChild(elementFieldValue);
                }
                elementContainer.AppendChild(elementFieldHolder);
            }
            //else the inner text of the node is only set/added if it's not default
            else
            {
                XmlElement element = docToMakeElementsFrom.CreateElement(propertyOfComponentToSave.Name);
                element.InnerText = propertyOfComponentToSave.GetValue(componentToSave).ToString();
                string defaultFieldValue = propertyOfComponentToSave.GetValue(databaseComponentWithDefaultValues).ToString();
                //only save node values when they are not default
                if (!element.InnerText.Equals(defaultFieldValue))
                {
                    element.InnerText = MacroUtils.MacroReplace(element.InnerText, ReplacementTypes.TextEscape);
                    elementContainer.AppendChild(element);
                }
            }
        }

        private static void SavePropertiesToXmlAttributes(IXmlSerializable listEntry, XmlElement elementContainer, IDatabaseComponent componentToSave)
        {
            //iterate through each of the attributes and nodes in the arrays to allow for listing in custom order
            foreach (string attributeToSave in listEntry.PropertiesForSerializationAttributes())
            {
                PropertyInfo propertyOfPackage = listEntry.GetType().GetProperty(attributeToSave);
                //attributs are value types, so just set it
                elementContainer.SetAttribute(propertyOfPackage.Name, propertyOfPackage.GetValue(componentToSave).ToString());
            }
        }
        #endregion

        #region Component Parsing methods
        /// <summary>
        /// Parse a list of patch instructions from an Xml file into patch objects
        /// </summary>
        /// <param name="patches">The list of patches to parse into</param>
        /// <param name="filename">The name of the file to parse from</param>
        /// <param name="originalNameFromZip">The original name when extracted from the zip file during install time</param>
        public static void AddPatchesFromFile(List<Patch> patches, string filename, string originalNameFromZip = null)
        {
            //make an Xml document to get all patches
            XmlDocument doc = LoadXmlDocument(filename, XmlLoadType.FromFile);
            if (doc == null)
                return;
            //make new patch object for each entry
            //remember to add lots of logging
            XmlNodeList XMLpatches = GetXmlNodesFromXPath(doc, "//patchs/patch");
            if (XMLpatches == null || XMLpatches.Count == 0)
            {
                Logging.Error("File {0} contains no patch entries", filename);
                return;
            }
            Logging.Info("Adding {0} patches from patchFile: {1}", XMLpatches.Count, filename);
            foreach (XmlNode patchNode in XMLpatches)
            {
                Patch p = new Patch
                {
                    NativeProcessingFile = Path.GetFileName(filename),
                    ActualPatchName = originalNameFromZip
                };
                Logging.Debug("Adding patch from file: {0} -> original name: {1}", Path.GetFileName(filename), originalNameFromZip);
                
                //we have the patchNode "patch" object, now we need to get it's children to actually get the properties of said patch
                foreach (XmlNode property in patchNode.ChildNodes)
                {
                    //each element in the Xml gets put into the
                    //the corresponding attribute for the Patch instance
                    switch (property.Name)
                    {
                        case "type":
                            p.Type = property.InnerText.Trim();
                            break;
                        case "mode":
                            p.Mode = property.InnerText.Trim();
                            break;
                        case "patchPath":
                            p.PatchPath = property.InnerText.Trim();
                            break;
                        case "followPath":
                            p.FollowPath = CommonUtils.ParseBool(property.InnerText.Trim(), false);
                            break;
                        case "file":
                            p.File = property.InnerText.Trim();
                            break;
                        case "path":
                            p.Path = property.InnerText.Trim();
                            break;
                        case "version":
                            p.Version = CommonUtils.ParseInt(property.InnerText.Trim(), 1);
                            break;
                        case "line":
                            if (!string.IsNullOrWhiteSpace(property.InnerText.Trim()))
                                p.Lines = property.InnerText.Trim().Split(',');
                            break;
                        case "search":
                            p.Search = property.InnerText.Trim();
                            break;
                        case "replace":
                            p.Replace = property.InnerText.Trim();
                            break;
                    }
                }
                patches.Add(p);
            }
        }

        /// <summary>
        /// Parse a list of shortcut instructions from an Xml file into shortcut objects
        /// </summary>
        /// <param name="shortcuts">The list of shortcuts to parse into</param>
        /// <param name="filename">The name of the file to parse from</param>
        public static void AddShortcutsFromFile(List<Shortcut> shortcuts, string filename)
        {
            //make an Xml document to get all shortcuts
            XmlDocument doc = LoadXmlDocument(filename, XmlLoadType.FromFile);
            if (doc == null)
            {
                Logging.Error("Failed to parse Xml shortcut file, skipping");
                return;
            }
            //make new patch object for each entry
            //remember to add lots of logging
            XmlNodeList XMLshortcuts = GetXmlNodesFromXPath(doc, "//shortcuts/shortcut");
            if (XMLshortcuts == null || XMLshortcuts.Count == 0)
            {
                Logging.Warning("File {0} contains no shortcut entries", filename);
                return;
            }
            Logging.Info("Adding {0} shortcuts from shortcutFile: {1}", XMLshortcuts.Count, filename);
            foreach (XmlNode patchNode in XMLshortcuts)
            {
                Shortcut sc = new Shortcut();
                //we have the patchNode "patch" object, now we need to get it's children to actually get the properties of said patch
                foreach (XmlNode property in patchNode.ChildNodes)
                {
                    //each element in the Xml gets put into the
                    //the corresponding attribute for the Patch instance
                    switch (property.Name)
                    {
                        case "path":
                            sc.Path = property.InnerText.Trim();
                            break;
                        case "name":
                            sc.Name = property.InnerText.Trim();
                            break;
                        case "enabled":
                            sc.Enabled = CommonUtils.ParseBool(property.InnerText.Trim(), false);
                            break;
                    }
                }
                shortcuts.Add(sc);
            }
        }

        /// <summary>
        /// Parse a list of Xml unpack instructions from an Xml file into XmlUnpack objects
        /// </summary>
        /// <param name="xmlUnpacks">The list of XmlUnpacks to parse into</param>
        /// <param name="filename">The name of the file to parse from</param>
        public static void AddXmlUnpackFromFile(List<XmlUnpack> xmlUnpacks, string filename)
        {
            //make an Xml document to get all Xml Unpacks
            XmlDocument doc = LoadXmlDocument(filename, XmlLoadType.FromFile);
            if (doc == null)
            {
                Logging.Error("failed to parse Xml file");
                return;
            }
            //make new patch object for each entry
            //remember to add lots of logging
            XmlNodeList XMLUnpacks = GetXmlNodesFromXPath(doc, "//files/file");
            if (XMLUnpacks == null || XMLUnpacks.Count == 0)
            {
                Logging.Error("File {0} contains no XmlUnapck entries", filename);
                return;
            }
            Logging.Info("Adding {0} Xml unpack entries from file: {1}", XMLUnpacks.Count, filename);
            foreach (XmlNode patchNode in XMLUnpacks)
            {
                XmlUnpack xmlup = new XmlUnpack();
                //we have the patchNode "patch" object, now we need to get it's children to actually get the properties of said patch
                foreach (XmlNode property in patchNode.ChildNodes)
                {
                    //each element in the Xml gets put into the
                    //the corresponding attribute for the Patch instance
                    switch (property.Name)
                    {
                        case "pkg":
                            xmlup.Pkg = property.InnerText.Trim();
                            break;
                        case "directoryInArchive":
                            xmlup.DirectoryInArchive = property.InnerText.Trim();
                            break;
                        case "fileName":
                            xmlup.FileName = property.InnerText.Trim();
                            break;
                        case "extractDirectory":
                            xmlup.ExtractDirectory = property.InnerText.Trim();
                            break;
                        case "newFileName":
                            xmlup.NewFileName = property.InnerText.Trim();
                            break;
                    }
                }
                xmlUnpacks.Add(xmlup);
            }
        }

        /// <summary>
        /// Parse a list of Xml atlas creation instructions from an Xml file into Atlas objects
        /// </summary>
        /// <param name="atlases">The list of Atlases to parse into</param>
        /// <param name="filename">The name of the file to parse from</param>
        public static void AddAtlasFromFile(List<Atlas> atlases, string filename)
        {
            //make an Xml document to get all Xml Unpacks
            XmlDocument doc = LoadXmlDocument(filename, XmlLoadType.FromFile);
            if (doc == null)
                return;
            //make new patch object for each entry
            //remember to add lots of logging
            XmlNodeList XMLAtlases = GetXmlNodesFromXPath(doc, "//atlases/atlas");
            if (XMLAtlases == null || XMLAtlases.Count == 0)
            {
                Logging.Error("File {0} contains no atlas entries", filename);
                return;
            }
            Logging.Info("Adding {0} atlas entries from file: {1}", XMLAtlases.Count, filename);
            foreach (XmlNode atlasNode in XMLAtlases)
            {
                Atlas sc = new Atlas();
                //we have the patchNode "patch" object, now we need to get it's children to actually get the properties of said patch
                foreach (XmlNode property in atlasNode.ChildNodes)
                {
                    //each element in the Xml gets put into the
                    //the corresponding attribute for the Patch instance
                    switch (property.Name)
                    {
                        case "pkg":
                            sc.Pkg = property.InnerText.Trim();
                            break;
                        case "directoryInArchive":
                            sc.DirectoryInArchive = property.InnerText.Trim();
                            break;
                        case "atlasFile":
                            sc.AtlasFile = property.InnerText.Trim();
                            break;
                        case "mapFile":
                            sc.MapFile = property.InnerText.Trim();
                            break;
                        case "powOf2":
                            sc.PowOf2 = CommonUtils.ParseBool(property.InnerText.Trim(), false);
                            break;
                        case "square":
                            sc.Square = CommonUtils.ParseBool(property.InnerText.Trim(), false);
                            break;
                        case "fastImagePacker":
                            sc.FastImagePacker = CommonUtils.ParseBool(property.InnerText.Trim(), false);
                            break;
                        case "padding":
                            sc.Padding = CommonUtils.ParseInt(property.InnerText.Trim(), 1);
                            break;
                        case "atlasWidth":
                            sc.AtlasWidth = CommonUtils.ParseInt(property.InnerText.Trim(), 2400);
                            break;
                        case "atlasHeight":
                            sc.AtlasHeight = CommonUtils.ParseInt(property.InnerText.Trim(), 8192);
                            break;
                        case "atlasSaveDirectory":
                            sc.AtlasSaveDirectory = property.InnerText.Trim();
                            break;
                        case "imageFolders":
                            foreach (XmlNode imageFolder in property.ChildNodes)
                            {
                                sc.ImageFolderList.Add(imageFolder.InnerText.Trim());
                            }
                            break;
                    }
                }
                atlases.Add(sc);
            }
        }

        public static XmlDocument SavePatchToXmlDocument(List<Patch> PatchesList)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement patchHolder = doc.CreateElement("patchs");
            doc.AppendChild(patchHolder);
            int counter = 0;
            foreach (Patch patch in PatchesList)
            {
                Logging.Patcher("[SavePatchToXmlDocument]: Saving patch {0} of {1}: {2}", LogLevel.Info, ++counter, PatchesList.Count, patch.ToString());
                Logging.Patcher("{0}", LogLevel.Info, patch.DumpPatchInfoForLog);
                XmlElement xmlPatch = doc.CreateElement("patch");
                patchHolder.AppendChild(xmlPatch);

                XmlElement version = doc.CreateElement("version");
                version.InnerText = patch.Version.ToString();
                xmlPatch.AppendChild(version);

                XmlElement type = doc.CreateElement("type");
                type.InnerText = patch.Type;
                xmlPatch.AppendChild(type);

                XmlElement mode = doc.CreateElement("mode");
                mode.InnerText = patch.Mode;
                xmlPatch.AppendChild(mode);

                XmlElement patchPath = doc.CreateElement("patchPath");
                patchPath.InnerText = patch.PatchPath;
                xmlPatch.AppendChild(patchPath);

                XmlElement followPath = doc.CreateElement("followPath");
                followPath.InnerText = patch.Version == 1 ? false.ToString() : patch.FollowPath.ToString();
                xmlPatch.AppendChild(followPath);

                XmlElement file = doc.CreateElement("file");
                file.InnerText = patch.File;
                xmlPatch.AppendChild(file);

                if (patch.Type.Equals("regex"))
                {
                    XmlElement line = doc.CreateElement("line");
                    line.InnerText = string.Join(",", patch.Lines);
                    xmlPatch.AppendChild(line);
                }
                else
                {
                    XmlElement line = doc.CreateElement("path");
                    line.InnerText = patch.Path;
                    xmlPatch.AppendChild(line);
                }

                XmlElement search = doc.CreateElement("search");
                search.InnerText = patch.Search;
                xmlPatch.AppendChild(search);

                XmlElement replace = doc.CreateElement("replace");
                replace.InnerText = MacroUtils.MacroReplace(patch.Replace, ReplacementTypes.TextEscape);
                xmlPatch.AppendChild(replace);
            }
            return doc;
        }
        #endregion
    }
}
 