using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToe
{
    public class GameState
    {
        public Player[,] GameGrid { get; private set; }
        public Player CurrentPlayer { get; private set; }
        public int TurnsPassed { get; private set; }
        public bool GameOver { get; private set; }
        public Bot Bot { get; private set; }
        public bool BotStarts { get; private set; }
        public int PlayerWins { get; private set; } = 0;
        public int BotWins { get; private set; } = 0;
        public int Ties { get; private set; } = 0;


        public event Action<int, int> MoveMade;
        public event Action<GameResult> GameEnded;
        public event Action GameStarted;

        public GameState()
        {
            GameGrid = new Player[3, 3];
            Bot = new Bot(this);
            BotStarts = Bot.ShouldBotStartFirst();
            CurrentPlayer = BotStarts ? Player.O : Player.X;
            TurnsPassed = 0;
            GameOver = false;

        }

        private bool CanMakeMove(int row, int col)
        {
            return !GameOver && GameGrid[row, col] == Player.None;
        }

        private bool IsGridFull()
        {
            return TurnsPassed >= 9;
        }

        private void SwitchPlayer()
        {
            CurrentPlayer = CurrentPlayer == Player.X ? Player.O : Player.X;
        }

        private bool AreSquaresMarked((int, int)[] squares, Player player)
        {
            foreach ((int r, int c) in squares)
            {
                if (GameGrid[r, c] != player)
                {
                    return false;
                }
            }
            return true;
        }

        private bool DidMoveWin(int r, int c, out WinInfo winInfo)
        {
            (int, int)[] row = { (r, 0), (r, 1), (r, 2) };
            (int, int)[] col = { (0, c), (1, c), (2, c) };
            (int, int)[] mainDiag = { (0, 0), (1, 1), (2, 2) };
            (int, int)[] antiDiag = { (0, 2), (1, 1), (2, 0) };

            if (AreSquaresMarked(row, CurrentPlayer))
            {
                winInfo = new WinInfo { WinType = WinType.Row, Number = r };
                return true;
            }

            if (AreSquaresMarked(col, CurrentPlayer))
            {
                winInfo = new WinInfo { WinType = WinType.Column, Number = c };
                return true;
            }

            if (AreSquaresMarked(mainDiag, CurrentPlayer))
            {
                winInfo = new WinInfo { WinType = WinType.MainDiagonal };
                return true;
            }
            if (AreSquaresMarked(antiDiag, CurrentPlayer))
            {
                winInfo = new WinInfo { WinType = WinType.AntiDiagonal };
                return true;
            }

            winInfo = null;
            return false;
        }

        private bool DidMoveEndGame(int r, int c, out GameResult gameResult)
        {
            if (DidMoveWin(r, c, out WinInfo winInfo))
            {
                gameResult = new GameResult { Winner = CurrentPlayer, WinInfo = winInfo };

                // Update scores
                if (CurrentPlayer == Player.X) PlayerWins++;
                else if (CurrentPlayer == Player.O) BotWins++;

                return true;
            }

            if (IsGridFull())
            {
                gameResult = new GameResult { Winner = Player.None };
                return true;
            }

            gameResult = null;
            return false;
        }

        public void ResetScores()
        {
            PlayerWins = 0;
            BotWins = 0;
            Ties = 0;
        }

        public void MakeMove(int row, int col)
        {
            if (!CanMakeMove(row, col))
            {
                throw new InvalidOperationException("Invalid move");
            }
            GameGrid[row, col] = CurrentPlayer;
            TurnsPassed++;

            if (DidMoveEndGame(row, col, out GameResult gameResult))
            {
                GameOver = true;
                MoveMade?.Invoke(row, col);
                if (gameResult.Winner == Player.None)
                {
                    Ties++;
                }
                GameEnded?.Invoke(gameResult);
            }
            else
            {
                SwitchPlayer();
                MoveMade?.Invoke(row, col);
            }
        }

        public void Reset()
        {
            GameGrid = new Player[3, 3];
            BotStarts = !BotStarts;
            CurrentPlayer = BotStarts ? Player.O : Player.X;
            TurnsPassed = 0;
            GameOver = false;
            GameStarted?.Invoke();

        }
        public bool CheckForWinner(Player player)
        {
            for (int i = 0; i < 3; i++)
            {
                // Rows
                if (GameGrid[i, 0] == player && GameGrid[i, 1] == player && GameGrid[i, 2] == player)
                    return true;

                // Columns
                if (GameGrid[0, i] == player && GameGrid[1, i] == player && GameGrid[2, i] == player)
                    return true;
            }

            // Diagonals
            if (GameGrid[0, 0] == player && GameGrid[1, 1] == player && GameGrid[2, 2] == player)
                return true;

            if (GameGrid[0, 2] == player && GameGrid[1, 1] == player && GameGrid[2, 0] == player)
                return true;

            return false;
        }


    }
}