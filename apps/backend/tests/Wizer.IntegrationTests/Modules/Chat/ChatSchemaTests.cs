using Wizer.Api.Modules.Chat.Entities;
using Wizer.Api.Shared.Auth;
using Wizer.IntegrationTests.Infrastructure;

namespace Wizer.IntegrationTests.Modules.Chat;

[Collection(PostgresCollection.Name)]
public class ChatSchemaTests(PostgresFixture fixture)
{
    [Fact]
    public async Task InsertMessage_SameClientMessageIdTwice_ViolatesIdempotencyKey()
    {
        // BR-CHAT-08: a resend is stored once.
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var schoolClass = await builder.ClassAsync();
        var sender = await builder.UserAsync();
        var channel = new ChatChannel { ClassId = schoolClass.Id, Type = ChatChannelType.Class };
        await fixture.SaveAsync(channel);
        var clientMessageId = Guid.NewGuid();
        await fixture.SaveAsync(NewMessage(channel.Id, sender.Id, clientMessageId));

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(NewMessage(channel.Id, sender.Id, clientMessageId));

        Assert.Equal("ux_chat_messages_sender_id_client_message_id", constraint);
    }

    [Fact]
    public async Task InsertChannel_TeamChannelWithoutTeam_ViolatesOwnerCheck()
    {
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var schoolClass = await builder.ClassAsync();

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(
            new ChatChannel { ClassId = schoolClass.Id, Type = ChatChannelType.Team });

        Assert.Equal("ck_chat_channels_owner", constraint);
    }

    [Fact]
    public async Task InsertAccessGrant_SecondPendingForTeam_ViolatesSinglePending()
    {
        // BR-CHAT-15: one pending request per team.
        var builder = await new SchemaBuilder(fixture).WithInstitutionAsync();
        var schoolClass = await builder.ClassAsync();
        var team = await builder.TeamAsync(schoolClass.Id);
        var teacher = await builder.UserAsync(PlatformRole.Teacher);
        await fixture.SaveAsync(new ChatAccessGrant { TeamId = team.Id, TeacherId = teacher.Id, Status = ChatAccessStatus.Pending });

        var constraint = await fixture.SaveAndGetViolatedConstraintAsync(
            new ChatAccessGrant { TeamId = team.Id, TeacherId = teacher.Id, Status = ChatAccessStatus.Pending });

        Assert.Equal("ux_chat_access_grants_team_id_pending", constraint);
    }

    private static ChatMessage NewMessage(Guid channelId, Guid senderId, Guid clientMessageId) => new()
    {
        ChannelId = channelId,
        SenderId = senderId,
        ClientMessageId = clientMessageId,
        Content = "Hola",
    };
}
