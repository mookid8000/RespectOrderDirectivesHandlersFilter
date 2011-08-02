using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel;

namespace Test
{
    // I'm not a fan of regions... but in order to distribute only one source file, I have decided to let
    // go of my vanity and just cram all my codez into some... sorry!

    #region the handlers filter

    public class RespectOrderDirectivesHandlersFilter : IHandlersFilter
    {
        readonly Type typeToCareAbout;
        readonly HandlerSorter handlerSorter = new HandlerSorter();

        public RespectOrderDirectivesHandlersFilter(Type typeToCareAbout)
        {
            this.typeToCareAbout = typeToCareAbout;
        }

        public bool HasOpinionAbout(Type service)
        {
            return service == typeToCareAbout;
        }

        public IHandler[] SelectHandlers(Type service, IHandler[] handlers)
        {
            return handlerSorter.Sort(handlers);
        }
    }

    #endregion

    #region attributes

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ExecutesAfterAttribute : Attribute
    {
        readonly Type type;

        public ExecutesAfterAttribute(Type type)
        {
            this.type = type;
        }

        public Type Type
        {
            get { return type; }
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ExecutesBeforeAttribute : Attribute
    {
        readonly Type type;

        public ExecutesBeforeAttribute(Type type)
        {
            this.type = type;
        }

        public Type Type
        {
            get { return type; }
        }
    }

    #endregion

    #region interfaces

    public interface IExecuteAfter<T> { }

    public interface IExecuteBefore<T> { }
    
    #endregion

    #region extensions

    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> items, Action<T> handle)
        {
            foreach (var item in items)
            {
                handle(item);
            }
        }
    }
    
    #endregion

    #region sorter

    public class HandlerSorter
    {
        readonly IDictionary<string, IDictionary<Type, int>> cachedOrders = new Dictionary<string, IDictionary<Type, int>>();

        public IHandler[] Sort(IHandler[] handlers)
        {
            var orders = GetOrders(handlers);
            
            var handlersToReturn = new IHandler[handlers.Length];
            Array.Copy(handlers, handlersToReturn, handlers.Length);
            Array.Sort(handlersToReturn, (o1, o2) => orders[o1.ComponentModel.Implementation].CompareTo(orders[o2.ComponentModel.Implementation]));
            
            return handlersToReturn;
        }

        IDictionary<Type, int> GetOrders(IHandler[] handlers)
        {
            var key = GetKey(handlers);

            lock (cachedOrders)
            {
                return cachedOrders.ContainsKey(key)
                           ? cachedOrders[key]
                           : cachedOrders[key] = BuildTypeOrderDictionary(handlers);
            }
        }

        string GetKey(IEnumerable<IHandler> nodes)
        {
            return string.Join(",", nodes.Select(n => n.ComponentModel.Implementation.FullName).ToArray());
        }

        IDictionary<Type, int> BuildTypeOrderDictionary(IHandler[] handlers)
        {
            var nodes = GetNodes(handlers);

            AssertNoCycles(nodes);

            var orderedNodes = Sort(nodes);

            return ToDictionary(orderedNodes);
        }

        List<Node> GetNodes(IEnumerable<IHandler> handlers)
        {
            var nodes = handlers.Select(o => o.ComponentModel.Implementation)
                .Distinct()
                .Select(t => new Node(t))
                .ToList();

            foreach (var node in nodes)
            {
                var currentNode = node;

                var dependencies = nodes
                    .Where(n => n.MustExecuteBefore(currentNode)
                                || currentNode.MustExecuteAfter(n))
                    .ToList();

                node.Incoming.AddRange(dependencies);
            }

            return nodes;
        }

        void AssertNoCycles(List<Node> nodes)
        {
            foreach (var node in nodes)
            {
                nodes.ForEach(n => n.SetColor(Colors.Black));
                node.SetColor(Colors.Brown);
                var stack = new Stack<Node>();
                AssertNoCycles(node, nodes, stack);
            }

            nodes.ForEach(n => n.SetColor(Colors.Black));
        }

        void AssertNoCycles(Node node, IEnumerable<Node> nodes, Stack<Node> stack)
        {
            stack.Push(node);

            foreach (var incoming in node.Incoming)
            {
                if (incoming.Color == Colors.Brown)
                {
                    var text = string.Format("Cannot determine order - cycle detected: {0}", string.Join(", ", stack.Select(s => s.Type.Name).ToArray()));

                    throw new InvalidOperationException(text);
                }

                incoming.SetColor(Colors.White);

                AssertNoCycles(incoming, nodes, stack);
            }

            stack.Pop();
        }

        /// <summary>
        /// Sort nodes using the following algo:
        ///     1: Make WHITE all nodes that have no dependencies
        ///     2: Find a node that is BLACK and has only WHITE dependencies
        ///     3: Color that node WHITE
        ///     4: If not all nodes are WHITE goto 2
        /// 
        /// - collecting nodes in a list in the order they have been colored WHITE.
        /// </summary>
        /// <param name="nodes">List of nodes to figure out an execution order for</param>
        /// <returns>Ordered list of nodes in the order they should be executed</returns>
        List<Node> Sort(IEnumerable<Node> nodes)
        {
            var executionOrder = new List<Node>();

            nodes.Where(n => !n.Incoming.Any())
                .ForEach(n =>
                {
                    executionOrder.Add(n);
                    n.SetColor(Colors.White);
                });

            while (!nodes.All(n => n.Color == Colors.White))
            {
                var nextNodeToExecute =
                    nodes.First(n => n.Color == Colors.Black
                                     && n.Incoming.All(i => i.Color == Colors.White));

                nextNodeToExecute.SetColor(Colors.White);

                executionOrder.Add(nextNodeToExecute);
            }

            return executionOrder;
        }

        IDictionary<Type, int> ToDictionary(List<Node> nodes)
        {
            var dict = new Dictionary<Type, int>();

            for (var counter = 0; counter < nodes.Count; counter++)
            {
                dict[nodes[counter].Type] = counter;
            }

            return dict;
        }

        class Node
        {
            public Node(Type type)
            {
                Type = type;
                Incoming = new List<Node>();
            }

            public Type Type { get; private set; }

            public List<Node> Incoming { get; private set; }

            public Colors Color { get; private set; }

            public void SetColor(Colors newColor)
            {
                Color = newColor;
            }

            public override string ToString()
            {
                return string.Format("{0} (color: {1}, before: {2})",
                                     Type.Name,
                                     Color,
                                     string.Join(", ", Incoming.Select(i => i.Type.Name)));
            }

            public bool MustExecuteAfter(Node node)
            {
                var interfaceToLookFor = typeof(IExecuteAfter<>).MakeGenericType(node.Type);

                return Type.GetInterfaces().Any(i => i == interfaceToLookFor)
                       || AttributesOfType<ExecutesAfterAttribute>().Any(a => a.Type == node.Type);
            }

            public bool MustExecuteBefore(Node node)
            {
                var interfaceToLookFor = typeof(IExecuteBefore<>).MakeGenericType(node.Type);

                return Type.GetInterfaces().Any(i => i == interfaceToLookFor)
                       || AttributesOfType<ExecutesBeforeAttribute>().Any(a => a.Type == node.Type);
            }

            IEnumerable<T> AttributesOfType<T>() where T : Attribute
            {
                return Type.GetCustomAttributes(typeof(T), false).Cast<T>();
            }
        }

        enum Colors
        {
            White,
            Black,
            Brown,
        }
    }
    #endregion
}
