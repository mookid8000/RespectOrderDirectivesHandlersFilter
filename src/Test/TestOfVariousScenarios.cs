using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using NUnit.Framework;

namespace Test
{
    [TestFixture]
    public class TestOfVariousScenarios
    {
        interface ISomeInterface {}
        class SomeRandomClass  : ISomeInterface{}
        class AnotherRandomClass  : ISomeInterface{}
        class ThirdRandomClass  : ISomeInterface{}
        class FourthRandomClass  : ISomeInterface{}
        class DependsOnRandomClasses
        {
            readonly IEnumerable<ISomeInterface> someInterfaces;

            public DependsOnRandomClasses(IEnumerable<ISomeInterface> someInterfaces)
            {
                this.someInterfaces = someInterfaces;
            }
        }

        [Test]
        public void DoesntDoAnythingWhenClassesAreNotDecorated()
        {
            var container = new WindsorContainer();
            
            container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel));
            container.Kernel.AddHandlersFilter(new RespectOrderDirectivesHandlersFilter(typeof (ISomeInterface)));

            container.Register(Component.For<ISomeInterface>().ImplementedBy<SomeRandomClass>(),
                               Component.For<ISomeInterface>().ImplementedBy<AnotherRandomClass>(),
                               Component.For<ISomeInterface>().ImplementedBy<ThirdRandomClass>(),
                               Component.For<ISomeInterface>().ImplementedBy<FourthRandomClass>(),
                               Component.For<DependsOnRandomClasses>());

            var dependsOnRandomClasses = container.Resolve<DependsOnRandomClasses>();

            Assert.Pass("Yay!");
        }
    }
}