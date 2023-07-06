using System;
using System.Collections.Generic;

namespace Inventario;

public partial class Detalle
{
    public int Productoid { get; set; }

    public int Ventaid { get; set; }

    public int Cantidad { get; set; }

    public virtual Producto Producto { get; set; } = null!;

    public virtual Ventum Venta { get; set; } = null!;
}
