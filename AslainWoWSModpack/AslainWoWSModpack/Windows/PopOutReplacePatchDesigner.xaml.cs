﻿using AslainWoWSModpack.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AslainWoWSModpack.Windows
{
    /// <summary>
    /// Interaction logic for PopOutReplacePatchDesigner.xaml
    /// </summary>
    public partial class PopOutReplacePatchDesigner : RelhaxWindow
    {
        /// <summary>
        /// Create an instance of the PopOutReplacePatchDesigner Window
        /// </summary>
        public PopOutReplacePatchDesigner(ModpackSettings modpackSettings) : base(modpackSettings)
        {
            InitializeComponent();
        }
    }
}
