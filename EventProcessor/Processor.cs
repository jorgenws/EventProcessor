using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace EventProcessor
{
    internal class Processor
    {
        IEnumerable<ISubscriber> _subscribers;
        IDocumentStore _documentStore;

        public Processor(IEnumerable<ISubscriber> subscribers, IDocumentStore documentStore)
        {
            _subscribers = subscribers;
            _documentStore = documentStore;
        }

        public void Initialize()
        {
            IBinarySerializer binarySerializer = new BinarySerializer();
            var documentFactory = new DocumentFactory(binarySerializer);
            var processCache = new ProcessCache(_documentStore, documentFactory);

            var options = new DataflowBlockOptions { BoundedCapacity = 1000000 };
            var inputBlock = new BufferBlock<IEvent>(options);

            var broadcastBlock = new BroadcastBlock<IEvent>(e => e);

            inputBlock.LinkTo(broadcastBlock);

            var processedDocumentsBlock = new BufferBlock<SubscriberResult>(options);

            foreach (var subscriber in _subscribers)
            {
                var subscriberPreloadBlock = new TransformBlock<IEvent, IEvent>(e => Preload(processCache, subscriber, e));
                var subscriberProcessBlock = new TransformBlock<IEvent, SubscriberResult>(e=>Process(processCache, binarySerializer, subscriber, e));

                broadcastBlock.LinkTo(subscriberPreloadBlock, e => subscriber.CanHandleEvent(e.GetType()));
                subscriberPreloadBlock.LinkTo(subscriberProcessBlock);
                subscriberProcessBlock.LinkTo(processedDocumentsBlock);
            }

            //The BoundedCapacity is set to one to wait for the write operation to finish before we get a new batch.
            //This will increase the batch size and performance
            var writeBlock = new ActionBlock<IReadOnlyList<SubscriberResult>>(rs => WriteDocuments(rs), 
                                                                              new ExecutionDataflowBlockOptions { BoundedCapacity = 1 });
            
            //Starts the dynamic batching to ensure that the process doesnt stop while waiting for a batchblock (for example)            
            Task.Factory.StartNew(()=>BatchItems(processedDocumentsBlock, writeBlock));
        }

        private static IEvent Preload(ProcessCache cache, ISubscriber subscriber, IEvent @event)
        {
            cache.Preload(subscriber.DocumentType, subscriber.GetDocumentIdsFor(@event));
            return @event;
        }

        private static SubscriberResult Process(ProcessCache cache, IBinarySerializer serializer, ISubscriber subscriber, IEvent @event)
        {
            var documents = cache.Load(subscriber.DocumentType, subscriber.GetDocumentIdsFor(@event));
            subscriber.UpdateDocument(@event, documents);

            var processedDocuments = new List<ProcessedDocument>();
            foreach (var document in documents)
            {
                //TODO: check that it uses the actual type, not the interface type...
                var serializedDocument = serializer.Serialize(document);
                processedDocuments.Add(new ProcessedDocument { DocumentId = document.Id, SerializedDocument = serializedDocument });
            }

            return new SubscriberResult { SerialNumber = @event.SerialNumber, ProcessedDocuments = processedDocuments };
        }

        private static async Task BatchItems<T>(IReceivableSourceBlock<T> source, ITargetBlock<IReadOnlyList<T>> target)
        {
            while (true)
            {
                var messages = new List<T>();

                if(!await source.OutputAvailableAsync())
                {
                    //source was completed
                    target.Complete();
                    return;
                }

                T item;
                while (source.TryReceive(out item))
                    messages.Add(item);

                target.Post(messages);
            }
        }

        private static void WriteDocuments(IReadOnlyCollection<SubscriberResult> processedDocuments)
        {
            //purge older duplicates
            //create batch and send to repository
        }
    }
}
