 using Microsoft.AspNetCore.Mvc;

namespace dotnet_react_typescript.Controllers;

[ApiController]
[Route("[controller]")]
public class GamesController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<GamesController> _logger;

    public GamesController(ILogger<GamesController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IEnumerable<Games>> Get()
    {
        // return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        // {
        //     Date = DateTime.Now.AddDays(index),
        //     TemperatureC = Random.Shared.Next(-20, 55),
        //     Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        // })
        // .ToArray();
        var summoner = Summoner.GetSummoner("rtseuztz");
        var games = await Games.getGames(summoner.Puuid);
        return games;
    }
}
