using Microsoft.SqlServer.Dac.Deployment;
using Microsoft.SqlServer.Dac.Extensibility;
using Microsoft.SqlServer.TransactSql.ScriptDom;

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

        var arguments = context.Arguments;
        var options = context.Options;

        var variables = context.Options.SqlCommandVariableValues;
        
        if (!variables.TryGetValue("Intent", out var intentAsString) ||
            !Enum.TryParse<ReleaseIntent>(intentAsString, out var intent))
        {
            throw new InvalidOperationException("A value indicating intent must be specified, and it must be valid");
        }

        if (options.AllowTableRecreation && intent != ReleaseIntent.RecreateTable)
        {
            throw new InvalidOperationException("Cannot recreate table when release intent does not indicate this should happen");
        }

        var visitor = new Visitor();

        var next = context.PlanHandle.Head;
        while (next is not null)
        {
            if (next is DeploymentScriptDomStep domStep &&
                domStep.Script is not null)
            {
                domStep.Script.Accept(visitor);
            }

            next = next.Next;
        }

        PublishMessage(new ExtensibilityError("Message", Severity.Message));

        //throw new NotImplementedException();
    }
}

public class Visitor : TSqlConcreteFragmentVisitor
{
    public override void ExplicitVisit(AlterTableAlterColumnStatement node)
    {
        var existing = node.Options.OfType<OnlineIndexOption>().FirstOrDefault();

        if (existing is not null)
        {
            existing.OptionState = OptionState.On;
            return;
        }

        var option = new OnlineIndexOption
        {
            OptionKind = IndexOptionKind.Online,
            OptionState = OptionState.On
        };

        node.Options.Add(option);
    }
}

public enum ReleaseIntent
{
    CreateIndexOnly,
    DropObjectOnly,
    RecreateTable,
    Standard
}
