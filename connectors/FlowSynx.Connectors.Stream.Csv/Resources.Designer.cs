﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FlowSynx.Connectors.Stream.Csv {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("FlowSynx.Connectors.Stream.Csv.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to About operation is not supported for CSV stream!.
        /// </summary>
        internal static string AboutOperrationNotSupported {
            get {
                return ResourceManager.GetString("AboutOperrationNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Amazon S3 Storage connector doesn&apos;t support as callee connector!.
        /// </summary>
        internal static string CalleeConnectorNotSupported {
            get {
                return ResourceManager.GetString("CalleeConnectorNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Connector for manage Comma Separated Values (CSV) data..
        /// </summary>
        internal static string ConnectorDescription {
            get {
                return ResourceManager.GetString("ConnectorDescription", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Create operation is not supported for CSV stream!.
        /// </summary>
        internal static string CreateOperrationNotSupported {
            get {
                return ResourceManager.GetString("CreateOperrationNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Data is not in valid format. Data must be in array type..
        /// </summary>
        internal static string DataMustBeInValidFormat {
            get {
                return ResourceManager.GetString("DataMustBeInValidFormat", resourceCulture);
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
        ///   Looks up a localized string similar to For reading functionality your filter must return single item..
        /// </summary>
        internal static string FilteringDataMustReturnASingleItem {
            get {
                return ResourceManager.GetString("FilteringDataMustReturnASingleItem", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Data must have value..
        /// </summary>
        internal static string ForWritingDataMustHaveValue {
            get {
                return ResourceManager.GetString("ForWritingDataMustHaveValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No items found with the given filter&apos;..
        /// </summary>
        internal static string NoItemsFoundWithTheGivenFilter {
            get {
                return ResourceManager.GetString("NoItemsFoundWithTheGivenFilter", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The entered path is not a csv file. The file must be ended with .csv extension..
        /// </summary>
        internal static string ThePathIsNotCsvFile {
            get {
                return ResourceManager.GetString("ThePathIsNotCsvFile", resourceCulture);
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
        ///   Looks up a localized string similar to The specified path must be not empty!.
        /// </summary>
        internal static string TheSpecifiedPathMustBeNotEmpty {
            get {
                return ResourceManager.GetString("TheSpecifiedPathMustBeNotEmpty", resourceCulture);
            }
        }
    }
}
