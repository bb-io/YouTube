namespace Tests.YouTube;
[TestClass]
public sealed class AssemblyLifecycle : TestBase
{
    [AssemblyCleanup]
    public static async Task RemoveCreatedTestResources() => await new AssemblyLifecycle().Cleanup();
}
