﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FlowSynx.Application {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("FlowSynx.Application.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to The given config name {0} is already exist!.
        /// </summary>
        internal static string AddConfigHandlerItemIsAlreadyExist {
            get {
                return ResourceManager.GetString("AddConfigHandlerItemIsAlreadyExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The configuration was added successfully..
        /// </summary>
        internal static string AddConfigHandlerSuccessfullyAdded {
            get {
                return ResourceManager.GetString("AddConfigHandlerSuccessfullyAdded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Name should be not empty..
        /// </summary>
        internal static string AddConfigValidatorNameValueMustNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("AddConfigValidatorNameValueMustNotNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The config name accepts only Latin letters and numbers, as well as underscores. Additionally, the name must begin with an alphabetic letter..
        /// </summary>
        internal static string AddConfigValidatorNameValueOnlyAcceptLatingCharacters {
            get {
                return ResourceManager.GetString("AddConfigValidatorNameValueOnlyAcceptLatingCharacters", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The entered type for connector &apos;{0}&apos; is not valid or not exist!.
        /// </summary>
        internal static string AddConfigValidatorTypeValueIsNotValid {
            get {
                return ResourceManager.GetString("AddConfigValidatorTypeValueIsNotValid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Type should be not empty..
        /// </summary>
        internal static string AddConfigValidatorTypeValueMustNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("AddConfigValidatorTypeValueMustNotNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Workflow &apos;{0}&apos; is already exist..
        /// </summary>
        internal static string AddWorkflowNameIsAlreadyExist {
            get {
                return ResourceManager.GetString("AddWorkflowNameIsAlreadyExist", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The compression was done successfully..
        /// </summary>
        internal static string CompressHandlerSuccessfullyCompress {
            get {
                return ResourceManager.GetString("CompressHandlerSuccessfullyCompress", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Entity should be not empty..
        /// </summary>
        internal static string CompressValidatorEntityShouldNotBeNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("CompressValidatorEntityShouldNotBeNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Compress type value must be [ Zip | GZip | Tar ]. By default it is Zip..
        /// </summary>
        internal static string CompressValidatorTypeValueShouldBeValidMessage {
            get {
                return ResourceManager.GetString("CompressValidatorTypeValueShouldBeValidMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error in initializing &apos;{0}&apos; connector instance!.
        /// </summary>
        internal static string ConnectorInItializingInstanceErrorMessage {
            get {
                return ResourceManager.GetString("ConnectorInItializingInstanceErrorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Namespace value must be [ Storage| Messaging | Stream ]. By default it is Storage..
        /// </summary>
        internal static string ConnectorValidatorConnectorNamespaceValueMustBeValidMessage {
            get {
                return ResourceManager.GetString("ConnectorValidatorConnectorNamespaceValueMustBeValidMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified destination-path is of a different type than the source-path..
        /// </summary>
        internal static string CopyDestinationPathIsDifferentThanSourcePath {
            get {
                return ResourceManager.GetString("CopyDestinationPathIsDifferentThanSourcePath", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The copy was done successfully..
        /// </summary>
        internal static string CopyHandlerSuccessfullyCopy {
            get {
                return ResourceManager.GetString("CopyHandlerSuccessfullyCopy", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Destination entity should be not empty..
        /// </summary>
        internal static string CopyValidatorDestinationEntityValueShouldNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("CopyValidatorDestinationEntityValueShouldNotNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Source entity should be not empty..
        /// </summary>
        internal static string CopyValidatorSourceEntityValueShouldNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("CopyValidatorSourceEntityValueShouldNotNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The entity was created successfully..
        /// </summary>
        internal static string CreateHandlerSuccessfullyDeleted {
            get {
                return ResourceManager.GetString("CreateHandlerSuccessfullyDeleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Entity should be not empty..
        /// </summary>
        internal static string CreateValidatorEntityShouldNotBeNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("CreateValidatorEntityShouldNotBeNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The given configs are deleted..
        /// </summary>
        internal static string DeleteConfigHandlerSuccessfullyDeleted {
            get {
                return ResourceManager.GetString("DeleteConfigHandlerSuccessfullyDeleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Name should be not empty..
        /// </summary>
        internal static string DeleteConfigValidatorNameValueMustNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("DeleteConfigValidatorNameValueMustNotNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The entities were deleted successfully..
        /// </summary>
        internal static string DeleteHandlerSuccessfullyDeleted {
            get {
                return ResourceManager.GetString("DeleteHandlerSuccessfullyDeleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Entity should be not empty..
        /// </summary>
        internal static string DeleteValidatorEntityValueShouldNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("DeleteValidatorEntityValueShouldNotNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There is something wrong happened during compress data..
        /// </summary>
        internal static string ErrorDuringCompressData {
            get {
                return ResourceManager.GetString("ErrorDuringCompressData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Entity should be not empty..
        /// </summary>
        internal static string ExistValidatorEntityValueShouldNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("ExistValidatorEntityValueShouldNotNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The value from ({0}) could not be extracted!.
        /// </summary>
        internal static string FileSystemDateParserCannotExtractValue {
            get {
                return ResourceManager.GetString("FileSystemDateParserCannotExtractValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The given datetime is not valid!.
        /// </summary>
        internal static string FileSystemDateParserInvalidInput {
            get {
                return ResourceManager.GetString("FileSystemDateParserInvalidInput", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; FileSystem not found!.
        /// </summary>
        internal static string FileSystemRemotePathParserFileSystemNotFoumd {
            get {
                return ResourceManager.GetString("FileSystemRemotePathParserFileSystemNotFoumd", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The value from ({0}) could not be extracted!.
        /// </summary>
        internal static string FileSystemSizeParserCannotExtractValue {
            get {
                return ResourceManager.GetString("FileSystemSizeParserCannotExtractValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The given size is not valid!.
        /// </summary>
        internal static string FileSystemSizeParserInvalidInput {
            get {
                return ResourceManager.GetString("FileSystemSizeParserInvalidInput", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid Property. Order By Format: Property, Property2 ASC, Property2 DESC.
        /// </summary>
        internal static string FileSystemSortParserInvalidProperty {
            get {
                return ResourceManager.GetString("FileSystemSortParserInvalidProperty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid Property. Given sorting property name &apos;{0}&apos; is not valid..
        /// </summary>
        internal static string FileSystemSortParserInvalidPropertyName {
            get {
                return ResourceManager.GetString("FileSystemSortParserInvalidPropertyName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sort direction &apos;{0}&apos; for &apos;{1}&apos; is not valid..
        /// </summary>
        internal static string FileSystemSortParserInvalidSortDirection {
            get {
                return ResourceManager.GetString("FileSystemSortParserInvalidSortDirection", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid Sorting string &apos;{0}&apos;. Order By Format: Property, Property2 ASC, Property2 DESC.
        /// </summary>
        internal static string FileSystemSortParserInvalidSortingTerm {
            get {
                return ResourceManager.GetString("FileSystemSortParserInvalidSortingTerm", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to One or more validation failures have occurred..
        /// </summary>
        internal static string InputValidationExceptionBaseMessage {
            get {
                return ResourceManager.GetString("InputValidationExceptionBaseMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Entity should be not empty..
        /// </summary>
        internal static string ListValidatorEntityValueShouldNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("ListValidatorEntityValueShouldNotNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Level value must be [ Dbug | Info | Warn | Fail | Crit ]. By default it is Info..
        /// </summary>
        internal static string LogsValidatorKindValueMustBeValidMessage {
            get {
                return ResourceManager.GetString("LogsValidatorKindValueMustBeValidMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The specified destination-path is of a different type than the source-path..
        /// </summary>
        internal static string MoveDestinationPathIsDifferentThanSourcePath {
            get {
                return ResourceManager.GetString("MoveDestinationPathIsDifferentThanSourcePath", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The move was done successfully..
        /// </summary>
        internal static string MoveHandlerSuccessfullyMoved {
            get {
                return ResourceManager.GetString("MoveHandlerSuccessfullyMoved", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The source and destination path are identical and overlap..
        /// </summary>
        internal static string MoveTheSourceAndDestinationPathAreIdenticalAndOverlap {
            get {
                return ResourceManager.GetString("MoveTheSourceAndDestinationPathAreIdenticalAndOverlap", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Destination entity should be not empty..
        /// </summary>
        internal static string MoveValidatorDestinationEntityValueShouldNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("MoveValidatorDestinationEntityValueShouldNotNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Source entity should be not empty..
        /// </summary>
        internal static string MoveValidatorSourceEntityValueShouldNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("MoveValidatorSourceEntityValueShouldNotNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The given type {0} is not valid!.
        /// </summary>
        internal static string NamespaceParserInvalidType {
            get {
                return ResourceManager.GetString("NamespaceParserInvalidType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There is no data found for compress..
        /// </summary>
        internal static string NoDataToCompress {
            get {
                return ResourceManager.GetString("NoDataToCompress", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The directory was purged successfully..
        /// </summary>
        internal static string PurgeDirectoryHandlerSuccessfullyPurged {
            get {
                return ResourceManager.GetString("PurgeDirectoryHandlerSuccessfullyPurged", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Entity should be not empty..
        /// </summary>
        internal static string PurgeDirectoryValidatorPathValueMustNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("PurgeDirectoryValidatorPathValueMustNotNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Entity should be not empty..
        /// </summary>
        internal static string ReadValidatorEntityValueShouldNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("ReadValidatorEntityValueShouldNotNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Kind value must be [ File | Directory | FileAndDirectory ]. By default it is FileAndDirectory..
        /// </summary>
        internal static string SizeValidatorKindValueMustBeValidMessage {
            get {
                return ResourceManager.GetString("SizeValidatorKindValueMustBeValidMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {PropertyName} should be not empty..
        /// </summary>
        internal static string SizeValidatorPathValueMustNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("SizeValidatorPathValueMustNotNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TransferKind value must be Copy or Move. By default it is Copy..
        /// </summary>
        internal static string TransferKindValidatorTypeValueShouldBeValidMessage {
            get {
                return ResourceManager.GetString("TransferKindValidatorTypeValueShouldBeValidMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The file was writen successfully..
        /// </summary>
        internal static string WriteHandlerSuccessfullyWriten {
            get {
                return ResourceManager.GetString("WriteHandlerSuccessfullyWriten", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Data should be not empty..
        /// </summary>
        internal static string WriteValidatorDataValueShouldNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("WriteValidatorDataValueShouldNotNullOrEmptyMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Entity should be not empty..
        /// </summary>
        internal static string WriteValidatorEntityValueShouldNotNullOrEmptyMessage {
            get {
                return ResourceManager.GetString("WriteValidatorEntityValueShouldNotNullOrEmptyMessage", resourceCulture);
            }
        }
    }
}
