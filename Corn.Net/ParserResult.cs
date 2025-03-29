using System.Diagnostics;

namespace Corn.Net;

public interface IResult
{
    public static IResult<TNew> Upcast<TOld, TNew>(IResult<TOld> result) where TOld : TNew
    {
        return result switch
        {
            Ok<TOld> r => new Ok<TNew>(r.Value),
            Error<TOld> r => r.Map<TNew>(),
            _ => throw new UnreachableException(),
        };
    }
}

public interface IResult<T>;

public record struct Ok<T>(T Value) : IResult<T>
{
    public static implicit operator T(Ok<T> ok) => ok.Value;
}

public readonly record struct Error<T>(ParserError Err) : IResult<T>
{
    public static Error<T> FromMessage(string message)
    {
        return new Error<T>(new ParserError(message));
    }
    
    public Error<TNew> Map<TNew>()
    {
        return new Error<TNew>(Err);
    }
}

// public struct ParserResult<T>
// {
//     public T? Value;
//     public ParserError? Error;
//
//     public static ParserResult<T> Ok(T value)
//     {
//         return new ParserResult<T> { Value = value };
//     }
//
//     public static ParserResult<object> Err(ParserError error)
//     {
//         return new ParserResult<object> { Error = error };
//     }
//
//     public static ParserResult<T> Err(string message)
//     {
//         return new ParserResult<T> { Error = new ParserError(message) };
//     }
//
//     // public ParserResult<TNew> Map<TNew>()
//     // where T : TNew
//     // {
//     //     return new ParserResult<TNew> { Value = this.Value };
//     // }
//     //
//     // public ParserResult<TNew> MapError<TNew>()
//     // {
//     //     return new ParserResult<TNew> { Value = Value, Error = Error };
//     // }
//
//     public static implicit operator ParserResult<T>(T value)
//     {
//         return new ParserResult<T> { Value = value };
//     }
//
//     public static implicit operator ParserResult<T>(ParserError error)
//     {
//         return new ParserResult<T> { Error = error };
//     }
// }

public record struct ParserError(string Message);