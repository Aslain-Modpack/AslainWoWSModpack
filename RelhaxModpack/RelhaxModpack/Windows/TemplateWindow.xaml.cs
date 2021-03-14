﻿using RelhaxModpack.Settings;
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

namespace RelhaxModpack.Windows
{
    /// <summary>
    /// Interaction logic for TemplateWindow.xaml
    /// </summary>
    public partial class TemplateWindow : RelhaxWindow
    {
        /// <summary>
        /// Create an instance of TemplateWindow
        /// </summary>
        public TemplateWindow(ModpackSettings modpackSettings) : base(modpackSettings)
        {
            InitializeComponent();
        }
    }
}
