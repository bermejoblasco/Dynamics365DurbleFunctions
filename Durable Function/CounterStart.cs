
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace VSSample
{
    public static class CounterStart
    {
        [FunctionName("CounterStart")]
        public static async Task Run(
            [QueueTrigger("operation")] string instanceId,            
            [OrchestrationClient] DurableOrchestrationClient client, TraceWriter log)
        {
            var count = Convert.ToInt32(instanceId);
            if (count <= 2)
            {
                for (int i = 1; i <= count; i++)
                {
                    log.Info($"Client Operation : Start Counter {i}");
                    await client.StartNewAsync("Counter", i);
                }
            }
            else
            {
                log.Info($"Client Operation : Start Counter {count}");
                await client.StartNewAsync("Counter", count);
            }
        }
    }
}
