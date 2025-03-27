using BlueDeep.Client;
using Microsoft.Extensions.DependencyInjection;

namespace BlueDeepExample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();
            services.AddSingleton<BlueDeepClient>(provider => new BlueDeepClient("localhost", 9090));
            var serviceProvider = services.BuildServiceProvider();

            var client = serviceProvider.GetService<BlueDeepClient>() ?? throw new ArgumentNullException(nameof(BlueDeepClient));
            
            //Subscribe on Topic "testTopic1"
            await client.SubscribeAsync<TestMessage>("testTopic1", TestFuncAsync);
       

            _=Task.Run(async () =>
            {
                var i = 0;
                while (true)
                {
                    await client.PublishAsync("testTopic1", new TestMessage(DateTime.UtcNow, i++.ToString()), 0);
                    Task.Delay(1000).Wait();
                }
            });

            Console.ReadLine();
            // while (true)
            // {
            //     Task.Delay((int)Random.Shared.NextInt64(100)).Wait();
            // }
        }
        
        private static Task TestFuncAsync(TestMessage i)
        {
            Console.WriteLine($"Start processing 'topic1' dt={i.Date}\tval={i.Message}");
            Task.Delay(5000).Wait();
            Console.WriteLine($"End processing 'topic1' dt={i.Date}\tval={i.Message}");
            return Task.CompletedTask;
        }
    }
    public record TestMessage(DateTime Date, string Message);
}