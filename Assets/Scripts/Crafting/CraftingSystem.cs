using System.Collections.Generic;
using UnityEngine;

namespace CraftingGame.Crafting
{
    /// <summary>
    /// Evaluates crafting recipes against a crafting grid.
    /// </summary>
    public class CraftingSystem
    {
        private readonly List<CraftingRecipe> recipes = new List<CraftingRecipe>();

        public CraftingSystem(IEnumerable<CraftingRecipe> recipes)
        {
            this.recipes.AddRange(recipes);
        }

        public CraftingRecipe FindMatch(ItemStack[,] grid)
        {
            foreach (var recipe in recipes)
            {
                if (Matches(recipe, grid))
                {
                    return recipe;
                }
            }

            return null;
        }

        private bool Matches(CraftingRecipe recipe, ItemStack[,] grid)
        {
            if (recipe.Pattern == null || recipe.Pattern.Length != 3)
            {
                return false;
            }

            // Determine bounds of non-empty cells in the grid.
            if (!TryGetBounds(grid, out var minX, out var minY, out var maxX, out var maxY))
            {
                // Empty grid cannot match a recipe with output.
                return false;
            }

            var trimmedWidth = maxX - minX + 1;
            var trimmedHeight = maxY - minY + 1;

            for (int offsetY = 0; offsetY <= 3 - trimmedHeight; offsetY++)
            {
                for (int offsetX = 0; offsetX <= 3 - trimmedWidth; offsetX++)
                {
                    if (PatternMatches(recipe, grid, minX, minY, trimmedWidth, trimmedHeight, offsetX, offsetY))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool PatternMatches(CraftingRecipe recipe, ItemStack[,] grid, int minX, int minY, int width, int height, int offsetX, int offsetY)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    char symbol = recipe.Pattern[y][x];
                    var expectedItem = recipe.GetItemForSymbol(symbol);
                    bool patternSlotIsEmpty = symbol == '.';

                    var gridX = x - offsetX + minX;
                    var gridY = y - offsetY + minY;

                    bool withinTrimmedBounds = gridX >= minX && gridX < minX + width && gridY >= minY && gridY < minY + height;

                    if (!withinTrimmedBounds)
                    {
                        if (!patternSlotIsEmpty)
                        {
                            return false;
                        }

                        continue;
                    }

                    var stack = grid[gridX, gridY];
                    if (patternSlotIsEmpty)
                    {
                        if (!stack.IsEmpty)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (stack.IsEmpty || stack.Item != expectedItem)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool TryGetBounds(ItemStack[,] grid, out int minX, out int minY, out int maxX, out int maxY)
        {
            minX = grid.GetLength(0);
            minY = grid.GetLength(1);
            maxX = -1;
            maxY = -1;

            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (!grid[x, y].IsEmpty)
                    {
                        if (x < minX) minX = x;
                        if (y < minY) minY = y;
                        if (x > maxX) maxX = x;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            return maxX >= minX && maxY >= minY;
        }
    }
}
