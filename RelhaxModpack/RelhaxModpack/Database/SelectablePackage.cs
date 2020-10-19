﻿using RelhaxModpack.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Text;
using RelhaxModpack.Utilities;
using RelhaxModpack.Utilities.Enums;

namespace RelhaxModpack.Database
{
    /// <summary>
    /// A package that can be selected in the UI, most commonly a mod or a configuration parameter for a mod
    /// </summary>
    public class SelectablePackage : DatabasePackage, IDatabaseComponent, IComponentWithDependencies, IXmlSerializable
    {
        #region Xml serialization
        /// <summary>
        /// Defines a list of properties in the class to be serialized into xml attributes
        /// </summary>
        /// <returns>A list of string property names</returns>
        /// <remarks>Xml attributes will always be written, xml elements are optional</remarks>
        public override string[] PropertiesForSerializationAttributes()
        {
            return base.PropertiesForSerializationAttributes().Concat(SelectablePackagePropertiesToXmlParseAttributes.ToArray()).ToArray();
        }

        /// <summary>
        /// Defines a list of properties in the class to be serialized into xml elements
        /// </summary>
        /// <returns>A list of string property names</returns>
        /// <remarks>Xml attributes will always be written, xml elements are optional</remarks>
        public override string[] PropertiesForSerializationElements()
        {
            return base.PropertiesForSerializationElements().Concat(SelectablePackagePropertiesToXmlParseElements.ToArray()).ToArray();
        }

        private static readonly List<string> SelectablePackagePropertiesToXmlParseAttributes = new List<string>()
        {
            nameof(Name),
            nameof(Type),
            nameof(Visible)
        };

        private static readonly List<string> SelectablePackagePropertiesToXmlParseElements = new List<string>()
        {
            nameof(Description),
            nameof(UpdateComment),
            nameof(PopularMod),
            nameof(GreyAreaMod),
            nameof(ObfuscatedMod),
            nameof(FromWGmods),
            nameof(ShowInSearchList),
            nameof(Medias),
            nameof(UserFiles),
            nameof(ConflictingPackages),
            nameof(Dependencies),
            nameof(Packages)
        };
        #endregion

        #region Selection file processing
        private static readonly List<string> SelectablePackagePropertiesToSaveForSelectionFile = new List<string>()
        {
            nameof(FlagForSelectionSave)
        };

        /// <summary>
        /// Defines a list of properties in the class to be serialized into xml attributes for selection files
        /// </summary>
        /// <returns>The base array, with SelectablePackage options concatenated</returns>
        public override string[] AttributesToXmlParseSelectionFiles()
        {
            return base.AttributesToXmlParseSelectionFiles().Concat(SelectablePackagePropertiesToSaveForSelectionFile.ToArray()).ToArray();
        }
        #endregion

        #region Database Properties
        /// <summary>
        /// Create an instance of the SelectablePackage class and over-ride DatabasePackage default values
        /// </summary>
        public SelectablePackage()
        {
            InstallGroup = 4;
            PatchGroup = 4;
        }

        /// <summary>
        /// The display name of the package
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The name of the package with the version macro replaced for use display
        /// </summary>
        public string NameFormatted
        {
            get
            {
                return Name
                    .Replace(@"{version}", Version)
                    .Replace(@"{author}", Author);
            }
        }

        /// <summary>
        /// The Category object reference
        /// </summary>
        public Category ParentCategory { get; set; } = null;

        /// <summary>
        /// The type of selectable package logic to follow (see SelectionTypes enumeration for options)
        /// </summary>
        public SelectionTypes Type { get; set; } = SelectionTypes.none;

        /// <summary>
        /// The reference for the direct parent of this package
        /// </summary>
        public SelectablePackage Parent { get; set; } = null;

        /// <summary>
        /// The reference for the absolute top of the package tree
        /// </summary>
        public SelectablePackage TopParent { get; set; } = null;

        /// <summary>
        /// A flag to determine whether or not the mod should be shown in UI
        /// </summary>
        public bool Visible { get; set; } = false;

        /// <summary>
        /// Update comments of the package
        /// </summary>
        public string UpdateComment { get; set; } = string.Empty;

        /// <summary>
        /// Gets an escaped version of the UpdateComment property, replacing literal '\n' with special character '\n', for example
        /// </summary>
        /// <seealso cref="UpdateComment"/>
        public string UpdateCommentEscaped
        {
            get { return MacroUtils.MacroReplace(UpdateComment, ReplacementTypes.TextUnescape); }
        }

        /// <summary>
        /// Gets a user display formatted version of the UpdateCommentEscaped property, with a time stamp (if available). If no comment, a translated 'noComment' entry is returned
        /// </summary>
        /// <seealso cref="UpdateCommentEscaped"/>
        /// <seealso cref="UpdateComment"/>
        public string UpdateCommentFormatted
        {
            get
            {
              return string.Format("{0}\n{1}", string.IsNullOrWhiteSpace(this.UpdateComment) ?
                Translations.GetTranslatedString("noUpdateInfo") : this.UpdateCommentEscaped,
                this.Timestamp == 0 ? Translations.GetTranslatedString("noTimestamp") : CommonUtils.ConvertFiletimeTimestampToDate(this.Timestamp));
            }
        }

        /// <summary>
        /// description of the package
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets an escaped version of the Description property, replacing literal '\n' with special character '\n', for example
        /// </summary>
        /// <seealso cref="Description"/>
        public string DescriptionEscaped
        {
            get { return MacroUtils.MacroReplace(Description, ReplacementTypes.TextUnescape); }
        }

        /// <summary>
        /// Gets a user display formatted version of the UpdateCommentEscaped property. Additionally could contain an encrypted, controversial or popular entry.
        /// If no description, a translated 'noDescription' entry is returned
        /// </summary>
        /// <seealso cref="DescriptionEscaped"/>
        /// <seealso cref="Description"/>
        public string DescriptionFormatted
        {
            get
            {
                StringBuilder descriptionBuilder = new StringBuilder();
                if (this.ObfuscatedMod)
                    descriptionBuilder.AppendFormat("-- {0} --\n", Translations.GetTranslatedString("encryptedInDescription"));

                if (this.GreyAreaMod)
                    descriptionBuilder.AppendFormat("-- {0} --\n", Translations.GetTranslatedString("controversialInDescription"));

                if (this.PopularMod)
                    descriptionBuilder.AppendFormat("-- {0} --\n", Translations.GetTranslatedString("popularInDescription"));

                if (this.FromWGmods)
                    descriptionBuilder.AppendFormat("-- {0} --\n", Translations.GetTranslatedString("fromWgmodsInDescription"));

                if(string.IsNullOrWhiteSpace(Description))
                    return string.Format("{0}\n{1}", descriptionBuilder.ToString(), Translations.GetTranslatedString("noDescription"));

                else
                    return string.IsNullOrWhiteSpace(descriptionBuilder.ToString()) ? DescriptionEscaped : string.Format("{0}\n{1}", descriptionBuilder.ToString(), DescriptionEscaped);
            }
        }

        /// <summary>
        /// Flag to determine if the package is popular
        /// </summary>
        public bool PopularMod { get; set; } = false;

        /// <summary>
        /// Flag to determine if the package is of controversial nature
        /// </summary>
        public bool GreyAreaMod { get; set; } = false;

        /// <summary>
        /// Flag to determine if the package is obfuscated/encrypted and can't be checked for viruses or malware
        /// </summary>
        public bool ObfuscatedMod { get; set; } = false;

        /// <summary>
        /// Flag to determine if the package is from the offical WoT mod portal
        /// </summary>
        public bool FromWGmods { get; set; } = false;

        /// <summary>
        /// Flag to determine any packages of this package should be sorted (by name)
        /// </summary>
        public bool SortChildPackages { get; set; } = false;

        /// <summary>
        /// Used as internal flag for if application settings is checked "SaveDisabledModsInSelection". Allows for disabled mods to be saved back to the user's selection
        /// </summary>
        public bool FlagForSelectionSave { get; set; } = false;

        /// <summary>
        /// Field for whether the package is selected to install
        /// </summary>
        protected internal bool _Checked = false;

        /// <summary>
        /// Property for if the package is selected by the user to install. handles all color change and single_dropdown updating code
        /// </summary>
        public bool Checked
        {
            get
            {
                return _Checked;
            }
            set
            {
                //set the internal checked value
                _Checked = value;

                //run code to determine if a standard option or a dropdown
                int dropDownSelectionType = -1;
                if (Type == SelectionTypes.single_dropdown1)
                {
                    dropDownSelectionType = 0;
                }
                else if (Type == SelectionTypes.single_dropdown2)
                {
                    dropDownSelectionType = 1;
                }

                //run the checked UI code
                //if the UI component is not null, it's a checkbox or radiobutton
                if (UIComponent != null)
                {
                    UIComponent.OnCheckedChanged(value);
                }
                //null UI component is a combobox
                else
                {
                    //inside here is for comboboxes (checked)
                    if (Enabled && dropDownSelectionType > -1 && IsStructureEnabled)
                    {
                        //go to the parent array list above this that holds the combobox and run the UI code
                        Parent.RelhaxWPFComboBoxList[dropDownSelectionType].OnDropDownSelectionChanged(this, value);
                    }
                }

                //determine if we should perform color change on the UI component(s)
                bool UIComponentColorChange = false;
                if (ModpackSettings.ModSelectionView == SelectionView.DefaultV2 && ModpackSettings.EnableColorChangeDefaultV2View)
                    UIComponentColorChange = true;
                else if (ModpackSettings.ModSelectionView == SelectionView.Legacy && ModpackSettings.EnableColorChangeLegacyView)
                    UIComponentColorChange = true;

                if(UIComponentColorChange && Visible && IsStructureVisible)
                {
                    //if the UI component is not null, it's a checkbox or radiobutton
                    if (UIComponent != null)
                    {
                        //set panel and text color based on true or false for checkbox or radiobutton
                        switch (_Checked)
                        {
                            case true:
                                if (UIComponent.PanelColor != UISettings.CurrentTheme.SelectionListSelectedPanelColor.Brush)
                                    UIComponent.PanelColor = UISettings.CurrentTheme.SelectionListSelectedPanelColor.Brush;
                                UIComponent.TextColor = UISettings.CurrentTheme.SelectionListSelectedTextColor.Brush;
                                break;
                            case false:
                                if (!AnyPackagesChecked())
                                {
                                    if (UIComponent.PanelColor != UISettings.CurrentTheme.SelectionListNotSelectedPanelColor.Brush)
                                        UIComponent.PanelColor = UISettings.CurrentTheme.SelectionListNotSelectedPanelColor.Brush;
                                    UIComponent.TextColor = UISettings.CurrentTheme.SelectionListNotSelectedTextColor.Brush;
                                }
                                break;
                        }
                    }
                    //null UI component is a combobox
                    else if (dropDownSelectionType > -1)
                    {
                        //set panel and text color based on true of false for dropdown option
                        switch (_Checked)
                        {
                            case true:
                                if (ParentBorder.Background != UISettings.CurrentTheme.SelectionListSelectedPanelColor.Brush)
                                    ParentBorder.Background = UISettings.CurrentTheme.SelectionListSelectedPanelColor.Brush;
                                break;
                            case false:
                                if (!AnyPackagesChecked())
                                {
                                    if (ParentBorder.Background != UISettings.CurrentTheme.SelectionListNotSelectedPanelColor.Brush)
                                        ParentBorder.Background = UISettings.CurrentTheme.SelectionListNotSelectedPanelColor.Brush;
                                }
                                break;
                        }
                    }

                    //toggle the Tab Color based on if anything is selected, done for level -1 top item
                    if (Level == -1)
                    {
                        //workarounds
                        //top item is not going to correct color
                        if (ModpackSettings.ModSelectionView == SelectionView.Legacy)
                        {
                            if (_Checked)
                            {
                                TreeView.Background = UISettings.CurrentTheme.SelectionListSelectedPanelColor.Brush;
                            }
                            else
                            {
                                TreeView.Background = UISettings.CurrentTheme.SelectionListNotSelectedPanelColor.Brush;
                            }
                        }
                        else if (ModpackSettings.ModSelectionView == SelectionView.DefaultV2)
                        {
                            if (!_Checked)
                            {
                                ParentBorder.Background = UISettings.CurrentTheme.SelectionListNotSelectedPanelColor.Brush;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Overrides DatabasePackage.Enabled property. Used to toggle if the mod should be selectable and installed in the selection list.
        /// The override also enables the triggering of the UI components to reflect the user's selection changes.
        /// </summary>
        public override bool Enabled
        {
            get { return _Enabled; }
            set
            {
                _Enabled = value;
                if (UIComponent != null)
                    UIComponent.OnEnabledChanged(value);
            }
        }

        /// <summary>
        /// The level in the database tree where the package resides.
        /// Category header is -1, each child is +1 from there
        /// </summary>
        public int Level { get; set; } = -2;

        /// <summary>
        /// The list of cache files that should be backed up before wiping the directory
        /// </summary>
        public List<UserFile> UserFiles { get; set; } = new List<UserFile>();

        /// <summary>
        /// The list of child SelectablePackage entries in this instance of SelectablePackages
        /// </summary>
        public List<SelectablePackage> Packages { get; set; } = new List<SelectablePackage>();

        /// <summary>
        /// List of media preview items associated with this package, shown in the preview window on right click of component
        /// </summary>
        public List<Media> Medias { get; set; } = new List<Media>();

        /// <summary>
        /// A list of packages (from dependencies list) that this package is dependent on in order to be installed
        /// </summary>
        public List<DatabaseLogic> Dependencies { get; set; } = new List<DatabaseLogic>();

        /// <summary>
        /// A list of any SelectablePackages that conflict with this mod. A conflict will result the package not being processed.
        /// Refer to examples for more information
        /// </summary>
        public string ConflictingPackages { get; set; } = string.Empty;

        /// <summary>
        /// Returns a list of the ConflictingPackages string property
        /// </summary>
        public List<string> ConflictingPackagesList
        {
            get { return ConflictingPackages.Split(new string[] { "," },StringSplitOptions.RemoveEmptyEntries).ToList(); }
        }

        /// <summary>
        /// Toggle if the package should appear in the search list
        /// </summary>
        public bool ShowInSearchList { get; set; } = true;

        #endregion

        #region UI Properties Shared
        /// <summary>
        /// The UI element reference for this package
        /// </summary>
        public IPackageUIComponent UIComponent;

        /// <summary>
        /// The UI element reference for the direct parent of this package
        /// </summary>
        public IPackageUIComponent ParentUIComponent;

        /// <summary>
        /// The UI element reference for the absolute top of the package tree
        /// </summary>
        public IPackageUIComponent TopParentUIComponent;

        /// <summary>
        /// The list of WPF combo boxes for each combobox type
        /// </summary>
        public RelhaxWPFComboBox[] RelhaxWPFComboBoxList;

        /// <summary>
        /// The border for the legacy view to allow for putting all children in the border. sits inside TreeViewItem. WPF component
        /// </summary>
        public Border ChildBorder;

        /// <summary>
        /// The StackPanel to allow the child TreeViewItems to stack upon each other. sits inside the border. WPF component
        /// </summary>
        public StackPanel ChildStackPanel;

        /// <summary>
        /// The border that this component is in. WPF component
        /// </summary>
        public Border ParentBorder;

        /// <summary>
        /// The StackPanel that this item is inside. WPF component
        /// </summary>
        public StackPanel ParentStackPanel;
        #endregion

        #region UI Properties Default View
        /// <summary>
        /// ContentControl item to allow for right-clicking of disabled components. defaultv2 WPF component
        /// </summary>
        public ContentControl @ContentControl;

        /// <summary>
        /// Component used only in the top SelectablePackage to allow for scrolling of the package lists for each category
        /// </summary>
        public ScrollViewer @ScrollViewer;
        #endregion

        #region UI Properties OMC Legacy View
        /// <summary>
        /// The TreeViewItem that corresponds to this package. legacy WPF component
        /// </summary>
        public StretchingTreeViewItem @TreeViewItem;

        /// <summary>
        /// The TreeView that this package is in. legacy WPF component
        /// </summary>
        public StretchingTreeView @TreeView;

        /// <summary>
        /// The TabItem UI reference
        /// </summary>
        public TabItem TabIndex;
        #endregion

        #region Other Properties and Methods
        /// <summary>
        /// The level at which this package will be installed, factoring if the category (if SelectablePackage) is set to offset the install group with the package level
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public override int InstallGroupWithOffset
        {
            get
            {
                if(Level == -2)
                {
                    throw new BadMemeException("You forgot to perform database linking to set level");
                }
                if(ParentCategory == null)
                {
                    throw new BadMemeException("You forgot to perform database linking to make ParentCategory not null");
                }
                if (ParentCategory.OffsetInstallGroups)
                    return InstallGroup + Level;
                else
                    return InstallGroup;
            }
        }

        /// <summary>
        /// Provides a complete path of the name fields from the top package down to where this package is located in the tree
        /// </summary>
        public override string CompletePath
        {
            get
            {
                if(ParentCategory == null)
                {
                    return base.CompletePath;
                }
                //level is taken care of in createModStructure, so use that
                List<string> parentPackages = new List<string>();
                SelectablePackage package = this;
                while(package != null && package.Level > -1)
                {
                    parentPackages.Add(package.NameFormatted);
                    package = package.Parent;
                }
                parentPackages.Add(ParentCategory.Name);
                parentPackages.Reverse();
                return string.Join("->",parentPackages);
            }
        }

        /// <summary>
        /// Provides a complete path of the packageName fields from the top package down to where this package is located in the tree
        /// </summary>
        public override string CompletePackageNamePath
        {
            get
            {
                if (ParentCategory == null)
                {
                    return base.CompletePackageNamePath;
                }
                //level is taken care of in createModStructure, so use that
                List<string> parentPackages = new List<string>();
                SelectablePackage package = this;
                while (package != null && package.Level > -1)
                {
                    parentPackages.Add(package.PackageName);
                    package = package.Parent;
                }
                parentPackages.Reverse();
                return string.Join("->", parentPackages);
            }
        }

        /// <summary>
        /// Provides a complete tree style path to the package using its UID, starting with the category
        /// </summary>
        public override string CompleteUIDPath
        {
            get
            {
                if (ParentCategory == null)
                {
                    return base.CompletePackageNamePath;
                }
                //level is taken care of in createModStructure, so use that
                List<string> parentPackages = new List<string>();
                SelectablePackage package = this;
                while (package != null && package.Level > -1)
                {
                    parentPackages.Add(package.UID);
                    package = package.Parent;
                }
                parentPackages.Reverse();
                return string.Join("->", parentPackages);
            }
        }

        /// <summary>
        /// Determines if the UI package structure to this package is of all visible components.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public bool IsStructureVisible
        {
            get
            {
                if (Parent == null || TopParent == null)
                    throw new BadMemeException("RUN THE LINKING CODE FIRST");
                if (!Visible) return false;
                bool structureVisible = true;
                SelectablePackage parentRef = Parent;
                //TopParent is the category header and thus is always visible
                while (parentRef != TopParent)
                {
                    if (!parentRef.Visible)
                    {
                        structureVisible = false;
                        //at this point it's false so we can exit early and save a few cycles
                        break;
                    }
                    parentRef = parentRef.Parent;
                }
                return structureVisible;
            }
        }

        /// <summary>
        /// Determines if all parent packages leading to this package are enabled. In other words, it checks if the path to this package is enabled
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations")]
        public bool IsStructureEnabled
        {
            get
            {
                if (Parent == null || TopParent == null)
                    throw new BadMemeException("RUN THE LINKING CODE FIRST");
                if (!Enabled) return false;
                bool structureEnabled = true;
                SelectablePackage parentRef = Parent;
                while (parentRef != TopParent)
                {
                    if (!parentRef.Enabled)
                    {
                        structureEnabled = false;
                        //at this point it's false so we can exit early and save a few cycles
                        break;
                    }
                    parentRef = parentRef.Parent;
                }
                return structureEnabled;
            }
        }

        /// <summary>
        /// Returns the display name of the package for the UI, with version macros replaced and any other statuses appended
        /// </summary>
        public string NameDisplay
        {
            get
            {
                //get the original formatted name
                //get if the package is forced visible
                //get if the package is forced enabled
                //get if the package is need to be download "(updated)" text (and size)
                //(only happens if level > -1)
                string nameDisplay = NameFormatted;
                if (ModpackSettings.ForceVisible && !IsStructureVisible)
                    nameDisplay = string.Format("{0} [{1}]", nameDisplay, Translations.GetTranslatedString("invisible"));
                if (ModpackSettings.ForceEnabled && !IsStructureEnabled)
                    nameDisplay = string.Format("{0} [{1}]", nameDisplay, Translations.GetTranslatedString("disabled"));
                if(Level > -1 && DownloadFlag)
                {
                    nameDisplay = string.Format("{0} ({1})", nameDisplay, Translations.GetTranslatedString("updated"));
                    if (Size > 0)
                        nameDisplay = string.Format("{0} ({1})", nameDisplay, FileUtils.SizeSuffix(Size, 1, true));
                }
                //escape character fix
                return nameDisplay.Replace("_","__");
            }
        }

        /// <summary>
        /// Returns a string representation of the timestamp of when the zip file of this package was last modified
        /// </summary>
        public string TimeStampString
        {
            get
            {
                return CommonUtils.ConvertFiletimeTimestampToDate(Timestamp);
            }
        }

        /// <summary>
        /// Returns the display tool tip string, or the translation string for "no description"
        /// </summary>
        public string ToolTipString
        {
            get
            {
                string toolTipResult = string.IsNullOrWhiteSpace(Description) ?
                    Translations.GetTranslatedString("noDescription") : DescriptionEscaped;
                return string.Format("{0}\n\n{1}{2}",
                    toolTipResult, Translations.GetTranslatedString("lastUpdated"), TimeStampString).Replace("_","__");
            }
        }

        /// <summary>
        /// Alphabetical sorting of packages by PackageName property at this level (not recursive)
        /// </summary>
        /// <param name="x">First package to compare</param>
        /// <param name="y">Second package to compare</param>
        /// <returns></returns>
        public static int CompareModsPackageName(SelectablePackage x, SelectablePackage y)
        {
            return x.PackageName.CompareTo(y.PackageName);
        }

        /// <summary>
        /// Alphabetical sorting of packages by NameFormatted property at this level (not recursive)
        /// </summary>
        /// <param name="x">First package to compare</param>
        /// <param name="y">Second package to compare</param>
        /// <returns></returns>
        public static int CompareModsName(SelectablePackage x, SelectablePackage y)
        {
            return x.NameFormatted.CompareTo(y.NameFormatted);
        }

        /// <summary>
        /// Allows for display in a combobox and when debugging
        /// </summary>
        /// <returns>The nameFormatted property of the package</returns>
        public override string ToString()
        {
            return NameFormatted;
        }

        /// <summary>
        /// Check if the color change should be changed on or off, based on if any other packages at this level are enabled and checked
        /// </summary>
        /// <returns>True if another package at this level is checked and enabled, false otherwise</returns>
        public bool AnyPackagesChecked()
        {
            foreach (SelectablePackage sp in Parent.Packages)
            {
                if (sp.Enabled && sp.Checked)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the structure above and below this package is valid (all mandatory child options checked, parent checked), false otherwise
        /// </summary>
        /// <remarks>This assumes that the database linking/reference code has been run, otherwise a null exception will occur</remarks>
        public bool IsStructureValid
        {
            get
            {
                if (this.Checked && (!this.Parent.Checked && this.Parent.Level != -1))
                    return false;

                bool hasSingles = false;
                bool singleSelected = false;
                bool hasDD1 = false;
                bool DD1Selected = false;
                bool hasDD2 = false;
                bool DD2Selected = false;

                //first check if this package has any of these children type
                foreach (SelectablePackage childPackage in this.Packages)
                {
                    if ((childPackage.Type == SelectionTypes.single1) && childPackage.Enabled)
                    {
                        hasSingles = true;
                        //check if the child package is selected. it's fine to overwrite the bool cause we're
                        //just wanting to know if *any* child packages of this type are checked
                        if (childPackage.Checked)
                            singleSelected = true;
                    }
                    else if ((childPackage.Type == SelectionTypes.single_dropdown1) && childPackage.Enabled)
                    {
                        hasDD1 = true;
                        if (childPackage.Checked)
                            DD1Selected = true;
                    }
                    else if (childPackage.Type == SelectionTypes.single_dropdown2 && childPackage.Enabled)
                    {
                        hasDD2 = true;
                        if (childPackage.Checked)
                            DD2Selected = true;
                    }
                }

                //now make sure that for each of the above types, at least one is checked
                if (hasSingles && !singleSelected)
                {
                    return false;
                }
                if (hasDD1 && !DD1Selected)
                {
                    return false;
                }
                if (hasDD2 && !DD2Selected)
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Create an instance of the SelectablePackage class and over-ride DatabasePackage default values, while using values provided for copy objects
        /// </summary>
        /// <param name="packageToCopyFrom">The package to copy the information from</param>
        /// <param name="deep">Set to true to copy list objects, false to use new lists</param>
        public SelectablePackage(DatabasePackage packageToCopyFrom, bool deep) : base(packageToCopyFrom,deep)
        {
            InstallGroup = 4;
            PatchGroup = 4;

            if (packageToCopyFrom is Dependency dep)
            {
                if(deep)
                {
                    foreach (DatabaseLogic file in dep.Dependencies)
                        this.Dependencies.Add(DatabaseLogic.Copy(file));
                }
            }
            else if (packageToCopyFrom is SelectablePackage sp)
            {
                this.Type = sp.Type;
                this.Name = "WRITE_NEW_NAME";
                this.Visible = sp.Visible;
                this.Size = 0;

                this.UpdateComment = string.Empty;
                this.Description = string.Empty;
                this.PopularMod = false;
                this._Checked = false;

                this.Level = -2;
                this.UserFiles = new List<UserFile>();
                this.Packages = new List<SelectablePackage>();
                this.Medias = new List<Media>();
                this.Dependencies = new List<DatabaseLogic>();
                this.ConflictingPackages = string.Empty;
                this.ShowInSearchList = sp.ShowInSearchList;

                if (deep)
                {
                    this.UpdateComment = sp.UpdateComment;
                    this.Description = sp.Description;
                    this.PopularMod = sp.PopularMod;
                    this._Checked = sp._Checked;

                    foreach (UserFile file in this.UserFiles)
                        this.UserFiles.Add(UserFile.DeepCopy(file));

                    foreach (Media file in this.Medias)
                        this.Medias.Add(Media.Copy(file));

                    foreach (DatabaseLogic file in this.Dependencies)
                        this.Dependencies.Add(DatabaseLogic.Copy(file));
                }
            }
        }
        #endregion
    }
}
