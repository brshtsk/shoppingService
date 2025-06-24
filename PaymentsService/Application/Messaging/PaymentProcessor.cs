using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentsService.Domain.Entities;
using Contracts.Messaging.Events;
using PaymentsService.Persistence;

namespace PaymentsService.Application.Messaging
{
    public class PaymentProcessor
    {
        private readonly PaymentsDbContext _db;
        private readonly ILogger<PaymentProcessor> _logger;

        public PaymentProcessor(PaymentsDbContext db, ILogger<PaymentProcessor> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Обрабатывает событие создания заказа: дедупликация, попытка списания, запись Inbox и Outbox
        /// </summary>
        public async Task<PaymentCompletedEvent> ProcessOrderCreatedAsync(OrderCreatedEvent evt)
        {
            // Дедупликация: если уже было
            bool already = await _db.InboxMessages.AnyAsync(im => im.Id == evt.EventId);
            if (already)
            {
                _logger.LogInformation("OrderCreatedEvent {EventId} уже обработано", evt.EventId);
                // Вернём событие с тем же OrderId, но считаем, что уже ранее списали или отказали.
                // Можно вернуть Success = false, но тут предполагаем, что повторно ничего не делаем.
                return new PaymentCompletedEvent(Guid.NewGuid(), evt.OrderId, evt.UserId, success: true, evt.Amount);
            }

            // Ищем счёт
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == evt.UserId);
            bool success = false;
            if (account == null)
            {
                _logger.LogWarning("Счёт для пользователя {UserId} не найден", evt.UserId);
            }
            else
            {
                // Попытка списания
                success = account.TryDebit(evt.Amount);
                if (!success)
                {
                    _logger.LogWarning("Недостаточно средств на счёте {AccountId} для заказа {OrderId}", account.Id, evt.OrderId);
                }
            }

            // Записываем в InboxMessage
            _db.InboxMessages.Add(new InboxMessage(evt.EventId, nameof(OrderCreatedEvent)));

            // Формируем событие PaymentCompletedEvent
            var paymentEvent = new PaymentCompletedEvent(
                eventId: Guid.NewGuid(),
                orderId: evt.OrderId,
                userId: evt.UserId,
                success: success,
                amount: evt.Amount);

            // Сериализуем и записываем в Outbox
            string payload = JsonSerializer.Serialize(paymentEvent);
            _db.OutboxMessages.Add(new OutboxMessage(nameof(PaymentCompletedEvent), payload));

            await _db.SaveChangesAsync();
            _logger.LogInformation("Записан Outbox для PaymentCompletedEvent (OrderId={OrderId}, Success={Success})", evt.OrderId, success);

            return paymentEvent;
        }
    }
}
