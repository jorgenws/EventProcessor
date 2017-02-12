using System;
using System.Collections.Generic;

namespace EventProcessor
{
    public abstract class Subscriber<T> : ISubscriber where T : IDocument
    {
        private Dictionary<Type, IEventAction> _eventActions;

        private List<IEventActionHolder> _holders;

        public Type DocumentType { get { return typeof(T); } }

        public Subscriber()
        {
            _eventActions = new Dictionary<Type, IEventAction>();
            _holders = new List<IEventActionHolder>();
        }

        public abstract void SetUp();

        public IGetDocuments<U,T> On<U>() where U : IEvent
        {
            EventActionHolder<U, T> holder = new EventActionHolder<U, T>();
            _holders.Add(holder);
            return holder;
        }

        internal void Build()
        {
            foreach (var holder in _holders)
            {
                var eventAction = holder.Build();
                _eventActions.Add(eventAction.EventType, eventAction);

            }
        }

        bool ISubscriber.CanHandleEvent(Type @eventType)
        {
            return _eventActions.ContainsKey(eventType);
        }

        List<Guid> ISubscriber.GetDocumentIdsFor(IEvent @event)
        {
            Func<IEvent, List<Guid>> getDocuments = _eventActions[@event.GetType()].GetDocumentIds;
            return getDocuments(@event);
        }

        void ISubscriber.UpdateDocument(IEvent @event, List<IDocument> documents)
        {
            Action<IEvent, List<IDocument>> update = _eventActions[@event.GetType()].ModifyDocument;
            update(@event, documents);
        }
    }

    internal interface ISubscriber
    {
        Type DocumentType { get; }
        bool CanHandleEvent(Type @eventType);
        List<Guid> GetDocumentIdsFor(IEvent @event);
        void UpdateDocument(IEvent @event, List<IDocument> document);
    }

    internal class EventAction<T, U> : IEventAction  where T : IEvent where U :IDocument
    {
        public Type EventType { get { return typeof(T); } }
        private Func<T, List<Guid>> _getDocumentIds;
        private Action<T, U> _modifyDocument;

        public EventAction(Func<T, List<Guid>> getDocuments, Action<T,U> modifyDocument)
        {
            _getDocumentIds = getDocuments;
            _modifyDocument = modifyDocument;
        }

        public void ModifyDocument(IEvent @event, List<IDocument> documents)
        {
            foreach (var document in documents)
                _modifyDocument((T)@event, (U)document);
        }

        public List<Guid> GetDocumentIds(IEvent @event)
        {
            return _getDocumentIds((T)@event);
        }
    }

    internal interface IEventAction
    {
        Type EventType { get; }

        void ModifyDocument(IEvent @event, List<IDocument> documents);
        List<Guid> GetDocumentIds(IEvent @event);
    }
}
