namespace FlowSynx.Application.Features.WorkflowExecutions.Command.ExecuteWorkflow;

public class WorkflowTaskPosition
{
    public WorkflowTaskPosition(double x, double y)
    {
        if (double.IsNaN(x) || double.IsInfinity(x))
            throw new ArgumentOutOfRangeException(nameof(x), "X must be a finite number.");
        if (double.IsNaN(y) || double.IsInfinity(y))
            throw new ArgumentOutOfRangeException(nameof(y), "Y must be a finite number.");

        X = x;
        Y = y;
    }

    public double X { get; set; }
    public double Y { get; set; }
}