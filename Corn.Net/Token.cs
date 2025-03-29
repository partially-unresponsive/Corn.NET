namespace Corn.Net;

public interface IToken;

internal struct TokenBraceOpen : IToken;
internal struct TokenBraceClose : IToken;
internal struct TokenBracketOpen : IToken;
internal struct TokenBracketClose : IToken;
internal struct TokenEquals : IToken;
internal struct TokenDoubleQuote : IToken;
internal struct TokenSpread : IToken;
internal struct TokenPathSeparator : IToken;
internal struct TokenLet : IToken;
internal struct TokenIn : IToken;
internal struct TokenTrue : IToken;
internal struct TokenFalse : IToken;
internal struct TokenNull : IToken;

internal readonly struct TokenPathSegment(string value) : IToken
{
    public string Value { get; } = value;
}

internal readonly struct TokenFloat(double value) : IToken
{
    public double Value { get; } = value;
}

internal readonly struct TokenInteger(int value) : IToken
{
    public int Value { get; } = value;
}

internal readonly struct TokenCharEscape(char value) : IToken
{
    public char Value { get; } = value;
}

internal readonly struct TokenCharSequence(string value) : IToken
{
    public string Value { get; } = value;
}

internal readonly struct TokenInput(string value) : IToken
{
    public string Value { get; } = value;
}