using Microsoft.SqlServer.Dac;
using Microsoft.SqlServer.Dac.Deployment;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Soresu.Test;

public class UnitTest1
{
    [Theory]
    //[InlineData(
    //    "create table A (Id int primary key);",
    //    "create table A (Id int primary key, C1 int not null default 0);",
    //    typeof(AlterTableAddTableElementStatement))]
    [InlineData(
        """
        create table A (
            Id int primary key,
            C1 int not null
                constraint CK1 check (C1=1)
                constraint UQ1 unique
                constraint DF1 default 0,
            C2 int not null
                constraint CK2 check (C2=1)
                constraint DF2 default 0
                constraint UQ2 unique,
            constraint AK1 unique (C1),
            constraint AK1ToBeDropped unique (C1),
            constraint TCK1 check (C1=1),
            constraint TCK1ToBeDropped check (C1=1),
            constraint AK2 unique (C1, C2),
            constraint TCK2 check (C1=C2)
        );
        """,
        """
        create table A (
            Id int primary key,
            C1 bigint not null
                constraint CK1 check (C1=1)
                constraint UQ1 unique
                constraint DF1 default 0,
            C2 bigint not null
                constraint CK2 check (C2=1)
                constraint DF2 default 0
                constraint UQ2 unique,
            constraint AK1 unique (C1),
            constraint TCK1 check (C1=1),
            constraint AK2 unique (C1, C2),
            constraint TCK2 check (C1=C2)
        );
        """,
        typeof(AlterTableAlterColumnStatement))]
    public void Test1(string currentSql, string targetSql, Type type)
    {
        using var sourcePackage = GetDacPackage(targetSql);
        using var targetPackage = GetDacPackage(currentSql);

        var dacDeployOptions = new DacDeployOptions
        {
            AllowTableRecreation = false
        };

        dacDeployOptions.SetVariable("Environment", "Production");
        dacDeployOptions.SetVariable("Intent", ReleaseIntent.CreateIndexOnly.ToString());

        var report = DacServices.GenerateDeployReport(sourcePackage, targetPackage, "MyDatabase", dacDeployOptions);
        var script = DacServices.GenerateDeployScript(sourcePackage, targetPackage, "MyDatabase", dacDeployOptions);

        var fragment = Parse(script);
        var statements = GetStatements(script, type).FirstOrDefault();
    }

    [Fact]
    public void Test2()
    {
        var sql = "alter table dbo.A alter column C1 int not null with (online = on);";
        var fragment = Parse(sql);
    }

    private static TSqlModel GetTSqlModel(string sql)
    {
        var model = new TSqlModel(SqlServerVersion.Sql130, null);

        model.AddObjects(sql);

        var validationErrors = model.Validate();
        if (validationErrors.Any())
        {
            throw new InvalidOperationException("Warnings or errors present in model");
        }

        return model;
    }

    private static TSqlFragment Parse(string sql)
    {
        var parser = new TSql130Parser(false, SqlEngineType.Standalone);
        using var sr = new StringReader(sql);

        return parser.Parse(sr, out _);
    }

    private static TSqlStatement[] GetStatements<T>(string sql)
        => GetStatements(sql, typeof(T));

    private static TSqlStatement[] GetStatements(string sql, Type type)
    {
        var fragment = (TSqlScript)Parse(sql);

        var statements = fragment.Batches
            .SelectMany(x => x.Statements)
            .Where(x => x.GetType() == type)
            .ToArray();

        return statements;
    }
    private static DacPackage GetDacPackage(string sql)
    {
        var model = GetTSqlModel(sql);

        var stream = new MemoryStream();

        var packageMetadata = new PackageMetadata()
        {
            Name = "MyPackage",
            Description = "My description",
            Version = "1.0.0.0"
        };

        var packageOptions = new PackageOptions
        {
            DeploymentContributors = new List<DeploymentContributorInformation>
            {
                new DeploymentContributorInformation
                {
                    ExtensionId = "Soresu",
                    Version = Version.Parse("1.0.0.0")
                }
            }
        };

        DacPackageExtensions.BuildPackage(stream, model, packageMetadata, packageOptions);

        var package = DacPackage.Load(stream);

        return package;
    }
}
