using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Mines.cs — core game logic for the WPF Mines game.
// Responsible for generating a 5x5 layout with mines ("m") and gems/safe cells ("g"),
// tracking revealed gems, and computing the cashout multiplier based on current progress.
namespace OpenSteak_Mines_WPF.Games
{
    /// <summary>
    /// Core, UI-agnostic game logic for the Mines game.
    /// - Maintains a fixed 5x5 grid represented as a 1D <see cref="string"/> array (`"g"` = gem/safe, `"m"` = mine).
    /// - Places mines with a small configurable bias to simulate a house edge.
    /// - Tracks progress via <see cref="RevealedGems"/>.
    /// - Exposes odds-based cashout multiplier calculation.
    ///
    /// This class has no WPF dependencies and can be reused by other frontends.
    /// </summary>
    public class Mines
    {
        /// <summary>
        /// Size of the grid in one dimension. The board is <c>GridSize x GridSize</c> (5x5 = 25 cells).
        /// </summary>
        private const int GridSize = 5; // Fixed grid size

        /// <summary>Probability factor used to bias mine placement slightly.</summary>
        private const double HouseEdge = 0.1; // Example house edge (10%)

        private readonly string[] layout;

        /// <summary>
        /// Number of safe tiles already revealed by the user in the current round.
        /// </summary>
        public int RevealedGems { get; set; }

        /// <summary>
        /// Number of mines to place on the grid for the current round.
        /// Must be in the range 1..24 (inclusive) for a 25-cell grid.
        /// </summary>
        public int MinesCount { get; set; } // Number of mines to place

        /// <summary>
        /// Create a new instance and initialize an empty layout (all safe cells).
        /// </summary>
        public Mines()
        {
            layout = new string[GridSize * GridSize]; // Initialize array for 5x5 grid
            InitializeLayout(); // Initialize layout with 'g'
        }

        /// <summary>
        /// Get the grid size in one dimension (5 for a 5x5 grid).
        /// </summary>
        public int GetGridSize()
        {
            return GridSize;
        }

        /// <summary>
        /// Start (or restart) a round by placing mines on the current layout.
        /// This method is synchronous to avoid race conditions with UI rendering.
        /// </summary>
        public void Start()
        {
            // Run synchronously to avoid race conditions with the UI
            StartGame();
        }

        /// <summary>
        /// Get a direct reference to the internal 1D layout array.
        /// Index mapping: <c>index = row * GridSize + col</c>.
        /// Values: <c>"g"</c> = gem/safe, <c>"m"</c> = mine.
        /// </summary>
        public string[] GetLayout()
        {
            return layout;
        }

        /// <summary>
        /// Internal entry point to perform a round setup.
        /// </summary>
        private void StartGame()
        {
            PlaceMines(); // Place mines based on the house edge
        }

        /// <summary>
        /// Reset the layout and place <see cref="MinesCount"/> mines at random positions
        /// with a small bias defined by <see cref="HouseEdge"/>.
        /// </summary>
        private void PlaceMines()
        {
            Random random = new Random();
            InitializeLayout(); // Ensure layout is reset

            int placedMines = 0;
            while (placedMines < MinesCount)
            {
                int currentIndex = GetBiasedRandomIndex(random);
                if (layout[currentIndex] == "g")
                {
                    layout[currentIndex] = "m";
                    placedMines++;
                }
            }
        }

        /// <summary>
        /// Compute the cashout multiplier based on the probability of having avoided a mine so far.
        /// The result is rounded to 2 decimals and includes a 1% house factor (0.99).
        /// </summary>
        /// <returns>Multiplier value such as <c>1.23</c> representing <c>1.23x</c>.</returns>
        public decimal GetCashoutMultiplier()
        {
            decimal payout = 1;
            for (int i = 0; i < this.MinesCount; i++)
            {
                payout = payout * ((GridSize * GridSize) - RevealedGems - i) / ((GridSize * GridSize) - i);
            }
            return Math.Round(0.99m / payout, 2);
        }

        /// <summary>
        /// Initialize the layout to all-safe <c>"g"</c> cells.
        /// </summary>
        private void InitializeLayout()
        {
            for (int i = 0; i < layout.Length; i++)
            {
                layout[i] = "g"; // Initialize all cells as 'g'
            }
        }

        /// <summary>
        /// Return a random index in <paramref name="layout"/> with a small chance to shift the choice
        /// by a few positions to create a simple bias.
        /// </summary>
        private int GetBiasedRandomIndex(Random random)
        {
            // Get a random index
            int index = random.Next(layout.Length);

            // Introduce bias based on the house edge
            if (random.NextDouble() < HouseEdge)
            {
                // Apply bias to select an index in a specific region more frequently
                int offset = random.Next(5); // Small offset for bias
                index = (index + offset) % layout.Length;
            }

            return index;
        }
    }
}

