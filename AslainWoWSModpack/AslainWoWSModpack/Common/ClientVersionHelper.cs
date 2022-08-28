using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using AslainWoWSModpack.Utilities.Enums;

namespace AslainWoWSModpack.Common
{
    public abstract class ClientVersionHelper
    {
        protected string clientVersionFull;

        public List<string> ClientPaths { get; protected set; }

        public bool ClientInfoParsed { get; protected set; } = false;

        public string ParsedClientInstallDirectory { get; protected set; }

        public bool ParseClientInfo(string providedClientExePath)
        {
            if (string.IsNullOrEmpty(providedClientExePath) || !File.Exists(providedClientExePath))
                throw new BadMemeException(string.Format("The provided client path does not exist: {0}", providedClientExePath));

            ClientInfoParsed = ParseSelectedClientInfo(providedClientExePath);
            return ClientInfoParsed;
        }

        public string ClientVersionFull
        {
            get
            {
                return ClientInfoParsed ? clientVersionFull : string.Empty;
            }
        }

        public void FindClients(string providedClientExePath = null)
        {
            if (!string.IsNullOrEmpty(providedClientExePath) && !File.Exists(providedClientExePath))
                throw new BadMemeException(string.Format("The provided client path does not exist: {0}", providedClientExePath));

            if (string.IsNullOrEmpty(providedClientExePath))
            {
                //auto detect / try to find clients
                ClientPaths = AutoDetectClients();
            }
            else
            {
                ClientPaths = new List<string>
                {
                    providedClientExePath
                };
            }
        }

        public abstract string ClientVersion { get; }

        protected abstract bool ParseSelectedClientInfo(string providedClientExePath);

        public abstract List<string> AutoDetectClients();
    }
}
