using System;
using System.Collections.Generic;
using System.Diagnostics;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using NUnit.Framework;

namespace Test
{
    [TestFixture]
    public class TestPerformance
    {
        [Test]
        public void CompareExecutionTimeWithAndWithoutTheHandlersFilter()
        {
            CarryOutTheTest(GetWindsorContainerWithHandlerFilter(), "with the handler filter");
            CarryOutTheTest(GetWindsorContainerWithoutHandlerFilter(), "without the handler filter");
            CarryOutTheTest(GetWindsorContainerWithHandlerFilter(), "with the handler filter");
            CarryOutTheTest(GetWindsorContainerWithoutHandlerFilter(), "without the handler filter");
            CarryOutTheTest(GetWindsorContainerWithHandlerFilter(), "with the handler filter");
            CarryOutTheTest(GetWindsorContainerWithoutHandlerFilter(), "without the handler filter");
        }

        static WindsorContainer GetWindsorContainerWithHandlerFilter()
        {
            var container = new WindsorContainer();
            container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel));
            container.Kernel.AddHandlersFilter(new RespectOrderDirectivesHandlersFilter(typeof(IInjection)));
            container.Register(Component.For<NeedsInjections>());
            container.Register(AllTypes.FromThisAssembly().BasedOn<IInjection>().WithService.Base());
            return container;
        }

        static WindsorContainer GetWindsorContainerWithoutHandlerFilter()
        {
            var container = new WindsorContainer();
            container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel));
            container.Register(Component.For<NeedsInjections>());
            container.Register(AllTypes.FromThisAssembly().BasedOn<IInjection>().WithService.Base());
            return container;
        }

        static void CarryOutTheTest(WindsorContainer container, string message)
        {
            var stopwatch = Stopwatch.StartNew();
            const int numberOfCalls = 100000;
            for (var counter = 0; counter < numberOfCalls; counter++)
            {
                var needsInjections = container.Resolve<NeedsInjections>();
                container.Release(needsInjections);
            }
            Console.WriteLine("With: {0:0.0} s for {1} calls ({2})", 
                stopwatch.Elapsed.TotalSeconds, 
                numberOfCalls,
                message);
        }
    }

    public interface IInjection { }

    [ExecutesBefore(typeof(Injection2))]
    public class Injection1 : IInjection { }

    [ExecutesBefore(typeof(Injection3))]
    public class Injection2 : IInjection { }

    [ExecutesBefore(typeof(Injection4))]
    public class Injection3 : IInjection { }

    [ExecutesBefore(typeof(Injection5))]
    public class Injection4 : IInjection { }

    [ExecutesBefore(typeof(Injection6))]
    public class Injection5 : IInjection { }

    [ExecutesAfter(typeof(Injection7))]
    public class Injection6 : IInjection { }

    public class Injection7 : IInjection { }

    public class NeedsInjections
    {
        readonly IEnumerable<IInjection> injections;

        public NeedsInjections(IEnumerable<IInjection> injections)
        {
            this.injections = injections;
        }
    }
}