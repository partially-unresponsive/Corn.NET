namespace Corn.Net;

public static class NameHelper
{
    public static string TokenName<T>()
        where T : IToken
    {
        return typeof(T) switch
        {
            { } t when t == typeof(TokenBraceOpen) => "{",
            { } t when t == typeof(TokenBraceClose) => "}",
            { } t when t == typeof(TokenBracketOpen) => "[",
            { } t when t == typeof(TokenBracketClose) => "]",
            { } t when t == typeof(TokenEquals) => "=",
            { } t when t == typeof(TokenDoubleQuote) => "\"",
            { } t when t == typeof(TokenSpread) => "..",
            { } t when t == typeof(TokenPathSeparator) => ".",
            { } t when t == typeof(TokenLet) => "let",
            { } t when t == typeof(TokenIn) => "in",
            { } t when t == typeof(TokenTrue) => "true",
            { } t when t == typeof(TokenFalse) => "false",
            { } t when t == typeof(TokenNull) => "null",
            { } t when t == typeof(TokenPathSegment) => "<path_seg>",
            { } t when t == typeof(TokenFloat) => "<float>",
            { } t when t == typeof(TokenInteger) => "<integer>",
            { } t when t == typeof(TokenCharEscape) => "<char_esc>",
            { } t when t == typeof(TokenCharSequence) => "<char_seq>",
            { } t when t == typeof(TokenInput) => "<input>",
            _ => "<unknown>"
        };
    }

    public static string TokenName<T>(T token)
        where T : IToken
    {
        return TokenName<T>();
    }
}