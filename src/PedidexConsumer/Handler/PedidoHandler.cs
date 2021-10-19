using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PedidexConsumer.Domain;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PedidexConsumer
{
    public class PedidoHandler : BackgroundService
    {
        private readonly ILogger<PedidoHandler> _logger;

        public PedidoHandler(ILogger<PedidoHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Aguardando pedido...");

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "pedidoQueue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            while (!stoppingToken.IsCancellationRequested)
            {
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        var pedido = System.Text.Json.JsonSerializer.Deserialize<Pedido>(message);

                        Console.WriteLine($"pedido: {pedido.NumeroPedido}|{pedido.Valor:N2}");

                        channel.BasicAck(ea.DeliveryTag, false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Erro ao processar pedido da fila", ex);
                        channel.BasicNack(ea.DeliveryTag, false, true);
                    }
                };

                channel.BasicConsume(queue: "pedidoQueue",
                                     autoAck: false,
                                     consumer: consumer);

            }
            await Task.CompletedTask;
        }
    }
}
