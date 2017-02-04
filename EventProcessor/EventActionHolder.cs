using System;
using System.Collections.Generic;

namespace EventProcessor
{
    internal class EventActionHolder<T, U> : IOn<T, U>, IGetDocuments<T, U>, IModifyDocument<T, U>, IEventActionHolder where T : IEvent where U : IDocument
    {
        Type _eventType;
        Func<T, List<Guid>> _getDocumentIds;
        Action<T, U> _updateDocument;

        public Type EventType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IGetDocuments<T, U> On()
        {
            _eventType = typeof(T);
            return this;
        }

        public IModifyDocument<T, U> For(Func<T, List<Guid>> getDocumentIds)
        {
            _getDocumentIds = getDocumentIds;
            return this;
        }

        public void Update(Action<T, U> updateDocument)
        {
            _updateDocument = updateDocument;
        }

        public IEventAction Build()
        {
            return new EventAction<T, U>(_getDocumentIds, _updateDocument);
        }

    }

    public interface IOn<T, U>
    {
        IGetDocuments<T, U> On();
    }

    public interface IGetDocuments<T, U>
    {
        IModifyDocument<T, U> For(Func<T, List<Guid>> getDocumentIds);
    }

    public interface IModifyDocument<T, U>
    {
        void Update(Action<T, U> updateDocument);
    }

    internal interface IEventActionHolder
    {
        IEventAction Build();
    }
}