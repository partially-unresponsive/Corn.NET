using System.Text.Json;
using System.Text.Json.Serialization;
using Corn.Net;

const string input = "let { $foo = 42 } in { foo = [ $foo ] }";

var tokens = Tokenizer.Tokenize(input);

switch (Parser.Parse(tokens))
{
    case Ok<RuleConfig> config:
        switch (new Evaluator().Evaluate(config))
        {
            case Ok<ObjectValue> value:
                var jsonOptions = new JsonSerializerOptions { Converters = { new ElementConverter() },  WriteIndented = true };
                Console.WriteLine(JsonSerializer.Serialize(value.Value.Value, jsonOptions));
                break;
            case Error<ObjectValue> error:
                throw new Exception(error.Err.ToString());
        }

        break;
    case Error<RuleConfig> error:
        Console.Error.WriteLine("Error: {0}", error);
        break;
}

internal class ElementConverter : JsonConverter<IValue>
{
    public override IValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, IValue value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case ArrayValue arrayValue:
                JsonSerializer.Serialize(writer, arrayValue.Value, options);
                break;
            case BoolValue boolValue:
                JsonSerializer.Serialize(writer, boolValue.Value, options);
                break;
            case FloatValue floatValue:
                JsonSerializer.Serialize(writer, floatValue.Value, options);
                break;
            case IntValue intValue:
                JsonSerializer.Serialize(writer, intValue.Value, options);
                break;
            case ObjectValue objectValue:
                JsonSerializer.Serialize(writer, objectValue.Value, options);
                break;
            case StringValue stringValue:
                JsonSerializer.Serialize(writer, stringValue.Value, options);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}