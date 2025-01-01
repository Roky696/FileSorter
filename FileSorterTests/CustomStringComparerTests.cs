using FileSorter;

namespace FileSorterTests
{
    public class CustomStringComparerTests
    {
        private readonly CustomStringComparer _comparer = new();

        [TestCase("1. Apple", "2. Banana is yellow", -1)]
        [TestCase("2. Banana is yellow", "1. Apple", 1)]
        [TestCase("1. Apple", "1. Apple", 0)]
        [TestCase("1. Apple", "2. Apple", -1)]
        [TestCase("2. Apple", "1. Apple", 1)]
        [TestCase("1. Apple", "415. Apple", -1)]
        [TestCase("1. Apple", "", -1)]
        [TestCase("", "1. Apple", 1)]
        [TestCase("", "", 0)]
        [TestCase("1. Apple", null, -1)]
        [TestCase(null, "1. Apple", 1)]
        [TestCase(null, null, 0)]
        public void TestTwoStrings(string? stringA, string? stringB, int expectedResult)
        {
            //Arrange
            //Act
            var result = _comparer.Compare(stringA, stringB);

            //Assert
            Assert.That(result, Is.EqualTo(expectedResult));
        }
    }
}