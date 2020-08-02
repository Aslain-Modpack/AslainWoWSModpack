﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RelhaxModpack.Atlases
{
    /// <summary>
    /// A class that serves as a description of an atlas file with processing instructions
    /// </summary>
    public class Atlas
    {
        /// <summary>
        /// Complete path to the package file
        /// </summary>
        /// <remarks>This is loaded from the xml file. The package itself is a non-compressed zip file</remarks>
        public string Pkg { get; set; } = string.Empty;

        /// <summary>
        /// File name of the atlas image file to extract
        /// </summary>
        /// <remarks>This is loaded from the xml file</remarks>
        public string AtlasFile { get; set; } = string.Empty;

        /// <summary>
        /// File name of the atlas map file to extract
        /// </summary>
        /// <remarks>This is loaded from the xml file</remarks>
        public string MapFile { get; set; } = string.Empty;

        /// <summary>
        /// Path inside the pkg file to the filename to process. If Pkg is empty, this is the path to the atlas and map file
        /// </summary>
        /// <remarks>This is loaded from the xml file</remarks>
        public string DirectoryInArchive { get; set; } = string.Empty;

        /// <summary>
        /// Path to place the generated atlas image file and xml map
        /// </summary>
        /// <remarks>This is loaded from the xml file</remarks>
        public string AtlasSaveDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Width of the new atlases file. 0 = get from original atlas file
        /// </summary>
        /// <remarks>This is loaded from the xml file</remarks>
        public int AtlasWidth { get; set; } = 0;

        /// <summary>
        /// Height of the new atlases file. 0 = get from original atlas file
        /// </summary>
        /// <remarks>This is loaded from the xml file</remarks>
        public int AtlasHeight { get; set; } = 0;

        /// <summary>
        /// Padding of the new atlases file (amount of pixels as a border between each image)
        /// </summary>
        /// <remarks>This is loaded from the xml file</remarks>
        public int Padding { get; set; } = 1;

        /// <summary>
        /// Creating an atlas file only with log base 2 numbers (16, 32, 64, etc.)
        /// </summary>
        /// <remarks>This is loaded from the xml file</remarks>
        public bool PowOf2 { get; set; } = false;

        /// <summary>
        /// Creating an atlas file only in a square (same width and height of atlas)
        /// </summary>
        /// <remarks>This is loaded from the xml file</remarks>
        public bool Square { get; set; } = false;

        /// <summary>
        /// allow to accept first successful image optimization layout
        /// </summary>
        /// <remarks>This is loaded from the xml file</remarks>
        public bool FastImagePacker { get; set; } = true;

        /// <summary>
        /// List of folders that could contain images to replace original images
        /// </summary>
        /// <remarks>This is loaded from the xml file</remarks>
        public List<string> ImageFolderList { get; set; } = new List<string>();

        /// <summary>
        /// The list of textures in each atlas
        /// </summary>
        /// <remarks>This is *not* loaded from the xml file and is used internally</remarks>
        public List<Texture> TextureList { get; set; } = new List<Texture>();

        /// <summary>
        /// The file path where the original atlas image file will be extracted/copied to
        /// </summary>
        /// <remarks>This is *not* loaded from the xml file and is used internally</remarks>
        public string TempAtlasImageFilePath { get; set; } = string.Empty;

        /// <summary>
        /// The file path where the original atlas map file will be extracted/copied to
        /// </summary>
        /// <remarks>This is *not* loaded from the xml file and is used internally</remarks>
        public string TempAtlasMapFilePath { get; set; } = string.Empty;

        /// <summary>
        /// The file path where the created atlas image file will be placed
        /// </summary>
        /// <remarks>This is *not* loaded from the xml file and is used internally</remarks>
        public string AtlasImageFilePath { get; set; } = string.Empty;

        /// <summary>
        /// The file path where the created map file will be placed
        /// </summary>
        /// <remarks>This is *not* loaded from the xml file and is used internally</remarks>
        public string AtlasMapFilePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Returns a string representation of the object
        /// </summary>
        /// <returns>The atlas file name</returns>
        public override string ToString()
        {
            return string.Format("AtlasFile: {0}", string.IsNullOrEmpty(AtlasFile) ? "(empty)" : AtlasFile);
        }
    }
}
