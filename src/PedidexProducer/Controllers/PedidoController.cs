using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PedidexProducer.Domain;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Text.Json;

namespace PedidexProducer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PedidoController : ControllerBase
    {
        private readonly ILogger<PedidoController> _logger;
        public PedidoController(ILogger<PedidoController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public IActionResult Adicionar(Pedido pedido)
        {
            try
            {
                var factory = new ConnectionFactory() { HostName = "localhost" };
                using var connection = factory.CreateConnection();
                using var channel = connection.CreateModel();

                channel.QueueDeclare(queue: "pedidoQueue",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                string message = JsonSerializer.Serialize(pedido);

                _logger.LogInformation($"Enviando pedido | {DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}");

                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "", routingKey: "pedidoQueue", basicProperties: null, body: body);

                return Accepted(pedido);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Erro ao enviar pedido", ex);
                return BadRequest();
            }
        }
    }
}
