using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using RecipeHub.Services.Interfaces;
using RecipeHub.Services.Models;
using RecipeHub.Web.Models;
using System.Security.Claims;

namespace RecipeHub.Web.Controllers
{
    /// <summary>
    /// MVC controller responsible for managing recipe-related operations
    /// such as listing, viewing, creating, editing, and deleting recipes.
    /// </summary>
    [Authorize]
    public class RecipesController : Controller
    {
        private readonly IRecipeService _recipeService;
        private readonly IWebHostEnvironment _env;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecipesController"/> class.
        /// </summary>
        /// <param name="recipeService">Service responsible for recipe business logic.</param>
        /// <param name="env">Web hosting environment for file storage access.</param>
        public RecipesController(IRecipeService recipeService, IWebHostEnvironment env)
        {
            _recipeService = recipeService;
            _env = env;
        }

        /// <summary>
        /// Displays a list of recipes with optional filtering
        /// by search term, category, or ingredient.
        /// </summary>
        /// <param name="q">Search query (title or ingredient).</param>
        /// <param name="categoryId">Optional category filter.</param>
        /// <param name="ingredient">Optional ingredient filter.</param>
        /// <returns>The recipe index view.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Index(string q, int? categoryId, string ingredient)
        {
            var recipes = (await _recipeService.GetAllAsync()).ToList();

            var categories = (await _recipeService.GetCategoriesAsync()).ToList();
            ViewBag.Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            });

            var ingredients = (await _recipeService.GetIngredientsAsync()).ToList();
            ViewBag.Ingredients = ingredients.Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = i.Name
            });

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                recipes = recipes
                    .Where(r => r.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                             || r.Ingredients.Any(i => i.Contains(term, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (categoryId.HasValue)
            {
                var cat = categories.FirstOrDefault(c => c.Id == categoryId.Value)?.Name;
                if (!string.IsNullOrEmpty(cat))
                {
                    recipes = recipes
                        .Where(r => string.Equals(r.Category, cat, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
            }

            if (!string.IsNullOrWhiteSpace(ingredient))
            {
                var ingr = ingredient.Trim();
                recipes = recipes
                    .Where(r => r.Ingredients.Any(i => i.Contains(ingr, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            return View(recipes);
        }

        /// <summary>
        /// Displays detailed information for a specific recipe.
        /// </summary>
        /// <param name="id">The ID of the recipe.</param>
        /// <returns>The details view or 404 if not found.</returns>
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _recipeService.GetByIdAsync(id);
            if (recipe == null)
                return NotFound();

            return View(recipe);
        }

        /// <summary>
        /// Displays the recipe creation form.
        /// </summary>
        /// <returns>The create view.</returns>
        public async Task<IActionResult> Create()
        {
            var model = new RecipeCreateViewModel();

            var categories = await _recipeService.GetCategoriesAsync();
            model.Categories = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            });

            var ingredients = await _recipeService.GetIngredientsAsync();
            model.Ingredients = ingredients.Select(i => new SelectListItem
            {
                Value = i.Id.ToString(),
                Text = i.Name
            });

            return View(model);
        }

        /// <summary>
        /// Handles submission of a new recipe.
        /// </summary>
        /// <param name="model">The recipe creation view model.</param>
        /// <returns>Redirects to Index on success; otherwise returns the view with validation errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RecipeCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            string? imagePath = null;

            if (model.Image != null && model.Image.Length > 0)
            {
                var folder = Path.Combine(_env.WebRootPath, "images", "recipes");
                Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.Image.FileName)}";
                var filePath = Path.Combine(folder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.Image.CopyToAsync(stream);

                imagePath = $"/images/recipes/{fileName}";
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var serviceModel = new RecipeCreateModel
            {
                Title = model.Title,
                Instructions = model.Instructions,
                CategoryId = model.CategoryId,
                IngredientIds = model.SelectedIngredientIds,
                ImagePath = imagePath
            };

            await _recipeService.CreateAsync(serviceModel, userId);

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Displays the edit form for a specific recipe.
        /// Only accessible by the owner or an administrator.
        /// </summary>
        /// <param name="id">The ID of the recipe to edit.</param>
        /// <returns>The edit view or appropriate authorization result.</returns>
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                // Only authenticated users should reach here because controller is [Authorize]
                var isAdmin = User.IsInRole("Admin");
                string? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                    userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!isAdmin)
                {
                    if (string.IsNullOrEmpty(userId) || !await _recipeService.IsOwnerAsync(id, userId))
                        return Forbid();
                }

                var recipe = await _recipeService.GetForEditAsync(id);
                if (recipe == null)
                    return NotFound();

                var vm = new RecipeEditViewModel
                {
                    Id = recipe.Id,
                    Title = recipe.Title,
                    Instructions = recipe.Instructions,
                    CategoryId = recipe.CategoryId,
                    IngredientIds = recipe.IngredientIds,
                    ImagePath = recipe.ImagePath
                };

                await PopulateDropdowns(vm);
                ViewBag.CanDelete = isAdmin || (!string.IsNullOrEmpty(userId) && await _recipeService.IsOwnerAsync(id, userId));

                return View(vm);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unable to open edit page: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Handles submission of edited recipe data.
        /// Only accessible by the owner or an administrator.
        /// </summary>
        /// <param name="id">The ID of the recipe.</param>
        /// <param name="model">The updated recipe data.</param>
        /// <returns>Redirects to Index on success.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RecipeEditViewModel model)
        {
            try
            {
                var isAdmin = User.IsInRole("Admin");
                string? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                    userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!isAdmin)
                {
                    if (string.IsNullOrEmpty(userId) || !await _recipeService.IsOwnerAsync(id, userId))
                        return Forbid();
                }

                if (!ModelState.IsValid)
                {
                    await PopulateDropdowns(model);
                    ViewBag.CanDelete = isAdmin || (!string.IsNullOrEmpty(userId) && await _recipeService.IsOwnerAsync(id, userId));
                    return View(model);
                }

                if (model.NewImage != null && model.NewImage.Length > 0)
                {
                    var folder = Path.Combine(_env.WebRootPath, "images", "recipes");
                    Directory.CreateDirectory(folder);

                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.NewImage.FileName)}";
                    var filePath = Path.Combine(folder, fileName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await model.NewImage.CopyToAsync(stream);

                    model.ImagePath = $"/images/recipes/{fileName}";
                }

                var serviceModel = new RecipeEditModel
                {
                    Id = model.Id,
                    Title = model.Title,
                    Instructions = model.Instructions,
                    CategoryId = model.CategoryId,
                    IngredientIds = model.IngredientIds,
                    ImagePath = model.ImagePath
                };

                await _recipeService.EditAsync(id, serviceModel);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unable to save changes: " + ex.Message;
                return RedirectToAction(nameof(Edit), new { id });
            }
        }

        /// <summary>
        /// Deletes a recipe.
        /// Only accessible by the owner or an administrator.
        /// </summary>
        /// <param name="id">The ID of the recipe to delete.</param>
        /// <returns>Redirects to Index.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var isAdmin = User.IsInRole("Admin");
                string? userId = null;
                if (User.Identity?.IsAuthenticated == true)
                    userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!isAdmin)
                {
                    if (string.IsNullOrEmpty(userId) || !await _recipeService.IsOwnerAsync(id, userId))
                        return Forbid();
                }

                await _recipeService.DeleteAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unable to delete recipe: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Populates category and ingredient dropdowns for create/edit forms.
        /// </summary>
        /// <param name="model">The view model containing dropdown collections.</param>
        private async Task PopulateDropdowns(dynamic model)
        {
            var categories = await _recipeService.GetCategoriesAsync();
            var categoryItems = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
            model.Categories = categoryItems;
            ViewBag.Categories = categoryItems;

            var ingredients = await _recipeService.GetIngredientsAsync();
            var ingredientItems = ingredients.Select(i => new SelectListItem(i.Name, i.Id.ToString())).ToList();
            model.Ingredients = ingredientItems;
            ViewBag.Ingredients = ingredientItems;
        }
    }
}
