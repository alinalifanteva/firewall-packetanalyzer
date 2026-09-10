using FirewallDb.Data;
using FirewallDb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using FirewallApi.Services;

namespace FirewallApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase // базовый контроллер для работы с апи
{
    private readonly AppDbContext _context; // приватный контекст
    private static readonly DateTime _startTime = DateTime.UtcNow;
    private readonly IptablesService _iptablesService;

    public RulesController(AppDbContext context, IptablesService iptablesService)
    {
        _context = context;
        _iptablesService = iptablesService;
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
        if (rule == null) return BadRequest("Rule cannot be null");

        // 1. Сохраняем в БД
        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();
        var rules = await _context.Rules.OrderBy(r => r.Priority).ToListAsync(); // все поавила по приоритету
        // 2. Применяем в iptables (если правило активно)
        await _iptablesService.SyncRulesAsync(rules); // апитайблес с бд

        // 3. Возвращаем ответ
        return CreatedAtAction(nameof(GetRules), new { id = rule.Id }, rule);
    }

    [HttpGet("metrics")] // статистика правил
    public async Task<IActionResult> GetMetrics()
    {
        var total = await _context.Rules.CountAsync();
        var allowCount = await _context.Rules.CountAsync(r => r.Action == RuleAction.ALLOW);
        var dennyCount = await _context.Rules.CountAsync(r => r.Action == RuleAction.DENY);
        var (cpuUsage, ramUsageMB) = GetSystemMetrics();
        return Ok(new
        {
            TotalRules = total,
            AllowRules = allowCount,
            DenyRules = dennyCount,
            Uptime = DateTime.UtcNow - _startTime, // добавть стартайм
            CpuUsage = cpuUsage,
            RamUsageMB = ramUsageMB
        });
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

        var rules = await _context.Rules.OrderBy(r => r.Priority).ToListAsync();
        await _iptablesService.SyncRulesAsync(rules);
         
        return Ok(existingRule);
    }

    [HttpDelete("{id}")] // удалить правило по /api/rules/id
    public async Task<IActionResult> DeleteRule(int id)
    {
        var rule = await _context.Rules.FindAsync(id);
        if (rule == null)
            return NotFound($"Rule with ID {id} not found");

        _context.Rules.Remove(rule);
        await _context.SaveChangesAsync();

        var rules = await _context.Rules.OrderBy(r => r.Priority).ToListAsync();
        await _iptablesService.SyncRulesAsync(rules);

        return NoContent();
    }

    [HttpPost("allow")]
    public async Task<IActionResult> CreateAllowRule([FromBody] Rule rule)
    {
        if (rule == null) return BadRequest("Rule cannot be null");

        rule.Action = RuleAction.ALLOW;

        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        var rules = await _context.Rules.OrderBy(r => r.Priority).ToListAsync();
        await _iptablesService.SyncRulesAsync(rules);

        return CreatedAtAction(nameof(GetRule), new { id = rule.Id }, rule);
    }

    [HttpPost("block")]
    public async Task<IActionResult> CreateBlockRule([FromBody] Rule rule)
    {
        if (rule == null) return BadRequest("Rule cannot be null");

        rule.Action = RuleAction.DENY;

        _context.Rules.Add(rule);
        await _context.SaveChangesAsync();

        var rules = await _context.Rules.OrderBy(r => r.Priority).ToListAsync();
        await _iptablesService.SyncRulesAsync(rules);

        return CreatedAtAction(nameof(GetRule), new { id = rule.Id }, rule);
    }

    private (double Cpu, double Ram) GetSystemMetrics()
    {
        var process = Process.GetCurrentProcess();
        var cpuTime = process.TotalProcessorTime.TotalSeconds;
        var elapsedTime = (DateTime.UtcNow - _startTime).TotalSeconds;
        double cpuUsage = elapsedTime > 0
        ? (cpuTime / (elapsedTime * Environment.ProcessorCount)) * 100
        : 0;

        double ramUsageMB = process.WorkingSet64 / (1024.0 * 1024.0);
        return (Math.Round(cpuUsage, 2), Math.Round(ramUsageMB, 2));
    }

    [HttpGet("/metrics")] // формат для прометеуса, эндпоинт для метрик в правильном формате
    public async Task<IActionResult> PrometheusMetrics()
    {
        var total = await _context.Rules.CountAsync();
        var allowCount = await _context.Rules.CountAsync(r => r.Action == RuleAction.ALLOW);
        var denyCount = await _context.Rules.CountAsync(r => r.Action == RuleAction.DENY);
        var (cpuUsage, ramUsageMB) = GetSystemMetrics();
        var uptime = (DateTime.UtcNow - _startTime).TotalSeconds;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# HELP total_rules Total number of rules");
        sb.AppendLine("# TYPE total_rules gauge");
        sb.AppendLine($"total_rules {total}");
        sb.AppendLine("# HELP allow_rules Number of ALLOW rules");
        sb.AppendLine("# TYPE allow_rules gauge");
        sb.AppendLine($"allow_rules {allowCount}");
        sb.AppendLine("# HELP deny_rules Number of DENY rules");
        sb.AppendLine("# TYPE deny_rules gauge");
        sb.AppendLine($"deny_rules {denyCount}");
        sb.AppendLine("# HELP cpu_usage_percent CPU usage in percent");
        sb.AppendLine("# TYPE cpu_usage_percent gauge");
        sb.AppendLine($"cpu_usage_percent {cpuUsage}");
        sb.AppendLine("# HELP ram_usage_mb RAM usage in MB");
        sb.AppendLine("# TYPE ram_usage_mb gauge");
        sb.AppendLine($"ram_usage_mb {ramUsageMB}");
        sb.AppendLine("# HELP uptime_seconds Uptime in seconds");
        sb.AppendLine("# TYPE uptime_seconds gauge");
        sb.AppendLine($"uptime_seconds {uptime:F2}");

        return Content(sb.ToString(), "text/plain");
    }

}