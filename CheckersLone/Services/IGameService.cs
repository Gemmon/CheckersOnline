using CheckersLone.Models;

namespace CheckersLone.Services
{
    public interface IGameService
    {
        Task MakeMove(int playerId, int fromX, int fromY, int toX, int toY);
        Board GetBoardState();
    }
}
