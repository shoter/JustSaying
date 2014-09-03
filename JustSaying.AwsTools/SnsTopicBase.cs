using System.Linq;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageSerialisation;
using NLog;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools
{
    public abstract class SnsTopicBase : IMessagePublisher
    {
        private readonly IMessageSerialisationRegister _serialisationRegister; // ToDo: Grrr...why is this here even. GET OUT!
        public string Arn { get; protected set; }
        public IAmazonSimpleNotificationService Client { get; protected set; }
        private static readonly Logger EventLog = LogManager.GetLogger("EventLog");
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        public SnsTopicBase(IMessageSerialisationRegister serialisationRegister)
        {
            _serialisationRegister = serialisationRegister;
        }

        public abstract bool Exists();

        public bool IsSubscribed(SqsQueueBase queue)
        {
            var result = Client.ListSubscriptionsByTopic(new ListSubscriptionsByTopicRequest(Arn));
            
            return result.Subscriptions.Any(x => !string.IsNullOrEmpty(x.SubscriptionArn) && x.Endpoint == queue.Arn);
        }

        public bool Subscribe(SqsQueueBase queue)
        {
            var response = Client.Subscribe(new SubscribeRequest(Arn, "sqs", queue.Arn));
            if (!string.IsNullOrEmpty(response.SubscriptionArn))
            {
                queue.AddPermission(this);
                Log.Info(string.Format("Subscribed Queue to Topic - Queue: {0}, Topic: {1}", queue.Arn, Arn));
                return true;
            }
            Log.Info(string.Format("Failed to subscribe Queue to Topic: {0}, Topic: {1}", queue.Arn, Arn));
            return false;
        }

        public void Publish(Message message)
        {
            var messageToSend = _serialisationRegister.GetSerialiser(message.GetType()).Serialise(message);
            var messageType = message.GetType().Name;

            Client.Publish(new PublishRequest
                               {
                                   Subject = messageType,
                                   Message = messageToSend,
                                   TopicArn = Arn
                               });

            EventLog.Info("Published message: '{0}' with content {1}", messageType, messageToSend);
        }
    }
}