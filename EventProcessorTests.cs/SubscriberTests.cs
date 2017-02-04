using EventProcessor;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventProcessorTests
{
    [TestFixture]
    public class SubscriberTests
    {
        readonly Guid _documentId1 = Guid.Parse("{68EE437E-B64F-4674-B8B3-5EADB4EAE308}");
        readonly Guid _documentId2 = Guid.Parse("{89B3D0B8-8517-4FD1-8162-5E796C57251D}");


        [Test]
        public void CanHandleEvent()
        {
            var subscriber = new TestSubscriber();
            subscriber.SetUp();
            subscriber.Build();

            Assert.IsTrue(subscriber.CanHandleEvent(typeof(Event)));
        }

        [Test]
        public void GetDocumentIds()
        {
            var subscriber = new TestSubscriber();
            subscriber.SetUp();
            subscriber.Build();

            var @event = new Event
            {
                DocumentIds = new List<Guid>(new[] { _documentId1, _documentId2 }),
                Value = 42
            };

            List<Guid> documentIds = subscriber.GetDocumentIdsFor(@event);

            CollectionAssert.Contains(documentIds, _documentId1);
            CollectionAssert.Contains(documentIds, _documentId2);
        }

        [Test]
        public void UpdateDocument()
        {
            var subscriber = new TestSubscriber();
            subscriber.SetUp();
            subscriber.Build();

            var @event = new Event
            {
                DocumentIds = new List<Guid>(new[] { _documentId1, _documentId2 }),
                Value = 42
            };

            var dto1 = new Document { Id = _documentId1, Value = 0 };
            var dto2 = new Document { Id = _documentId2, Value = 0 };
            var documents = new List<IDocument>();
            documents.Add(dto1);
            documents.Add(dto2);

            subscriber.UpdateDocument(@event, documents);

            Assert.AreEqual(42, dto1.Value);
            Assert.AreEqual(42, dto2.Value);
        }
    }

    public class TestSubscriber : Subscriber<Document>
    {
        public override void SetUp()
        {
            On<Event>().For(e => e.DocumentIds).Update((e, d) => d.Value = e.Value);
        }
    }

    public class Document : IDocument
    {
        public Guid Id { get; set; }
        public int Value { get; set; }
    }

    public class Event : IEvent
    {
        public List<Guid> DocumentIds { get; set; }

        public int Value { get; set; }
    }
}
