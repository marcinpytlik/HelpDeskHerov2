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
    [HttpPost("{id:int}/comments")]
public async Task<IActionResult> AddComment(
    int id,
    AddCommentRequest request,
    CancellationToken cancellationToken)
{
    await _tickets.AddCommentAsync(
        id,
        request,
        cancellationToken);

    return NoContent();
}
[HttpGet("{id:int}/history")]
public async Task<ActionResult<IReadOnlyList<TicketHistoryItemDto>>> GetHistory(
    int id,
    CancellationToken cancellationToken)
{
    var result = await _tickets.GetHistoryAsync(
        id,
        cancellationToken);

    return Ok(result);
}
[HttpDelete("{id:int}")]
public async Task<IActionResult> Delete(
    int id,
    CancellationToken cancellationToken)
{
    await _tickets.DeleteAsync(id, cancellationToken);

    return NoContent();
}
[HttpPost("{id:int}/restore")]
public async Task<IActionResult> Restore(
    int id,
    CancellationToken cancellationToken)
{
    await _tickets.RestoreAsync(id, cancellationToken);

    return NoContent();
}
}