using System;
using Xunit;

namespace Domain.Tests
{
    public class HelloGeneratorTest
    {
        [Fact]
        public void TestHello()
        {
            var generator = new HelloGenerator(who => $"Hello {who}!");
            var hello = generator.GenerateHello("Alice");
            Assert.Equal("Hello Alice!", hello);
        }
    }
}
