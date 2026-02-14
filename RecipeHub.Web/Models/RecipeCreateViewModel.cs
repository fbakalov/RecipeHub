using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RecipeHub.Web.Models
{
    public class RecipeCreateViewModel
    {
        public string Title { get; set; } = null!;
        public string Instructions { get; set; } = null!;

        public int CategoryId { get; set; }

        public List<int> SelectedIngredientIds { get; set; } = new();

        public IEnumerable<SelectListItem> Categories { get; set; }
            = new List<SelectListItem>();

        public IEnumerable<SelectListItem> Ingredients { get; set; }
            = new List<SelectListItem>();

        // uploaded image file
        public IFormFile? Image { get; set; }
    }
}
