namespace Corn.Net.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {;
        Assert.Pass();
        Tokenizer.Tokenize("{ foo = 42 }");
    }
}