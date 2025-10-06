using TestProject.Core.AgentWorkflowAggregate;

namespace TestProject.Infrastructure.Data.Config;

public class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
  public void Configure(EntityTypeBuilder<WorkflowStep> builder)
  {
    builder.HasKey(s => s.Id);

    builder.Property(s => s.StepName)
      .IsRequired()
      .HasMaxLength(DataSchemaConstants.DefaultNameLength);

    builder.Property(s => s.Description)
      .IsRequired();

    builder.Property(s => s.StepType)
      .HasConversion<string>()
      .IsRequired();

    builder.Property(s => s.Status)
      .HasConversion<string>()
      .IsRequired();

    builder.Property(s => s.Input);
    builder.Property(s => s.Output);
    builder.Property(s => s.ErrorMessage);
  }
}
