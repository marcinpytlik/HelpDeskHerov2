using HelpDeskHero.Application.Tickets;
using HelpDeskHero.Application.Tickets.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskHero.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketApplicationService _tickets;

    public TicketsController(ITicketApplicationService tickets)
    {
        _tickets = tickets;
    }

    [HttpPost]
    public async Task<ActionResult<TicketDetailsDto>> Create(
        CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tickets.CreateAsync(request, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDetailsDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _tickets.GetByIdAsync(id, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TicketListItemDto>>> Search(
        [FromQuery] TicketSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tickets.SearchAsync(request, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:int}/change-state")]
    public async Task<ActionResult<TicketDetailsDto>> ChangeState(
        int id,
        ChangeTicketStateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tickets.ChangeStateAsync(
            id,
            request,
            cancellationToken);

        return Ok(result);
    }
}