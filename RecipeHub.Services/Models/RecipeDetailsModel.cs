using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeHub.Services.Models
{
    public class RecipeDetailsModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string Instructions { get; set; } = null!;

        public string Category { get; set; } = null!;

        public IEnumerable<string> Ingredients { get; set; } = new List<string>();

        public string AuthorId { get; set; } = null!;

        public string? ImagePath { get; set; }
    }
}
