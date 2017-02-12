using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace EventProcessor
{
    internal class DocumentFactory : IDocumentFactory
    {
        ConcurrentDictionary<Type, Func<IDocument>> _newDocumentFactory;
        ConcurrentDictionary<Type, Func<IBinarySerializer, byte[], IDocument>> _deserializeDocumentFactory;

        IBinarySerializer _serializer;

        public DocumentFactory(IBinarySerializer serializer)
        {
            _newDocumentFactory = new ConcurrentDictionary<Type, Func<IDocument>>();
            _deserializeDocumentFactory = new ConcurrentDictionary<Type, Func<IBinarySerializer, byte[], IDocument>>();
            _serializer = serializer;
        }

        public IDocument CreateNew(Guid documentId, Type documentType)
        {
            IDocument document;
            if (!_newDocumentFactory.ContainsKey(documentType))
            {
                var factory = Expression.Lambda<Func<IDocument>>(Expression.New(documentType)).Compile();
                _newDocumentFactory.AddOrUpdate(documentType, d => factory, (key, oldValue) => factory);
            }

            document = _newDocumentFactory[documentType]();
            document.Id = documentId;

            return document; ;
        }

        public IDocument Create(byte[] bytes, Type documentType)
        {
            IDocument document;
            if (!_deserializeDocumentFactory.ContainsKey(documentType))
            {
                var instance = Expression.Parameter(typeof(IBinarySerializer));
                var parameter = Expression.Parameter(typeof(byte[]));
                var method = typeof(IBinarySerializer).GetMethod("Deserialize").MakeGenericMethod(documentType);
                var methodCall = Expression.Call(instance, method, new Expression[] { parameter });
                
                var deserializer = Expression.Lambda<Func<IBinarySerializer, byte[], IDocument>>(methodCall, instance, parameter).Compile();

                _deserializeDocumentFactory.AddOrUpdate(documentType, deserializer, (key, oldValue) => deserializer);
            }

            document = _deserializeDocumentFactory[documentType](_serializer, bytes);
            return document;
        }
    }

    internal interface IDocumentFactory
    {
        IDocument CreateNew(Guid id, Type documentType);
        IDocument Create(byte[] bytes, Type documentType);
    }
}
