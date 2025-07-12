namespace FlowSynx.Application.Models;

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
    #endregion

    #region Auding error codes
    AuditsApplying                                  = 2101,
    AuditsGetList                                   = 2102,
    AuditGetItem                                    = 2103,
    AuditNotFound                                   = 2104,
    #endregion

    #region Plugin Configuration error codes
    PluginConfiguration                             = 2201,
    PluginConfigurationNotFound                     = 2202,
    PluginConfigurationIsAlreadyExist               = 2203,
    PluginConfigurationList                         = 2204,
    PluginConfigurationGetItem                      = 2205,
    PluginConfigurationCheckExistence               = 2206,
    PluginConfigurationAdd                          = 2207,
    PluginConfigurationUpdate                       = 2208,
    PluginConfigurationDelete                       = 2209,
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
    #endregion

    #region Workflow error codes
    WorkflowNotFound                                = 2401,
    WorkflowMustBeNotEmpty                          = 2402,
    WorkflowNameMustHaveValue                       = 2403,
    WorkflowHasDuplicateNames                       = 2404,
    WorkflowMissingDependencies                     = 2405,
    WorkflowCyclicDependencies                      = 2406,
    WorkflowsGetList                                = 2407,
    WorkflowGetItem                                 = 2408,
    WorkflowCheckExistence                          = 2409,
    WorkflowAdd                                     = 2410,
    WorkflowUpdate                                  = 2411,
    WorkflowDelete                                  = 2412,
    WorkflowsGetExecutionList                       = 2413,
    WorkflowGetExecutionItem                        = 2414,
    WorkflowExecutionCheckExistence                 = 2415,
    WorkflowExecutionAdd                            = 2416,
    WorkflowExecutionUpdate                         = 2417,
    WorkflowExecutionDelete                         = 2418,
    WorkflowTaskExecutionsList                      = 2419,
    WorkflowGetTaskExecutionItem                    = 2420,
    WorkflowTaskExecutionAdd                        = 2421,
    WorkflowTaskExecutionUpdate                     = 2422,
    WorkflowTaskExecutionDelete                     = 2423,
    WorkflowTriggersList                            = 2424,
    WorkflowActiveTriggersList                      = 2425,
    WorkflowGetTriggerItem                          = 2426,
    WorkflowTriggersAdd                             = 2427,
    WorkflowTriggersUpdate                          = 2428,
    WorkflowTriggersDelete                          = 2429,
    WorkflowFailedDependenciesTask                  = 2430,
    WorkflowFailedExecution                         = 2431,
    WorkflowExecutionInitilizeFailed                = 2432,
    WorkflowTaskExecutionCanceled                   = 2433,
    WorkflowExecutionCanceled                       = 2434,
    WorkflowCancellationRegistry                    = 2435,
    WorkflowExecutionNotFound                       = 2436,
    WorkflowExecutionTaskNotFound                   = 2437,
    WorkflowTriggerNotFound                         = 2438,
    #endregion

    #region ExpressionParser
    ExpressionParserOutputNotFound = 2601,
    #endregion

    #region Unknown error codes
    UnknownError                                    = 9999
    #endregion
}