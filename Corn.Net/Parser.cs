using System.Diagnostics;

namespace Corn.Net;

public static class Parser
{
    public static IResult<RuleConfig> Parse(List<IToken> tokens)
    {
        var rule = new RuleConfig();

        var token = tokens[0];
        if (token is TokenLet)
        {
            var assignBlock = ParseAssignBlock(tokens);
            switch (assignBlock)
            {
                case Ok<RuleAssignBlock> assign:
                    rule.AssignBlock = assign;
                    break;
                case Error<RuleAssignBlock> error:
                    return error.Map<RuleConfig>();
            }
        }

        var obj = ParseObject(tokens);
        switch (obj)
        {
            case Ok<RuleObject> o:
                rule.Value = o;
                break;
            case Error<RuleObject> error:
                return error.Map<RuleConfig>();
        }

        return new Ok<RuleConfig>(rule);
    }

    private static IResult<RuleAssignBlock> ParseAssignBlock(List<IToken> tokens)
    {
        if(tokens.ConsumeStatic<TokenLet>(out var err)) 
            return new Error<RuleAssignBlock>(err.Value);
        
        if (tokens.ConsumeStatic<TokenBraceOpen>(out err))
            return new Error<RuleAssignBlock>(err.Value);

        var rule = new RuleAssignBlock();

        var token = tokens[0];
        while (token is not TokenBraceClose)
        {
            var res = ParseAssignment(tokens);
            switch (res)
            {
                case Ok<RuleAssignment> assign:
                    rule.Assignments.Add(assign);
                    break;
                case Error<RuleAssignment> error:
                    return error.Map<RuleAssignBlock>();
            }

            token = tokens[0];
        }

        var inKw = tokens[1];
        if (inKw is not TokenIn)
            return Error<RuleAssignBlock>.FromMessage("expected `in`, got " + inKw);

        tokens.RemoveRange(0, 2);

        return new Ok<RuleAssignBlock>(rule);
    }

    private static IResult<RuleAssignment> ParseAssignment(List<IToken> tokens)
    {
        if (tokens.Count < 3)
            return Error<RuleAssignment>.FromMessage("unexpected end of input");

        var input = tokens[0];
        var equals = tokens[1];

        if (input is not TokenInput ti)
            return Error<RuleAssignment>.FromMessage("expected `input`, got " + input);

        if (equals is not TokenEquals)
            return Error<RuleAssignment>.FromMessage("expected `=`, got " + equals);

        tokens.RemoveRange(0, 2);

        var value = ParseValue(tokens);
        return value switch
        {
            Ok<IValueRule> v => new Ok<RuleAssignment>(new RuleAssignment(ti.Value, v.Value)),
            Error<IValueRule> error => error.Map<RuleAssignment>(),
            _ => throw new UnreachableException()
        };
    }

    private static IResult<T> ParseValue<T>(List<IToken> tokens)
        where T : IValueRule
    {
        return (IResult<T>)ParseValue(tokens);
    }

    private static IResult<IValueRule> ParseValue(List<IToken> tokens)
    {
        var token = tokens[0];

        var rule = token switch
        {
            TokenFloat t => (new Ok<IValueRule>(new RuleFloat(t.Value)), true),
            TokenInteger t => (new Ok<IValueRule>(new RuleInteger(t.Value)), true),
            TokenInput t => (new Ok<IValueRule>(new RuleInput(t.Value)), true),
            TokenTrue => (new Ok<IValueRule>(new RuleBoolean(true)), true),
            TokenFalse => (new Ok<IValueRule>(new RuleBoolean(false)), true),
            TokenNull => (new Ok<IValueRule>(new RuleNull()), true),
            TokenDoubleQuote => (IResult.Upcast<RuleString, IValueRule>(ParseString(tokens)), false),
            TokenBraceOpen => (IResult.Upcast<RuleObject, IValueRule>(ParseObject(tokens)), false),
            TokenBracketOpen => (IResult.Upcast<RuleArray, IValueRule>(ParseArray(tokens)), false),
            _ => (Error<IValueRule>.FromMessage("expected `value`, got " + token), false)
        };

        if (rule.Item2) tokens.RemoveAt(0);
        return rule.Item1;
    }

    private static IResult<RuleObject> ParseObject(List<IToken> tokens)
    {
        if (tokens.ConsumeStatic<TokenBraceOpen>(out var err)) 
            return new Error<RuleObject>(err.Value);

        var rule = new RuleObject();

        var token = tokens[0];
        while (token is not TokenBraceClose)
        {
            switch (token)
            {
                case TokenSpread:
                    var spread = ParseSpread(tokens);
                    switch (spread)
                    {
                        case Ok<RuleSpread> s:
                            rule.Rules.Add(s.Value);
                            break;
                        case Error<RuleSpread> error:
                            return error.Map<RuleObject>();
                    }

                    break;
                case TokenPathSegment:
                    var pair = ParsePair(tokens);
                    switch (pair)
                    {
                        case Ok<RulePair> p:
                            rule.Rules.Add(p.Value);
                            break;
                        case Error<RulePair> error:
                            return error.Map<RuleObject>();
                    }

                    break;
                default:
                    return Error<RuleObject>.FromMessage("expected one of `..` or `path_seg`, got " + token);
            }

            token = tokens[0];
        }

        if (tokens.ConsumeStatic<TokenBraceClose>(out err)) 
            return new Error<RuleObject>(err.Value);

        return new Ok<RuleObject>(rule);
    }

    private static IResult<RuleSpread> ParseSpread(List<IToken> tokens)
    {
        if (tokens.ConsumeStatic<TokenSpread>(out var err)) 
            return new Error<RuleSpread>(err.Value);

        var input = tokens[0];
        if (input is not TokenInput i)
            return Error<RuleSpread>.FromMessage("expected `input`, got " + input);

        tokens.RemoveAt(0);

        var rule = new RuleSpread(i.Value);
        return new Ok<RuleSpread>(rule);
    }

    private static IResult<RulePair> ParsePair(List<IToken> tokens)
    {
        var rule = new RulePair();
        
        var path = ParsePath(tokens);
        switch (path)
        {
            case Ok<RulePath> p:
                rule.Path = p.Value;
                break;    
            case Error<RulePath> error:
                return error.Map<RulePair>();
        }

        if (tokens.ConsumeStatic<TokenEquals>(out var err)) 
            return new Error<RulePair>(err.Value);

        var value = ParseValue(tokens);
        switch (value)
        {
            case Ok<IValueRule> v:
                rule.Value = v.Value;
                break;
            case Error<IValueRule> error:
                return error.Map<RulePair>();
        }
        
        return new Ok<RulePair>(rule);
    }

    private static IResult<RulePath> ParsePath(List<IToken> tokens)
    {
        var pathSeg = tokens[0];

        if (pathSeg is not TokenPathSegment)
            return Error<RulePath>.FromMessage("expected `path_seg`, got " + pathSeg);

        var rule = new RulePath();

        while (pathSeg is TokenPathSegment s)
        {
            rule.Value.Add(s.Value);

            var dot = tokens[1];
            if (dot is TokenPathSeparator) tokens.RemoveRange(0, 2);
            else tokens.RemoveAt(0);

            pathSeg = tokens[1];
        }

        return new Ok<RulePath>(rule);
    }

    private static IResult<RuleArray> ParseArray(List<IToken> tokens)
    {
        if (tokens.ConsumeStatic<TokenBracketOpen>(out var err)) 
            return new Error<RuleArray>(err.Value);

        var rule = new RuleArray();

        var token = tokens[0];
        while (token is not TokenBracketClose)
        {
            var res = token switch
            {
                TokenSpread => IResult.Upcast<RuleSpread, IArrayRule>(ParseSpread(tokens)),
                _ => IResult.Upcast<IValueRule, IArrayRule>(ParseValue(tokens)),
            };

            switch (res)
            {
                case Ok<IArrayRule> a:
                    rule.Rules.Add(a.Value);
                    break;
                case Error<IArrayRule> error:
                    return error.Map<RuleArray>();
            }

            token = tokens[0];
        }

        if (tokens.ConsumeStatic<TokenBracketClose>(out err)) 
            return new Error<RuleArray>(err.Value);

        return new Ok<RuleArray>(rule);
    }

    private static IResult<RuleString> ParseString(List<IToken> tokens)
    {
        if (tokens.ConsumeStatic<TokenDoubleQuote>(out var err)) 
            return new Error<RuleString>(err.Value);

        var rule = new RuleString();

        var token = tokens[0];
        while (token is not TokenDoubleQuote)
        {
            IResult<IStringRule> part = token switch
            {
                TokenCharSequence seq => new Ok<IStringRule>(new RuleCharSequence(seq.Value)),
                TokenCharEscape esc => new Ok<IStringRule>(new RuleCharEscape(esc.Value)),
                TokenInput input => new Ok<IStringRule>(new RuleInput(input.Value)),
                _ => Error<IStringRule>.FromMessage("expected one of `char_seq`, `char_escape` or `input`, got " + token)
            };

            switch (part)
            {
                case Ok<IStringRule> r:
                    rule.Rules.Add(r.Value);
                    break;
                case Error<IStringRule> error:
                    return error.Map<RuleString>();
            }

            tokens.RemoveAt(0);
            token = tokens[0];
        }

        if (tokens.ConsumeStatic<TokenDoubleQuote>(out err)) 
            return new Error<RuleString>(err.Value);

        return new Ok<RuleString>(rule);
    }
}