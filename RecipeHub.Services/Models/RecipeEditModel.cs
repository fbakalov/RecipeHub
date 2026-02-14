using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeHub.Services.Models
{
    public class RecipeEditModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string Instructions { get; set; } = null!;

        public int CategoryId { get; set; }

        public List<int> IngredientIds { get; set; } = new();

        // stored relative image path (e.g. /images/recipes/abc.jpg)
        public string? ImagePath { get; set; }
    }
}
