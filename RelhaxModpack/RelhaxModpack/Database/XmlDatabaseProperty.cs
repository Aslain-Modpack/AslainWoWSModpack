﻿using RelhaxModpack.Utilities.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelhaxModpack.Database
{
    public class XmlDatabaseProperty
    {
        public string XmlName { get; set; }

        public XmlEntryType XmlEntryType { get; set; }

        public string PropertyName { get; set; }

        public override string ToString()
        {
            return string.Format("{0} = {1}, {2} = {3}, {4} = {5}", nameof(XmlName), XmlName, nameof(XmlEntryType), XmlEntryType.ToString(), nameof(PropertyName), PropertyName);
        }
    }
}