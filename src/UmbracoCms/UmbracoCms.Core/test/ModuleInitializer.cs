using System.Runtime.CompilerServices;
using Argon;
using Umbraco.Compose.Integrations.UmbracoCms.Core.Tests.Unit.TestUtils;

namespace Umbraco.Compose.Integrations.UmbracoCms.Core.Tests.Unit;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        Verifier.UseSourceFileRelativeDirectory("snapshots");
        VerifierSettings.AddExtraSettings(s => s.TypeNameHandling = TypeNameHandling.Auto);
        VerifierSettings.DontIgnoreEmptyCollections();
        VerifierSettings.AddExtraSettings(_ =>
            _.Converters.Add(new JsonSchemaConverter())
        );
        VerifySystemJson.Initialize();
    }
}
