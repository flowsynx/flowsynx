namespace FlowSynx.Application.Models;

public enum ErrorCode
{
    None                                            = 0,

    #region Application error codes
    ApplicationStartArgumentIsRequired              = 1001,
    ApplicationEndpoint                             = 1002,
    ApplicationLocation                             = 1003,
    ApplicationVersion                              = 1004,
    ApplicationTriggerProcessing                    = 1005,
    ApplicationHealthCheck                          = 1006,
    ApplicationConfigureServer                      = 1007,
    ApplicationOpenApiService                       = 1008,
    #endregion

    #region Behavior error codes
    BehaviorUnhandledException                      = 1201,
    BehaviorPerformanceLongRunning                  = 1202,
    BehaviorPerformanceError                        = 1203,
    #endregion

    #region Serialization error codes
    Serialization                                   = 1302,
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
    SecurityAthenticationIsRequired                 = 1550,
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
    #endregion

    #region Auding error codes
    AuditsApplying                                  = 2101,
    AuditsGetList                                   = 2102,
    AuditGetItem                                    = 2103,
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
    PluginTypeGetItem                               = 2304,
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
    #endregion

    #region Unknown error codes
    UnknownError                                    = 9999
    #endregion
}