using System;
using Autofac;
using webapi.Models;
using webapi.Services;

namespace webapi.Modules
{
    public class DefaultModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // InstancePerLifetimeScope：同一个Lifetime生成的对象是同一个实例
            // SingleInstance：单例模式，每次调用，都会使用同一个实例化的对象；每次都用同一个对象
            // InstancePerDependency：默认模式，每次调用，都会重新实例化对象；每次请求都创建一个新的对象
            builder.RegisterType<Operation>().As<IOperationTransient>().InstancePerDependency();
            builder.RegisterType<Operation>().As<IOperationScoped>().InstancePerLifetimeScope();
            builder.RegisterType<Operation>().As<IOperationSingleton>().SingleInstance();
            builder.Register(t => new Operation(Guid.Empty){}).As<IOperationSingletonInstance>();

            builder.RegisterType<OperationService>().As<IOperationService>();
            builder.RegisterType<OperationService>().As<IOperationService>().AsSelf();
        }
    }
}
