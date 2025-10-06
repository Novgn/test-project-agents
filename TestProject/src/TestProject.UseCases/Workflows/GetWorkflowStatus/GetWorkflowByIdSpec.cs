namespace TestProject.UseCases.Workflows.GetWorkflowStatus;

public class GetWorkflowByIdSpec : Specification<WorkflowRun>
{
  public GetWorkflowByIdSpec(Guid workflowId)
  {
    Query
      .Where(w => w.Id == workflowId)
      .Include(w => w.Steps.OrderBy(s => s.Sequence));
  }
}
