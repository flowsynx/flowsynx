namespace FlowSynx.Domain.Primitives;

public enum ErrorCode
{
    None                                            = 0,

    #region Application error codes
    ApplicationStartArgumentIsRequired              = 1001,
    ApplicationEndpoint                             = 1002,
    PluginsLocation                                 = 1003,
    ApplicationVersion                              = 1004,
    ApplicationTriggerProcessing                    = 1005,
    ApplicationHealthCheck                          = 1006,
    ApplicationConfigureServer                      = 1007,
    ApplicationOpenApiService                       = 1008,
    #endregion

    #region Behavior error codes
    #endregion

    #region Serialization error codes
    Serialization                                   = 1301,
    DeserializerEmptyValue                          = 1302,
    SerializerEmptyValue                            = 1303,
    #endregion

    #region Validation error codes
    InputValidation                                 = 1401,
    #endregion

    #region Security error codes
    SecurityGetUserId                               = 1501,
    SecurityGetUserName                             = 1502,
    SecurityCheckIsAuthenticated                    = 1503,
    SecurityGetUserRoles                            = 1504,
    SecurityBasicAuthenticationMustHaveUniqueNames  = 1505,
    SecurityInitializedError                        = 1506,
    SecurityAuthenticationIsRequired                = 1550,
    SecurityConfigurationInvalidScheme              = 1506,
    #endregion

    #region Database error codes
    DatabaseConnection                              = 1601,
    DatabaseCreation                                = 1602,
    DatabaseDataSeeder                              = 1603,
    DatabaseSaveData                                = 1604,
    DatabaseDeleteData                              = 1605,
    DatabaseModelCreating                           = 1606,
    DatabaseTransaction                             = 1607,
    #endregion

    #region Logging error codes
    LogsList                                        = 2001,
    LogGetItem                                      = 2002,
    LogAdd                                          = 2003,
    LoggerCreation                                  = 2004,
    LoggerTemplateInvalidProperty                   = 2005,
    LoggerConfigurationInvalidProviderName          = 2006,
    #endregion

    #region Auding error codes
    AuditsApplying                                  = 2101,
    AuditsGetList                                   = 2102,
    AuditGetItem                                    = 2103,
    AuditNotFound                                   = 2104,
    #endregion

    #region Plugins error codes
    PluginNotFound                                  = 2301,
    PluginTypeNotFound                              = 2302,
    PluginSpecificationsInvalid                     = 2303,
    PluginsGetList                                  = 2304,
    PluginGetItem                                   = 2305,
    PluginTypeGetItem                               = 2306,
    PluginCheckExistence                            = 2307,
    PluginRegistryFailedToFetchDataFromUrl          = 2308,
    PluginRegistryPluginNotFound                    = 2309,
    PluginAdd                                       = 2310,
    PluginDelete                                    = 2311,
    PluginInstall                                   = 2312,
    PluginUninstall                                 = 2313,
    PluginInstallationNotFound                      = 2314,
    PluginLoader                                    = 2315,
    PluginCouldNotLoad                              = 2316,
    PluginChecksumValidationFailed                  = 2317,
    PluginCompatibility                             = 2318,
    PluginTypeShouldHaveValue                       = 2319,
    PluginTypeConfigShouldHaveValue                 = 2320,
    PluginTypeInvalidInput                          = 2321,
    PluginRegistryPluginVersionsNotFound            = 2322,
    #endregion

    #region ExpressionParser
    ExpressionParserKeyNotFound = 2601,
    #endregion

    #region Secret error codes
    SecretConfigurationInvalidProviderName = 2701,
    #endregion

    #region Database
    DatabaseProviderNotSupported           = 2801,
    #endregion

    #region AI error codes
    AIConfigurationInvalidProviderName     = 3001,
    AIAgentExecutionFailed                   = 3002,
    #endregion

    #region Notification error codes
    NotificationConfigurationInvalidProviderName = 3101,
    #endregion

    #region Unknown error codes
    UnknownError = 9999
    #endregion
}
