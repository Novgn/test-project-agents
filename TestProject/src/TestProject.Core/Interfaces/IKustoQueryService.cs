namespace TestProject.Core.Interfaces;

/// <summary>
/// Service for querying Azure Kusto (Data Explorer)
/// </summary>
public interface IKustoQueryService
{
  /// <summary>
  /// Queries Kusto to find appropriate converters and detectors for ETW provider using providerId and ruleId
  /// </summary>
  Task<KustoQueryResult> FindConvertersAndDetectorsAsync(string providerId, string ruleId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Fetches detector execution results from Kusto
  /// </summary>
  Task<DetectorExecutionResults> GetDetectorResultsAsync(string detectorName, DateTime since, CancellationToken cancellationToken = default);

  /// <summary>
  /// Analyzes detector performance metrics
  /// </summary>
  Task<PerformanceMetrics> AnalyzeDetectorPerformanceAsync(string detectorName, CancellationToken cancellationToken = default);
}

public record KustoQueryResult(
  string[] Converters,
  string[] ExistingDetectors,
  Dictionary<string, string> RecommendedConfiguration
);

public record DetectorExecutionResults(
  int TotalExecutions,
  int AnomaliesDetected,
  double AverageExecutionTimeMs,
  DateTime LastExecution,
  string[] SampleAnomalies
);

public record PerformanceMetrics(
  double SuccessRate,
  double AverageLatencyMs,
  int TotalEvents,
  Dictionary<string, int> ErrorCounts
);
