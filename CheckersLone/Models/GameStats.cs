namespace CheckersLone.Models
{
    public class GameStats
    {
        public int Id { get; set; }
        public string Color { get; set; }
        public int Wins { get; set; }

        public DateTime Date { get; set; }
    }
}
