namespace CheckersLone.Models
{
    public class CheckersGame
    {
        public string[,] Board { get; private set; } = new string[8, 8];
        public string CurrentPlayer { get; private set; } = "white"; 

        public CheckersGame()
        {
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if ((row + col) % 2 == 1)
                        Board[row, col] = "black";
                }
            }

            for (int row = 5; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    if ((row + col) % 2 == 1)
                        Board[row, col] = "white";
                }
            }
        }

        public bool Move(int fromX, int fromY, int toX, int toY)
        {
            if (!IsValidMove(fromX, fromY, toX, toY))
                return false;

            Board[toX, toY] = Board[fromX, fromY];
            Board[fromX, fromY] = null;
            CurrentPlayer = CurrentPlayer == "white" ? "black" : "white"; 
            return true;
        }

        private bool IsValidMove(int fromX, int fromY, int toX, int toY)
        {
            if (fromX < 0 || fromX >= 8 || fromY < 0 || fromY >= 8 ||
                toX < 0 || toX >= 8 || toY < 0 || toY >= 8)
                return false;

            var piece = Board[fromX, fromY];
            if (piece == null || piece != CurrentPlayer)
                return false;

            if (Board[toX, toY] != null)
                return false;

            int direction = piece == "white" ? 1 : -1;
            if (toX - fromX == direction && Math.Abs(toY - fromY) == 1)
                return true;

            return false;
        }
    }

}
