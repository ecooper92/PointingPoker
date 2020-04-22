using System;
using System.Linq;
using System.Threading.Tasks;

namespace PointingPoker.Data
{
    public class WeatherForecastService
    {
        public event Action<WeatherForecast[]> OnForcastUpdate;

        public WeatherForecastService()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(2000);
                    OnForcastUpdate?.Invoke(GenerateForcasts(DateTime.Now));
                }
            });
        }

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public Task<WeatherForecast[]> GetForecastAsync(DateTime startDate)
        {
            return Task.FromResult(GenerateForcasts(startDate));
        }

        private WeatherForecast[] GenerateForcasts(DateTime startDate)
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = startDate.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            }).ToArray();
        }
    }
}
