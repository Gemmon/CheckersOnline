using Microsoft.AspNetCore.Mvc.RazorPages;
using CheckersLone.Models;
using CheckersLone.Services;
using Microsoft.EntityFrameworkCore;

public class PrivacyModel : PageModel
{
    private readonly GameService _gameService;

    public PrivacyModel(GameService gameService)
    {
        _gameService = gameService;
    }

    public List<Player> Players { get; set; }

    public List<Game> Games { get; set; }
    public void OnGet()
    {
        Players = _gameService.GetAllPlayers();
        Games = _gameService.GetAllGames();
    }
}
