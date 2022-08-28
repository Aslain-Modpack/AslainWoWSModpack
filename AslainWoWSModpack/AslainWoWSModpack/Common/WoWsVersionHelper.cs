using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using AslainWoWSModpack.Xml;

namespace AslainWoWSModpack.Common
{
    public class WoWsVersionHelper : ClientVersionHelper
    {
        public const string WoWsGameInfoXmlXpath = "/protocol/game/part_versions/version";

        public override string ClientVersion
        {
            get
            {
                if (string.IsNullOrEmpty(ClientVersionFull))
                    return string.Empty;
                StringBuilder sb = new StringBuilder();
                string[] versionArray = ClientVersionFull.Split('.');
                sb.Append(versionArray[0]);
                sb.Append(".");
                sb.Append(versionArray[1]);
                sb.Append(".");
                sb.Append(versionArray[2]);
                sb.Append(".");
                sb.Append(versionArray[3]);
                return sb.ToString();
            }
        }

        protected override bool ParseSelectedClientInfo(string providedClientExePath)
        {
            //we're first given an exe path, so first make the path to the game info xml
            string gameInfoXmlPath = Path.GetDirectoryName(providedClientExePath);
            gameInfoXmlPath = Path.Combine(gameInfoXmlPath, ApplicationConstants.WoWsGameInfoXml);
            if (!File.Exists(gameInfoXmlPath))
            {
                Logging.Error($"Game info xml does not exist at path {gameInfoXmlPath}");
                return false;
            }

            //get the version nodes with attributes "client" and "sdcontent"
            string clientVersionXpath = WoWsGameInfoXmlXpath + "[@name='client']/@installed";
            string sdContentXpath = WoWsGameInfoXmlXpath + "[@name='sdcontent']/@installed";

            //filter to just get the "installed" attribute
            string clientVersion = XmlUtils.GetXmlStringFromXPath(gameInfoXmlPath, clientVersionXpath);
            string sdContentVersion = XmlUtils.GetXmlStringFromXPath(gameInfoXmlPath, sdContentXpath);

            //parse to int and determine which is newer
            string clientSubVersion = clientVersion.Split('.')[4];
            string sdContentSubVersion = sdContentVersion.Split('.')[4];
            int clientSubVersionInt = int.Parse(clientSubVersion);
            int sdContentSubVersionInt = int.Parse(sdContentSubVersion);
            int maxSubVersion = clientSubVersionInt >= sdContentSubVersionInt? clientSubVersionInt: sdContentSubVersionInt;
            clientVersionFull = clientSubVersionInt >= sdContentSubVersionInt ? clientVersion : sdContentVersion;
            ParsedClientInstallDirectory = Path.Combine(Path.GetDirectoryName(providedClientExePath), "bin", maxSubVersion.ToString());

            //ensure the bin folder has a folder of the same name. that's the folder to use for installing mods
            if (!Directory.Exists(ParsedClientInstallDirectory))
            {
                Logging.Error($"Parsed client install directory {ParsedClientInstallDirectory} does not exist!");
                ParsedClientInstallDirectory = null;
                return false;
            }
            return true;
        }

        public override List<string> AutoDetectClients()
        {
            throw new NotImplementedException();
        }
    }
}
