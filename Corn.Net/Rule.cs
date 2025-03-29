namespace Corn.Net;

public interface IRule;
public interface IValueRule : IArrayRule;
public interface IObjectRule : IRule;
public interface IArrayRule : IRule;
public interface IStringRule : IRule;

public struct RuleConfig : IRule
{
    public RuleAssignBlock? AssignBlock;
    public RuleObject Value;
}

public struct RuleAssignBlock : IRule
{
    public List<RuleAssignment> Assignments = [];

    public RuleAssignBlock()
    {
    }
}

public record struct RuleAssignment(string Name, IValueRule Value) : IRule;

public struct RuleObject : IValueRule
{
    public readonly List<IObjectRule> Rules = [];

    public RuleObject()
    {
    }
}

public record struct RulePair(RulePath Path, IValueRule Value) : IObjectRule;
public struct RulePath(): IRule
{
    public List<string> Value { get; set; } = [];
}

public struct RulePathSegment : IRule;

public record struct RuleSpread(string Value) : IObjectRule, IArrayRule;

public struct RuleArray : IValueRule
{
    public List<IArrayRule> Rules = [];

    public RuleArray()
    {
    }
}
public record struct RuleInput(string Value) : IValueRule, IStringRule;
public record struct RuleBoolean(bool Value): IValueRule;
public record struct RuleFloat(double Value): IValueRule;
public record struct RuleInteger(int Value) : IValueRule;

public struct RuleString : IValueRule
{
    public List<IStringRule> Rules = [];

    public RuleString()
    {
    }
}

public record struct RuleCharSequence(string Value) : IStringRule;
public record struct RuleCharEscape(char Value): IStringRule;
public struct RuleNull: IValueRule;