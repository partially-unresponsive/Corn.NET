using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Corn.Net;

public static class Extensions
{
    /// <summary>
    /// Checks if this is a whitespace char.
    /// </summary>
    /// <param name="rune"></param>
    /// <returns></returns>
    public static bool IsWhitespace(this Rune rune)
    {
        return rune.Value is ' ' or '\t' or '\n' or '\r';
    }

    /// <summary>
    ///  Checks if this is 0-9.
    /// </summary>
    /// <param name="rune"></param>
    /// <returns></returns>
    public static bool IsDigit(this Rune rune)
    {
        return rune.Value is >= 48 and <= 57;
    }

    /// <summary>
    ///  Checks if this is 0-9, A-F, a-f.
    /// </summary>
    /// <param name="rune"></param>
    /// <returns></returns>
    public static bool IsHexDigit(this Rune rune)
    {
        return rune.IsDigit()
               || rune.Value is >= 'A' and <= 'F'
               || rune.Value is >= 'a' and <= 'f';
    }

    /// <summary>
    /// Checks if this is A-Z, a-z.
    /// </summary>
    /// <param name="rune"></param>
    /// <returns></returns>
    public static bool IsLetter(this Rune rune)
    {
        return rune.Value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    /// <summary>
    /// Checks if this is 0-9, A-Z, a-z.
    /// </summary>
    /// <param name="rune"></param>
    /// <returns></returns>
    public static bool IsAlphanumeric(this Rune rune)
    {
        return rune.IsDigit() || rune.IsLetter();
    }

    /// <summary>
    /// Mutably removes elements from the start of the list
    /// while they match the predicate.
    ///
    /// The removed elements are returned. 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="predicate"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T> DrainWhile<T>(this List<T> source, Func<T, bool> predicate)
    {
        var slice = new List<T>();
        while (predicate(source.First()))
        {
            slice.Add(source.First());
            source.Remove(source.First());
        }

        return slice;
    }

    public static string Stringify(this List<Rune> runes)
    {
        var sb = new StringBuilder();
        foreach (var rune in runes)
        {
            sb.Append(rune);
        }

        return sb.ToString();
    }

    public static bool ConsumeStatic<TToken>(this List<IToken> tokens, [NotNullWhen(true)] out ParserError? error)
        where TToken : IToken
    {
        var token = tokens[0];
        
        if (token is not TToken)
        {
            error = new ParserError($"expected `{NameHelper.TokenName<TToken>()}`, got {NameHelper.TokenName(token)}");
            return true;
        }
        
        tokens.RemoveAt(0);

        error = null;
        return false;
    }
}