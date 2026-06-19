using System;

/// <summary>
/// 命令类型枚举，按优先级从高到低排列。
/// </summary>
public enum CommandType
{
    None,
    CodeInput,   // MEM_INIT_* / EMPATHY_* / PROMETHEUS_*
    Kill9,       // kill -9
    Meta,        // whoami, who are you, 普罗米修斯, Prometheus, sorry
    General      // help, clear, 未知命令
}

public enum MetaCommandKind
{
    None,
    WhoAmI,      // whoami / who are you
    Prometheus,  // 普罗米修斯 / Prometheus
    Sorry        // sorry
}

/// <summary>
/// 命令解析结果。
/// </summary>
public struct CommandResult
{
    public CommandType type;
    public string rawInput;
    public string normalizedInput;
    public MetaCommandKind metaKind;
    public string codeInput; // 仅 CodeInput 类型有值

    public static CommandResult None => new CommandResult { type = CommandType.None };
}

/// <summary>
/// 命令解析器 — 关键词匹配（大小写不敏感）、优先级路由。
/// 优先级：代码输入 > kill -9 > Meta命令 > 通用命令
/// </summary>
public static class CommandParser
{
    /// <summary>
    /// 解析用户输入，返回带类型的 CommandResult。
    /// </summary>
    public static CommandResult Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return CommandResult.None;

        var result = new CommandResult
        {
            rawInput = input,
            normalizedInput = input.Trim().ToLowerInvariant()
        };

        var upper = input.Trim().ToUpperInvariant();

        // 优先级 1: 代码输入 (MEM_INIT_*, EMPATHY_*, PROMETHEUS_*)
        if (IsCodeInput(upper, out string code))
        {
            result.type = CommandType.CodeInput;
            result.codeInput = code;
            return result;
        }

        // 优先级 2: kill -9
        if (upper == "KILL -9" || upper == "KILL -9")
        {
            result.type = CommandType.Kill9;
            return result;
        }
        // 覆盖 "kill-9" 无空格变体
        if (upper == "KILL-9")
        {
            result.type = CommandType.Kill9;
            return result;
        }

        // 优先级 3: Meta 命令
        if (IsWhoAmI(upper, out MetaCommandKind metaKind))
        {
            result.type = CommandType.Meta;
            result.metaKind = metaKind;
            return result;
        }

        if (IsPrometheus(upper))
        {
            result.type = CommandType.Meta;
            result.metaKind = MetaCommandKind.Prometheus;
            return result;
        }

        if (upper == "SORRY" || upper == "对不起" || upper == "抱歉")
        {
            result.type = CommandType.Meta;
            result.metaKind = MetaCommandKind.Sorry;
            return result;
        }

        // 优先级 4: 通用命令
        result.type = CommandType.General;
        return result;
    }

    /// <summary>
    /// 检查是否为代码输入，返回匹配的代码字符串。
    /// </summary>
    private static bool IsCodeInput(string upper, out string code)
    {
        code = null;

        if (upper.StartsWith("MEM_INIT_"))
            code = upper;
        else if (upper.StartsWith("EMPATHY_"))
            code = upper;
        else if (upper.StartsWith("PROMETHEUS_"))
            code = upper;
        else
            return false;

        return code.Length > 5; // 确保不只是前缀本身
    }

    private static bool IsWhoAmI(string upper, out MetaCommandKind kind)
    {
        kind = MetaCommandKind.None;

        switch (upper)
        {
            case "WHOAMI":
            case "WHO AM I":
            case "WHO ARE YOU":
            case "你是谁":
            case "你是谁？":
            case "我是谁":
            case "我是什么":
                kind = MetaCommandKind.WhoAmI;
                return true;
            default:
                return false;
        }
    }

    private static bool IsPrometheus(string upper)
    {
        return upper == "PROMETHEUS"
            || upper == "普罗米修斯"
            || upper == "普罗米修斯？"
            || upper == "你是普罗米修斯"
            || upper == "你是普罗米修斯？";
    }
}
