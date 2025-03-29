namespace Corn.Net;

public static class Tokenizer
{
    public static List<IToken> Tokenize(string input)
    {
        var runes = input.EnumerateRunes().ToList();
        var tokens = new List<IToken>();
        
        var state = new Stack<TokenizerState>();
        state.Push(TokenizerState.TopLevel);

        while (runes.Count > 0)
        {
            var length = runes.Count;
            var currentState = state.Peek();
            
            if (currentState != TokenizerState.String)
            {
                _ = runes.DrainWhile(r => r.IsWhitespace());
                
                if (runes.Count > 1 && runes[0].Value == '/' && runes[1].Value == '/')
                {
                    _ = runes.DrainWhile(r => r.Value != '\n');
                }
            }

            var matchers = MatcherFactory.GetMatchers(currentState);

            foreach (var matcher in matchers)
            {
                var token = matcher.Matcher(runes);
                if (token is null) continue;
                
                tokens.Add(token);
                matcher.StateChanger?.Invoke(state);
                break;
            }
            
            // assert
            if (length == runes.Count) throw new Exception("length unchanged");
        }

        return tokens;
    }
}