using CheckersLone.Data;
using CheckersLone.Hubs;
using CheckersLone.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CheckersLone.Services
{
    public class GameService
    {

        public List<List<string>> Board { get; private set; }
        public string CurrentPlayer { get; private set; } = "red"; 

        private bool continueCapture = false;

        private int captureX, captureY; 

        private readonly IHubContext<GameHub> _hubContext;

        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public GameService(IHubContext<GameHub> hubContext, IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
            _hubContext = hubContext;
            InitializeBoard();
        }



        public void InitializeBoard()
        {
            Board = new List<List<string>>();
            for (int row = 0; row < 8; row++)
            {
                var rowList = new List<string>();
                for (int col = 0; col < 8; col++)
                {
                    if (row < 3 && (row + col) % 2 == 1)
                        rowList.Add("red");
                    else if (row > 4 && (row + col) % 2 == 1)
                        rowList.Add("blue");
                    else
                        rowList.Add(null);
                }
                Board.Add(rowList);
            }
        }



        public bool MakeMove(int fromX, int fromY, int toX, int toY)
        {
            if (continueCapture && (fromX != captureX || fromY != captureY)) 
                return false; 

            string currentPlayer = Board[fromX][fromY];

            if (currentPlayer == null || Board[toX][toY] != null || !currentPlayer.StartsWith(CurrentPlayer))
                return false;


            int dx = toX - fromX;
            int dy = toY - fromY;
           
            if (currentPlayer.EndsWith("_king"))
            {
                if (Math.Abs(dx) == Math.Abs(dy)) 
                {
                    int stepX = dx > 0 ? 1 : -1;
                    int stepY = dy > 0 ? 1 : -1;

                    bool foundEnemy = false; 
                    int x = fromX + stepX, y = fromY + stepY;

                    int enemyX = -1, enemyY = -1; 

                    while (x != toX && y != toY)
                    {
                        if (Board[x][y] != null)
                        {
                            if (Board[x][y].StartsWith(CurrentPlayer)) 
                                return false;

                            if (foundEnemy) 
                                return false;

                            foundEnemy = true;
                            enemyX = x; 
                            enemyY = y;
                        }

                        x += stepX;
                        y += stepY;
                    }

                    if (foundEnemy) 
                    {
                        Board[enemyX][enemyY] = null; 
                    }

                    Board[toX][toY] = currentPlayer;
                    Board[fromX][fromY] = null;

                    ChangeTurn();
                    if (IsGameOver())
                    {
                        return true; 
                    }
                    return true;
                }
            }

            
            if (Math.Abs(dx) == 1 && Math.Abs(dy) == 1 && !continueCapture)
            {
                
                if ((currentPlayer == "red" && dx == 1) || (currentPlayer == "blue" && dx == -1))
                {
                    Board[toX][toY] = currentPlayer;
                    Board[fromX][fromY] = null;
                    CheckForPromotion(toX, toY);
                    ChangeTurn();
                    if (IsGameOver())
                    {
                        return true; 
                    }
                    return true;
                }
            }

            
            if (Math.Abs(dx) == 2 && Math.Abs(dy) == 2)
            {
                int middleX = fromX + dx / 2;
                int middleY = fromY + dy / 2;

                if (Board[middleX][middleY] != null && Board[middleX][middleY] != currentPlayer)
                {
                    Board[toX][toY] = currentPlayer;
                    Board[fromX][fromY] = null;
                    Board[middleX][middleY] = null; 
                    CheckForPromotion(toX, toY);
                    if (CanCapture(toX, toY))
                    {
                        continueCapture = true; 
                        captureX = toX; 
                        captureY = toY;
                        return true;
                    }

                    ChangeTurn();
                    if (IsGameOver())
                    {
                        return true; 
                    }
                    continueCapture = false;
                    return true;
                }
            }

            return false;
        }
        private void CheckForPromotion(int x, int y)
        {
            if (Board[x][y] == "red" && x == 7)
                Board[x][y] = "red_king";
            if (Board[x][y] == "blue" && x == 0)
                Board[x][y] = "blue_king";
        }
        private void ChangeTurn()
        {
            CurrentPlayer = CurrentPlayer == "red" ? "blue" : "red";
            continueCapture = false; 
        }

        private bool CanCapture(int fromX, int fromY) 
        { int[] dx = { 1, 1, -1, -1 };
          int[] dy = { 1, -1, 1, -1 };
          string currentPlayer = Board[fromX][fromY];
          for (int dir = 0; dir < 4; dir++) 
          { 
                int x = fromX + dx[dir];
                int y = fromY + dy[dir];
                if (x + dx[dir] >= 0 && y + dy[dir] >= 0 && x + dx[dir] < 8 && y + dy[dir] < 8)
                {
                    if (Board[x][y] != null && !Board[x][y].StartsWith(currentPlayer) && Board[x + dx[dir]][y + dy[dir]] == null) 
                        return true;
                }
            } 
            return false;
        }

        public List<(int, int)> GetValidMoves(int fromX, int fromY)
        {
            var moves = new List<(int, int)>();
            string currentPlayer = Board[fromX][fromY];

            if (currentPlayer == null)
                return moves;

           
            int[] dx = { 1, 1, -1, -1 };
            int[] dy = { 1, -1, 1, -1 };

            
            for (int i = 0; i < 4; i++)
            {
                int toX = fromX + dx[i];
                int toY = fromY + dy[i];

               
                if (toX >= 0 && toY >= 0 && toX < 8 && toY < 8 && Board[toX][toY] == null)
                {
                    if ((currentPlayer == "red" && dx[i] == 1) || (currentPlayer == "blue" && dx[i] == -1) || currentPlayer.EndsWith("_king"))
                        moves.Add((toX, toY));
                }

               
                toX = fromX + 2 * dx[i];
                toY = fromY + 2 * dy[i];
                int middleX = fromX + dx[i];
                int middleY = fromY + dy[i];

                if (toX >= 0 && toY >= 0 && toX < 8 && toY < 8 && Board[toX][toY] == null && Board[middleX][middleY] != null && !Board[middleX][middleY].StartsWith(currentPlayer))
                {
                    moves.Add((toX, toY));
                }
            }

           
            if (currentPlayer.EndsWith("_king"))
            {
               
                for (int i = 0; i < 4; i++)
                {
                    int step = 1;
                    while (true)
                    {
                        int toX = fromX + dx[i] * step;
                        int toY = fromY + dy[i] * step;

                       
                        if (toX < 0 || toY < 0 || toX >= 8 || toY >= 8)
                            break;

                        
                        if (Board[toX][toY] == null)
                        {
                            moves.Add((toX, toY));
                        }
                        
                        else if (Board[toX][toY] != null && !Board[toX][toY].StartsWith(currentPlayer))
                        {
                            
                            int afterX = toX + dx[i];
                            int afterY = toY + dy[i];
                            if (afterX >= 0 && afterY >= 0 && afterX < 8 && afterY < 8 && Board[afterX][afterY] == null)
                            {
                                moves.Add((afterX, afterY));
                            }
                            break; 
                        }
                       
                        else
                        {
                            break;
                        }
                        step++;
                    }
                }
            }

            return moves;
        }



        public bool IsGameOver()
        {
            
            bool hasRedPieces = Board.Any(row => row.Any(cell => cell?.StartsWith("red") == true));
            bool hasBluePieces = Board.Any(row => row.Any(cell => cell?.StartsWith("blue") == true));

            if (!hasRedPieces)
            {
                EndGame("blue");
                return true;
            }

            if (!hasBluePieces)
            {
                EndGame("red");
                return true;
            }

           
            bool opponentCanMove = false;

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if (Board[row][col]?.StartsWith(CurrentPlayer == "red" ? "blue" : "red") == true)
                    {
                        var validMoves = GetValidMoves(row, col);
                        if (validMoves.Count > 0)
                        {
                            opponentCanMove = true;
                            break;
                        }
                    }
                }
                if (opponentCanMove)
                    break;
            }

            if (!opponentCanMove)
            {
                EndGame(CurrentPlayer); 
                return true;
            }

            return false;
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
            UpdateGameStats(winner);


            _hubContext.Clients.All.SendAsync("GameOver", winner);
        }
        private void UpdateGameStats(string winnerColor)
        {
            
            using var context = _dbContextFactory.CreateDbContext();

            
            var gameStat = new GameStats
            {
                Color = winnerColor,
                Wins = 1, 
                Date = DateTime.Now  
            };

            
            context.GameStats.Add(gameStat);

          
            context.SaveChanges();
        }



        public void RestartGame()
        {
            
            InitializeBoard();

            
            CurrentPlayer = "red"; 
            continueCapture = false; 

           
            Console.WriteLine("Gra została zrestartowana.");
            _hubContext.Clients.All.SendAsync("GameRestarted", Board, CurrentPlayer);
        }


        public async Task<object> Login(string playerName, string playerPassword)
        {
           
            using var dbContext = _dbContextFactory.CreateDbContext();
            var player = dbContext.Players.SingleOrDefault(p => p.Name == playerName);

            if (player != null && player.Password == playerPassword)
            {

                
                return new { success = true };
            }

           
            return new { success = false };
        }

        public List<Player> GetAllPlayers()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.Players.ToList();
        }

        public List<Game> GetAllGames()
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            return dbContext.Games.ToList();
        }

    }


}
