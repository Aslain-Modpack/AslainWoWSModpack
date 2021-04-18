﻿using Microsoft.Win32;
using RelhaxModpack.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RelhaxModpack.Utilities
{
    /// <summary>
    /// A structure to help with searching for inside the registry by providing a base area to start, and a string search path
    /// </summary>
    public struct RegistrySearch
    {
        /// <summary>
        /// Where to base the search in the registry (current_user, local_machiene, etc.)
        /// </summary>
        public RegistryKey Root;

        /// <summary>
        /// The absolute folder path to the desired registry entries
        /// </summary>
        public string Searchpath;
    }

    /// <summary>
    /// A utility class to handle registry requests
    /// </summary>
    public static class RegistryUtils
    {
        /// <summary>
        /// Checks the registry to get the latest location of where WoT is installed, includes exe in the name
        /// </summary>
        /// <returns>True if operation success</returns>
        public static string AutoFindWoTDirectoryFirst()
        {
            List<string> searchPathWoT = AutoFindWoTDirectoryList();

            if (searchPathWoT == null || searchPathWoT.Count == 0)
            {
                Logging.Warning("No valid paths found");
                return null;
            }

            //return first result found
            Logging.Info("Returning first result in search: {0}", searchPathWoT[0]);
            return searchPathWoT[0];
        }

        /// <summary>
        /// Checks the registry to get the latest location of where WoT is installed, includes exe in the name
        /// </summary>
        /// <returns>A list of all unique valid game paths</returns>
        public static List<string> AutoFindWoTDirectoryList()
        {
            RegistryKey result;
            List<string> searchPathWoT = new List<string>();

            //check replay link locations (the last game instance the user opened)
            //key is null, value is path
            //example value: "C:\TANKS\World_of_Tanks_NA\win64\WorldOfTanks.exe" "%1"
            RegistrySearch[] registryEntriesGroup1 = new RegistrySearch[]
            {
                new RegistrySearch(){Root = Registry.LocalMachine, Searchpath = @"SOFTWARE\Classes\.wotreplay\shell\open\command"},
                new RegistrySearch(){Root = Registry.CurrentUser, Searchpath = @"Software\Classes\.wotreplay\shell\open\command"}
            };

            foreach (RegistrySearch searchPath in registryEntriesGroup1)
            {
                Logging.Debug("Searching in registry root {0} with path {1}", searchPath.Root.Name, searchPath.Searchpath);
                result = GetRegistryKeys(searchPath);
                if (result != null)
                {
                    foreach (string valueInKey in result.GetValueNames())
                    {
                        string possiblePath = result.GetValue(valueInKey) as string;
                        if (!string.IsNullOrWhiteSpace(possiblePath) && possiblePath.ToLower().Contains("worldoftanks.exe"))
                        {
                            string[] splitResult = possiblePath.Split('"');
                            //get index 1 option
                            possiblePath = splitResult[1].Trim();
                            Logging.Debug("Possible path found: {0}", possiblePath);
                            searchPathWoT.Add(possiblePath);
                        }
                    }
                    result.Dispose();
                    result = null;
                }
            }

            //key is WoT path, don't care about value
            RegistrySearch[] registryEntriesGroup2 = new RegistrySearch[]
            {
                new RegistrySearch(){Root = Registry.CurrentUser, Searchpath = @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache"},
                new RegistrySearch(){Root = Registry.CurrentUser, Searchpath = @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store"}
            };

            foreach (RegistrySearch searchPath in registryEntriesGroup2)
            {
                Logging.Debug("Searching in registry root {0} with path {1}", searchPath.Root.Name, searchPath.Searchpath);
                result = GetRegistryKeys(searchPath);
                if (result != null)
                {
                    foreach (string possiblePath in result.GetValueNames())
                    {
                        if (!string.IsNullOrWhiteSpace(possiblePath) && possiblePath.ToLower().Contains("worldoftanks.exe"))
                        {
                            Logging.Debug("Possible path found: {0}", possiblePath);
                            searchPathWoT.Add(possiblePath);
                        }
                    }
                    result.Dispose();
                    result = null;
                }
            }

            Logging.Debug("Filter out win32/64 options");
            for (int i = 0; i < searchPathWoT.Count; i++)
            {
                string potentialResult = searchPathWoT[i];
                //if it has win32 or win64, filter it out
                if (potentialResult.Contains(ApplicationConstants.WoT32bitFolderWithSlash) || potentialResult.Contains(ApplicationConstants.WoT64bitFolderWithSlash))
                {
                    potentialResult = potentialResult.Replace(ApplicationConstants.WoT32bitFolderWithSlash, string.Empty).Replace(ApplicationConstants.WoT64bitFolderWithSlash, string.Empty);
                }
                searchPathWoT[i] = potentialResult;
            }

            Logging.Debug("Filter out options to non existent locations");
            searchPathWoT.RemoveAll(match => !File.Exists(match));

            Logging.Debug("Filter out duplicates");
            searchPathWoT = searchPathWoT.Distinct().ToList();

            foreach (string path in searchPathWoT)
            {
                Logging.Info("Valid path found: {0}", path);
            }

            return searchPathWoT;
        }

        /// <summary>
        /// Gets all registry keys that exist in the given search base and path
        /// </summary>
        /// <param name="search">The RegistrySearch structure to specify where to search and where to base the search</param>
        /// <returns>The RegistryKey object of the folder in registry, or null if the search failed</returns>
        public static RegistryKey GetRegistryKeys(RegistrySearch search)
        {
            try
            {
                return search.Root.OpenSubKey(search.Searchpath);
            }
            catch (Exception ex)
            {
                Logging.Exception("Failed to get registry entry");
                Logging.Exception(ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Finds the location of the Wargaming Game center installation directory from the registry
        /// </summary>
        /// <returns>The location of wgc.exe if found, else null</returns>
        public static string AutoFindWgcDirectory()
        {
            string wgcRegistryKeyLoc = @"Software\Classes\wgc\shell\open\command";
            Logging.Debug("Searching registry ({0}) for wgc location", wgcRegistryKeyLoc);
            //search for the location of the game center from the registry
            RegistryKey wgcKey = GetRegistryKeys(new RegistrySearch() { Root = Registry.CurrentUser, Searchpath = wgcRegistryKeyLoc });
            string actualLocation = null;
            if (wgcKey != null)
            {
                Logging.Debug("Not null key, checking results");
                foreach (string valueInKey in wgcKey.GetValueNames())
                {
                    string wgcPath = wgcKey.GetValue(valueInKey) as string;
                    Logging.Debug("Parsing result name '{0}' with value '{1}'", valueInKey, wgcPath);
                    if (!string.IsNullOrWhiteSpace(wgcPath) && wgcPath.ToLower().Contains("wgc.exe"))
                    {
                        //trim front
                        wgcPath = wgcPath.Substring(1);
                        //trim end
                        wgcPath = wgcPath.Substring(0, wgcPath.Length - 6);
                        Logging.Debug("Parsed to new value of '{0}', checking if file exists");
                        if (File.Exists(wgcPath))
                        {
                            Logging.Debug("Exists, use this for wgc start");
                            actualLocation = wgcPath;
                            break;
                        }
                        else
                        {
                            Logging.Debug("Not exist, continue to search");
                        }
                    }
                }
            }
            return actualLocation;
        }
    }
}
