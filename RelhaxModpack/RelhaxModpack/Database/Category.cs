﻿using System.Collections.Generic;
using RelhaxModpack.UI;
using RelhaxModpack.Database;
using System.Windows.Controls;
using System;
using System.Linq;

namespace RelhaxModpack.Database
{
    /// <summary>
    /// a category is what makes up each tab in the mod selection display window. It holds the first level of list of SelectablePackages
    /// </summary>
    public class Category : IDatabaseComponent, IComponentWithDependencies, IXmlSerializable
    {
        #region Xml serialization
        /// <summary>
        /// Defines a list of properties in the class to be serialized into xml attributes
        /// </summary>
        /// <returns>A list of string property names</returns>
        /// <remarks>Xml attributes will always be written, xml elements are optional</remarks>
        public string[] PropertiesForSerializationAttributes()
        {
            return new string[] { nameof(Name), nameof(OffsetInstallGroups) };
        }

        /// <summary>
        /// Defines a list of properties in the class to be serialized into xml elements
        /// </summary>
        /// <returns>A list of string property names</returns>
        /// <remarks>Xml attributes will always be written, xml elements are optional</remarks>
        public string[] PropertiesForSerializationElements()
        {
            return new string[] { nameof(Dependencies), nameof(Maintainers) };
        }
        #endregion

        #region Database Properties
        /// <summary>
        /// The category name displayed to the user in the selection list
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The xml filename of this category. Used in database structure V2
        /// </summary>
        public string XmlFilename { get; set; } = string.Empty;

        /// <summary>
        /// Get or set if the installGroup property of all packages in this category will be offset by each package's level in the package tree
        /// </summary>
        public bool OffsetInstallGroups { get; set; } = true;

        /// <summary>
        /// A list of database managers who are known to maintain this component
        /// </summary>
        public string Maintainers { get; set; } = string.Empty;

        /// <summary>
        /// Returns a list database managers who are known to maintain this component
        /// </summary>
        public List<string> MaintainersList
        {
            get { return Maintainers.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList(); }
        }

        /// <summary>
        /// The list of packages contained in this category
        /// </summary>
        public List<SelectablePackage> Packages { get; set; } = new List<SelectablePackage>();

        /// <summary>
        /// When a databasePackage, the internal packageName. When category, the category name
        /// </summary>
        public string ComponentInternalName { get { return Name; } }

        /// <summary>
        /// List of dependencies of this category (Any package selected in this category needs these dependencies)
        /// </summary>
        public List<DatabaseLogic> Dependencies { get; set; } = new List<DatabaseLogic>();

        //https://stackoverflow.com/questions/1759352/how-to-mark-a-method-as-obsolete-or-deprecated
        /// <summary>
        /// The install group number of the category. Used to denote which install thread it is assigned to.
        /// Two (or more) categories can have the same number to be on the same install thread
        /// </summary>
        [Obsolete("This is for legacy database compatibility and will be ignored in Relhax V2")]
        public int InstallGroup = 0;
        #endregion

        #region UI Properties
        /// <summary>
        /// The TabItem object reference
        /// </summary>
        public TabItem TabPage { get; set; } = null;

        /// <summary>
        /// The package created at selection list building that represents the header of this category
        /// </summary>
        public SelectablePackage CategoryHeader { get; set; } = null;

        /// <summary>
        /// Reference for the UI element of this package in the database editor
        /// </summary>
        public TreeViewItem EditorTreeViewItem { get; set; } = null;
        #endregion

        #region Other Properties and Methods
        /// <summary>
        /// Sorts the Categories by their name property. Currently not implemented.
        /// </summary>
        /// <param name="x">The first Category to compare</param>
        /// <param name="y">The second Category to compare</param>
        /// <returns>1 if y is later in the alphabet, 0 if equal, -1 else</returns>
        public static int CompareCatagories(Category x, Category y)
        {
            return x.Name.CompareTo(y.Name);
        }

        /// <summary>
        /// Output the object to a string representation
        /// </summary>
        /// <returns>The name of the category</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Returns a single depth (flat) list of packages in the category. Leveling is preserved (a sub-package will be directly below the parent in the list)
        /// </summary>
        /// <returns>The list of packages</returns>
        public List<SelectablePackage> GetFlatPackageList()
        {
            List<SelectablePackage> flatPackageList = new List<SelectablePackage>();
            foreach(SelectablePackage selectablePackage in Packages)
            {
                flatPackageList.Add(selectablePackage);
                if (selectablePackage.Packages.Count > 0)
                    GetFlatPackageList(flatPackageList, selectablePackage.Packages);
            }
            return flatPackageList;
        }

        private void GetFlatPackageList(List<SelectablePackage> flatPackageList, List<SelectablePackage> selectablePackages)
        {
            foreach (SelectablePackage selectablePackage in selectablePackages)
            {
                flatPackageList.Add(selectablePackage);
                if (selectablePackage.Packages.Count > 0)
                    GetFlatPackageList(flatPackageList, selectablePackage.Packages);
            }
        }

        /// <summary>
        /// Check if any packages in this category are selected for install
        /// </summary>
        /// <returns>Try if any package is selected, false otherwise</returns>
        public bool AnyPackagesChecked()
        {
            foreach(SelectablePackage package in GetFlatPackageList())
            {
                if (package.Checked)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if at least one package is enabled and checked from the first level of packages in the category
        /// </summary>
        public bool IsAnyPackageCheckedEnabled()
        {
            bool anyPackages = false;
            foreach (SelectablePackage sp in this.Packages)
            {
                if (sp.Enabled && sp.Checked)
                    anyPackages = true;
            }
            return anyPackages;
        }

        /// <summary>
        /// Returns true if at least one package is enabled and checked and visible from the first level of packages in the category
        /// </summary>
        public bool IsAnyPackageCheckedEnabledVisible()
        {
            bool anyPackages = false;
            foreach (SelectablePackage sp in this.Packages)
            {
                if (sp.Enabled && sp.Checked && sp.Visible)
                    anyPackages = true;
            }
            return anyPackages;
        }
        #endregion
    }
}
