using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using RecipeHub.Data;
using RecipeHub.Data.Data.Models;
using RecipeHub.Services.Models;
using RecipeHub.Services.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RecipeHub.Tests
{
    public class RecipeServiceTests
    {
        private ApplicationDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private async Task SeedDataAsync(ApplicationDbContext ctx)
        {
            ctx.Categories.Add(new Category { Id = 1, Name = "Dessert" });
            ctx.Ingredients.Add(new Ingredient { Id = 1, Name = "Sugar" });
            ctx.Ingredients.Add(new Ingredient { Id = 2, Name = "Flour" });
            await ctx.SaveChangesAsync();
        }

        private async Task<int> CreateRecipeAsync(string dbName)
        {
            using var ctx = CreateInMemoryContext(dbName);
            var service = new RecipeService(ctx);

            var model = new RecipeCreateModel
            {
                Title = "Cake",
                Instructions = "Bake it",
                CategoryId = 1,
                IngredientIds = new List<int> { 1, 2 },
                ImagePath = "/images/recipes/test.jpg"
            };

            return await service.CreateAsync(model, "user-1");
        }

        [Test]
        public async Task CreateAsync_ShouldCreateRecipe()
        {
            var dbName = Guid.NewGuid().ToString();

            using (var ctx = CreateInMemoryContext(dbName))
            {
                await SeedDataAsync(ctx);
            }

            var createdId = await CreateRecipeAsync(dbName);

            using (var ctx = CreateInMemoryContext(dbName))
            {
                Assert.AreEqual(1, ctx.Recipes.Count());
                Assert.Greater(createdId, 0);
            }
        }

        [Test]
        public async Task GetByIdAsync_ShouldReturnRecipe()
        {
            var dbName = Guid.NewGuid().ToString();

            using (var ctx = CreateInMemoryContext(dbName))
            {
                await SeedDataAsync(ctx);
            }

            var createdId = await CreateRecipeAsync(dbName);

            using (var ctx = CreateInMemoryContext(dbName))
            {
                var service = new RecipeService(ctx);
                var recipe = await service.GetByIdAsync(createdId);

                Assert.NotNull(recipe);
                Assert.AreEqual("Cake", recipe.Title);
                Assert.AreEqual("Dessert", recipe.Category);
                Assert.AreEqual(2, recipe.Ingredients.Count());
                Assert.AreEqual("/images/recipes/test.jpg", recipe.ImagePath);
            }
        }

        [Test]
        public async Task EditAsync_ShouldUpdateRecipe()
        {
            var dbName = Guid.NewGuid().ToString();

            using (var ctx = CreateInMemoryContext(dbName))
            {
                await SeedDataAsync(ctx);
            }

            var createdId = await CreateRecipeAsync(dbName);

            using (var ctx = CreateInMemoryContext(dbName))
            {
                var service = new RecipeService(ctx);

                var editModel = new RecipeEditModel
                {
                    Id = createdId,
                    Title = "Cupcake",
                    Instructions = "Bake it differently",
                    CategoryId = 1,
                    IngredientIds = new List<int> { 1 }
                };

                await service.EditAsync(createdId, editModel);
            }

            using (var ctx = CreateInMemoryContext(dbName))
            {
                var service = new RecipeService(ctx);
                var recipe = await service.GetByIdAsync(createdId);

                Assert.AreEqual("Cupcake", recipe.Title);
                Assert.AreEqual(1, recipe.Ingredients.Count());
            }
        }

        [Test]
        public async Task IsOwnerAsync_ShouldReturnCorrectResult()
        {
            var dbName = Guid.NewGuid().ToString();

            using (var ctx = CreateInMemoryContext(dbName))
            {
                await SeedDataAsync(ctx);
            }

            var createdId = await CreateRecipeAsync(dbName);

            using (var ctx = CreateInMemoryContext(dbName))
            {
                var service = new RecipeService(ctx);

                Assert.IsTrue(await service.IsOwnerAsync(createdId, "user-1"));
                Assert.IsFalse(await service.IsOwnerAsync(createdId, "other-user"));
            }
        }

        [Test]
        public async Task DeleteAsync_ShouldRemoveRecipe()
        {
            var dbName = Guid.NewGuid().ToString();

            using (var ctx = CreateInMemoryContext(dbName))
            {
                await SeedDataAsync(ctx);
            }

            var createdId = await CreateRecipeAsync(dbName);

            using (var ctx = CreateInMemoryContext(dbName))
            {
                var service = new RecipeService(ctx);
                await service.DeleteAsync(createdId);
            }

            using (var ctx = CreateInMemoryContext(dbName))
            {
                Assert.AreEqual(0, ctx.Recipes.Count());
            }
        }
    }
}
