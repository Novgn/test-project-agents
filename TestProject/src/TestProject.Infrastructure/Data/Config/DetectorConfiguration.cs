using TestProject.Core.DetectorAggregate;

namespace TestProject.Infrastructure.Data.Config;

public class DetectorConfiguration : IEntityTypeConfiguration<Detector>
{
  public void Configure(EntityTypeBuilder<Detector> builder)
  {
    builder.HasKey(d => d.Id);

    builder.Property(d => d.Name)
      .IsRequired()
      .HasMaxLength(DataSchemaConstants.DefaultNameLength);

    builder.Property(d => d.Description)
      .IsRequired();

    builder.Property(d => d.ETWProvider)
      .IsRequired()
      .HasMaxLength(DataSchemaConstants.DefaultNameLength);

    builder.Property(d => d.ConverterName)
      .IsRequired()
      .HasMaxLength(DataSchemaConstants.DefaultNameLength);

    builder.Property(d => d.BranchName)
      .HasMaxLength(DataSchemaConstants.DefaultNameLength);

    builder.Property(d => d.PullRequestUrl);

    builder.Property(d => d.Status)
      .IsRequired()
      .HasMaxLength(50);
  }
}
