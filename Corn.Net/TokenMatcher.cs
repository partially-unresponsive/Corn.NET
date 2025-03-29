using System.Globalization;
using System.Text;
using MatcherFunc = System.Func<System.Collections.Generic.List<System.Text.Rune>, Corn.Net.IToken?>;

namespace Corn.Net;

public record struct TokenMatcher(
    MatcherFunc Matcher,
    Action<Stack<TokenizerState>>? StateChanger = null);

public static class MatcherFactory
{
    private static readonly MatcherFunc MatcherBraceOpen = CharTokenMatcher<TokenBraceOpen>('{');
    private static readonly MatcherFunc MatcherBraceClose = CharTokenMatcher<TokenBraceClose>('}');

    private static readonly MatcherFunc MatcherBracketOpen = CharTokenMatcher<TokenBracketOpen>('[');
    private static readonly MatcherFunc MatcherBracketClose = CharTokenMatcher<TokenBracketClose>(']');

    private static readonly MatcherFunc MatcherLet = StringTokenMatcher<TokenLet>("let");
    private static readonly MatcherFunc MatcherIn = StringTokenMatcher<TokenIn>("in");
    private static readonly MatcherFunc MatcherTrue = StringTokenMatcher<TokenTrue>("true");
    private static readonly MatcherFunc MatcherFalse = StringTokenMatcher<TokenFalse>("false");
    private static readonly MatcherFunc MatcherNull = StringTokenMatcher<TokenNull>("null");

    private static readonly MatcherFunc MatcherEquals = CharTokenMatcher<TokenEquals>('=');
    private static readonly MatcherFunc MatcherDoubleQuote = CharTokenMatcher<TokenDoubleQuote>('"');
    private static readonly MatcherFunc MatcherPathSeparator = CharTokenMatcher<TokenPathSeparator>('.');
    private static readonly MatcherFunc MatcherSpread = StringTokenMatcher<TokenSpread>("..");

    private static readonly MatcherFunc MatcherQuotedPathSegment =
        DynamicTokenMatcher(MatchQuotedPathSegment);

    private static readonly MatcherFunc MatcherPathSegment = DynamicTokenMatcher(MatchPathSegment);

    private static readonly MatcherFunc MatcherInput = DynamicTokenMatcher(MatchInput);
    private static readonly MatcherFunc MatcherFloat = DynamicTokenMatcher(MatchFloat);
    private static readonly MatcherFunc MatcherHexInteger = DynamicTokenMatcher(MatchHexInteger);
    private static readonly MatcherFunc MatcherInteger = DynamicTokenMatcher(MatchInteger);

    private static readonly MatcherFunc MatcherCharEscape = DynamicTokenMatcher(MatchCharEscape);
    private static readonly MatcherFunc MatcherCharSequence = DynamicTokenMatcher(MatchCharSequence);

    private static readonly List<TokenMatcher> TopLevel =
    [
        new(MatcherBraceOpen, StateChanger.Push(TokenizerState.Object)),
        new(MatcherLet, StateChanger.Push(TokenizerState.AssignBlock)),
    ];

    private static readonly List<TokenMatcher> AssignBlock =
    [
        new(MatcherIn, StateChanger.Pop),
        new(MatcherBraceOpen),
        new(MatcherBraceClose),
        new(MatcherInput),
        new(MatcherEquals, StateChanger.Push(TokenizerState.Value))
    ];

    private static readonly List<TokenMatcher> Object =
    [
        new(MatcherBraceClose, StateChanger.Pop),

        new(MatcherEquals, StateChanger.Push(TokenizerState.Value)),
        new(MatcherSpread),
        new(MatcherPathSeparator),
        new(MatcherInput),
        new(MatcherQuotedPathSegment),
        new(MatcherPathSegment)
    ];

    private static readonly List<TokenMatcher> Array =
    [
        new(MatcherBracketClose, StateChanger.Pop),

        new(MatcherBraceOpen, StateChanger.Push(TokenizerState.Object)),
        new(MatcherBracketOpen, StateChanger.Push(TokenizerState.Array)),

        new(MatcherSpread),

        new(MatcherTrue),
        new(MatcherFalse),
        new(MatcherNull),

        new(MatcherDoubleQuote, StateChanger.Push(TokenizerState.String)),

        new(MatcherInput),
        new(MatcherFloat),
        new(MatcherHexInteger),
        new(MatcherInteger),
    ];

    private static readonly List<TokenMatcher> Value =
    [
        new(MatcherBraceOpen, StateChanger.Replace(TokenizerState.Object)),
        new(MatcherBracketOpen, StateChanger.Replace(TokenizerState.Array)),

        new(MatcherTrue, StateChanger.Pop),
        new(MatcherFalse, StateChanger.Pop),
        new(MatcherNull, StateChanger.Pop),

        new(MatcherDoubleQuote, StateChanger.Replace(TokenizerState.String)),

        new(MatcherInput, StateChanger.Pop),
        new(MatcherFloat, StateChanger.Pop),
        new(MatcherHexInteger, StateChanger.Pop),
        new(MatcherInteger, StateChanger.Pop),

        new(MatcherBracketClose, StateChanger.Pop),
    ];

    private static readonly List<TokenMatcher> String =
    [
        new(MatcherDoubleQuote, StateChanger.Pop),

        new(MatcherInput),
        new(MatcherCharEscape),
        new(MatcherCharSequence)
    ];

    public static List<TokenMatcher> GetMatchers(TokenizerState state)
    {
        return state switch
        {
            TokenizerState.TopLevel => TopLevel,
            TokenizerState.AssignBlock => AssignBlock,
            TokenizerState.Value => Value,
            TokenizerState.Object => Object,
            TokenizerState.Array => Array,
            TokenizerState.String => String,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }

    private static MatcherFunc CharTokenMatcher<T>(char c)
        where T : IToken, new()

    {
        return input =>
        {
            if (input.FirstOrDefault().Value != c) return null;

            input.RemoveAt(0);
            return new T();
        };
    }

    private static MatcherFunc StringTokenMatcher<T>(string s)
        where T : IToken, new()
    {
        return input =>
        {
            if (input.Count < s.Length) return null;

            // TODO: Make more efficient
            if (input[..s.Length].Stringify() != s) return null;

            input.RemoveRange(0, s.Length);
            return new T();
        };
    }

    private static MatcherFunc DynamicTokenMatcher(MatcherFunc matcher)
    {
        return matcher.Invoke;
    }

    private static IToken? MatchInput(List<Rune> input)
    {
        if (input.Count < 2 || input[0].Value != '$') return null;

        var name = new StringBuilder("$");

        if (!input[1].IsLetter()) return null;
        name.Append((char)input[1].Value);
        
        input.RemoveRange(0, 2);

        var nameRest = input.DrainWhile(r => r.IsAlphanumeric() || r.Value == '_');

        foreach (var c in nameRest) name.Append((char)c.Value);
        
        return new TokenInput(name.ToString());
    }

    private static IToken? MatchQuotedPathSegment(List<Rune> input)
    {
        if (input.Count < 3 || input[0].Value != '\'') return null;

        int prev;
        var segment = input.DrainWhile(r =>
        {
            var match = r.Value != '\'';
            prev = r.Value;
            return match && prev != '\'';
        });

        return new TokenPathSegment(segment.Stringify());
    }

    private static IToken? MatchPathSegment(List<Rune> input)
    {
        var seg = input.DrainWhile(r => !r.IsWhitespace() && r.Value != '=' && r.Value != '.');

        if (seg.Count > 0)
        {
            return new TokenPathSegment(seg.Stringify());
        }

        return null;
    }

    private static IToken? MatchFloat(List<Rune> input)
    {
        var intString = input.TakeWhile(r => r.IsDigit()).ToList().Stringify();

        if (input.Count < intString.Length || input[intString.Length].Value != '.') return null;
        
        input.RemoveRange(0, intString.Length + 1);

        var decimalString = input
            .DrainWhile(r => r.IsDigit() || r.Value == '+' || r.Value == '-' || r.Value == 'e')
            .Stringify();

        var isValid = double.TryParse($"{intString}.{decimalString}", out var num);
        if(!isValid) return null;
        
        return new TokenFloat(num);
    }

    private static IToken? MatchHexInteger(List<Rune> input)
    {
        if (input.Count < 3) return null;
        if (input[0].Value != '0' && input[1].Value != 'x') return null;

        input.RemoveRange(0, 2);

        var digits = input.DrainWhile(r => r.IsHexDigit()).Stringify();

        var isValid = int.TryParse(digits, NumberStyles.HexNumber, null, out var num);

        if (isValid) return new TokenInteger(num);
        return null;
    }

    private static IToken? MatchInteger(List<Rune> input)
    {
        // TODO: Handle negatives, underscores
        var numString = input.DrainWhile(r => r.IsDigit());

        var isValid = int.TryParse(numString.Stringify(), out var num);

        if (isValid) return new TokenInteger(num);
        return null;
    }

    private static IToken? MatchCharEscape(List<Rune> input)
    {
        if (input.Count < 2 || input[0].Value != '\\') return null;

        const int lenUnicode = 2 + 4;
        if (input.Count >= lenUnicode && input[1].Value == 'u')
        {
            var codeS = input[2..6].Stringify();

            var isValid = int.TryParse(codeS, NumberStyles.HexNumber, null, out var num);
            if (!isValid) return null;

            input.RemoveRange(0, 6);
            return new TokenCharEscape((char)num);
        }

        var ch = input[1].Value switch
        {
            '\\' => '\\',
            '"' => '"',
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            '$' => '$',
            _ => '\0'
        };

        if (ch == 0) return null;

        input.RemoveRange(0, 2);
        return new TokenCharEscape(ch);
    }

    private static IToken MatchCharSequence(List<Rune> input)
    {
        var seq = input.DrainWhile(r => r.Value != '\\' && r.Value != '"' && r.Value != '$');

        return new TokenCharSequence(seq.Stringify());
    }
}