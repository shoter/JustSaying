﻿using System.Collections.Generic;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustBehave;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.AwsTools.UnitTests.Sqs
{
    class WhenFetchingQueueByName
    {
        private IAmazonSQS _client;
        private const int RetryCount = 3;

        [SetUp]
        protected void SetUp()
        {

            _client = Substitute.For<IAmazonSQS>();
            _client.ListQueues(Arg.Any<ListQueuesRequest>())
                .Returns(new ListQueuesResponse() { QueueUrls = new List<string>() { "some-queue-name" } });
            _client.GetQueueAttributes(Arg.Any<GetQueueAttributesRequest>())
                .Returns(new GetQueueAttributesResponse()
                {
                    Attributes = new Dictionary<string, string>() { { "QueueArn", "something:some-queue-name" } }
                });
        }

        [Then]
        public void IncorrectQueueNameDoNotMatch()
        {
            var sqsQueueByName = new SqsQueueByName("some-queue-name1", _client, RetryCount);
            Assert.IsFalse(sqsQueueByName.Exists());
        }

        [Then]
        public void IncorrectPartialQueueNameDoNotMatch()
        {
            var sqsQueueByName = new SqsQueueByName("some-queue", _client, RetryCount);
            Assert.IsFalse(sqsQueueByName.Exists());
        }

        [Then]
        public void CorrectQueueNameShouldMatch()
        {
            var sqsQueueByName = new SqsQueueByName("some-queue-name", _client, RetryCount);
            Assert.IsTrue(sqsQueueByName.Exists());
        }
    }

    public class WhenPublishing : BehaviourTest<SqsPublisher>
    {
        private readonly IMessageSerialisationRegister _serialisationRegister = Substitute.For<IMessageSerialisationRegister>();
        private readonly IAmazonSQS _sqs = Substitute.For<IAmazonSQS>();
        private const string Url = "https://blablabla/" + QueueName;
        private readonly GenericMessage _message = new GenericMessage();
        private const string QueueName = "queuename";

        protected override SqsPublisher CreateSystemUnderTest()
        {
            return new SqsPublisher(QueueName, _sqs, 0, _serialisationRegister);
        }

        protected override void Given()
        {

            var serialiser = Substitute.For<IMessageSerialiser<GenericMessage>>();
            _serialisationRegister.GetSerialiser(typeof(GenericMessage)).Returns(serialiser);
            _sqs.ListQueues(Arg.Any<ListQueuesRequest>()).Returns(new ListQueuesResponse{QueueUrls = new List<string>{Url}});
            _sqs.GetQueueAttributes(Arg.Any<GetQueueAttributesRequest>()).Returns(new GetQueueAttributesResponse());
        }

        protected override void When()
        {
            SystemUnderTest.Publish(_message);
        }

        [Then]
        public void MessageIsPublishedToQueue()
        {
            // ToDo: Can be better...
            _sqs.Received().SendMessage(Arg.Is<SendMessageRequest>(x => x.MessageBody.Contains("\"Message\":{\"Id\":\"" + _message.Id)));
        }

        [Then]
        public void MessageSubjectIsObjectType()
        {
            // ToDo: Can be better...
            _sqs.Received().SendMessage(Arg.Is<SendMessageRequest>(x => x.MessageBody.Contains("\"Subject\":\"GenericMessage\"")));
        }

        [Then]
        public void MessageIsPublishedToCorrectLocation()
        {
            _sqs.Received().SendMessage(Arg.Is<SendMessageRequest>(x => x.QueueUrl == Url));
        }
    }
}
