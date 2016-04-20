using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator.Models
{
    class ProductItemModel
    {
        public int Id { get; set; }
        public int Type { get; set; }
        public int Quantity { get; set; }
        public bool Changed { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public double Price { get; set; }

        public ProductItemModel()
        {
            Changed = false;
        }

        public void ReduceQuantity(int delta)
        {
            Quantity -= delta;
            Changed = true;
        }
    }
}
