using System.Diagnostics;
using System.Threading.Tasks;
using FirewallDb.Models;
using System.Linq;

namespace FirewallApi.Services;

public class IptablesService
{
    private readonly string _iptablesPath = "/usr/sbin/iptables";

    //public async Task AddRuleAsync(string ip, int port, string protocol, string action)
    //{
    //    string target = action == "ALLOW" ? "ACCEPT" : "DROP";
    //    string command = $"-I INPUT -s {ip} -p {protocol} --dport {port} -j {target}";
    //    await ExecuteIptablesCommandAsync(command);
    //}

    //public async Task RemoveRuleAsync(string ip, int port, string protocol, string action)
    //{
    //    string target = action == "ALLOW" ? "ACCEPT" : "DROP";
    //    string command = $"-D INPUT -s {ip} -p {protocol} --dport {port} -j {target}";
    //    await ExecuteIptablesCommandAsync(command);
    //}

    private async Task ExecuteIptablesCommandAsync(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _iptablesPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"iptables command failed: {error}");
        }
    }
    public async Task SyncRulesAsync(List<Rule> rules)
    {
        await ExecuteIptablesCommandAsync("-F INPUT"); // очистить цепочку инпут - удалить все правила
        // правила в порядке приоритета, сортировка в контроллере
        foreach (var rule in rules.OrderBy(r => r.Priority))
        {
            string target = rule.Action == RuleAction.ALLOW ? "ACCEPT" : "DROP";
            string ip = rule.Ip ?? "0.0.0.0/0";
            int port = rule.PortStart ?? 0;
            string protocol = rule.Protocol.ToString().ToLower();
            string command = $"-A INPUT -s {ip} -p {protocol} --dport {port} -j {target}";
            await ExecuteIptablesCommandAsync(command); // теперь нужна синхронизация после каждого изменения БД
        }
    }
}