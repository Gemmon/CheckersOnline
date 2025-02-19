using CheckersLone.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using CheckersLone.Models;
using CheckersLone.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static CheckersLone.Data.AppDbContext;
using Microsoft.EntityFrameworkCore.Internal;


namespace CheckersLone.Hubs
{
   
    public class GameHub : Hub
    {
        private static Dictionary<string, string> PlayerColors = new Dictionary<string, string>();
    
        private static CheckersGame Game = new CheckersGame();
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        private readonly GameService _gameService;
        private readonly IServiceProvider _serviceProvider;

        public GameHub(GameService gameService, IServiceProvider serviceProvider, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
            _gameService = gameService;
            _serviceProvider = serviceProvider;
        }

        public async Task JoinGame(string gameId, string playerName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

         
            await Clients.Caller.SendAsync("UpdateBoard", _gameService.Board);
            await Clients.Caller.SendAsync("UpdateCurrentPlayer", _gameService.CurrentPlayer);

            
            await Clients.Caller.SendAsync("PlayerColors", PlayerColors);

            await Clients.Group(gameId).SendAsync("PlayerJoined", playerName);
        }

        public async Task ChooseColor(string color)
        {
            string connectionId = Context.ConnectionId;

            
            if (PlayerColors.Values.Contains(color))
            {
                await Clients.Caller.SendAsync("ColorAlreadyTaken");
                return;
            }
            using var dbContext = _dbContextFactory.CreateDbContext();

            
            var player = await dbContext.Players.FirstOrDefaultAsync(p => p.ConnectionId == connectionId);

            if (player == null)
            {
                await Clients.Caller.SendAsync("PlayerNotFound");
                return;
            }
            player.Color = color;
            dbContext.Players.Update(player);

            var currentGame = await dbContext.Games.OrderByDescending(g => g.Id)
                                            .FirstOrDefaultAsync(g => g.Wynik == null);

            if (currentGame == null)
            {
                
                currentGame = new Game();
                dbContext.Games.Add(currentGame);
            }

            
            if (color == "red")
            {
                currentGame.Player1name = player.Name;
            }
            else if (color == "blue")
            {
                currentGame.Player2name = player.Name;
            }
            await dbContext.SaveChangesAsync();

            PlayerColors[connectionId] = color;

            await Clients.Caller.SendAsync("ColorAssigned", color);
        }

        private async Task NotifyGameOver(string winner)
        {
            await Clients.All.SendAsync("GameOver", winner);
        }

        private static Dictionary<string, string> LoggedInUsers = new Dictionary<string, string>();

        public async Task Login(string playerName, string playerPassword)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var player = await dbContext.Players.FirstOrDefaultAsync(p => p.Name == playerName);


            if (player != null && player.Password != playerPassword)
            {
                await Clients.Caller.SendAsync("LoginResult", new { success = false, reason = "InvalidPassword" });
                return;
            }

            if (player == null)
            {
                
                player = new Player { Name = playerName, Password = playerPassword, ConnectionId = Context.ConnectionId };
                dbContext.Players.Add(player);
                await dbContext.SaveChangesAsync();

                await Clients.Caller.SendAsync("LoginResult", new { success = true, isNewUser = true });
                await Clients.Caller.SendAsync("LoginSuccess", true);
                player.ConnectionId = Context.ConnectionId;
                if (!LoggedInUsers.ContainsKey(Context.ConnectionId))
                {
                    LoggedInUsers.Add(Context.ConnectionId, playerName);
                }
                await Clients.All.SendAsync("UpdateLoggedInUsers", LoggedInUsers.Values.ToList());
                return;
            }

            player.ConnectionId = Context.ConnectionId; 
            dbContext.Players.Update(player);
            await dbContext.SaveChangesAsync();


            await Clients.Caller.SendAsync("LoginResult", new { success = true, isNewUser = false });
            await Clients.Caller.SendAsync("LoginSuccess", true);
           
            player.ConnectionId = Context.ConnectionId;
            if (!LoggedInUsers.ContainsKey(Context.ConnectionId))
            {
                LoggedInUsers.Add(Context.ConnectionId, playerName);
            }

            
            await Clients.All.SendAsync("UpdateLoggedInUsers", LoggedInUsers.Values.ToList());
        }
        public async Task EndGame(string winner)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var game = dbContext.Games.OrderByDescending(g => g.Id).FirstOrDefault();
            if (game != null)
            {
                game.Wynik = winner;
                await dbContext.SaveChangesAsync();
            }

            await Clients.All.SendAsync("GameOver", winner);
        }


        public async Task RestartGame()
        {
            _gameService.RestartGame();
            
            PlayerColors.Clear();

            
            await Clients.All.SendAsync("UpdateBoard", _gameService.Board);
            using var dbContext = _dbContextFactory.CreateDbContext();

           
            var playersWithColors = dbContext.Players.Where(p => p.Color != null).ToList();
            foreach (var player in playersWithColors)
            {
                player.Color = null; 
            }

            await dbContext.SaveChangesAsync(); 

            
            PlayerColors.Clear();

            
            await Clients.All.SendAsync("ColorSelectionReset");
            
            await Clients.All.SendAsync("UpdateCurrentPlayer", _gameService.CurrentPlayer);

            
            await Clients.All.SendAsync("GameRestarted");

            
            await Clients.All.SendAsync("ColorSelectionReset");

            await Clients.All.SendAsync("GameRestarted");
        }

        public async Task GetBoard()
        {
            await Clients.Caller.SendAsync("UpdateBoard", _gameService.Board);
        }

        public async Task MakeMove(int fromX, int fromY, int toX, int toY)
        {
            string connectionId = Context.ConnectionId;

            if (!PlayerColors.TryGetValue(connectionId, out string playerColor))
            {
                await Clients.Caller.SendAsync("NoColorSelected");
                return;
            }

            if (_gameService.Board[fromX][fromY] == null || !_gameService.Board[fromX][fromY].StartsWith(playerColor))
            {
                await Clients.Caller.SendAsync("InvalidMove");
                return;
            }

            if (_gameService.MakeMove(fromX, fromY, toX, toY))
            {
                await Clients.All.SendAsync("UpdateBoard", _gameService.Board);
                await Clients.All.SendAsync("UpdateCurrentPlayer", _gameService.CurrentPlayer);
            }
            else
            {
                await Clients.Caller.SendAsync("InvalidMove");
            }
        }

        public async Task<List<List<int>>> GetValidMoves(int fromX, int fromY)
        {
            var validMoves = _gameService.GetValidMoves(fromX, fromY);

            
            return validMoves.Select(move => new List<int> { move.Item1, move.Item2 }).ToList();
        }


        public override async Task OnDisconnectedAsync(Exception exception)
        {
            string connectionId = Context.ConnectionId;

            
            if (PlayerColors.TryGetValue(connectionId, out string color))
            {
                Console.WriteLine($"Klient {Context.ConnectionId} został rozłączony. Powód: {exception?.Message}");
                
                PlayerColors.Remove(connectionId);

                
                await Clients.All.SendAsync("ColorReleased", color);
            }

            await base.OnDisconnectedAsync(exception);
        }

       
        public async Task Ping()
        {
            Console.WriteLine($"Ping od klienta {Context.ConnectionId}");
            await Task.CompletedTask;
        }

        public async Task SendMessage(string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", message);
        }

    }

}
