using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CraftingGame.Crafting
{
    /// <summary>
    /// Builds and manages the crafting and inventory UI entirely through code so the scene can stay lightweight.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public class CraftingUIManager : MonoBehaviour
    {
        private const int InventorySize = 16;
        private const int GridSize = 3;

        private readonly List<InventorySlotUI> inventorySlots = new List<InventorySlotUI>();
        private readonly InventorySlotUI[,] craftingSlots = new InventorySlotUI[GridSize, GridSize];
        private InventorySlotUI outputSlot;

        private ItemStack heldStack;

        private ItemStack[] inventory = new ItemStack[InventorySize];
        private ItemStack[,] craftingGrid = new ItemStack[GridSize, GridSize];

        private CraftingSystem craftingSystem;

        private Text heldItemLabel;

        private void Awake()
        {
            SetupCanvasForPixelArt();
            CreateUiHierarchy();
            var database = CreateDatabase();
            craftingSystem = new CraftingSystem(database.Recipes);
            inventory = database.InventoryStartState;
            RefreshInventoryUi();
            RefreshCraftingUi();
            UpdateOutput();
            UpdateHeldLabel();
        }

        private void SetupCanvasForPixelArt()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(320, 180);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 16f;
        }

        private void CreateUiHierarchy()
        {
            var root = CreatePanel("Root", transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            root.sizeDelta = new Vector2(300, 170);

            var title = CreateLabel("CraftingTitle", root, "Pixel Crafting", 18, TextAnchor.UpperCenter);
            title.rectTransform.anchoredPosition = new Vector2(0, -8);

            var columns = CreatePanel("Columns", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            columns.sizeDelta = new Vector2(280, 120);
            columns.anchoredPosition = new Vector2(0, -20);
            var columnsLayout = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
            columnsLayout.childAlignment = TextAnchor.UpperCenter;
            columnsLayout.spacing = 12;
            columnsLayout.padding = new RectOffset(12, 12, 12, 12);

            // Crafting column
            var craftingColumn = CreatePanel("CraftingColumn", columns, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            craftingColumn.sizeDelta = new Vector2(140, 120);
            var craftingLabel = CreateLabel("CraftingLabel", craftingColumn, "3x3 Crafting", 12, TextAnchor.UpperCenter);
            craftingLabel.rectTransform.anchoredPosition = new Vector2(0, -4);

            var craftingGridContainer = CreatePanel("CraftingGrid", craftingColumn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            craftingGridContainer.sizeDelta = new Vector2(96, 96);
            craftingGridContainer.anchoredPosition = new Vector2(-20, -28);
            var gridLayout = craftingGridContainer.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = Vector2.one * 28f;
            gridLayout.spacing = Vector2.one * 2f;

            for (int y = 0; y < GridSize; y++)
            {
                for (int x = 0; x < GridSize; x++)
                {
                    var slot = CreateSlot($"CraftingSlot_{x}_{y}", craftingGridContainer);
                    craftingSlots[x, y] = slot;
                    int capturedX = x;
                    int capturedY = y;
                    slot.Button.onClick.AddListener(() => OnCraftingSlotClicked(capturedX, capturedY));
                }
            }

            var outputContainer = CreatePanel("Output", craftingColumn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            outputContainer.sizeDelta = new Vector2(40, 40);
            outputContainer.anchoredPosition = new Vector2(48, -28);
            outputSlot = CreateSlot("OutputSlot", outputContainer);
            outputSlot.Button.onClick.AddListener(OnOutputSlotClicked);

            // Inventory column
            var inventoryColumn = CreatePanel("InventoryColumn", columns, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            inventoryColumn.sizeDelta = new Vector2(140, 120);
            var inventoryLabel = CreateLabel("InventoryLabel", inventoryColumn, "Inventar", 12, TextAnchor.UpperCenter);
            inventoryLabel.rectTransform.anchoredPosition = new Vector2(0, -4);

            var inventoryGridContainer = CreatePanel("InventoryGrid", inventoryColumn, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            inventoryGridContainer.sizeDelta = new Vector2(112, 112);
            inventoryGridContainer.anchoredPosition = new Vector2(0, -32);
            var inventoryLayout = inventoryGridContainer.gameObject.AddComponent<GridLayoutGroup>();
            inventoryLayout.cellSize = Vector2.one * 24f;
            inventoryLayout.spacing = Vector2.one * 2f;

            for (int i = 0; i < InventorySize; i++)
            {
                var slot = CreateSlot($"InventorySlot_{i}", inventoryGridContainer);
                inventorySlots.Add(slot);
                int capturedIndex = i;
                slot.Button.onClick.AddListener(() => OnInventorySlotClicked(capturedIndex));
            }

            heldItemLabel = CreateLabel("HeldItemLabel", root, "", 12, TextAnchor.LowerCenter).textComponent;
            heldItemLabel.rectTransform.anchoredPosition = new Vector2(0, -150);
            heldItemLabel.color = new Color32(255, 255, 255, 200);
        }

        private DatabaseData CreateDatabase()
        {
            ItemData CreateItem(string id, string name, Color color)
            {
                var texture = new Texture2D(16, 16)
                {
                    filterMode = FilterMode.Point
                };
                var pixels = new Color[16 * 16];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = color;
                }
                texture.SetPixels(pixels);
                texture.Apply();

                var sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
                return new ItemData(id, name, sprite);
            }

            var wood = CreateItem("wood", "Holz", new Color32(139, 101, 72, 255));
            var stone = CreateItem("stone", "Stein", new Color32(110, 110, 120, 255));
            var plank = CreateItem("plank", "Brett", new Color32(193, 156, 77, 255));
            var furnace = CreateItem("furnace", "Ofen", new Color32(80, 80, 80, 255));

            var recipes = new List<CraftingRecipe>
            {
                new CraftingRecipe(
                    "Planks",
                    new[]
                    {
                        "W..",
                        "W..",
                        "..."
                    },
                    new Dictionary<char, ItemData>
                    {
                        ['W'] = wood,
                        ['.'] = null
                    },
                    new ItemStack(plank, 4)
                ),
                new CraftingRecipe(
                    "Furnace",
                    new[]
                    {
                        "SSS",
                        "S.S",
                        "SSS"
                    },
                    new Dictionary<char, ItemData>
                    {
                        ['S'] = stone,
                        ['.'] = null
                    },
                    new ItemStack(furnace, 1)
                )
            };

            var startingInventory = new ItemStack[InventorySize];
            startingInventory[0] = new ItemStack(wood, 8);
            startingInventory[1] = new ItemStack(stone, 8);

            return new DatabaseData
            {
                Recipes = recipes.ToArray(),
                InventoryStartState = startingInventory
            };
        }

        private InventorySlotUI CreateSlot(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = Vector2.one * 24f;

            var image = go.GetComponent<Image>();
            image.color = new Color32(30, 30, 30, 255);
            image.material = null;
            image.raycastTarget = true;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color32(80, 80, 80, 255);
            outline.effectDistance = new Vector2(1, -1);

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color32(60, 60, 60, 255);
            colors.highlightedColor = new Color32(80, 80, 80, 255);
            colors.pressedColor = new Color32(100, 100, 100, 255);
            colors.selectedColor = new Color32(100, 100, 100, 255);
            colors.disabledColor = new Color32(30, 30, 30, 255);
            button.colors = colors;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRect = iconGo.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0);
            iconRect.anchorMax = new Vector2(1, 1);
            iconRect.offsetMin = new Vector2(4, 4);
            iconRect.offsetMax = new Vector2(-4, -4);

            var countLabel = new GameObject("Count", typeof(RectTransform), typeof(Text));
            countLabel.transform.SetParent(go.transform, false);
            var countRect = countLabel.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(1, 0);
            countRect.anchorMax = new Vector2(1, 0);
            countRect.anchoredPosition = new Vector2(-2, 2);
            countRect.sizeDelta = new Vector2(24, 12);

            var countText = countLabel.GetComponent<Text>();
            countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countText.fontSize = 10;
            countText.alignment = TextAnchor.LowerRight;
            countText.color = new Color32(220, 220, 220, 255);
            countText.raycastTarget = false;

            return new InventorySlotUI(go.GetComponent<Button>(), iconGo.GetComponent<Image>(), countText);
        }

        private RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            return rect;
        }

        private (Text textComponent, RectTransform rectTransform) CreateLabel(string name, RectTransform parent, string text, int fontSize, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 24);

            var label = go.GetComponent<Text>();
            label.text = text;
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;

            return (label, rect);
        }

        private void OnInventorySlotClicked(int index)
        {
            var slot = inventory[index];
            if (heldStack.IsEmpty)
            {
                if (!slot.IsEmpty)
                {
                    heldStack = slot.Split(1);
                    inventory[index] = slot;
                }
            }
            else
            {
                if (slot.IsEmpty)
                {
                    inventory[index] = heldStack;
                    heldStack = default;
                }
                else if (slot.Item == heldStack.Item)
                {
                    slot.Merge(heldStack);
                    heldStack = default;
                    inventory[index] = slot;
                }
            }

            RefreshInventoryUi();
            UpdateHeldLabel();
        }

        private void OnCraftingSlotClicked(int x, int y)
        {
            var slot = craftingGrid[x, y];
            if (heldStack.IsEmpty)
            {
                if (!slot.IsEmpty)
                {
                    heldStack = slot.Split(1);
                    craftingGrid[x, y] = slot;
                }
            }
            else
            {
                if (slot.IsEmpty)
                {
                    craftingGrid[x, y] = heldStack.Split(1);
                    if (heldStack.IsEmpty)
                    {
                        heldStack = default;
                    }
                }
                else if (slot.Item == heldStack.Item)
                {
                    slot.Count += 1;
                    heldStack.Count -= 1;
                    if (heldStack.Count <= 0)
                    {
                        heldStack = default;
                    }
                    craftingGrid[x, y] = slot;
                }
            }

            RefreshCraftingUi();
            UpdateHeldLabel();
            UpdateOutput();
        }

        private void OnOutputSlotClicked()
        {
            if (outputSlot.Stack.IsEmpty)
            {
                return;
            }

            if (!heldStack.IsEmpty && heldStack.Item != outputSlot.Stack.Item)
            {
                return;
            }

            var craftedItem = outputSlot.Stack;
            if (heldStack.IsEmpty)
            {
                heldStack = craftedItem;
            }
            else
            {
                heldStack.Count += craftedItem.Count;
            }

            // consume ingredients
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    if (!craftingGrid[x, y].IsEmpty)
                    {
                        craftingGrid[x, y].Count -= 1;
                        if (craftingGrid[x, y].Count <= 0)
                        {
                            craftingGrid[x, y] = default;
                        }
                    }
                }
            }

            RefreshCraftingUi();
            UpdateOutput();
            UpdateHeldLabel();
        }

        private void UpdateOutput()
        {
            var recipe = craftingSystem.FindMatch(craftingGrid);
            if (recipe == null)
            {
                outputSlot.SetStack(default);
                return;
            }

            outputSlot.SetStack(recipe.Output);
        }

        private void RefreshInventoryUi()
        {
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                var stack = inventory[i];
                inventorySlots[i].SetStack(stack);
            }
        }

        private void RefreshCraftingUi()
        {
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    craftingSlots[x, y].SetStack(craftingGrid[x, y]);
                }
            }
        }

        private void UpdateHeldLabel()
        {
            if (heldStack.IsEmpty)
            {
                heldItemLabel.text = "Kein Item ausgewählt";
            }
            else
            {
                heldItemLabel.text = $"In Hand: {heldStack.Item.DisplayName} x{heldStack.Count}";
            }
        }

        private class InventorySlotUI
        {
            public Button Button { get; }
            public ItemStack Stack { get; private set; }

            private readonly Image icon;
            private readonly Text countText;

            public InventorySlotUI(Button button, Image icon, Text countText)
            {
                Button = button;
                this.icon = icon;
                this.countText = countText;
                Stack = default;
            }

            public void SetStack(ItemStack stack)
            {
                Stack = stack;
                if (stack.IsEmpty)
                {
                    icon.enabled = false;
                    icon.sprite = null;
                    countText.text = string.Empty;
                }
                else
                {
                    icon.enabled = true;
                    icon.sprite = stack.Item.Icon;
                    countText.text = stack.Count > 1 ? stack.Count.ToString() : string.Empty;
                }
            }
        }

        private struct DatabaseData
        {
            public CraftingRecipe[] Recipes;
            public ItemStack[] InventoryStartState;
        }
    }
}
