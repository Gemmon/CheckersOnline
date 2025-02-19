using CheckersLone.Data;
using CheckersLone.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CheckersLone.Pages
{
    public class StatisticsModel : PageModel
    {
        private readonly AppDbContext _context;

        public StatisticsModel(AppDbContext context)
        {
            _context = context;
        }

        public List<GameStats> GameStats { get; set; }
        public int RedWins { get; set; }
        public int BlueWins { get; set; }
        public double RedWinPercentage { get; set; }
        public double BlueWinPercentage { get; set; }
        public string BackgroundColor { get; set; }
        public string BackgroundGradient { get; set; }
        public string TextColor { get; set; }  // Dodajemy property na kolor tekstu

        public void OnGet()
        {

            GameStats = _context.GameStats.ToList();
            int totalGames = _context.GameStats.Count();
            if (totalGames > 0)
            {
                RedWinPercentage = (double)_context.GameStats.Count(gs => gs.Color == "red") / totalGames * 100;
                BlueWinPercentage = (double)_context.GameStats.Count(gs => gs.Color == "blue") / totalGames * 100;
            }



            RedWins = _context.GameStats.Count(gs => gs.Color == "red");
            BlueWins = _context.GameStats.Count(gs => gs.Color == "blue");


            // Obliczamy t³o na podstawie procentów
            BackgroundGradient = GetBackgroundGradient(RedWinPercentage, BlueWinPercentage);
            TextColor = GetTextColor(RedWinPercentage, BlueWinPercentage);

        }

        // Funkcja do obliczenia gradientu
        private string GetBackgroundGradient(double redPercentage, double bluePercentage)
        {
            // Ustal intensywnoœæ kolorów na podstawie procentu
            int redIntensity = (int)(255 * (redPercentage / 100));
            int blueIntensity = (int)(255 * (bluePercentage / 100));

            // Zwróæ wartoœæ gradientu CSS w formacie
            return $"linear-gradient(to right, rgb({redIntensity}, 0, 0), rgb(0, 0, {blueIntensity}))";
        }
        private string GetTextColor(double redPercentage, double bluePercentage)
        {
            if (redPercentage > bluePercentage)
            {
                return "white"; // Czerwony t³o -> bia³y tekst
            }
            else
            {
                return "black"; // Niebieski t³o -> czarny tekst
            }
        }
    }
}
