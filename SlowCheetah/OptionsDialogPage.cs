//--------------------------------------------------------------------------------------
// DialogPageOptions.cs
//
// Tools, Options page for slow cheetah settings 
// 
// Copyright (c) Microsoft Corporation.  All rights reserved.
//--------------------------------------------------------------------------------------

using System;
using System.ComponentModel;
using System.Collections;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;
using System.Text;
using System.Globalization;
using System.Net;
using Shell = Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using System.Resources;
using System.Reflection;
using Microsoft.Win32;
using System.IO;

namespace SlowCheetah.VisualStudio
{
    [
        System.Runtime.InteropServices.GuidAttribute("01B6BAC2-0BD6-4ead-95AE-6D6DE30A6286"),
    ]
    internal class OptionsDialogPage : Shell.DialogPage
    {
        const string regOptionsKey = "ConfigTransform";
        const string regPreviewCmdLine = "PreviewCmdLine";
        const string regPreviewExe = "PreviewExe";
        const string regPreviewEnable = "EnablePreview";


        //---------------------------------------------------------------------
        /// <summary>
        /// Constructor called by VSIP just in time when user wants to view this 
        /// tools, options page
        /// </summary>
        //---------------------------------------------------------------------
        public OptionsDialogPage()
        {
            InitializeDefaults();
        }

        ///---------------------------------------------------------------------
        /// <summary> 
        /// Sets up our default values.
        /// </summary>
        ///---------------------------------------------------------------------
        private void InitializeDefaults()
        {
            string diffToolPath = SlowCheetahPackage.OurPackage.GetVsInstallDirectory();
            if(diffToolPath != null) {
                diffToolPath = Path.Combine(diffToolPath, "diffmerge.exe");                                
                PreviewToolExecutablePath = diffToolPath;
            }
            PreviewToolCommandLine = "{0} {1}";
            EnablePreview = true;
        }

        ///---------------------------------------------------------------------
        /// <summary>
        /// Properties we save. These become automation properties that make these available from
        /// DTE.
        /// </summary>
        ///---------------------------------------------------------------------
        [LocDisplayName("PreviewToolExecutablePath")]
        [LocDescription("PreviewToolExecutablePath")]
        public string PreviewToolExecutablePath { get; set; }
        [LocDisplayName("PreviewToolCommandLine")]
        [LocDescription("PreviewToolCommandLine")]
        public string PreviewToolCommandLine { get; set; }
        [LocDisplayName("RunPreviewTool")]
        [LocDescription("RunPreviewTool")]
        public bool EnablePreview { get; set; }

        //--------------------------------------------------------------------------------------
        // This event is raised when VS wants to deactivate this
        // page.  The page is deactivated unless the event is
        // cancelled.
        //--------------------------------------------------------------------------------------
        protected override void OnDeactivate(CancelEventArgs e)
        {
        }

        //--------------------------------------------------------------------------------------
        /// <summary>
        /// Save our settings to the specified XML writer so they can be exported
        /// </summary>
        /// <param name="writer">The VsSettings writer to write our values to</param>
        //--------------------------------------------------------------------------------------
        public override void SaveSettingsToXml(Microsoft.VisualStudio.Shell.Interop.IVsSettingsWriter writer)
        {
            try
            {
                base.SaveSettingsToXml(writer);

                // Write settings to XML
                writer.WriteSettingString(regPreviewExe, PreviewToolExecutablePath);
                writer.WriteSettingString(regPreviewCmdLine, PreviewToolCommandLine);
                writer.WriteSettingBoolean(regPreviewEnable, EnablePreview? 1 : 0);
            }
            catch (Exception e)
            {
                Debug.Assert(false, "Error exporting Slow Cheetah settings: " + e.Message);
            }
        }

        ///--------------------------------------------------------------------------------------
        /// <summary>
        /// Read settings as the result of a tools, import operation
        /// </summary>
        /// <param name="reader">VsSetings reader that contains our values as written previously</param>
        ///--------------------------------------------------------------------------------------
        public override void LoadSettingsFromXml(Microsoft.VisualStudio.Shell.Interop.IVsSettingsReader reader)
        {
            try
            {
                InitializeDefaults();
                string exePath, exeCmdLine;
                int enablePreview;
                if(SlowCheetahPackage.Succeeded(reader.ReadSettingString(regPreviewExe, out exePath)) && !string.IsNullOrEmpty(exePath))
                    PreviewToolExecutablePath = exePath;

                if(SlowCheetahPackage.Succeeded(reader.ReadSettingString(regPreviewCmdLine, out exeCmdLine)) && !string.IsNullOrEmpty(exeCmdLine))
                    PreviewToolCommandLine = exeCmdLine;
                if(SlowCheetahPackage.Succeeded(reader.ReadSettingBoolean(regPreviewEnable, out enablePreview)))
                    EnablePreview = enablePreview == 1;
            }
            catch (Exception e)
            {
                Debug.Assert(false, "Error importing Slow Cheetah settings: " + e.Message);
            }
        }

        ///--------------------------------------------------------------------------------------
        /// <summary>
        /// Load Tools-->Options settings from registry. Defaults are set in constructor so these
        /// will just override those values.
        /// </summary>
        ///--------------------------------------------------------------------------------------
        public override void LoadSettingsFromStorage()
        {
            try
            {
                InitializeDefaults();
                using(RegistryKey userRootKey = SlowCheetahPackage.OurPackage.UserRegistryRoot) {
                    using (RegistryKey cheetahKey = userRootKey.OpenSubKey(regOptionsKey))
                    {
                        if (cheetahKey != null) {
                            object previewTool = cheetahKey.GetValue(regPreviewExe);
                            if(previewTool != null && (previewTool is string) && !string.IsNullOrEmpty((string)previewTool)) {
                                PreviewToolExecutablePath = (string)previewTool;
                            }

                            object previewCmdLine = cheetahKey.GetValue(regPreviewCmdLine);
                            if (previewCmdLine != null && (previewCmdLine is string) && !string.IsNullOrEmpty((string)previewCmdLine)) {
                                PreviewToolCommandLine = (string)previewCmdLine;
                            }

                            object enablePreview = cheetahKey.GetValue(regPreviewEnable);
                            if (enablePreview != null && (enablePreview is int)) {
                                EnablePreview = ((int)enablePreview) == 1;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Assert(false, "Error reading Slow Cheetah settings from the registry: " + e.Message);
            }
        }

        ///--------------------------------------------------------------------------------------
        /// <summary>
        /// Save Tools-->Options settings to registry
        /// </summary>
        ///--------------------------------------------------------------------------------------
        public override void SaveSettingsToStorage()
        {
            try
            {
                base.SaveSettingsToStorage();
                using(RegistryKey userRootKey = SlowCheetahPackage.OurPackage.UserRegistryRoot) {
                    using (RegistryKey cheetahKey = userRootKey.CreateSubKey(regOptionsKey)) {
                        cheetahKey.SetValue(regPreviewExe, PreviewToolExecutablePath);
                        cheetahKey.SetValue(regPreviewCmdLine, PreviewToolCommandLine);
                        cheetahKey.SetValue(regPreviewEnable, EnablePreview? 1 : 0);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Assert(false, "Error saving Slow Cheetah settings to the registry:" + e.Message);
            }
        }

        protected override void OnApply(Shell.DialogPage.PageApplyEventArgs e)
        {
                e.ApplyBehavior = ApplyKind.Apply;
                base.OnApply(e);
        }
    }

    ///--------------------------------------------------------------------------------------------
    /// <summary>
    /// Attribute class allows us to loc the displayname for our properties. Property resource
    /// is expected to be named PropName_[propertyname]
    /// </summary>
    ///--------------------------------------------------------------------------------------------
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class LocDisplayNameAttribute : DisplayNameAttribute
    {
        private string _displayName;

        public LocDisplayNameAttribute(string name)
        {
            _displayName = name;
        }

        public override string DisplayName
        {
            get
            {
                string result = Resources.ResourceManager.GetString("PropName_" + _displayName);
                if (result == null)
                {
                    // Just return non-loc'd value
                    Debug.Assert(false, "String resource '" + _displayName + "' is missing");
                    result = _displayName;
                }

                return result;
            }
        }
    }
    ///--------------------------------------------------------------------------------------------
    /// <summary>
    /// Attribute class allows us to loc the description for our properties. Property resource
    /// is expected to be named PropDesc_[propertyname]
    /// </summary>
    ///--------------------------------------------------------------------------------------------
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class LocDescriptionAttribute : DescriptionAttribute
    {
        private string _descName;

        public LocDescriptionAttribute(string name)
        {
            _descName = name;
        }

        public override string Description
        {
            get
            {
                string result = Resources.ResourceManager.GetString("PropDesc_" + _descName);

                if (result == null)
                {
                    // Just return non-loc'd value
                    Debug.Assert(false, "String resource '" + _descName + "' is missing");
                    result = _descName;
                }
                return result;
            }
        }
    }


}
