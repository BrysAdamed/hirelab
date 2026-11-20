using Donations.Application;
using Microsoft.AspNetCore.Mvc;

namespace Donations.Api.Controllers;

public class DonationRequestController : ControllerBase
{
    private ICommandDispatcher _commands;

    public DonationRequestController(ICommandDispatcher commands)
    {
        _commands = commands;
    }

    [HttpPost("CreateDonationRequest")]
    public async Task<IActionResult> Create(CreateRequestCommand command)
    {
        Guid id = await _commands.SendAsync(command);
        return Ok(id);
    }

    [HttpPost("ProcessDonationRequest")]
    public async Task<IActionResult> Process(ProcessRequestCommand command)
    {
        await _commands.SendAsync(command);
        return Ok();
    }
}
