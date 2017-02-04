using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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

        public void Preload(Type documentType, List<Guid> documentIds)
        {
            var documents = _documents[documentType];
            documents.Preload(documentIds);
        }

        public List<IDocument> Load(Type documentType, List<Guid> documentIds)
        {
            var documents = _documents[documentType];
            return documents.Load(documentIds);
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
                            _documents.Add(documentId, new DocumentWrapper { Id = documentId, ReadCount = 1 });
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
                            result.Add(document.Document());
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
                            result.Add(document.Document());
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
        public byte[] SerializedDocument { get; set; }
        private IDocument _document;

        public IDocument Document()
        {
            if (_document != null)
                return _document;

            if (SerializedDocument != null)
            {
                //deserialize
            }

            var factory = Expression.Lambda<Func<IDocument>>(Expression.New(DocumentType)).Compile();

            _document = factory();
            _document.Id = Id;
            return _document;
        }

        public int ReadCount { get; set; }
        public int WriteCount { get; set; }
    }

    public interface IDocumentWrapper
    {
        Guid Id { get; set; }
        byte[] SerializedDocument { get; set; }
        IDocument Document();
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
