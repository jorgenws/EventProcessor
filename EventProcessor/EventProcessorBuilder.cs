using Autofac;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks.Dataflow;
using System;

namespace EventProcessor
{
    public class EventProcessorBuilder : ISetDocumentStore, IAddSubscribers, IAddDependencies, IEventProcessorBuilder
    {
        IDocumentStore _documentStore;
        IEnumerable<Assembly> _subscriberAssemblies;
        IEnumerable<Assembly> _dependenciesAssemblies;

        public IAddSubscribers SetRepository(IDocumentStore store)
        {
            _documentStore = store;
            return this;
        }

        public IAddDependencies AddSubscribers(IEnumerable<Assembly> assemblies)
        {
            _subscriberAssemblies = assemblies;
            return this;
        }

        public IEventProcessorBuilder AddDependencies(IEnumerable<Assembly> assemblies)
        {
            _dependenciesAssemblies = assemblies;
            return this;
        }

        public void Build()
        {
            var builder = new ContainerBuilder();

            var assemblies = new List<Assembly>();

            var finder = new TypeFinder();
            foreach (var subscriber in finder.FindTypesOf<ISubscriber>(assemblies))
                builder.RegisterType(subscriber).AsSelf();

            var dependenciesFactory = new DependenciesFactory();
            var dependencies = dependenciesFactory.Create(assemblies);
            foreach (var dependency in dependencies)
                dependency.Add(builder);

            var container = builder.Build();

            var subscribers = container.Resolve<IEnumerable<ISubscriber>>();

            var processor = new Processor(subscribers, _documentStore);


        }
    }

    public interface ISetDocumentStore
    {
        IAddSubscribers SetRepository(IDocumentStore repository);
    }

    public interface IAddSubscribers
    {
        IAddDependencies AddSubscribers(IEnumerable<Assembly> assemblies);
    }

    public interface IAddDependencies
    {
        IEventProcessorBuilder AddDependencies(IEnumerable<Assembly> assemblies);
    }

    public interface IEventProcessorBuilder
    {
        void Build();
    }

    internal class SubscriberResult
    {
        public ulong SerialNumber { get; set; }
        public List<ProcessedDocument> ProcessedDocuments { get; set; }
    }

    internal class ProcessedDocument
    {
        Guid DocumentId { get; set; }
        byte[] SerializedDocument { get; set; }
    }
}
