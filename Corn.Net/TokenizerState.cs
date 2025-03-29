namespace Corn.Net;

public enum TokenizerState
{
    TopLevel,
    AssignBlock,
    Value,
    Object,
    Array,
    String
}

internal static class StateChanger
{
    public static Action<Stack<TokenizerState>> Push(TokenizerState newState)
    {
        return state => state.Push(newState);
    }

    public static Action<Stack<TokenizerState>> Replace(TokenizerState newState)
    {
        return state =>
        {
            state.Pop();
            state.Push(newState);
        };
    }
    
    public static void Pop(Stack<TokenizerState> state)
    {
        state.Pop();
    }
}