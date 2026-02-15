using FlowSynx.Domain.Activities;

namespace FlowSynx.Application.Models;

public class ActivityJson
{
    public string ApiVersion { get; set; } = "activity/v1";
    public string Kind { get; set; } = "Activity";
    public ActivityMetadata Metadata { get; set; } = new ActivityMetadata();
    public ActivitySpecification Specification { get; set; } = new ActivitySpecification();
}