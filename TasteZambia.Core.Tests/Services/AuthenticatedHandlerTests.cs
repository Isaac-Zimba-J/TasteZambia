using System.Net;
using TasteZambia.Core.Data.Http;
using TasteZambia.Core.Services;

namespace TasteZambia.Core.Tests.Services;

public class AuthenticatedHandlerTests
{
    private sealed class FakeSession : IAuthSession
    {
        public string? AccessToken { get; set; } = "token-1";
        public int Refreshes;
        public Task EnsureSignedInAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> RefreshAsync(CancellationToken ct = default)
        {
            Refreshes++;
            AccessToken = "token-2";
            return Task.FromResult(true);
        }
    }

    private sealed class FailingSession : IAuthSession
    {
        public string? AccessToken => "token-1";
        public Task EnsureSignedInAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> RefreshAsync(CancellationToken ct = default) => Task.FromResult(false);
    }

    /// <summary>Answers 401 to "token-1" and 200 to any other token.</summary>
    private sealed class ScriptedServer : HttpMessageHandler
    {
        public List<string?> SeenTokens { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var token = request.Headers.Authorization?.Parameter;
            SeenTokens.Add(token);
            return Task.FromResult(new HttpResponseMessage(token == "token-1" ? HttpStatusCode.Unauthorized : HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task AttachesTheBearerToken()
    {
        var session = new FakeSession { AccessToken = "token-2" };
        var server = new ScriptedServer();
        var client = new HttpClient(new AuthenticatedHandler(session) { InnerHandler = server });

        var response = await client.GetAsync("http://api/anything");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["token-2"], server.SeenTokens);
    }

    [Fact]
    public async Task On401_RefreshesOnceAndRetriesOnce()
    {
        var session = new FakeSession();
        var server = new ScriptedServer();
        var client = new HttpClient(new AuthenticatedHandler(session) { InnerHandler = server });

        var response = await client.PostAsync("http://api/anything", new StringContent("{\"a\":1}"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, session.Refreshes);
        Assert.Equal(["token-1", "token-2"], server.SeenTokens);
    }

    [Fact]
    public async Task IfRefreshFails_The401IsReturnedNotLooped()
    {
        var server = new ScriptedServer();
        var client = new HttpClient(new AuthenticatedHandler(new FailingSession()) { InnerHandler = server });

        var response = await client.GetAsync("http://api/anything");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Single(server.SeenTokens);
    }
}
