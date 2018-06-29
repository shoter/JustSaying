using System;

namespace JustSaying.Messaging.Monitoring
{
    public interface IMessageMonitor
    {
        void HandleException(Type messageType);
        void HandleTime(long handleTimeMs);
        void IssuePublishingMessage();
        void IncrementThrottlingStatistic();
        void HandleThrottlingTime(long handleTimeMs);
        void PublishMessageTime(long handleTimeMs);
        void ReceiveMessageTime(long handleTimeMs, string queueName, string region);
    }
}
