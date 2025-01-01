using FileSorter;

namespace FileSorterTests
{
    public class FileCreatorTests
    {
        private FileCreator _fileCreator;

        [SetUp]
        public void Setup()
        {
            _fileCreator = new FileCreator();
        }

        #region CreateFile

        [Test]
        public void CreateFileShouldCreateFile()
        {
            //Arrange
            var filename = "test.test";
            var size = 100000;

            //Act
            _fileCreator.CreateFile(filename, size);

            //Assert
            Assert.That(File.Exists(filename), Is.True);

            var file = File.OpenRead(filename);

            Assert.That(file.Length, Is.AtLeast(size));

            file.Dispose();

            File.Delete(filename);
        }

        #endregion CreateFile

        #region CreateLine

        [Test]
        public void CreateSingleLineShouldCreateLine()
        {
            //Arrange
            //Act
            var line = _fileCreator.CreateLine();

            //Assert
            Assert.That(line, Is.Not.Null);
            Assert.That(line, Is.Not.Empty);
        }

        [Test]
        public void CreateSingleLineShouldCreateProperLine()
        {
            //Arrange
            //Act
            var line = _fileCreator.CreateLine();

            //Assert
            var splited = line.Split('.');
            Assert.That(splited.Length, Is.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(splited[0], Is.Not.Empty);
                Assert.That(splited[1], Is.Not.Empty);
            });
        }

        #endregion CreateLine

        #region CreateText

        [Test]
        public void CreateTextShouldCreateNonEmptyString()
        {
            //Arrange
            //Act
            var text = _fileCreator.CreateText();

            //Assert
            Assert.That(text, Is.Not.Null);
            Assert.That(text, Is.Not.Empty);
        }

        #endregion CreateText
    }
}