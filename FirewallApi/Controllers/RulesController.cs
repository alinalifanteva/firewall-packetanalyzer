using FirewallDb.Data;
using FirewallDb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FirewallApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase // базовый контроллер для работы с апи
{
    private readonly AppDbContext _context; // приватный контекст

    public RulesController(AppDbContext context)
    {
        _context = context;
    }

    //get api/rules асинхронный для работы с бд
    [HttpGet]
    public async Task<IActionResult> GetRules()
    {
        // методы из базы данных
        var rules = await _context.Rules.ToListAsync(); // вернуть лист
        return Ok(rules); // список вернуть
    }

    // c json-телом, джсон превратится в объект Rule создаст метод и в сохранится в БД
    [HttpPost]
    public async Task<IActionResult> CreateRule([FromBody] Rule rule)
    {
        if (rule == null) return BadRequest("Rulle cannot be null");

        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetRules), new { id = rule.Id }, rule);
    }
}