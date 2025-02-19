namespace CheckersLone.Models
{
    public class Player
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
        public string? Color { get; set; }
        public string? ConnectionId { get; set; }

        public ICollection<Game> GameHistory { get; set; }
    }
}
