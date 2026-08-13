using Xunit;

namespace Sample.UnitTests
{
    public class ClassWithLineDirectiveTest
    {
        [Fact]
        public void Test()
        {
            var value = ClassWithLineDirective.Compute(1);
            Assert.Equal(2, value);
        }
    }
}
