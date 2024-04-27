using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Priority_Queue;

namespace PriorityQueueTest
{
    public class TestHostService : BackgroundService
    {
        private readonly SimplePriorityQueue<string, int> _queue = new SimplePriorityQueue<string, int>();
        private readonly int _executeThreadCount;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TestHostService> _logger;

        public TestHostService(IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<TestHostService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _executeThreadCount = Math.Max(configuration.GetValue<int>("ExecuteThreadCount"), 4);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            _ = Task.Run(Producing, stoppingToken);
            await Task.WhenAll(
                Enumerable.Range(0, _executeThreadCount).Select(i =>
                {
                    return Task.Run(async () =>
                    {
                        var index = i;
                        await Processing(index);
                    }, stoppingToken);
                }));
        }

        private async Task Producing()
        {
            var arr = new[]
                { ("4 - Joseph", 4), ("2 - Tyler", 0), ("1 - Jason", 1), ("4 - Ryan", 4), ("3 - Valerie", 3) };

            while (true)
            {
                var random = new Random().Next(0, 6);

                if (random == 5)
                {
                    _logger.LogWarning("休息5秒钟");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
                else
                {
                    var id = Guid.NewGuid().ToString();
                    var (name, priority) = arr[random];

                    var value = $"{id}-{name}";
                    _queue.Enqueue(value, priority);
                    _logger.LogCritical($"生产者：{value},优先级{priority}");
                }
            }
        }

        private async Task Processing(int index)
        {
            _logger.LogInformation($"消费者({index}):启动");
            while (true)
            {
                if (_queue.Count == 0)
                {
                    _logger.LogWarning($"消费者({index})：暂无数据,休息20秒钟");
                    await Task.Delay(TimeSpan.FromSeconds(20));
                }
                else
                {
                    var nextUser = _queue.Dequeue();

                    _logger.LogWarning($"消费者({index})：消费{(nextUser)}，休息2秒钟");
                }
            }
        }
    }
}