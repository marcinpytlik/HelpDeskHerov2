using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HelpDeskHero.Application.Tickets.Dtos;
using HelpDeskHero.IntegrationTests.Common;
using Xunit;
namespace HelpDeskHero.IntegrationTests.Tickets;

[Collection(IntegrationTestCollection.Name)]
public sealed class TicketsApiTests:IntegrationTestBase
{
    private readonly HttpClient _client;

public TicketsApiTests(IntegrationTestFixture fixture)
        : base(fixture)
    {
        _client = Client;
    }
   

    [Fact]
    public async Task CreateTicket_Should_Return_Created_Ticket()
    {
        var request = new CreateTicketRequest
        {
            TicketTypeId = 1,
            OrganizationUnitId = null,
            Title = "Integration test ticket",
            Description = "Ticket created from integration test.",
            Priority = "High"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/tickets",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var ticket = await response.Content.ReadFromJsonAsync<TicketDetailsDto>();

        ticket.Should().NotBeNull();
        ticket!.Id.Should().BeGreaterThan(0);
        ticket.Number.Should().NotBeNullOrWhiteSpace();
        ticket.Title.Should().Be(request.Title);
        ticket.Priority.Should().Be("High");
    }

    [Fact]
    public async Task GetTicketById_Should_Return_Ticket()
    {
        var created = await CreateTicketAsync();

        var response = await _client.GetAsync($"/api/tickets/{created.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var ticket = await response.Content.ReadFromJsonAsync<TicketDetailsDto>();

        ticket.Should().NotBeNull();
        ticket!.Id.Should().Be(created.Id);
        ticket.Number.Should().Be(created.Number);
    }

    [Fact]
    public async Task SearchTickets_Should_Return_Tickets_List()
    {
        await CreateTicketAsync("Ticket visible on list");

        var response = await _client.GetAsync("/api/tickets?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tickets = await response.Content
            .ReadFromJsonAsync<List<TicketListItemDto>>();

        tickets.Should().NotBeNull();
        tickets!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateTicket_Without_Title_Should_Return_BadRequest()
    {
        var request = new CreateTicketRequest
        {
            TicketTypeId = 1,
            OrganizationUnitId = null,
            Title = "",
            Description = "Invalid ticket.",
            Priority = "High"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/tickets",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("validation_error");
        content.Should().Contain("Title");
    }

    [Fact]
    public async Task ChangeState_With_Invalid_Transition_Should_Return_BadRequest()
    {
        var ticket = await CreateTicketAsync();

        var request = new ChangeTicketStateRequest
        {
            ToStateId = 1,
            Comment = "Invalid transition test."
        };

        var response = await _client.PostAsJsonAsync(
            $"/api/tickets/{ticket.Id}/change-state",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("workflow_transition_not_allowed");
    }

    [Fact]
    public async Task AddComment_Should_Add_History_Entry()
    {
        var ticket = await CreateTicketAsync();

        var commentRequest = new AddCommentRequest
        {
            Body = "Comment from integration test.",
            IsInternal = true
        };

        var commentResponse = await _client.PostAsJsonAsync(
            $"/api/tickets/{ticket.Id}/comments",
            commentRequest);

        commentResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var historyResponse = await _client.GetAsync(
            $"/api/tickets/{ticket.Id}/history");

        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await historyResponse.Content
            .ReadFromJsonAsync<List<TicketHistoryItemDto>>();

        history.Should().NotBeNull();
        history!.Should().Contain(x =>
            x.EventType == "CommentAdded"
            && x.Comment == commentRequest.Body);
    }

    [Fact]
    public async Task DeleteTicket_Should_Hide_Ticket_From_Details()
    {
        var ticket = await CreateTicketAsync();

        var deleteResponse = await _client.DeleteAsync(
            $"/api/tickets/{ticket.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync(
            $"/api/tickets/{ticket.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RestoreTicket_Should_Make_Ticket_Available_Again()
    {
        var ticket = await CreateTicketAsync();

        var deleteResponse = await _client.DeleteAsync(
            $"/api/tickets/{ticket.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var restoreResponse = await _client.PostAsync(
            $"/api/tickets/{ticket.Id}/restore",
            content: null);

        restoreResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync(
            $"/api/tickets/{ticket.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateTicket_Should_Return_New_RowVersion()
    {
        var ticket = await CreateTicketAsync();

        var request = new UpdateTicketRequest
        {
            Title = "Updated from integration test",
            Description = "Updated description.",
            Priority = "High",
            RowVersion = ticket.RowVersion
        };

        var response = await _client.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}",
            request);
var body = await response.Content.ReadAsStringAsync();
Console.WriteLine(body);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<TicketDetailsDto>();

        updated.Should().NotBeNull();
        updated!.Title.Should().Be(request.Title);
        updated.RowVersion.Should().NotBeNullOrWhiteSpace();
        updated.RowVersion.Should().NotBe(ticket.RowVersion);
    }

    [Fact]
    public async Task UpdateTicket_With_Old_RowVersion_Should_Return_Conflict()
    {
        var ticket = await CreateTicketAsync();

        var firstUpdate = new UpdateTicketRequest
        {
            Title = "First update",
            Description = "First update wins.",
            Priority = "High",
            RowVersion = ticket.RowVersion
        };

        var firstResponse = await _client.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}",
            firstUpdate);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondUpdate = new UpdateTicketRequest
        {
            Title = "Second update with old row version",
            Description = "This should fail.",
            Priority = "High",
            RowVersion = ticket.RowVersion
        };

        var secondResponse = await _client.PutAsJsonAsync(
            $"/api/tickets/{ticket.Id}",
            secondUpdate);

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var content = await secondResponse.Content.ReadAsStringAsync();

        content.Should().Contain("concurrency_conflict");
    }

private async Task<TicketDetailsDto> CreateTicketAsync(
    string title = "Integration test ticket")
{
    var request = new CreateTicketRequest
    {
        TicketTypeId = 1,
        OrganizationUnitId = null,
        Title = title,
        Description = "Created by integration test.",
        Priority = "High"
    };

    var createResponse = await _client.PostAsJsonAsync(
        "/api/tickets",
        request);

    createResponse.EnsureSuccessStatusCode();

    var created = await createResponse.Content.ReadFromJsonAsync<TicketDetailsDto>();

    created.Should().NotBeNull();
    created!.Id.Should().BeGreaterThan(0);

    var getResponse = await _client.GetAsync(
        $"/api/tickets/{created.Id}");

    getResponse.EnsureSuccessStatusCode();

    var ticket = await getResponse.Content.ReadFromJsonAsync<TicketDetailsDto>();

    ticket.Should().NotBeNull();
    ticket!.RowVersion.Should().NotBeNullOrWhiteSpace();

    return ticket;
}
}
