using Microsoft.EntityFrameworkCore;
using RecipeHub.Data;
using RecipeHub.Data.Data.Models;
using RecipeHub.Services.Interfaces;
using RecipeHub.Services.Models;

namespace RecipeHub.Services.Services
{
    /// <summary>
    /// Provides business logic and data access operations
    /// related to <see cref="Recipe"/> entities.
    /// </summary>
    public class RecipeService : IRecipeService
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecipeService"/> class.
        /// </summary>
        /// <param name="context">The application's database context.</param>
        public RecipeService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Creates a new recipe and persists it to the database.
        /// </summary>
        /// <param name="model">The recipe creation data transfer model.</param>
        /// <param name="userId">The identifier of the user creating the recipe.</param>
        /// <returns>The ID of the newly created recipe.</returns>
        public async Task<int> CreateAsync(RecipeCreateModel model, string userId)
        {
            var recipe = new Recipe
            {
                Title = model.Title,
                Instructions = model.Instructions,
                CategoryId = model.CategoryId,
                UserId = userId,
                ImagePath = model.ImagePath,
                RecipeIngredients = model.IngredientIds
                    .Select(i => new RecipeIngredient { IngredientId = i })
                    .ToList()
            };

            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            return recipe.Id;
        }

        /// <summary>
        /// Retrieves all recipes for listing purposes.
        /// </summary>
        /// <returns>A collection of <see cref="RecipeListModel"/>.</returns>
        public async Task<IEnumerable<RecipeListModel>> GetAllAsync()
            => await _context.Recipes
                .Select(r => new RecipeListModel
                {
                    Id = r.Id,
                    Title = r.Title,
                    Instructions = r.Instructions,
                    Category = r.Category.Name,
                    Ingredients = r.RecipeIngredients
                        .Select(ri => ri.Ingredient.Name),
                    AuthorId = r.UserId,
                    ImagePath = r.ImagePath
                })
                .ToListAsync();

        /// <summary>
        /// Retrieves detailed information for a specific recipe.
        /// </summary>
        /// <param name="id">The ID of the recipe.</param>
        /// <returns>
        /// A <see cref="RecipeDetailsModel"/> if found; otherwise, <c>null</c>.
        /// </returns>
        public async Task<RecipeDetailsModel?> GetByIdAsync(int id)
            => await _context.Recipes
                .Where(r => r.Id == id)
                .Select(r => new RecipeDetailsModel
                {
                    Id = r.Id,
                    Title = r.Title,
                    Instructions = r.Instructions,
                    Category = r.Category.Name,
                    Ingredients = r.RecipeIngredients
                        .Select(ri => ri.Ingredient.Name),
                    AuthorId = r.UserId,
                    ImagePath = r.ImagePath
                })
                .FirstOrDefaultAsync();

        /// <summary>
        /// Updates an existing recipe with new data.
        /// </summary>
        /// <param name="id">The ID of the recipe to update.</param>
        /// <param name="model">The updated recipe data.</param>
        public async Task EditAsync(int id, RecipeEditModel model)
        {
            var recipe = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
                return;

            recipe.Title = model.Title;
            recipe.Instructions = model.Instructions;
            recipe.CategoryId = model.CategoryId;

            if (!string.IsNullOrWhiteSpace(model.ImagePath))
            {
                recipe.ImagePath = model.ImagePath;
            }

            // Remove existing ingredient relations
            recipe.RecipeIngredients.Clear();

            // Add updated ingredient relations
            recipe.RecipeIngredients = model.IngredientIds
                .Select(i => new RecipeIngredient
                {
                    RecipeId = id,
                    IngredientId = i
                })
                .ToList();

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a recipe from the database.
        /// </summary>
        /// <param name="id">The ID of the recipe to delete.</param>
        public async Task DeleteAsync(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);

            if (recipe != null)
            {
                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Retrieves all categories formatted for dropdown usage.
        /// </summary>
        /// <returns>A collection of <see cref="CategoryDropdownModel"/>.</returns>
        public async Task<IEnumerable<CategoryDropdownModel>> GetCategoriesAsync()
            => await _context.Categories
                .Select(c => new CategoryDropdownModel
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

        /// <summary>
        /// Retrieves all ingredients formatted for dropdown usage.
        /// </summary>
        /// <returns>A collection of <see cref="IngredientDropdownModel"/>.</returns>
        public async Task<IEnumerable<IngredientDropdownModel>> GetIngredientsAsync()
            => await _context.Ingredients
                .Select(i => new IngredientDropdownModel
                {
                    Id = i.Id,
                    Name = i.Name
                })
                .ToListAsync();

        /// <summary>
        /// Retrieves a recipe prepared for editing.
        /// </summary>
        /// <param name="id">The ID of the recipe.</param>
        /// <returns>
        /// A <see cref="RecipeEditModel"/> if found; otherwise, <c>null</c>.
        /// </returns>
        public async Task<RecipeEditModel?> GetForEditAsync(int id)
        {
            var recipe = await _context.Recipes
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
                return null;

            return new RecipeEditModel
            {
                Id = recipe.Id,
                Title = recipe.Title,
                Instructions = recipe.Instructions,
                CategoryId = recipe.CategoryId,
                IngredientIds = recipe.RecipeIngredients
                    .Select(ri => ri.IngredientId)
                    .ToList(),
                ImagePath = recipe.ImagePath
            };
        }

        /// <summary>
        /// Determines whether a specific user is the owner of a given recipe.
        /// </summary>
        /// <param name="recipeId">The recipe ID.</param>
        /// <param name="userId">The user ID.</param>
        /// <returns>
        /// <c>true</c> if the user owns the recipe; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> IsOwnerAsync(int recipeId, string userId)
        {
            return await _context.Recipes
                .AnyAsync(r => r.Id == recipeId && r.UserId == userId);
        }
    }
}
