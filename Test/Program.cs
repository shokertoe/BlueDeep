using Microsoft.Extensions.DependencyInjection;
using BlueDeep.Client;

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
            
            await client.SubscribeAsync<TestMessage>("testTopic1", async (i) =>
            {
                Console.WriteLine($"Recieve topic1 {i.Date} {i.Message}.Start processing.");
                await Task.Delay(5000);
                Console.WriteLine($"Recieve topic1 {i.Date} {i.Message}.End processing.");
            });
       

            _=Task.Run(async () =>
            {
                var i = 0;
                while (true)
                {
                    await client.PublishAsync("testTopic1", new TestMessage(DateTime.UtcNow, i++.ToString()), 0);
                    Task.Delay(5000).Wait();
                }
            });
            //
            // _=Task.Run(async () =>
            // {
            //     var i = 0;
            //     while (true)
            //     {
            //         await client.PushAsync("testTopic1", new TestMessage(DateTime.UtcNow, i--.ToString()), 0);
            //         Task.Delay((int)Random.Shared.NextInt64(100)).Wait();
            //     }
            // });
 
            // _=Task.Run(async () =>
            // {
            //     var i = 0;
            //     while (true)
            //     {
            //         //Console.WriteLine($"Push topic 2 {i}");
            //         await client.PushAsync("testTopic2", new TestMessage(DateTime.UtcNow, i++.ToString()), 0);
            //         Task.Delay((int)Random.Shared.NextInt64(10)).Wait();
            //     }
            // });
            
            while (true)
            {
                Task.Delay((int)Random.Shared.NextInt64(100)).Wait();
            }
        }
    }
    
    public record TestMessage(DateTime Date, string Message);
}