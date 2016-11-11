using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClassLibrary1
{
    public class Pedido
    {
        public string Id { get; private set; }

        public string Customer { get; set; }

        public List<string> Items { get; set; }

        public double ShippingFee { get; set; }

        public Pedido()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}