﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FlowSynx.Plugin.Storage.LocalFileSystem {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("FlowSynx.Plugin.Storage.LocalFileSystem.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to File &apos;{0}&apos; is already exist and can&apos;t be overwritten!.
        /// </summary>
        internal static string FileIsAlreadyExistAndCannotBeOverwritten {
            get {
                return ResourceManager.GetString("FileIsAlreadyExistAndCannotBeOverwritten", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No files found with the given filter in &apos;{0}&apos;..
        /// </summary>
        internal static string NoFilesFoundWithTheGivenFilter {
            get {
                return ResourceManager.GetString("NoFilesFoundWithTheGivenFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Plugin for local file system management. Local paths are considered as normal file system paths, e.g. /path/to/wherever.
        /// </summary>
        internal static string PluginDescription {
            get {
                return ResourceManager.GetString("PluginDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The entered path is not a directory..
        /// </summary>
        internal static string ThePathIsNotDirectory {
            get {
                return ResourceManager.GetString("ThePathIsNotDirectory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The entered path is not a file..
        /// </summary>
        internal static string ThePathIsNotFile {
            get {
                return ResourceManager.GetString("ThePathIsNotFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified directory path &apos;{0}&apos; is not a directory..
        /// </summary>
        internal static string TheSpecifiedDirectoryPathIsNotDirectory {
            get {
                return ResourceManager.GetString("TheSpecifiedDirectoryPathIsNotDirectory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified path &apos;{0}&apos; is not exist..
        /// </summary>
        internal static string TheSpecifiedPathIsNotExist {
            get {
                return ResourceManager.GetString("TheSpecifiedPathIsNotExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified path &apos;{0}&apos; is not a file..
        /// </summary>
        internal static string TheSpecifiedPathIsNotFile {
            get {
                return ResourceManager.GetString("TheSpecifiedPathIsNotFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified path must be not empty!.
        /// </summary>
        internal static string TheSpecifiedPathMustBeNotEmpty {
            get {
                return ResourceManager.GetString("TheSpecifiedPathMustBeNotEmpty", resourceCulture);
            }
        }
    }
}
