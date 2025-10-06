using TestProject.Core.AgentWorkflowAggregate;

namespace TestProject.Infrastructure.Data.Config;

public class WorkflowRunConfiguration : IEntityTypeConfiguration<WorkflowRun>
{
  public void Configure(EntityTypeBuilder<WorkflowRun> builder)
  {
    builder.HasKey(w => w.Id);

    builder.Property(w => w.UserId)
      .IsRequired()
      .HasMaxLength(DataSchemaConstants.DefaultNameLength);

    builder.Property(w => w.ETWDetails)
      .IsRequired();

    builder.Property(w => w.CurrentStepName)
      .HasMaxLength(DataSchemaConstants.DefaultNameLength);

    builder.Property(w => w.ErrorMessage);

    builder.Property(w => w.Status)
      .HasConversion<string>()
      .IsRequired();

    builder.HasMany(w => w.Steps as IEnumerable<WorkflowStep>)
      .WithOne()
      .HasForeignKey(s => s.WorkflowRunId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}
