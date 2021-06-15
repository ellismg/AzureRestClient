using Azure.Core.Pipeline;
using System;
using System.Threading.Tasks;

namespace Azure.Core
{
    class DelegatingHttpPipelinePolicy : HttpPipelinePolicy
    {
        private readonly string _key;
        public DelegatingHttpPipelinePolicy(string key)
        {
            _key = key;
        }

        public override void Process(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            if (message.TryGetProperty(_key, out object value))
            {
                ((HttpPipelinePolicy)value).Process(message, pipeline.Slice(1));
            }
            else
            {
                ProcessNext(message, pipeline);
            }
        }

        public override ValueTask ProcessAsync(HttpMessage message, ReadOnlyMemory<HttpPipelinePolicy> pipeline)
        {
            if (message.TryGetProperty(_key, out object value))
            {
                return ((HttpPipelinePolicy)value).ProcessAsync(message, pipeline.Slice(1));
            }
            else
            {
                return ProcessAsync(message, pipeline);
            }
        }
    }
}
