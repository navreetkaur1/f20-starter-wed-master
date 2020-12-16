using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetDuds.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string CustomerId { get; set; }
        public int Quantity { get; set; }
        public DateTime DateCreated { get; set; }
        public double Price { get; set; }

        // parent object ref
        public Product Product { get; set; }
    }
}
