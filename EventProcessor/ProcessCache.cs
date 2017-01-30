using System;
using System.Collections.Generic;

namespace EventProcessor
{
    internal class ProcessCache
    {
        private Dictionary<Type, DocumentDictionary> _documents;

        private IDocumentStore _documentStore;

        public ProcessCache(IDocumentStore documentStore)
        {
            _documents = new Dictionary<Type, DocumentDictionary>();
            _documentStore = documentStore;
        }

        internal class DocumentDictionary
        {
            private object _lock = new object();

            private Dictionary<Guid, IDocumentWrapper> _documents;
            private IDocumentStore _documentStore;

            public DocumentDictionary(IDocumentStore documentStore)
            {
                _documents = new Dictionary<Guid, IDocumentWrapper>();
                _documentStore = documentStore;
            }

            public void Preload(IEnumerable<Guid> documentIds)
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
                }
            }

            public IEnumerable<IDocumentWrapper> Load(IEnumerable<Guid> documentIds)
            {
                var externalDocuments = new List<Guid>();
                var result = new List<IDocumentWrapper>();

                lock (_lock)
                {

                    foreach (var documentId in documentIds)
                    {
                        if (_documents.ContainsKey(documentId))
                            result.Add(_documents[documentId]);
                        else
                            externalDocuments.Add(documentId);
                    }

                    var documents = _documentStore.Load(externalDocuments);
                    foreach (var document in documents)
                    {
                        document.ReadCount++;
                        _documents.Add(document.Id, document);
                    }
                    result.AddRange(documents);
                }

                return result;
            }

            public void UnloadRead(IEnumerable<Guid> documentIds)
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

            public void SetWrite(IEnumerable<Guid> documentIds)
            {
                lock (_lock)
                {
                    foreach (var documentId in documentIds)
                        _documents[documentId].WriteCount++;
                }
            }

            public void FinishedWriting(IEnumerable<Guid> documentIds)
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
        public byte[] SerializedDocument { get; set; }

        public T Document<T>(Guid id) where T : IDocument, new()
        {
            return new T();
        }

        public int ReadCount { get; set; }
        public int WriteCount { get; set; }
    }

    public interface IDocumentWrapper
    {
        Guid Id { get; set; }
        byte[] SerializedDocument { get; set; }
        T Document<T>(Guid id) where T : IDocument, new();
        int ReadCount { get; set; }
        int WriteCount { get; set; }
    }

    public interface IDocument
    {
        Guid Id { get; set; }
    }

    public interface IDocumentStore
    {
        IEnumerable<IDocumentWrapper> Load(IEnumerable<Guid> documentIds);
        void Save(IEnumerable<IDocumentWrapper> documents);
    }
}
