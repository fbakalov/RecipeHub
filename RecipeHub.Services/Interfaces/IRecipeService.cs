using RecipeHub.Services.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeHub.Services.Interfaces
{
    public interface IRecipeService
    {
        Task<int> CreateAsync(RecipeCreateModel model, string userId);
        Task<IEnumerable<RecipeListModel>> GetAllAsync();
        Task<RecipeDetailsModel?> GetByIdAsync(int id);
        Task<RecipeEditModel?> GetForEditAsync(int id);
        Task EditAsync(int id, RecipeEditModel model);
        Task DeleteAsync(int id);

        Task<IEnumerable<CategoryDropdownModel>> GetCategoriesAsync();
        Task<IEnumerable<IngredientDropdownModel>> GetIngredientsAsync();
        Task<bool> IsOwnerAsync(int recipeId, string userId);
    }
}

