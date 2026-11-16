namespace Tests.YouTube;
[TestClass]
public sealed class ConnectionValidatorTests : TestBase
{
    [TestMethod]
    public async Task ValidateConnection_AcceptsValidCredentials()
    {
        var result = await new ConnectionValidator(InvocationContext).ValidateConnection(InvocationContext.AuthenticationCredentialsProviders, CancellationToken.None);
        Assert.IsTrue(result.IsValid, result.Message);
    }

    [TestMethod]
    public async Task ValidateConnection_RejectsInvalidCredentials()
    {
        var result = await new ConnectionValidator(InvocationContext).ValidateConnection([new("api_key", "invalid-test-key")], CancellationToken.None);
        Assert.IsFalse(result.IsValid);
    }
}
