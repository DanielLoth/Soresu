using Microsoft.SqlServer.Dac.Deployment;
using Microsoft.SqlServer.Dac.Extensibility;

namespace Soresu;

[ExportDeploymentPlanModifier("Soresu", "1.0.0.0", PlatformCompatibility = TSqlPlatformCompatibility.Sql130)]
public class SoresuDeploymentPlanModifier : DeploymentPlanModifier
{
    protected override void OnExecute(DeploymentPlanContributorContext context)
    {
        //DeploymentPlanHandle planHandle = context.PlanHandle;
        //DeploymentStep lastStep = planHandle.Tail;

        //base.AddBefore(planHandle, lastStep, lastStep);
        //base.AddAfter(planHandle, lastStep, lastStep);
        //base.Remove(planHandle, lastStep);

        throw new NotImplementedException();
    }
}
