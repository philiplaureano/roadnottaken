using System;
using System.Threading.Tasks;
using Xunit;

namespace SampleTestProject
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
        }

        [Fact(Skip = "This test has been skipped")]
        public async Task Test2()
        {
            throw new NotImplementedException("TODO: Implement Test2");
        }

        [Fact]
        public async Task Test3()
        {
            throw new NotImplementedException("TODO: Implement Test3");
        }

        [Fact]
        public async Task Test4()
        {
            throw new NotImplementedException("TODO: Implement Test4");
        }

        [Fact]
        public async Task Test5()
        {
            throw new NotImplementedException("TODO: Implement Test5");
        }

        [Theory]
        [InlineData(1,2,3)]
        public async Task Test6(int a, int b, int c)
        {
            throw new NotImplementedException("TODO: Implement Test6");
        }
    }
}