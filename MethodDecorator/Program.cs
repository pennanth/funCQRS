using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MethodDecorator
{
    public static class Program
    {
        static void Main(string[] args)
        {
            // Decorators
            RegisterHandler(
                DecorateMethod<DoSomethingCommand>(DoSomethingHandler, LogCommand)
                .DecorateMethod(AuditStart, AuditEnd)
            );

            // Running with decorators
            DoSomethingCommand command = new DoSomethingCommand("World");
            ExecuteCommand(command);

            // Dependency injection
            RegisterInjection<Adder>(Add);
            RegisterInjection<Printer>(Print);

            RegisterCommandHandler<AddNumbersCommand, Adder, Printer>(AddNumbers);
            RegisterCommandAsyncHandler<AddNumbersCommand, Adder, Printer>(AddNumbersAsync);

            // Running with dependency injection
            AddNumbersCommand cmd2 = new AddNumbersCommand(3, 5);
            ExecuteCommand(cmd2);

            // Running async command handler with dependency injection
            Task.WaitAll(ExecuteCommandAsync(new AddNumbersCommand(8, 4)));

            Console.ReadLine();
        }

        private static void RegisterCommandHandler<T>(Action<T> handler) =>
            RegisterHandler(handler);

        static Dictionary<Type, object> asyncMethodMap = new Dictionary<Type, object>();
        private static void RegisterCommandAsyncHandler<T>(Func<T, Task> handler) =>
            asyncMethodMap.Add(typeof(T), handler);

        private static void RegisterCommandHandler<T1, T2, T3>(Action<T1, T2, T3> handler)
            where T1 : class
            where T2 : class
            where T3 : class
        {
            RegisterCommandHandler<T1>((T1 t) => handler(t, ResolveInjection<T2>(), ResolveInjection<T3>()));
        }

        private static void RegisterCommandAsyncHandler<T1, T2, T3>(Func<T1, T2, T3, Task> handler)
            where T1 : class
            where T2 : class
            where T3 : class
        {
            RegisterCommandAsyncHandler<T1>((T1 t) => handler(t, ResolveInjection<T2>(), ResolveInjection<T3>()));
        }

        private static void RegisterInjection<T>(T add) where T : class
        {
            methodMap.Add(typeof(T), add);
        }

        public static T ResolveInjection<T>() where T : class => methodMap[typeof(T)] as T;

        public delegate int Adder(int a, int b);
        public delegate void Printer(object str);

        public static void AddNumbers(AddNumbersCommand command, Adder adder, Printer printer)
        {
            int result = adder(command.A, command.B);
            printer(result);
        }

        public static async Task AddNumbersAsync(AddNumbersCommand command, Adder adder, Printer printer)
        {
            int result = adder(command.A, command.B);
            await Task.Run(() => printer(result));
        }

        private static int Add(int a, int b) => a + b;
        private static void Print(object str) => Console.WriteLine(str);

        public static Action<T> Resolve<T>() => methodMap[typeof(T)] as Action<T>;
        public static Func<T, Task> ResolveAsync<T>() => asyncMethodMap[typeof(T)] as Func<T, Task>;


        private static void ExecuteCommand<T>(T command) => Resolve<T>()?.Invoke(command);

        private static async Task ExecuteCommandAsync<T>(T command) => await ResolveAsync<T>()?.Invoke(command);


        public static void LogCommand<T>(T command)
        {
            Console.WriteLine("Doing logging");
        }

        public static void AuditStart<T>(T command)
        {
            Console.WriteLine("Starting audit");
        }

        public static void AuditEnd<T>(T command)
        {
            Console.WriteLine("Ending audit");
        }


        public static Action<T> DecorateMethod<T>(this Action<T> methodToDecorate, Action<T> decorator) =>
            (T t) => { decorator(t); methodToDecorate(t); };

        public static Action<T> DecorateMethod<T>(this Action<T> methodToDecorate, Action<T> decoratorBefore, Action<T> decoratorAfter) =>
            (T t) => { decoratorBefore(t); methodToDecorate(t); decoratorAfter(t); };


        static Dictionary<Type, object> methodMap = new Dictionary<Type, object>();
        public static void RegisterHandler<T>(Action<T> commandHandler)
        {
            methodMap.Add(typeof(T), commandHandler);
        }


        public static void DoSomethingHandler(DoSomethingCommand command)
        {
            Console.WriteLine($"Method {nameof(DoSomethingHandler)} is called with command parameter {command.Name}");
        }
    }

    public class AddNumbersCommand
    {
        public AddNumbersCommand(int v1, int v2)
        {
            this.A = v1;
            this.B = v2;
        }

        public int A { get; internal set; }
        public int B { get; internal set; }
    }

    public class DoSomethingCommand
    {
        public object Name { get; private set; }

        public DoSomethingCommand(string name)
        {
            this.Name = name;
        }

    }
}
