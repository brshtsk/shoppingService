using System;
using System.Text.Json;
using Contracts.Messaging.Events;
using Xunit;

namespace Contracts.Tests
{
    public class EventContractsTests
    {
        [Fact]
        public void OrderCreatedEvent_Serialization_PreservesAllProperties()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var amount = 123.45m;
            var original = new OrderCreatedEvent(eventId, orderId, userId, amount);

            // Act
            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<OrderCreatedEvent>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.EventId, deserialized.EventId);
            Assert.Equal(original.OrderId, deserialized.OrderId);
            Assert.Equal(original.UserId, deserialized.UserId);
            Assert.Equal(original.Amount, deserialized.Amount);
        }

        [Fact]
        public void PaymentCompletedEvent_Serialization_PreservesAllProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var success = true;
            var amount = 123.45m;
            var original = new PaymentCompletedEvent(id, orderId, userId, success, amount);

            // Act
            var json = JsonSerializer.Serialize(original);
            var deserialized = JsonSerializer.Deserialize<PaymentCompletedEvent>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.EventId, deserialized.EventId);
            Assert.Equal(original.OrderId, deserialized.OrderId);
            Assert.Equal(original.UserId, deserialized.UserId);
            Assert.Equal(original.Success, deserialized.Success);
            Assert.Equal(original.Amount, deserialized.Amount);
        }

        [Fact]
        public void OrderCreatedEvent_Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var eventId = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var amount = 123.45m;

            // Act
            var evt = new OrderCreatedEvent(eventId, orderId, userId, amount);

            // Assert
            Assert.NotEqual(Guid.Empty, evt.EventId);
            Assert.Equal(orderId, evt.OrderId);
            Assert.Equal(userId, evt.UserId);
            Assert.Equal(amount, evt.Amount);
        }

        [Fact]
        public void PaymentCompletedEvent_Constructor_SetsPropertiesCorrectly()
        {
            // Arrange
            var id = Guid.NewGuid();
            var orderId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var success = true;
            var amount = 123.45m;

            // Act
            var evt = new PaymentCompletedEvent(id, orderId, userId, success, amount);

            // Assert
            Assert.Equal(id, evt.EventId);
            Assert.Equal(orderId, evt.OrderId);
            Assert.Equal(userId, evt.UserId);
            Assert.Equal(success, evt.Success);
            Assert.Equal(amount, evt.Amount);
        }
    }
}