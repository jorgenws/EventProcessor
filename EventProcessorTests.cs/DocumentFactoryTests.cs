using EventProcessor;
using Moq;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;

namespace EventProcessorTests
{
    [TestFixture]
    public class DocumentFactoryTests
    {
        private IBinarySerializer _serializer;

        Guid _documentId = Guid.Parse("{A322E127-7EA5-40F2-B527-19B5019C6120}");

        public DocumentFactoryTests()
        {
            _serializer = new BinarySerializer();

            RuntimeTypeModel.Default.Add(typeof(Doc), false)
                .Add(1, "Id")
                .Add(2, "Value");
        }

        [Test]
        public void CreateNewDocument()
        {
            var factory = new DocumentFactory(_serializer);

            var document = factory.CreateNew(_documentId, typeof(Doc));

            Assert.NotNull(document);
            Assert.IsInstanceOf<Doc>(document);
            Assert.AreEqual(_documentId, document.Id);
        }

        [Test]
        public void CreateDocument()
        {
            const int value = 42;

            var doc = new Doc { Id = _documentId, Value = value };

            var bytes = _serializer.Serialize(doc);

            var factory = new DocumentFactory(_serializer);

            var document = factory.Create(bytes, typeof(Doc));

            Assert.NotNull(document);
            Assert.IsInstanceOf<Doc>(document);
            Assert.AreEqual(_documentId, document.Id);
            Assert.AreEqual(value, ((Doc)document).Value);
        }
    }

    public class Doc : IDocument
    {
        public Guid Id { get; set; }
        public int Value { get; set; }
    }
}
