using System.ComponentModel;
using TestProject.Core.Interfaces;

namespace TestProject.Infrastructure.Services.Agents.Tools;

/// <summary>
/// Function tool that allows AIAgent to query Kusto for detector patterns and examples
/// </summary>
public class KustoQueryTool(IKustoQueryService kustoService, ILogger<KustoQueryTool> logger)
{
  [Description("Queries the Kusto database to find existing detector patterns, converters, and best practices for ETW detectors")]
  public async Task<string> QueryDetectorPatterns(
    [Description("The ETW provider ID to search for")] string providerId,
    [Description("Optional: The rule ID or detector name to search for")] string? ruleId = null)
  {
    try
    {
      logger.LogInformation("AIAgent calling Kusto query tool for provider {ProviderId}", providerId);

      var result = await kustoService.FindConvertersAndDetectorsAsync(
        providerId,
        ruleId ?? "",
        CancellationToken.None);

      var response = $@"Found {result.Converters.Length} converters and {result.ExistingDetectors.Length} detectors.

**Converters:**
{string.Join("\n", result.Converters.Select(c => $"- {c}"))}

**Existing Detectors:**
{string.Join("\n", result.ExistingDetectors.Select(d => $"- {d}"))}

**Recommended Configuration:**
{string.Join("\n", result.RecommendedConfiguration.Select(kvp => $"- {kvp.Key}: {kvp.Value}"))}";


      return response;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error querying Kusto from AIAgent tool");
      return $"Error querying Kusto: {ex.Message}";
    }
  }
}
