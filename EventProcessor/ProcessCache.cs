using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace EventProcessor
{
    internal class ProcessCache
    {
        private ConcurrentDictionary<Type, DocumentDictionary> _documents;

        private IDocumentStore _documentStore;
        private IDocumentFactory _documentFactory;

        public ProcessCache(IDocumentStore documentStore, IDocumentFactory documentFactory)
        {            
            _documents = new ConcurrentDictionary<Type, DocumentDictionary>();
            _documentStore = documentStore;
            _documentFactory = documentFactory;
        }

        public void Preload(Type documentType, List<Guid> documentIds)
        {
            var documents = GetDictionary(documentType);
            documents.Preload(documentIds);
        }

        public List<IDocument> Load(Type documentType, List<Guid> documentIds)
        {
            var documents = GetDictionary(documentType);
            return documents.Load(documentIds);
        }

        private DocumentDictionary GetDictionary(Type documentType)
        {
            if (!_documents.ContainsKey(documentType))
            {
                var documentDictionary = new DocumentDictionary(_documentStore, _documentFactory, documentType);
                _documents.AddOrUpdate(documentType, documentDictionary, (key, oldValue) => documentDictionary);
            }

            return _documents[documentType];
        }

        internal class DocumentDictionary
        {
            private object _lock = new object();

            private Type _documentType;
            private Dictionary<Guid, IDocumentWrapper> _documents;
            private IDocumentStore _documentStore;
            private IDocumentFactory _documentFactory;

            public DocumentDictionary(IDocumentStore documentStore, IDocumentFactory documentFactory, Type documentType)
            {
                _documents = new Dictionary<Guid, IDocumentWrapper>();
                _documentStore = documentStore;
                _documentFactory = documentFactory;
                _documentType = documentType;
            }

            public void Preload(List<Guid> documentIds)
            {
                var externalDocuments = new List<Guid>();

                lock (_lock)
                {
                    //find loaded documents
                    var localDocuments = new List<IDocumentWrapper>();
                    foreach (var documentId in documentIds)
                    {
                        if (_documents.ContainsKey(documentId))
                            _documents[documentId].ReadCount++;
                        else
                            externalDocuments.Add(documentId);
                    }

                    //find documents that are not yet loaded
                    var documents = _documentStore.Load(externalDocuments);
                    foreach (var document in documents)
                    {
                        document.ReadCount++;
                        _documents.Add(document.Id, document);
                    }

                    //Add holders for not yet created documents
                    foreach (var documentId in externalDocuments)
                        if (!_documents.ContainsKey(documentId))
                        {
                            _documents.Add(documentId, new DocumentWrapper
                            {
                                Id = documentId,
                                Document = _documentFactory.CreateNew(documentId, _documentType),
                                ReadCount = 1
                            });
                        }
                }
            }

            public List<IDocument> Load(List<Guid> documentIds)
            {
                var externalDocuments = new List<Guid>();
                var result = new List<IDocument>();

                lock (_lock)
                {
                    foreach (var documentId in documentIds)
                    {
                        if (_documents.ContainsKey(documentId))
                        {
                            var document = _documents[documentId];
                            result.Add(document.Document);
                        }
                        else
                            externalDocuments.Add(documentId);
                    }

                    //There might be missing documents
                    if (externalDocuments.Any())
                    {
                        //If there are any missing documents start load procedure for those documents
                        Preload(externalDocuments);

                        foreach (var documentId in externalDocuments)
                        {
                            var document = _documents[documentId];
                            result.Add(document.Document);
                        }
                    }
                }

                return result;
            }

            public void UnloadRead(List<Guid> documentIds)
            {
                lock (_lock)
                {
                    foreach (var documentId in documentIds)
                    {
                        if (_documents.ContainsKey(documentId))
                        {
                            var document = _documents[documentId];
                            document.ReadCount--;

                            if (document.ReadCount == 0 && document.WriteCount == 0)
                                _documents.Remove(documentId);
                        }
                    }
                }
            }

            public void SetWrite(List<Guid> documentIds)
            {
                lock (_lock)
                {
                    foreach (var documentId in documentIds)
                        _documents[documentId].WriteCount++;
                }
            }

            public void FinishedWriting(List<Guid> documentIds)
            {
                lock (_lock)
                {
                    foreach (var documentId in documentIds)
                    {
                        var document = _documents[documentId];
                        document.WriteCount--;

                        if (document.ReadCount == 0 && document.WriteCount == 0)
                            _documents.Remove(documentId);
                    }
                }
            }
        }
    }

    internal class DocumentWrapper : IDocumentWrapper
    {
        public Guid Id { get; set; }
        public Type DocumentType { get; set; }
        public IDocument Document { get; set; }
        public int ReadCount { get; set; }
        public int WriteCount { get; set; }
    }

    public interface IDocumentWrapper
    {
        Guid Id { get; set; }
        IDocument Document { get; set; }
        int ReadCount { get; set; }
        int WriteCount { get; set; }
    }

    public interface IDocument
    {
        Guid Id { get; set; }
    }

    public interface IDocumentStore
    {
        IEnumerable<IDocumentWrapper> Load(List<Guid> documentIds);
        void Save(List<IDocumentWrapper> documents);
    }
}
