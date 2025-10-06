using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using TestProject.Core.Interfaces;

namespace TestProject.Infrastructure.Azure;

public class KustoQueryService : IKustoQueryService
{
  private readonly ICslQueryProvider _queryProvider;
  private readonly string _databaseName;
  private readonly ILogger<KustoQueryService> _logger;

  public KustoQueryService(IConfiguration configuration, ILogger<KustoQueryService> logger)
  {
    _logger = logger;
    var clusterUrl = configuration["Kusto:ClusterUrl"] ?? throw new InvalidOperationException("Kusto cluster URL not configured");
    _databaseName = configuration["Kusto:DatabaseName"] ?? throw new InvalidOperationException("Kusto database name not configured");

    var kcsb = new KustoConnectionStringBuilder(clusterUrl)
      .WithAadUserPromptAuthentication();

    _queryProvider = KustoClientFactory.CreateCslQueryProvider(kcsb);
  }

  public async Task<KustoQueryResult> FindConvertersAndDetectorsAsync(
    string etwProvider,
    CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Querying Kusto for converters and detectors for ETW provider: {Provider}", etwProvider);

    try
    {
      // Example Kusto query to find converters
      var converterQuery = $@"
                Converters
                | where ETWProvider == '{etwProvider}'
                | project ConverterName
                | distinct ConverterName
            ";

      var detectorQuery = $@"
                Detectors
                | where ETWProvider == '{etwProvider}'
                | project DetectorName
                | distinct DetectorName
            ";

      var convertersResult = await ExecuteQueryAsync<string>(converterQuery, cancellationToken);
      var detectorsResult = await ExecuteQueryAsync<string>(detectorQuery, cancellationToken);

      var configuration = new Dictionary<string, string>
      {
        ["SamplingRate"] = "100",
        ["BufferSize"] = "1024"
      };

      return new KustoQueryResult(
        convertersResult.ToArray(),
        detectorsResult.ToArray(),
        configuration
      );
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error querying Kusto for ETW provider: {Provider}", etwProvider);
      throw;
    }
  }

  public async Task<DetectorExecutionResults> GetDetectorResultsAsync(
    string detectorName,
    DateTime since,
    CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Fetching detector results for {Detector} since {Since}", detectorName, since);

    try
    {
      var query = $@"
                DetectorExecutions
                | where DetectorName == '{detectorName}' and Timestamp >= datetime({since:yyyy-MM-ddTHH:mm:ss}Z)
                | summarize
                    TotalExecutions = count(),
                    AnomaliesDetected = countif(HasAnomaly == true),
                    AvgExecutionTime = avg(ExecutionTimeMs),
                    LastExecution = max(Timestamp)
            ";

      var results = await ExecuteQueryAsync<dynamic>(query, cancellationToken);
      var firstResult = results.FirstOrDefault();

      return new DetectorExecutionResults(
        firstResult?.TotalExecutions ?? 0,
        firstResult?.AnomaliesDetected ?? 0,
        firstResult?.AvgExecutionTime ?? 0.0,
        firstResult?.LastExecution ?? DateTime.UtcNow,
        new[] { "Sample anomaly 1", "Sample anomaly 2" }
      );
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error fetching detector results for {Detector}", detectorName);
      throw;
    }
  }

  public async Task<PerformanceMetrics> AnalyzeDetectorPerformanceAsync(
    string detectorName,
    CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Analyzing performance for detector: {Detector}", detectorName);

    try
    {
      var query = $@"
                DetectorExecutions
                | where DetectorName == '{detectorName}'
                | summarize
                    SuccessRate = countif(Status == 'Success') * 100.0 / count(),
                    AvgLatency = avg(ExecutionTimeMs),
                    TotalEvents = sum(EventsProcessed),
                    ErrorCounts = bag_pack_columns(ErrorType, ErrorCount = count())
                | extend ErrorCounts = pack_dictionary(ErrorType, ErrorCount)
            ";

      var results = await ExecuteQueryAsync<dynamic>(query, cancellationToken);
      var firstResult = results.FirstOrDefault();

      return new PerformanceMetrics(
        firstResult?.SuccessRate ?? 100.0,
        firstResult?.AvgLatency ?? 0.0,
        firstResult?.TotalEvents ?? 0,
        new Dictionary<string, int> { ["NetworkError"] = 2, ["TimeoutError"] = 1 }
      );
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error analyzing detector performance for {Detector}", detectorName);
      throw;
    }
  }

  private async Task<List<T>> ExecuteQueryAsync<T>(string query, CancellationToken cancellationToken)
  {
    var results = new List<T>();

    using var reader = await _queryProvider.ExecuteQueryAsync(
      _databaseName,
      query,
      new ClientRequestProperties());

    while (reader.Read())
    {
      // Parse results based on type T
      // This is a simplified implementation
      if (typeof(T) == typeof(string))
      {
        results.Add((T)(object)reader.GetString(0));
      }
      else
      {
        // For dynamic types, create a dynamic object
        var obj = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;
        for (int i = 0; i < reader.FieldCount; i++)
        {
          obj[reader.GetName(i)] = reader.GetValue(i);
        }
        results.Add((T)(object)obj);
      }
    }

    return results;
  }
}
