using FastEndpoints;
using FluentValidation;

namespace test_project_agents.Web.Contributors;

/// <summary>
/// See: https://fast-endpoints.com/docs/validation
/// </summary>
public class GetContributorValidator : Validator<GetContributorByIdRequest>
{
  public GetContributorValidator()
  {
    RuleFor(x => x.ContributorId)
      .GreaterThan(0);
  }
}
