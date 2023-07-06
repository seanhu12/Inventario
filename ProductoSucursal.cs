using System;
using System.Collections.Generic;

namespace Inventario;

public partial class ProductoSucursal
{
    public int Productoid { get; set; }

    public int Sucursalid { get; set; }

    public int Stock { get; set; }

    public virtual Producto Producto { get; set; } = null!;

    public virtual Sucursal Sucursal { get; set; } = null!;
}
