using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Generator.Models
{
    class ProductItemAggModel
    {
        public int Id { get; set; }
        public string ProductInfo { get; set; }
        public int Quantity { get; set; }
        public DateTime Date { get; set; }
        public double Price { get; set; }
        public string Description { get; set; }
        public string CompanyInfo { get; set; }
        public string LocationInfo { get; set; }
    }
}
