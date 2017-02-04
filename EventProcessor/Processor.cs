using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace EventProcessor
{
    public class Processor
    {
        public void a()
        {
            IDocumentStore documentStore = null;
            var processCache = new ProcessCache(documentStore);


            var options = new DataflowBlockOptions
            {
                BoundedCapacity = 1000000
            };

            var inputBlock = new BufferBlock<IEvent>(options);

            //reflection to find subscribers

            //for subscribers create a transfromblock. Add predicate based on CanHandleEvent.
            //given the subscriber, we know the document type and given the event we know the documentids
            //start preload

            //next step is to deserialze the documents that are not deserialized yet.

            //now that we have actual documents we start to update them based on the events.

            //we clone the resulting doucment and send it to be stored with its event serial id number.


            //Batching document loading?

            //Batching document storage?
        }

    }
}
