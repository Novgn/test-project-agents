namespace TestProject.Core.DetectorAggregate;

public class DetectorResult : EntityBase<Guid>
{
  public Guid DetectorId { get; private set; }
  public DateTime ExecutedAt { get; private set; }
  public int EventsProcessed { get; private set; }
  public int AnomaliesDetected { get; private set; }
  public double ExecutionTimeMs { get; private set; }
  public string ResultSummary { get; private set; } = string.Empty;
  public string? RawData { get; private set; }

  private DetectorResult() { } // EF Core

  public DetectorResult(
    Guid detectorId,
    int eventsProcessed,
    int anomaliesDetected,
    double executionTimeMs,
    string resultSummary,
    string? rawData = null)
  {
    Id = Guid.NewGuid();
    DetectorId = Guard.Against.Default(detectorId, nameof(detectorId));
    EventsProcessed = Guard.Against.Negative(eventsProcessed, nameof(eventsProcessed));
    AnomaliesDetected = Guard.Against.Negative(anomaliesDetected, nameof(anomaliesDetected));
    ExecutionTimeMs = Guard.Against.Negative(executionTimeMs, nameof(executionTimeMs));
    ResultSummary = Guard.Against.NullOrEmpty(resultSummary, nameof(resultSummary));
    RawData = rawData;
    ExecutedAt = DateTime.UtcNow;
  }
}
