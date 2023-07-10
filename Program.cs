using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Timers;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Inventario;




namespace Inventario
{
    class Program
    {
        static async Task Main(string[] args)
        {
            gRPC().Wait();
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

                EnviarProductosDisponibles(channel);

                 var productosSeleccionadosConsumer = new EventingBasicConsumer(channel);
                productosSeleccionadosConsumer.Received += (model, ea) =>
                                {
                                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                                    var compra = JsonConvert.DeserializeObject<Compra>(message);
                                    
                                    GuardarCompra(channel,compra);
                                };

                                channel.BasicConsume(queue: "productos_seleccionados",
                                                    autoAck: true,
                                                    consumer: productosSeleccionadosConsumer);


                MostrarMenu(channel);
            }
        }

        static async Task gRPC()
        {
            // The port number must match the port of the gRPC server.
            using var channel = GrpcChannel.ForAddress("http://localhost:5138"); // Asegúrate de que este puerto coincide con el del servidor gRPC
            var client = new InventarioService.InventarioServiceClient(channel);

            var respuesta = await client.ObtenerTextoPlanoAsync(new SolicitudTextoPlano()); // Actualizado para usar tus nuevos mensajes
            Console.WriteLine("Texto recibido: " + respuesta.Texto); // Actualizado para usar 'respuesta.Texto'

            // Nuevo código para enviar un mensaje al sistema web cada 5 segundos indefinidamente
            while (true)
            {
                var mensaje = new MensajeRequest { Mensaje = "Hola, sistema web!" };
                var respuestaMensaje = await client.EnviarMensajeWebAsync(mensaje);
                Console.WriteLine("Respuesta del sistema web: " + respuestaMensaje.Respuesta);

                // Esperar 5 segundos antes de enviar el próximo mensaje
                await Task.Delay(5000);
            }
        }


        static void EnviarProductosDisponibles(IModel channel)
        {
            var timer = new System.Timers.Timer();  // Cambio aquí
            timer.Interval = 5000; 
            timer.Elapsed += (sender, e) =>
            {
                var productosDisponibles = ObtenerProductosDisponibles();

                var message = JsonConvert.SerializeObject(productosDisponibles);
                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                     routingKey: "productos_disponibles",
                                     basicProperties: null,
                                     body: body);

                
            };
            timer.Start();
        }
        static List<Producto> ObtenerProductosDisponibles()
        {
            var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";
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

        static void AgregarProducto(IModel channel)
        {
            Console.WriteLine("Ingrese el nombre del producto:");
            var nombre = Console.ReadLine();

            Console.WriteLine("Ingrese el precio del producto:");
            var precioStr = Console.ReadLine();
            int precio;
            if (!int.TryParse(precioStr, out precio))
            {
                Console.WriteLine("El precio ingresado no es válido.");
                return;
            }
            Console.WriteLine("Categoria id");
            var categoria = Console.ReadLine();
            

            var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                var query = "INSERT INTO Producto (nombre, precio , Categoriaid) VALUES (@nombre, @precio, @categoria)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@nombre", nombre);
                    command.Parameters.AddWithValue("@precio", precio);
                    command.Parameters.AddWithValue("@categoria", categoria);
                    command.ExecuteNonQuery();

                    Console.WriteLine("Producto agregado correctamente.");
                }
            }

            EnviarProductosDisponibles(channel);
            MostrarMenu(channel);
        }

        static void ActualizarProducto(IModel channel)
        {
            Console.WriteLine("Ingrese el ID del producto a actualizar:");
            var idStr = Console.ReadLine();
            int id;
            if (!int.TryParse(idStr, out id))
            {
                Console.WriteLine("El ID ingresado no es válido.");
                return;
            }

            Console.WriteLine("Ingrese el nuevo nombre del producto:");
            var nuevoNombre = Console.ReadLine();

            Console.WriteLine("Ingrese el nuevo precio del producto:");
            var nuevoPrecioStr = Console.ReadLine();
            int nuevoPrecio;
            if (!int.TryParse(nuevoPrecioStr, out nuevoPrecio))
            {
                Console.WriteLine("El nuevo precio ingresado no es válido.");
                return;
            }

            var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                var query = "UPDATE Producto SET nombre = @nuevoNombre, precio = @nuevoPrecio WHERE id = @productoId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@nuevoNombre", nuevoNombre);
                    command.Parameters.AddWithValue("@nuevoPrecio", nuevoPrecio);
                    command.Parameters.AddWithValue("@productoId", id);
                    command.ExecuteNonQuery();

                    Console.WriteLine("Producto actualizado correctamente.");
                }
            }

            EnviarProductosDisponibles(channel);
            MostrarMenu(channel);
        }

        static void AgregarSucursal(IModel channel)
{
    Console.WriteLine("Ingrese el nombre de la sucursal:");
    var nombre = Console.ReadLine();

    var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";

    using (var connection = new MySqlConnection(connectionString))
    {
        connection.Open();

        var query = "INSERT INTO Sucursal (nombre) VALUES (@nombre)";

        using (var command = new MySqlCommand(query, connection))
        {
            command.Parameters.AddWithValue("@nombre", nombre);
            command.ExecuteNonQuery();

            Console.WriteLine("Sucursal agregada correctamente.");
        }
    }

    EnviarProductosDisponibles(channel);
    MostrarMenu(channel);
}

        static void ActualizarSucursal(IModel channel)
        {
            Console.WriteLine("Ingrese el ID de la sucursal a actualizar:");
            var idStr = Console.ReadLine();
            int id;
            if (!int.TryParse(idStr, out id))
            {
                Console.WriteLine("El ID ingresado no es válido.");
                return;
            }

            Console.WriteLine("Ingrese el nuevo nombre de la sucursal:");
            var nuevoNombre = Console.ReadLine();

            var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                var query = "UPDATE Sucursal SET nombre = @nuevoNombre WHERE id = @sucursalId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@nuevoNombre", nuevoNombre);
                    command.Parameters.AddWithValue("@sucursalId", id);
                    command.ExecuteNonQuery();

                    Console.WriteLine("Sucursal actualizada correctamente.");
                }
            }

            EnviarProductosDisponibles(channel);
            MostrarMenu(channel);
        }

        static void AgregarCategoria(IModel channel)
    {
        Console.WriteLine("Ingrese el nombre de la categoría:");
        var nombre = Console.ReadLine();

        var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            var query = "INSERT INTO Categoria (nombre) VALUES (@nombre)";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@nombre", nombre);
                command.ExecuteNonQuery();

                Console.WriteLine("Categoría agregada correctamente.");
            }
        }

        EnviarProductosDisponibles(channel);
        MostrarMenu(channel);
    }

        static void ActualizarCategoria(IModel channel)
        {
            Console.WriteLine("Ingrese el ID de la categoría a actualizar:");
            var idStr = Console.ReadLine();
            int id;
            if (!int.TryParse(idStr, out id))
            {
                Console.WriteLine("El ID ingresado no es válido.");
                return;
            }

            Console.WriteLine("Ingrese el nuevo nombre de la categoría:");
            var nuevoNombre = Console.ReadLine();

            var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                var query = "UPDATE Categoria SET nombre = @nuevoNombre WHERE id = @categoriaId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@nuevoNombre", nuevoNombre);
                    command.Parameters.AddWithValue("@categoriaId", id);
                    command.ExecuteNonQuery();

                    Console.WriteLine("Categoría actualizada correctamente.");
                }
            }

            EnviarProductosDisponibles(channel);
            MostrarMenu(channel);
        }

        // Agregar y actualizar Caja
        static void AgregarCaja(IModel channel)
        {
            Console.WriteLine("Ingrese el número de la caja:");
            var numeroStr = Console.ReadLine();
            int numero;
            if (!int.TryParse(numeroStr, out numero))
            {
                Console.WriteLine("El número ingresado no es válido.");
                return;
            }

            var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                var query = "INSERT INTO Caja (numero) VALUES (@numero)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@numero", numero);
                    command.ExecuteNonQuery();

                    Console.WriteLine("Caja agregada correctamente.");
                }
            }

            EnviarProductosDisponibles(channel);
            MostrarMenu(channel);
        }

        static void ActualizarCaja(IModel channel)
    {
        Console.WriteLine("Ingrese el ID de la caja a actualizar:");
        var idStr = Console.ReadLine();
        int id;
        if (!int.TryParse(idStr, out id))
        {
            Console.WriteLine("El ID ingresado no es válido.");
            return;
        }

        Console.WriteLine("Ingrese el nuevo número de la caja:");
        var nuevoNumeroStr = Console.ReadLine();
        int nuevoNumero;
        if (!int.TryParse(nuevoNumeroStr, out nuevoNumero))
        {
            Console.WriteLine("El nuevo número ingresado no es válido.");
            return;
        }

        var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            var query = "UPDATE Caja SET numero = @nuevoNumero WHERE id = @cajaId";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@nuevoNumero", nuevoNumero);
                command.Parameters.AddWithValue("@cajaId", id);
                command.ExecuteNonQuery();

                Console.WriteLine("Caja actualizada correctamente.");
            }
        }

        EnviarProductosDisponibles(channel);
        MostrarMenu(channel);
    }  
        
        // Agregar producto a una sucursal
        static void AgregarProductoASucursal(IModel channel)
        {
            Console.WriteLine("Productos disponibles:");
            var productosDisponibles = ObtenerProductosDisponibles();
            foreach (var producto in productosDisponibles)
            {
                Console.WriteLine($"ID: {producto.Id}, Nombre: {producto.Nombre}, Precio: {producto.Precio}");
            }

            Console.WriteLine("Ingrese el ID del producto a agregar:");
            var productoIdStr = Console.ReadLine();
            int productoId;
            if (!int.TryParse(productoIdStr, out productoId))
            {
                Console.WriteLine("El ID del producto ingresado no es válido.");
                return;
            }

            Console.WriteLine("Ingrese el ID de la sucursal:");
            var sucursalIdStr = Console.ReadLine();
            int sucursalId;
            if (!int.TryParse(sucursalIdStr, out sucursalId))
            {
                Console.WriteLine("El ID de la sucursal ingresado no es válido.");
                return;
            }

            Console.WriteLine("Ingrese la cantidad de stock del producto:");
            var stockStr = Console.ReadLine();
            int stock;
            if (!int.TryParse(stockStr, out stock))
            {
                Console.WriteLine("La cantidad de stock ingresada no es válida.");
                return;
            }

            var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                var query = "INSERT INTO Producto_Sucursal (Productoid, Sucursalid, stock) VALUES (@productoId, @sucursalId, @stock)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@productoId", productoId);
                    command.Parameters.AddWithValue("@sucursalId", sucursalId);
                    command.Parameters.AddWithValue("@stock", stock);
                    command.ExecuteNonQuery();

                    Console.WriteLine("Producto agregado a la sucursal correctamente.");
                }
            }

            EnviarProductosDisponibles(channel);
            MostrarMenu(channel);
        }

        static List<Producto> ObtenerProductosDeSucursal(int sucursalId)
        {
            var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";
            var productosEnSucursal = new List<Producto>();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                var query = "SELECT Producto.id, Producto.nombre, Producto.precio FROM Producto INNER JOIN Producto_Sucursal ON Producto.id = Producto_Sucursal.Productoid WHERE Producto_Sucursal.Sucursalid = @sucursalId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@sucursalId", sucursalId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var id = reader.GetInt32("id");
                            var nombreProducto = reader.GetString("nombre");
                            var precio = reader.GetInt32("precio");

                            var producto = new Producto
                            {
                                Id = id,
                                Nombre = nombreProducto,
                                Precio = precio
                            };

                            productosEnSucursal.Add(producto);
                        }
                    }
                }
            }

            return productosEnSucursal;
        }

        // Eliminar producto de una sucursal
        static void EliminarProductoDeSucursal(IModel channel)
        {
            Console.WriteLine("Ingrese el ID de la sucursal:");
            var sucursalIdStr = Console.ReadLine();
            int sucursalId;
            if (!int.TryParse(sucursalIdStr, out sucursalId))
            {
                Console.WriteLine("El ID de la sucursal ingresado no es válido.");
                return;
            }

            Console.WriteLine("Productos en la sucursal:");
            var productosEnSucursal = ObtenerProductosDeSucursal(sucursalId);
            foreach (var producto in productosEnSucursal)
            {
                Console.WriteLine($"ID: {producto.Id}, Nombre: {producto.Nombre}, Precio: {producto.Precio}");
            }

            Console.WriteLine("Ingrese el ID del producto a eliminar:");
            var productoIdStr = Console.ReadLine();
            int productoId;
            if (!int.TryParse(productoIdStr, out productoId))
            {
                Console.WriteLine("El ID del producto ingresado no es válido.");
                return;
            }

            var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                var query = "DELETE FROM Producto_Sucursal WHERE Productoid = @productoId AND Sucursalid = @sucursalId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@productoId", productoId);
                    command.Parameters.AddWithValue("@sucursalId", sucursalId);
                    command.ExecuteNonQuery();

                    Console.WriteLine("Producto eliminado de la sucursal correctamente.");
                }
            }

            EnviarProductosDisponibles(channel);
            MostrarMenu(channel);
        }
   
        // Actualizar stock de un producto en una sucursal
       // Actualizar stock de un producto en una sucursal
        static void ActualizarStockASucursal(IModel channel)
        {
            Console.WriteLine("Ingrese el ID de la sucursal:");
            var sucursalIdStr = Console.ReadLine();
            int sucursalId;
            if (!int.TryParse(sucursalIdStr, out sucursalId))
            {
                Console.WriteLine("El ID de la sucursal ingresado no es válido.");
                return;
            }

            // Muestra los productos y su stock en la sucursal seleccionada
            var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT Producto.nombre, Producto_Sucursal.stock FROM Producto INNER JOIN Producto_Sucursal ON Producto.id = Producto_Sucursal.Productoid WHERE Producto_Sucursal.Sucursalid = @sucursalId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@sucursalId", sucursalId);
                    using (var reader = command.ExecuteReader())
                    {
                        Console.WriteLine("Productos en la sucursal:");
                        while (reader.Read())
                        {
                            var nombreProducto = reader.GetString("nombre");
                            var stock = reader.GetInt32("stock");
                            Console.WriteLine($"Nombre: {nombreProducto}, Stock: {stock}");
                        }
                    }
                }
            }

            Console.WriteLine("Ingrese el nombre del producto al que desea actualizar el stock:");
            var nombreProductoStr = Console.ReadLine();

            Console.WriteLine("Ingrese la cantidad que desea agregar o quitar al stock (use números negativos para quitar):");
            var cantidadStr = Console.ReadLine();
            int cantidad;
            if (!int.TryParse(cantidadStr, out cantidad))
            {
                Console.WriteLine("La cantidad ingresada no es válida.");
                return;
            }

            // Actualiza el stock del producto en la sucursal
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                var query = "UPDATE Producto_Sucursal INNER JOIN Producto ON Producto.id = Producto_Sucursal.Productoid SET Producto_Sucursal.stock = Producto_Sucursal.stock + @cantidad WHERE Producto.nombre = @nombreProducto AND Producto_Sucursal.Sucursalid = @sucursalId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@nombreProducto", nombreProductoStr);
                    command.Parameters.AddWithValue("@cantidad", cantidad);
                    command.Parameters.AddWithValue("@sucursalId", sucursalId);
                    command.ExecuteNonQuery();

                    Console.WriteLine("Stock actualizado correctamente.");
                }
            }

            EnviarProductosDisponibles(channel);
            MostrarMenu(channel);
        }

        static void GuardarCompra(IModel channel, Compra compra)
        {
            var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";
            Console.WriteLine("eNTRA");

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                var query = "INSERT INTO venta (correlativo, fecha, total, Cajaid, Sucursalid) VALUES (@correl, @fecha, @total, @caja, @sucursal )";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@correl", 2);
                    command.Parameters.AddWithValue("@fecha", compra.Fecha);
                    command.Parameters.AddWithValue("@total", compra.Total);
                    command.Parameters.AddWithValue("@caja", 1);
                    command.Parameters.AddWithValue("@sucursal", compra.Sucursal);
                    command.ExecuteNonQuery();

                    Console.WriteLine("Venta agregada correctamente.");
                }


                foreach (var producto in compra.Productos)
                {
                    int idProd = ObtenerIdProducto(producto.Key);

                    var detalleQuery = "INSERT INTO detalle (VentaId, Productoid, Cantidad) VALUES (LAST_INSERT_ID(), @producto, @cantidad)";
                    
                    using (var detalleCommand = new MySqlCommand(detalleQuery, connection))
                    {
                        detalleCommand.Parameters.AddWithValue("@producto", idProd);
                        detalleCommand.Parameters.AddWithValue("@cantidad", producto.Value);
                        detalleCommand.ExecuteNonQuery();
                    }
                }

                Console.WriteLine("Productos de la venta agregados correctamente.");
            }

            // Aquí puedes llamar a cualquier función que necesites ejecutar después de guardar la compra.
        }
        
       static int ObtenerIdProducto(string nombreProducto)
        {
            var connectionString = "server=localhost;user=root;password=alumno;database=Inventario";
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                var query = "SELECT id FROM Producto WHERE nombre = @nombreProducto";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@nombreProducto", nombreProducto);
                    var result = command.ExecuteScalar();
                    if(result != null)
                    {
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        throw new Exception("Producto no encontrado en la base de datos.");
                    }
                }
            }
        }

        

        static void MostrarMenu(IModel channel)
        {
            
            Console.WriteLine("==== Menú ====");
            Console.WriteLine("1. Agregar un producto");
            Console.WriteLine("2. Actualizar un producto");
            Console.WriteLine("3. Agregar una Sucursal");
            Console.WriteLine("4. Actualizar una Sucursal");
            Console.WriteLine("5. Agregar una Categoria");
            Console.WriteLine("6. Actualizar una Categoria");
            Console.WriteLine("7. Agregar una Caja");
            Console.WriteLine("8. Actualizar una Caja");
            Console.WriteLine("9. Agregar O Eliminar stock");   
            Console.WriteLine("10. Salir");

            Console.WriteLine("Ingrese el número de la opción deseada:");
            var opcionStr = Console.ReadLine();
            int opcion;
            if (!int.TryParse(opcionStr, out opcion))
            {
                Console.WriteLine("Opción no válida.");
                MostrarMenu(channel);
                return;
            }

            switch (opcion)
            {
                case 1:
                    AgregarProducto(channel);
                    break;
                case 2:
                    ActualizarProducto(channel);
                    break;
                case 3:
                    AgregarSucursal(channel);
                    break;    
                case 4:
                    ActualizarSucursal(channel);
                    break;    
                case 5:
                    AgregarCategoria(channel);
                    break; 
                case 6:
                    ActualizarCategoria(channel);
                    break;
                case 7:
                    AgregarCaja(channel);
                    break;
                case 8:
                    ActualizarCaja(channel);
                    break;
                case 9:
                    Console.WriteLine("1) Desea Agregar ");
                    Console.WriteLine("2) Actualizar ");
                    Console.WriteLine("3) Eliminar ");
                    Console.WriteLine("4) Salir ");
                    var resp = Console.ReadLine();
                    int opcion2;
                    if (!int.TryParse(resp, out opcion2))
                    {
                        Console.WriteLine("Opción no válida.");
                        MostrarMenu(channel);
                        return;
                    }
                    switch (opcion2) 
                    {
                        case 1:
                          AgregarProductoASucursal(channel);
                            break;
                          
                        case 2: 
                            ActualizarStockASucursal(channel);
                            break;
                        case 3:
                            EliminarProductoDeSucursal(channel);
                            MostrarMenu(channel);
                            break;
                        case 4:
                            Console.WriteLine("Gracias por su preferencia");
                            MostrarMenu(channel);
                            break;
                        default:
                            Console.WriteLine("Opción no válida.");
                            MostrarMenu(channel);
                            break;

                    }
                    break;                        
                case 10:
                    Console.WriteLine("Gracias por su preferencia");
                    break;
                default:
                    Console.WriteLine("Opción no válida.");
                    break;
            }
        }
    }



}

class Compra
{
    public string Sucursal { get; set; }
    public DateTime Fecha { get; set; }
    public Dictionary<string, int> Productos { get; set; }
     public int Total { get; set; }
}