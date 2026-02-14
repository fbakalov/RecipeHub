using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace RecipeHub.Web.Models
{
    public class RecipeEditViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Instructions { get; set; } = null!;
        public int CategoryId { get; set; }
        public List<int> IngredientIds { get; set; } = new();

        public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Ingredients { get; set; } = new List<SelectListItem>();

        public string? ImagePath { get; set; }
        public IFormFile? NewImage { get; set; }
    }
}
