namespace EventProcessor
{
    public interface IEvent
    {
        ulong SerialNumber { get; set; }
    }
}
