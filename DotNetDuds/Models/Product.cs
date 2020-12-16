using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetDuds.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(255)]
        public string Name { get; set; }

        [DisplayFormat(DataFormatString = "{0:c}")]
        [Range(0.01, 999999)]
        public double Price { get; set; }

        public string Description { get; set; }
        public string Image { get; set; }

        [Display(Name = "Category")]  // create more user-friendly column alias
        public int CategoryId { get; set; }

        // add reference to parent object
        public Category Category { get; set; }
    }
}
