using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToe
{
    public class Bot
    {
        private readonly Random random = new Random();
        private readonly GameState gameState;
        private bool isFirstMove = true;

        public Bot(GameState gameState)
        {
            this.gameState = gameState;
        }

        public bool ShouldBotStartFirst()
        {
            return random.Next(2) == 0;
        }

        public void MakeMove()
        {
            var emptyPositions = GetEmptyPositions();
            if (emptyPositions.Count == 0) return;

            // 1. Try to win (90% of the time)
            if (random.Next(100) < 90)
            {
                var winningMove = FindWinningMove(Player.O);
                if (winningMove != null)
                {
                    gameState.MakeMove(winningMove.Value.row, winningMove.Value.col);
                    isFirstMove = false;
                    return;
                }
            }

            // 2. Try to block opponent (70% of the time)
            if (random.Next(100) < 70)
            {
                var blockingMove = FindWinningMove(Player.X);
                if (blockingMove != null)
                {
                    gameState.MakeMove(blockingMove.Value.row, blockingMove.Value.col);
                    isFirstMove = false;
                    return;
                }
            }

            // 3. Try center or corners (60% of the time)
            if (random.Next(100) < 60)
            {
                var strategicMove = TrySmartPosition();
                if (strategicMove != null)
                {
                    gameState.MakeMove(strategicMove.Value.row, strategicMove.Value.col);
                    isFirstMove = false;
                    return;
                }
            }

            // 4. Fallback: pick random move
            var fallback = emptyPositions[random.Next(emptyPositions.Count)];
            gameState.MakeMove(fallback.row, fallback.col);
            isFirstMove = false;
        }

        private (int row, int col)? TrySmartPosition()
        {
            // Prefer center
            if (gameState.GameGrid[1, 1] == Player.None)
                return (1, 1);

            // Then corners
            var corners = new List<(int, int)> { (0, 0), (0, 2), (2, 0), (2, 2) };
            var openCorners = corners.Where(p => gameState.GameGrid[p.Item1, p.Item2] == Player.None).ToList();
            if (openCorners.Any())
                return openCorners[random.Next(openCorners.Count)];

            // Then edges
            var edges = new List<(int, int)> { (0, 1), (1, 0), (1, 2), (2, 1) };
            var openEdges = edges.Where(p => gameState.GameGrid[p.Item1, p.Item2] == Player.None).ToList();
            if (openEdges.Any())
                return openEdges[random.Next(openEdges.Count)];

            return null;
        }

        private List<(int row, int col)> GetEmptyPositions()
        {
            var list = new List<(int row, int col)>();
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (gameState.GameGrid[r, c] == Player.None)
                        list.Add((r, c));
                }
            }
            return list;
        }

        private (int row, int col)? FindWinningMove(Player player)
        {
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (gameState.GameGrid[r, c] == Player.None)
                    {
                        gameState.GameGrid[r, c] = player;
                        bool isWin = gameState.CheckForWinner(player);
                        gameState.GameGrid[r, c] = Player.None;

                        if (isWin)
                            return (r, c);
                    }
                }
            }
            return null;
        }
    }
}
