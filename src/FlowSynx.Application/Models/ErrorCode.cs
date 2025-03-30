namespace FlowSynx.Application.Models;

public enum ErrorCode
{
    None = 0,

    #region Application error codes
    ApplicationEndpoint = 1,
    ApplicationLocation = 2,
    ApplicationVersion = 3,
    ApplicationTriggerProcessing = 4,
    ApplicationHealthCheck = 5,
    ApplicationConfigureServer = 6,
    ApplicationOpenApiService = 7,
    #endregion

    #region Logging error codes
    LogsList = 1001,
    LogGetItem = 1002,
    LogAdd = 1003,
    LoggerCreation = 1004,
    #endregion

    #region Auding error codes
    AuditsApplying = 1101,
    AuditsGetList = 1102,
    AuditGetItem = 1103,
    #endregion

    #region Plugin Configuration error codes
    PluginConfiguration = 1201,
    PluginConfigurationNotFound = 1202,
    PluginConfigurationIsAlreadyExist = 1203,
    PluginConfigurationList = 1204,
    PluginConfigurationGetItem = 1205,
    PluginConfigurationCheckExistence = 1206,
    PluginConfigurationAdd = 1207,
    PluginConfigurationUpdate = 1208,
    PluginConfigurationDelete = 1209,
    #endregion

    #region Plugins error codes
    PluginNotFound = 1301,
    PluginTypeNotFound = 1302,
    PluginSpecificationsInvalid = 1303,
    PluginTypeGetItem = 1304,
    #endregion

    #region Serialization error codes
    Serialization = 1402,
    #endregion

    #region Validation error codes
    InputValidation = 1501,
    #endregion

    #region Database error codes
    DatabaseConnection = 1601,
    DatabaseCreation = 1602,
    DatabaseDataSeeder = 1603,
    DatabaseSaveData = 1604,
    DatabaseDeleteData = 1605,
    DatabaseModelCreating = 1606,
    DatabaseTransaction = 1607,
    #endregion

    #region Security error codes
    SecurityGetUserId = 1701,
    SecurityGetUserName = 1702,
    SecurityCheckIsAuthenticated = 1703,
    SecurityGetUserRoles = 1704,
    SecurityBasicAuthenticationMustHaveUniqueNames = 1705,
    SecurityInitializedError = 1706,
    SecurityAthenticationIsRequired = 1750,
    #endregion

    #region Workflow error codes
    WorkflowNotFound = 1801,
    WorkflowMustBeNotEmpty = 1802,
    WorkflowNameMustHaveValue = 1803,
    WorkflowHasDuplicateNames = 1804,
    WorkflowMissingDependencies = 1805,
    WorkflowCyclicDependencies = 1806,
    WorkflowsGetList = 1807,
    WorkflowGetItem = 1808,
    WorkflowCheckExistence = 1809,
    WorkflowAdd = 1810,
    WorkflowUpdate = 1811,
    WorkflowDelete = 1812,
    WorkflowsGetExecutionList = 1813,
    WorkflowGetExecutionItem = 1814,
    WorkflowExecutionCheckExistence = 1815,
    WorkflowExecutionAdd = 1816,
    WorkflowExecutionUpdate = 1817,
    WorkflowExecutionDelete = 1818,
    WorkflowTaskExecutionsList = 1819,
    WorkflowGetTaskExecutionItem = 1820,
    WorkflowTaskExecutionAdd = 1821,
    WorkflowTaskExecutionUpdate = 1822,
    WorkflowTaskExecutionDelete = 1823,
    WorkflowTriggersList = 1824,
    WorkflowActiveTriggersList = 1825,
    WorkflowGetTriggerItem = 1826,
    WorkflowTriggersAdd = 1827,
    WorkflowTriggersUpdate = 1828,
    WorkflowTriggersDelete = 1829,
    #endregion

    #region Behavior error codes
    BehaviorUnhandledException = 2001,
    BehaviorPerformanceLongRunning = 2002,
    BehaviorPerformanceError = 2003,
    #endregion

    #region Unknown error codes
    UnknownError = 9999
    #endregion
}