using System;
using System.Collections.Generic;

namespace Inventario;

public partial class Ventum
{
    public int Id { get; set; }

    public int Correlativo { get; set; }

    public DateTime Fecha { get; set; }

    public int Total { get; set; }

    public int Cajaid { get; set; }

    public int Sucursalid { get; set; }

    public virtual Caja Caja { get; set; } = null!;

    public virtual ICollection<Detalle> Detalles { get; set; } = new List<Detalle>();

    public virtual Sucursal Sucursal { get; set; } = null!;
}
