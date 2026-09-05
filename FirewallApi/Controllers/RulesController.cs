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
    private readonly DateTime _startTime;

    public RulesController(AppDbContext context)
    {
        _context = context;
        _startTime = DateTime.UtcNow;
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

    [HttpGet("metrics")] // статистика правил
    public async Task<IActionResult> GetMetrics()
    {
        var total = await _context.Rules.CountAsync();
        var allowCount = await _context.Rules.CountAsync(r => r.Action == RuleAction.ALLOW);
        var dennyCount = await _context.Rules.CountAsync(r => r.Action == RuleAction.DENY);

        return Ok(new
        {
            TotalRules = total,
            AllowRules = allowCount,
            DenyRules = dennyCount,
            Uptime = DateTime.UtcNow - _startTime // добавть стартайм
        }
            );
    }

    [HttpGet("{id}")] // найти правило по id
    public async Task<IActionResult> GetRule(int id)
    {
        var rule = await _context.Rules.FindAsync(id);
        if (rule == null)
            return NotFound($"Rule with ID {id} not found");
        return Ok(rule);
    }

    [HttpPut("{id}")] // обновление правила
    public async Task<IActionResult> UpdateRule(int id, [FromBody] Rule updatedRule)
    {
        if (id != updatedRule.Id)
            return BadRequest("ID mismatch");

        var existingRule = await _context.Rules.FindAsync(id);
        if (existingRule == null)
            return NotFound($"Rule with ID {id} not found");

        existingRule.Priority = updatedRule.Priority;
        existingRule.Action = updatedRule.Action;
        existingRule.Ip = updatedRule.Ip;
        existingRule.IpMask = updatedRule.IpMask;
        existingRule.PortStart = updatedRule.PortStart;
        existingRule.PortEnd = updatedRule.PortEnd;
        existingRule.Protocol = updatedRule.Protocol;
        existingRule.Description = updatedRule.Description;
        existingRule.CreatedBy = updatedRule.CreatedBy;

        await _context.SaveChangesAsync();
        return Ok(existingRule);
    }

    [HttpDelete("id")] // удалить правило по /api/rules/id
    public async Task<IActionResult> DeleteRule(int id)
    {
        var rule = await _context.Rules.FindAsync(id);
        if (rule == null)
            return NotFound($"Rule with ID {id} not found");

        _context.Rules.Remove(rule);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("allow")]
    public async Task<IActionResult> CreateAllowRule([FromBody] Rule rule)
    {
        rule.Action = RuleAction.ALLOW;
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetRule), new { id = rule.Id }, rule);
    }

    [HttpPost("block")] // быстрый эндпоинт
    public async Task<IActionResult> CreateBlockRule([FromBody] Rule rule)
    {
        rule.Action = RuleAction.DENY;
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetRule),  new { id = rule.Id }, rule);
    }
}