using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldFaith.Server.Services.Lobby;

namespace WorldFaith.Server.Controllers;

[ApiController]
[Route("api/lobby")]
[Authorize]
public class LobbyController : ControllerBase
{
    private readonly ILobbyService _lobbyService;

    public LobbyController(ILobbyService lobbyService)
    {
        _lobbyService = lobbyService;
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms()
    {
        var list = await _lobbyService.GetLobbyListAsync();
        return Ok(list);
    }
}
