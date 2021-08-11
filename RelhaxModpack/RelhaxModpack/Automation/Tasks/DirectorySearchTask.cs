﻿using RelhaxModpack.Database;
using RelhaxModpack.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelhaxModpack.Automation.Tasks
{
    public abstract class DirectorySearchTask : DirectoryTask, IXmlSerializable
    {
        public const string SEARCH_ALL = "*";

        public string SearchPattern { get; set; } = SEARCH_ALL;

        public string Recursive { get; set; }

        protected bool recursive;

        protected string[] searchResults;

        protected bool ableToParseRecursive = false;

        #region Xml Serialization
        public override string[] PropertiesForSerializationAttributes()
        {
            return base.PropertiesForSerializationAttributes().Concat(new string[] { nameof(SearchPattern), nameof(Recursive)}).ToArray();
        }
        #endregion

        #region Task Execution
        public override void ProcessMacros()
        {
            base.ProcessMacros();
            SearchPattern = ProcessMacro(nameof(SearchPattern), SearchPattern);
            Recursive = ProcessMacro(nameof(Recursive), Recursive);

            if (bool.TryParse(Recursive, out bool result))
            {
                ableToParseRecursive = true;
                recursive = result;
            }
        }

        public override void ValidateCommands()
        {
            base.ValidateCommands();
            if (ValidateCommandStringNullEmptyTrue(nameof(SearchPattern), SearchPattern))
                return;
            if (ValidateCommandStringNullEmptyTrue(nameof(Recursive), Recursive))
                return;

            if (ValidateCommandFalse(ableToParseRecursive, string.Format("Unable to parse the arg Recursive from given string {0}", Recursive)))
                return;
        }

        public async override Task RunTask()
        {
            RunSearch();
        }

        protected virtual void RunSearch()
        {
            searchResults = FileUtils.FileSearch(DirectoryPath, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly, false, false, SearchPattern);
        }

        public override void ProcessTaskResults()
        {
            if (ProcessTaskResultTrue(searchResults == null, "The searchResult array returned null"))
                return;

            if (ProcessTaskResultTrue(searchResults.Count() == 0, "The searchResult array returned 0 results"))
                return;
        }
        #endregion
    }
}
