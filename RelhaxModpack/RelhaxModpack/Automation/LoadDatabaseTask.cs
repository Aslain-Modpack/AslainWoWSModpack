﻿using RelhaxModpack.Database;
using RelhaxModpack.Utilities.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelhaxModpack.Automation
{
    public class LoadDatabaseTask : DatabaseTask, IDatabaseTask, IXmlSerializable
    {
        public const string TaskCommandName = "load_database";

        public override string Command { get { return TaskCommandName; } }

        #region Xml serialization
        public override string[] PropertiesForSerializationAttributes()
        {
            return base.PropertiesForSerializationAttributes();
        }
        #endregion

        #region Task execution
        public override void ProcessMacros()
        {
            base.ProcessMacros();
        }

        public override void ValidateCommands()
        {
            base.ValidateCommands();

            //CustomDatabasePath *could* be empty, if the user wants to use the one set in his AutomationSettings path
            if (!string.IsNullOrWhiteSpace(CustomDatabasePath))
            {
                string directoryPath = Path.GetDirectoryName(CustomDatabasePath);
                //the directory path could be null if the user wants to load/save the database right here and is using a relative path
                if (ValidateCommand(!string.IsNullOrWhiteSpace(directoryPath) && !Directory.Exists(directoryPath), string.Format("ExitCode {0}: The path to load the database does not exist: {1}", ExitCode, directoryPath)))
                    return;
            }
        }

        public override async Task RunTask()
        {
            await DatabaseManager.LoadDatabaseTestAsync(string.IsNullOrEmpty(CustomDatabasePath) ? AutomationSettings.Filename : CustomDatabasePath);
        }

        public override void ProcessTaskResults()
        {
            base.ProcessTaskResults();
        }
        #endregion
    }
}
