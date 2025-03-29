namespace Corn.Net;

public interface IValue;
public readonly record struct ObjectValue(OrderedDictionary<string, IValue> Value) : IValue;
public readonly record struct ArrayValue(List<IValue> Value) : IValue;

public readonly record struct StringValue(string Value) : IValue;
public readonly record struct BoolValue(bool Value) : IValue;
public readonly record struct IntValue(int Value) : IValue;
public readonly record struct FloatValue(double Value) : IValue;