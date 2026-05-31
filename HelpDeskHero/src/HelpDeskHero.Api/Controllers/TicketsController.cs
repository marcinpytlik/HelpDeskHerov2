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
    public IActionResult GetById(int id)
    {
        return Ok(new { Id = id });
    }
}