using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TicTacToe
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<Player, ImageSource> imageSources = new()
        {
            {Player.X, new BitmapImage(new Uri("pack://application:,,,/Assets/X15.png"))},
            {Player.O, new BitmapImage(new Uri("pack://application:,,,/Assets/O15.png"))}
        };

        private readonly Dictionary<Player, ObjectAnimationUsingKeyFrames> animations = new()
        {
            {Player.X, new ObjectAnimationUsingKeyFrames() },
            {Player.O, new ObjectAnimationUsingKeyFrames() }

        };
        private readonly DoubleAnimation fadeOutAnimation = new DoubleAnimation
        {

            Duration = TimeSpan.FromSeconds(0.5),
            From = 1,
            To = 0
        };
        private readonly DoubleAnimation fadeInAnimation = new DoubleAnimation
        {

            Duration = TimeSpan.FromSeconds(0.5),
            From = 0,
            To = 1
        };

        private readonly Image[,] imageControls = new Image[3, 3];
        private readonly GameState gameState = new GameState();

        private readonly DoubleAnimation titleAnimation;
        private readonly DoubleAnimation buttonScaleAnimation;
        public MainWindow()
        {
            InitializeComponent();
            GoFullscreen();
            titleAnimation = new DoubleAnimation
            {
                From = 0.5,
                To = 1,
                Duration = TimeSpan.FromSeconds(1.5),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new ElasticEase { Oscillations = 1, Springiness = 4 }
            };

            buttonScaleAnimation = new DoubleAnimation
            {
                From = 0.95,
                To = 1.05,
                Duration = TimeSpan.FromSeconds(1),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase()
            };

            // Start animations
            var titleTransform = (ScaleTransform)((TextBlock)StartScreen.FindName("TextBlock")).RenderTransform;
            var buttonTransform = (ScaleTransform)((Button)StartScreen.FindName("StartButton")).RenderTransform;

            titleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, titleAnimation);
            titleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, titleAnimation);
            buttonTransform.BeginAnimation(ScaleTransform.ScaleXProperty, buttonScaleAnimation);
            buttonTransform.BeginAnimation(ScaleTransform.ScaleYProperty, buttonScaleAnimation);
            SetUpGameGrid();
            SetUpAnimations();
            UpdateScoreboard();

            // Ensure proper initial state
            GameGrid.IsEnabled = true;
            GameCanvas.IsHitTestVisible = true;
            EndScreeen.Visibility = Visibility.Collapsed;

            gameState.MoveMade += OnMoveMade;
            gameState.GameEnded += OnGameEnded;
            gameState.GameStarted += OnGameRestarted;

            PlayerImage.Source = imageSources[gameState.CurrentPlayer];
        }
        private void GoFullscreen()
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
        }
        private void SetUpGameGrid()
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    Image imageControl = new();
                    GameGrid.Children.Add(imageControl);
                    imageControls[row, col] = imageControl;
                }
            }
        }
        private void SetUpAnimations()
        {
            animations[Player.X].Duration = TimeSpan.FromSeconds(0.25);
            animations[Player.O].Duration = TimeSpan.FromSeconds(0.25);
            for (int i = 0; i < 16; i++)
            {
                Uri xUri = new Uri($"pack://application:,,,/Assets/X{i}.png");
                BitmapImage xImg = new BitmapImage(xUri);
                DiscreteObjectKeyFrame xKeyFrame = new DiscreteObjectKeyFrame(xImg);
                animations[Player.X].KeyFrames.Add(xKeyFrame);

                Uri oUri = new Uri($"pack://application:,,,/Assets/O{i}.png");
                BitmapImage oImg = new BitmapImage(oUri);
                DiscreteObjectKeyFrame oKeyFrame = new DiscreteObjectKeyFrame(oImg);
                animations[Player.O].KeyFrames.Add(oKeyFrame);
            }
        }
        private async Task FadeOut(UIElement element)
        {
            element.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
            await Task.Delay(fadeOutAnimation.Duration.TimeSpan);
            element.Opacity = 0;
        }
        private async Task FadeIn(UIElement element)
        {
            element.Visibility = Visibility.Visible;
            element.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
            await Task.Delay(fadeInAnimation.Duration.TimeSpan);

        }
        private async Task TransitionToEndScreen(string text, ImageSource winnerImage)
        {
            GameGrid.IsEnabled = false; // Explicitly disable grid
            await Task.WhenAll(FadeOut(GameCanvas), FadeOut(TurnPanel));
            ResultText.Text = text;
            WinnerImage.Source = winnerImage;
            EndScreeen.Visibility = Visibility.Visible; // Ensure it's visible before fading in
            await FadeIn(EndScreeen);
        }
        private async Task TransitionToGameScreen()
        {
            await FadeOut(EndScreeen);
            EndScreeen.Visibility = Visibility.Collapsed; // Important - completely remove from layout
            Line.Visibility = Visibility.Hidden;

            // Reset grid state before making visible
            GameGrid.IsEnabled = true;
            GameCanvas.IsHitTestVisible = true;

            await Task.WhenAll(FadeIn(TurnPanel), FadeIn(GameCanvas));
        }
        private (Point, Point) FindLinePoints(WinInfo winInfo)
        {
            double squareSize = GameGrid.Width / 3;
            double margin = squareSize / 2;
            if (winInfo.WinType == WinType.Row)
            {
                double y = winInfo.Number * squareSize + margin;
                return (new Point(0, y), new Point(GameGrid.Width, y));
            }
            if (winInfo.WinType == WinType.Column)
            {
                double x = winInfo.Number * squareSize + margin;
                return (new Point(x, 0), new Point(x, GameGrid.Height));
            }
            if (winInfo.WinType == WinType.MainDiagonal)
            {
                return (new Point(0, 0), new Point(GameGrid.Width, GameGrid.Height));
            }
            return (new Point(GameGrid.Width, 0), new Point(0, GameGrid.Height));
        }

        private async Task ShowLine(WinInfo winInfo)
        {
            (Point start, Point end) = FindLinePoints(winInfo);
            Line.X1 = start.X;
            Line.Y1 = start.Y;

            DoubleAnimation x2Animation = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(.25),
                From = start.X,
                To = end.X
            };
            DoubleAnimation y2Animation = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(.25),
                From = start.Y,
                To = end.Y
            };

            Line.Visibility = Visibility.Visible;
            Line.BeginAnimation(Line.X2Property, x2Animation);
            Line.BeginAnimation(Line.Y2Property, y2Animation);
            await Task.Delay(x2Animation.Duration.TimeSpan);

        }

        private void OnMoveMade(int row, int col)
        {
            Player player = gameState.GameGrid[row, col];
            imageControls[row, col].BeginAnimation(Image.SourceProperty, animations[player]);
            PlayerImage.Source = imageSources[gameState.CurrentPlayer];

            // Only trigger bot if it's the bot's turn and the game is not over
            if (!gameState.GameOver && gameState.CurrentPlayer == Player.O)
            {
                Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(500);
                    // Double-check before making the move
                    if (!gameState.GameOver && gameState.CurrentPlayer == Player.O)
                        gameState.Bot.MakeMove();
                });
            }
        }
        private async void OnGameEnded(GameResult gameResult)
        {
            await Task.Delay(1000);
            UpdateScoreboard(); // Add this line

            if (gameResult.Winner == Player.None)
            {
                await TransitionToEndScreen("It's a tie!", null);
            }
            else
            {
                await ShowLine(gameResult.WinInfo);
                await Task.Delay(1000);
                await TransitionToEndScreen($"Winner:", imageSources[gameResult.Winner]);
            }
        }
        private async void OnGameRestarted()
        {
            StartScreen.Visibility = Visibility.Collapsed;
            GameContent.Visibility = Visibility.Visible;
            
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    imageControls[r, c].BeginAnimation(Image.SourceProperty, null);
                    imageControls[r, c].Source = null;
                }
            }


            PlayerImage.Source = imageSources[gameState.CurrentPlayer];
            Line.Visibility = Visibility.Hidden;


            GameCanvas.InvalidateVisual();
            GameGrid.InvalidateVisual();

            await TransitionToGameScreen();

            if (gameState.CurrentPlayer == Player.O)
            {
                _ = Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(500);
                    // Double-check before making the move
                    if (!gameState.GameOver && gameState.CurrentPlayer == Player.O)
                        gameState.Bot.MakeMove();
                });
            }
        }
        private void GameGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Only allow player to move if it's their turn and the game is not over
            if (gameState.GameOver || gameState.CurrentPlayer != Player.X)
                return;

            double squareSize = GameGrid.ActualWidth / 3;
            Point clickPosition = e.GetPosition(GameGrid);
            int row = (int)(clickPosition.Y / squareSize);
            int col = (int)(clickPosition.X / squareSize);

            try
            {
                gameState.MakeMove(row, col);
            }
            catch (InvalidOperationException ex)
            {
                // Visual feedback - flash red while keeping X/O visible
                var imageControl = imageControls[row, col];

                // Create a red overlay that won't affect the X/O image
                var overlay = new Rectangle
                {
                    Fill = Brushes.Red,
                    Opacity = 0.5,  // Semi-transparent
                    Width = GameGrid.ActualWidth / 3,
                    Height = GameGrid.ActualHeight / 3
                };

                // Add overlay to canvas in same position
                GameCanvas.Children.Add(overlay);
                Canvas.SetLeft(overlay, col * (GameGrid.ActualWidth / 3));
                Canvas.SetTop(overlay, row * (GameGrid.ActualHeight / 3));

                // Animate the overlay fading out
                var animation = new DoubleAnimation
                {
                    From = 0.5,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.3)
                };

                animation.Completed += (s, _) => GameCanvas.Children.Remove(overlay);
                overlay.BeginAnimation(OpacityProperty, animation);
            }
        }
        private void UpdateScoreboard()
        {
            PlayerWinsText.Text = gameState.PlayerWins.ToString();
            BotWinsText.Text = gameState.BotWins.ToString();
            TieText.Text = gameState.Ties.ToString();
        }
        private void ResetScoresButton_Click(object sender, RoutedEventArgs e)
        {
            gameState.ResetScores();
            UpdateScoreboard();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (gameState.GameOver)
            {
                gameState.Reset();
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Animate the start screen fading out
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            StartScreen.BeginAnimation(OpacityProperty, fadeOut);
            await Task.Delay(fadeOut.Duration.TimeSpan);

            StartScreen.Visibility = Visibility.Collapsed;
            GameContent.Visibility = Visibility.Visible;

            // Fade in game content
            GameContent.Opacity = 0;
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(0.5),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            GameContent.BeginAnimation(OpacityProperty, fadeIn);

            // Initialize game state
            UpdateScoreboard();
            GameGrid.IsEnabled = true;
            GameCanvas.IsHitTestVisible = true;
            EndScreeen.Visibility = Visibility.Collapsed;

            PlayerImage.Source = imageSources[gameState.CurrentPlayer];

            if (gameState.CurrentPlayer == Player.O)
            {
                await Task.Delay(500);
                // Double-check before making the move
                if (!gameState.GameOver && gameState.CurrentPlayer == Player.O)
                    gameState.Bot.MakeMove();
            }

        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape && GameContent.Visibility == Visibility.Visible)
            {
                TogglePauseMenu();
            }
            base.OnKeyDown(e);
        }

        private void TogglePauseMenu()
        {
            if (PauseMenu.Visibility == Visibility.Visible)
            {
                PauseMenu.Visibility = Visibility.Collapsed;
                GameGrid.IsEnabled = true;
            }
            else
            {
                PauseMenu.Visibility = Visibility.Visible;
                GameGrid.IsEnabled = false;
            }
        }

        private void ResumeButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePauseMenu();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MusicToggle_Click(object sender, RoutedEventArgs e)
        {
            if (App.BackgroundMusic != null)
            {
                if (MusicToggle.IsChecked == true)
                {
                    App.BackgroundMusic.Play();
                    MusicToggle.Content = "🔊";
                }
                else
                {
                    App.BackgroundMusic.Pause();
                    MusicToggle.Content = "🔇";
                }
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (App.BackgroundMusic != null)
            {
                App.BackgroundMusic.Volume = e.NewValue;
            }
        }

    }
}