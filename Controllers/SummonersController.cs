 using Microsoft.AspNetCore.Mvc;

namespace dotnet_react_typescript.Controllers;

[ApiController]
[Route("[controller]")]
public class SummonersController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<SummonersController> _logger;

    public SummonersController(ILogger<SummonersController> logger)
    {
        _logger = logger;
    }

    [HttpGet("{name}")]
    public async Task<Summoner> Get(string name)
    {
        var summoner = await Summoner.GetSummoner(name);
        return summoner;
    }
}
