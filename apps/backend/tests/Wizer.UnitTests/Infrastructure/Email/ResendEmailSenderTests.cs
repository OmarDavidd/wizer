using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Wizer.Api.Infrastructure.Email;

namespace Wizer.UnitTests.Infrastructure.Email;

public class ResendEmailSenderTests
{
    [Fact]
    public async Task SendAsync_PostsTheEmailToResendWithTheApiKey()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        var sender = CreateSender(handler, apiKey: "re_test_key");

        await sender.SendAsync(new EmailMessage("ana@uni.edu", "Asunto", "Cuerpo"), CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://api.resend.com/emails", handler.Request.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal("re_test_key", handler.Request.Headers.Authorization.Parameter);
        using var body = JsonDocument.Parse(handler.Body!);
        Assert.Equal("Wizer <no-reply@wizer.omard.space>", body.RootElement.GetProperty("from").GetString());
        Assert.Equal("ana@uni.edu", body.RootElement.GetProperty("to")[0].GetString());
        Assert.Equal("Asunto", body.RootElement.GetProperty("subject").GetString());
        Assert.Equal("Cuerpo", body.RootElement.GetProperty("text").GetString());
    }

    [Fact]
    public async Task SendAsync_ResendRejects_Throws()
    {
        var sender = CreateSender(new RecordingHandler(HttpStatusCode.UnprocessableEntity), apiKey: "re_test_key");

        await Assert.ThrowsAsync<HttpRequestException>(
            () => sender.SendAsync(new EmailMessage("ana@uni.edu", "Asunto", "Cuerpo"), CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_ApiKeyNotConfigured_ThrowsWithoutCallingResend()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        var sender = CreateSender(handler, apiKey: null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(new EmailMessage("ana@uni.edu", "Asunto", "Cuerpo"), CancellationToken.None));

        Assert.Contains("Email:ResendApiKey", exception.Message);
        Assert.Null(handler.Request);
    }

    private static ResendEmailSender CreateSender(HttpMessageHandler handler, string? apiKey) =>
        new(new SingleClientFactory(handler), Options.Create(new EmailOptions
        {
            From = "Wizer <no-reply@wizer.omard.space>",
            ResendApiKey = apiKey,
        }));

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class RecordingHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent("{}") };
        }
    }
}
