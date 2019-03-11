using System;
using webapi.Models;

namespace webapi.Services
{
    public class OperationService : IOperationService
    {
        public OperationService(IOperationTransient transient,
            IOperationScoped scoped,
            IOperationSingleton singleton,
            IOperationSingletonInstance singletonInstance
            )
        {
            TransientOperation = transient;
            ScopedOperation = scoped;
            SingletonOperation = singleton;
            SingletonInstanceOperation = singletonInstance;
        }

        public IOperationTransient TransientOperation { get; }
        public IOperationScoped ScopedOperation { get; }
        public IOperationSingleton SingletonOperation { get; }
        public IOperationSingletonInstance SingletonInstanceOperation { get; }

        public void Todo()
        {
            // throw new NotImplementedException();
        }
    }
}
