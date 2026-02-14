using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeHub.Data.Data.Models
{
    public class Recipe
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string Instructions { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public ICollection<RecipeIngredient> RecipeIngredients { get; set; }
            = new List<RecipeIngredient>();

        // relative URL to stored image under wwwroot (e.g. /images/recipes/abc.jpg)
        public string? ImagePath { get; set; }
    }
}

