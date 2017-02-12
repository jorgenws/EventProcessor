using Autofac;

namespace EventProcessor
{
    public interface IDependencies
    {
        void Add(ContainerBuilder conatiner);
    }
}
