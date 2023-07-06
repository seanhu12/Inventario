using System;
using System.Collections.Generic;

namespace Inventario;

public partial class Caja
{
    public int Id { get; set; }

    public int Numero { get; set; }

    public virtual ICollection<Ventum> Venta { get; set; } = new List<Ventum>();
}
