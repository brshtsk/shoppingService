using Microsoft.AspNetCore.Mvc;
using OrdersService.Application.Services;

namespace OrdersService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// Создать заказ: вернёт orderId.
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest req)
        {
            var orderId = await _orderService.CreateOrderAsync(req.UserId, req.Amount);
            return Accepted(new { OrderId = orderId });
        }

        /// <summary>
        /// Получить список заказов пользователя.
        /// </summary>
        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetOrders(Guid userId)
        {
            var list = await _orderService.GetOrdersAsync(userId);
            return Ok(list);
        }

        /// <summary>
        /// Получить статус заказа.
        /// </summary>
        [HttpGet("{orderId:guid}/status")]
        public async Task<IActionResult> GetStatus(Guid orderId)
        {
            var status = await _orderService.GetOrderStatusAsync(orderId);
            if (status == null) return NotFound();
            return Ok(new OrderStatusResponse(orderId, status));
        }
    }
}