using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using NUnit.Framework;

namespace Test
{
    /// <summary>
    /// This is to test that the code from http://mookid.dk/oncode/archives/2295 works
    /// </summary>
    [TestFixture]
    public class TestBasedOnCodeFromBlogPost
    {
        [Test]
        public void WorksWithClassesFromBlogPost()
        {
            var container = new WindsorContainer();
            container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel));
            container.Kernel.AddHandlersFilter(new RespectOrderDirectivesHandlersFilter(typeof(ITask)));

            container.Register(AllTypes.FromThisAssembly().BasedOn<ITask>().WithService.Base(),
                               Component.For<TaskExecutor>());

            var taskExecutor = container.Resolve<TaskExecutor>();

            var messages = taskExecutor.ExecuteTasks().ToArray();

            Assert.AreEqual(new[]
                                {
                                    "Executing ValidateCreditCards",
                                    "Executing ValidateDebitAccountBalance",
                                    "Executing ExecutePayment",
                                    "Executing GenerateReceipt",
                                    "Executing ReportWarnings"
                                }, messages);
        }

        public class TaskExecutor
        {
            readonly IEnumerable<ITask> tasks;

            public TaskExecutor(IEnumerable<ITask> tasks)
            {
                this.tasks = tasks;
            }

            public IEnumerable<string> ExecuteTasks()
            {
                return tasks.Select(task => task.Execute());
            }
        }

        public interface ITask
        {
            string Execute();
        }

        public class ExecutePayment : ITask
        {
            public string Execute()
            {
                return string.Format("Executing {0}", GetType().Name);
            }
        }

        [ExecutesBefore(typeof(ExecutePayment))]
        [ExecutesBefore(typeof(ValidateDebitAccountBalance))]
        public class ValidateCreditCards : ITask
        {
            public string Execute()
            {
                return string.Format("Executing {0}", GetType().Name);
            }
        }

        [ExecutesBefore(typeof(ExecutePayment))]
        public class ValidateDebitAccountBalance : ITask
        {
            public string Execute()
            {
                return string.Format("Executing {0}", GetType().Name);
            }
        }

        [ExecutesAfter(typeof(ExecutePayment))]
        public class ReportWarnings : ITask
        {
            public string Execute()
            {
                return string.Format("Executing {0}", GetType().Name);
            }
        }

        [ExecutesAfter(typeof(ExecutePayment))]
        [ExecutesBefore(typeof(ReportWarnings))]
        public class GenerateReceipt : ITask
        {
            public string Execute()
            {
                return string.Format("Executing {0}", GetType().Name);
            }
        }
    }
}