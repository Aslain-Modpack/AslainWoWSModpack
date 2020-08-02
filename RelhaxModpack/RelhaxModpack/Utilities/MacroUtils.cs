﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelhaxModpack.Utilities
{
    /// <summary>
    /// The types of text macro replacements
    /// </summary>
    public enum ReplacementTypes
    {
        /// <summary>
        /// Replacing macros with file paths
        /// </summary>
        FilePath,

        /// <summary>
        /// Replacing patch arguments of the patch object
        /// </summary>
        PatchArguementsReplace,

        /// <summary>
        /// Replacing modpack created macros (like [quote]) with the corresponding characters
        /// </summary>
        PatchFiles,

        /// <summary>
        /// Replacing literal interpretations of special characters like newline and tab with escaped versions
        /// </summary>
        TextEscape,

        /// <summary>
        /// Replacing escaped versions of special characters like newline and tab with the literal interpretations
        /// </summary>
        TextUnescape,

        /// <summary>
        /// Replacing zip path macros with absolute extraction paths
        /// </summary>
        ZipFilePath
    }

    /// <summary>
    /// A utility class to handle macros
    /// </summary>
    public static class MacroUtils
    {
        //https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/how-to-initialize-a-dictionary-with-a-collection-initializer
        //build at install time
        /// <summary>
        /// The dictionary to store filepath macros
        /// </summary>
        public static Dictionary<string, string> FilePathDict = new Dictionary<string, string>();

        /// <summary>
        /// The dictionary to store patch argument (replace) macros
        /// </summary>
        public static Dictionary<string, string> PatchArguementsReplaceDict = new Dictionary<string, string>()
        {
            {@"[sl]", @"/" }
        };

        /// <summary>
        /// The dictionary to store patch file replacement macros
        /// </summary>
        public static Dictionary<string, string> PatchFilesDict = new Dictionary<string, string>()
        {
            //add all patch file escape characters
            //key (look for), value (replaced with)
            {@"""[xvm_dollar]", @"$" },
            {@"[xvm_rbracket]""", @"}" },
            {@"[lbracket]", @"{" },
            {@"[rbracket]", @"}" },
            {@"[quote]", "\"" },
            {@"[colon]", @":" },
            {@"[dollar]", @"$" },
        };

        /// <summary>
        /// The dictionary to store escaped text characters with the literal versions
        /// </summary>
        public static Dictionary<string, string> TextUnscapeDict = new Dictionary<string, string>()
        {
            //ORDER MATTERS
            {@"\n", "\n" },
            {@"\r", "\r" },
            {@"\t", "\t" },
            //legacy compatibility (i can't believe i did this....)
            {@"newline", "\n" }
        };

        /// <summary>
        /// The dictionary to store literal versions of characters with their escaped versions
        /// </summary>
        public static Dictionary<string, string> TextEscapeDict = new Dictionary<string, string>()
        {
            //ORDER MATTERS
            {"\n", @"\n" },
            {"\r", @"\r" },
            {"\t", @"\t" }
        };

        /// <summary>
        /// Builds the Filepath macro dictionary with settings that should be parsed from the Settings class
        /// </summary>
        public static void BuildFilepathMacroList()
        {
            if (FilePathDict == null)
                throw new BadMemeException("REEEEEEEEEE");
            FilePathDict.Clear();
            //add macro versions first then regular versions
            FilePathDict.Add(@"{versiondir}", Settings.WoTClientVersion);
            FilePathDict.Add(@"{tanksversion}", Settings.WoTClientVersion);
            FilePathDict.Add(@"{tanksonlinefolderversion}", Settings.WoTModpackOnlineFolderVersion);
            FilePathDict.Add(@"{appdata}", Settings.AppDataFolder);
            FilePathDict.Add(@"{appData}", Settings.AppDataFolder);
            FilePathDict.Add(@"{app}", Settings.WoTDirectory);
            FilePathDict.Add(@"versiondir", Settings.WoTClientVersion);
        }

        /// <summary>
        /// Performs a replacement of macros using the specified macro replace operation
        /// </summary>
        /// <param name="inputString">The string to replace the macros of</param>
        /// <param name="type">The type of macro replace operation</param>
        /// <returns>The replaced string</returns>
        public static string MacroReplace(string inputString, ReplacementTypes type)
        {
            //itterate through each entry depending on the dictionary. if the key is contained in the string, replace it
            //use a switch to get which dictionary reaplce we will use
            Dictionary<string, string> dictionary = null;
            switch (type)
            {
                case ReplacementTypes.FilePath:
                    dictionary = FilePathDict;
                    break;
                case ReplacementTypes.PatchArguementsReplace:
                    dictionary = PatchArguementsReplaceDict;
                    break;
                case ReplacementTypes.PatchFiles:
                    dictionary = PatchFilesDict;
                    break;
                case ReplacementTypes.TextEscape:
                    dictionary = TextEscapeDict;
                    break;
                case ReplacementTypes.TextUnescape:
                    dictionary = TextUnscapeDict;
                    break;
            }
            if (dictionary == null)
            {
                Logging.Error("macro replace dictionary is null! type={0}", type.ToString());
                return inputString;
            }
            for (int i = 0; i < dictionary.Count; i++)
            {
                string key = dictionary.ElementAt(i).Key;
                string replace = dictionary.ElementAt(i).Value;
                //https://stackoverflow.com/questions/444798/case-insensitive-containsstring
                //it's an option, not actually used here cause it would be a lot of work to implement
                //could also try regex, may be easlier to ignore case, but then might have to make it an option
                //so for now, no
                if (inputString.Contains(key))
                    inputString = inputString.Replace(key, replace);
            }
            return inputString;
        }
    }
}
