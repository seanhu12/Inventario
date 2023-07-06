using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

namespace Inventario
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" }; // Cambia el valor si RabbitMQ no se encuentra en localhost
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "productos_disponibles",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                channel.QueueDeclare(queue: "productos_seleccionados",
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var productosDisponibles = ObtenerProductosDisponibles();

                var message = JsonConvert.SerializeObject(productosDisponibles);
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                     routingKey: "productos_disponibles",
                                     basicProperties: null,
                                     body: body);

                Console.WriteLine("Listado de productos disponibles enviado a Caja.");

                var productosSeleccionadosConsumer = new EventingBasicConsumer(channel);
                productosSeleccionadosConsumer.Received += (model, ea) =>
                {
                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var productosSeleccionados = JsonConvert.DeserializeObject<List<Producto>>(message);
                    Console.WriteLine("Productos seleccionados recibidos:");
                    foreach (var producto in productosSeleccionados)
                    {
                        Console.WriteLine($"Nombre: {producto.Nombre}, Precio: {producto.Precio}");
                    }
                };

                channel.BasicConsume(queue: "productos_seleccionados",
                                     autoAck: true,
                                     consumer: productosSeleccionadosConsumer);

                Console.WriteLine("Presiona Enter para cerrar la aplicación...");
                Console.ReadLine();
            }
        }

        static List<Producto> ObtenerProductosDisponibles()
        {
            var connectionString = "server=localhost;user=root;password=;database=Inventario";
            var productosDisponibles = new List<Producto>();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                var query = "SELECT nombre, precio FROM Producto";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var nombreProducto = reader.GetString("nombre");
                            var precio = reader.GetInt32("precio");

                            var producto = new Producto
                            {
                                Nombre = nombreProducto,
                                Precio = precio
                            };

                            productosDisponibles.Add(producto);
                        }
                    }
                }
            }

            return productosDisponibles;
        }
    }

    class Producto
    {
        public string Nombre { get; set; }
        public int Precio { get; set; }
    }
}
