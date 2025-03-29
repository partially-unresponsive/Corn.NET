using System.Text;

namespace Corn.Net;

public class Evaluator
{
    private readonly Dictionary<string, IValue> _inputs = new();

    public IResult<ObjectValue> Evaluate(RuleConfig config)
    {
        if (config.AssignBlock.HasValue) EvaluateAssignBlock(config.AssignBlock.Value);

        return EvaluateObject(config.Value);
    }

    public void Reset()
    {
        _inputs.Clear();
    }

    private IResult<object> EvaluateAssignBlock(RuleAssignBlock block)
    {
        foreach (var rule in block.Assignments)
        {
            switch (EvaluateValue(rule.Value))
            {
                case Ok<IValue> value:
                    _inputs.Add(rule.Name, value.Value);
                    break;
                case Error<IValue> error:
                    return error.Map<object>();
            }
        }

        return new Ok<object>();
    }

    private IResult<IValue> EvaluateValue(IValueRule rule)
    {
        return rule switch
        {
            RuleArray ruleArray => IResult.Upcast<ArrayValue, IValue>(EvaluateArray(ruleArray)),
            RuleBoolean ruleBoolean => new Ok<IValue?>(new BoolValue(ruleBoolean.Value)),
            RuleFloat ruleFloat => new Ok<IValue?>(new FloatValue(ruleFloat.Value)),
            RuleInput ruleInput => GetInput(ruleInput.Value),
            RuleInteger ruleInteger => new Ok<IValue?>(new IntValue(ruleInteger.Value)),
            RuleNull => new Ok<IValue?>(null),
            RuleObject ruleObject => IResult.Upcast<ObjectValue, IValue>(EvaluateObject(ruleObject)),
            RuleString ruleString => IResult.Upcast<StringValue, IValue>(EvaluateString(ruleString)),
            _ => throw new ArgumentOutOfRangeException(nameof(rule))
        };
    }

    private IResult<IValue> GetInput(string name)
    {
        var hasValue = _inputs.TryGetValue(name, out var value);

        if (name.StartsWith("$_env"))
        {
            var str = Environment.GetEnvironmentVariable(name["$_env".Length..]);
            if (value is not null) return new Ok<IValue>(new StringValue(str!));
        }

        if (hasValue) return new Ok<IValue>(value!);
        return new Error<IValue>(new ParserError("Env variable not found"));
    }

    private IResult<ObjectValue> EvaluateObject(RuleObject obj)
    {
        var dict = new OrderedDictionary<string, IValue>();
        foreach (var rule in obj.Rules)
        {
            switch (rule)
            {
                case RulePair pair:
                    switch (EvaluateValue(pair.Value))
                    {
                        case Ok<IValue> value:
                            if (AddAtPath(dict, pair.Path.Value, value.Value) is Error<object> err)
                                return err.Map<ObjectValue>();
                            break;
                        case Error<IValue> error:
                            return error.Map<ObjectValue>();
                    }

                    break;
                case RuleSpread spread:
                    switch (GetInput(spread.Value))
                    {
                        case Ok<IValue> { Value: ObjectValue objectValue }:
                            foreach (var (k, v) in objectValue.Value)
                            {
                                dict[k] = v;
                            }

                            break;
                        case Ok<IValue>:
                            return new Error<ObjectValue>(
                                new ParserError("Attempted to spread non-object input into object"));
                        case Error<IValue> error:
                            return error.Map<ObjectValue>();
                    }

                    break;
                default:
                    return new Error<ObjectValue>(new ParserError($"invalid rule: {rule}"));
            }
        }

        return new Ok<ObjectValue>(new ObjectValue(dict));
    }

    private IResult<object> AddAtPath(OrderedDictionary<string, IValue> rules, List<string> path, IValue value)
    {
        var curr = rules;
        for (var i = 0; i < path.Count; i++)
        {
            var seg = path[i];

            if (i == path.Count - 1)
            {
                rules[seg] = value;
                return new Ok<object>();
            }

            if (rules.TryGetValue(seg, out var val))
            {
                if (val is ObjectValue objectValue) curr = objectValue.Value;
                else
                    return new Error<object>(
                        new ParserError($"Attempted to use key-chaining on non-object type: {val}"));
            }
            else
            {
                var child = new ObjectValue();
                curr[seg] = child;
                curr = child.Value;
            }
        }

        return new Ok<object>();
    }

    private IResult<ArrayValue> EvaluateArray(RuleArray array)
    {
        var list = new List<IValue>();

        foreach (var rule in array.Rules)
        {
            switch (rule)
            {
                case IValueRule r:
                    switch (EvaluateValue(r))
                    {
                        case Ok<IValue> value:
                            list.Add(value.Value);
                            break;
                        case Error<IValue> error:
                            return error.Map<ArrayValue>();
                    }

                    break;
                case RuleSpread spread:
                    switch (GetInput(spread.Value))
                    {
                        case Ok<IValue> { Value: ArrayValue arrayValue }:
                            list.AddRange(arrayValue);
                            break;
                        case Ok<IValue>:
                            return new Error<ArrayValue>(
                                new ParserError("Attempted to spread non-array input into array"));
                        case Error<IValue> error:
                            return error.Map<ArrayValue>();
                    }

                    break;
                default:
                    return new Error<ArrayValue>(new ParserError($"invalid rule: {rule}"));
            }
        }

        return new Ok<ArrayValue>(new ArrayValue(list));
    }

    private IResult<StringValue> EvaluateString(RuleString str)
    {
        var sb = new StringBuilder();

        foreach (var rule in str.Rules)
        {
            switch (rule)
            {
                case RuleCharSequence seq:
                    sb.Append(seq.Value);
                    break;
                case RuleCharEscape esc:
                    sb.Append(esc.Value);
                    break;
                case RuleInput input:
                    switch (GetInput(input.Value))
                    {
                        case Ok<IValue> { Value: StringValue stringValue }:
                            sb.Append(stringValue.Value);
                            break;
                        case Ok<IValue>:
                            return new Error<StringValue>(
                                new ParserError("Attempted to interpolate non-string input into string"));
                        case Error<IValue> error:
                            return error.Map<StringValue>();
                    }
                    break;
                default:
                    return new Error<StringValue>(new ParserError($"invalid rule: {rule}"));
            }
        }

        return new Ok<StringValue>(new StringValue(sb.ToString()));
    }
}