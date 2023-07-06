using System;
using System.Collections.Generic;

namespace Inventario;

public partial class Sucursal
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<ProductoSucursal> ProductoSucursals { get; set; } = new List<ProductoSucursal>();

    public virtual ICollection<Ventum> Venta { get; set; } = new List<Ventum>();
}
