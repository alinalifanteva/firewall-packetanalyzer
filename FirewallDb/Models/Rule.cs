using System;

namespace FirewallDb.Models;

public enum RuleAction { ALLOW, DENY }
public enum ProtocolType { TCP, UDP, ICMP }


public class Rule
{
    public int Id { get; set; }
    public int Priority { get; set; } // чем меньше число, тем раньше правило применяется
    public RuleAction Action { get; set; } = RuleAction.ALLOW;

    public string? Ip { get; set; }
    public string? IpMask { get; set; }

    public int? PortStart { get; set; }
    public int? PortEnd { get; set; }
    // диапазон портов - с одним портом 80, с диапазоном от 80 до 443, со всеми портами
    // выставить оба нулл - все порты, только порт старт только один порт, оба поля это диапазон
    public ProtocolType Protocol { get; set; } = ProtocolType.TCP;

    public string? Description {  get; set; } // описание правила
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; 
    // дата создания правила, сохраняем по серверу и можно преобразовать для локалки
    public string? CreatedBy { get; set; } // кто создал правило 

}