namespace FlowSynx.Plugins.Azure.Files.Exceptions;

internal enum ErrorCodes
{
    AzureFilesShareNameMustNotEmpty                     = 11101,
    AzureFilesAccountKeyMustNotEmpty                    = 11102,
    AzureFilesAccountNameMustNotEmpty                   = 11103,
    AzureFilesPathMustBeNotEmpty                        = 11104,
    AzureFilesPathIsNotDirectory                        = 11105,
    AzureFilesThePathMustBeFile                         = 11106,
    AzureFilesThePathIsNotExist                         = 11107,
    AzureFilesResourceNotExist                          = 11108,
    AzureFilesShareItemNotFound                         = 11109,
    AzureFilesInvalidPathEntered                        = 11110,
    AzureFilesParentNotFound                            = 11111,
    AzureFilesSomethingWrongHappenedDuringProcessing    = 11112,
    AzureFilesFileIsAlreadyExistAndCannotBeOverwritten  = 11113,
    AzureFilesTheResourceNameContainsInvalidCharacters  = 11114,
}