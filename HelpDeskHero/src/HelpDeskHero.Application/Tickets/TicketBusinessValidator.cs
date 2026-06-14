using HelpDeskHero.Application.Common;
using HelpDeskHero.Application.Tickets.Dtos;
using HelpDeskHero.Domain.Enums;

namespace HelpDeskHero.Application.Tickets;

public sealed class TicketBusinessValidator
{
    public void ValidateCreate(CreateTicketRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.TicketTypeId <= 0)
        {
            errors["TicketTypeId"] = ["Ticket type is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors["Title"] = ["Title is required."];
        }
        else if (request.Title.Length > 200)
        {
            errors["Title"] = ["Title cannot be longer than 200 characters."];
        }

        if (!Enum.TryParse<TicketPriority>(
                request.Priority,
                ignoreCase: true,
                out _))
        {
            errors["Priority"] = ["Priority has invalid value."];
        }

        if (errors.Count > 0)
        {
            throw new AppValidationException(errors);
        }
    }
}