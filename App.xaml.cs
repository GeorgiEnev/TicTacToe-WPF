using System;
using System.Windows;
using System.Windows.Media;

namespace TicTacToe
{
    public partial class App : Application
    {
        public static MediaPlayer BackgroundMusic { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            BackgroundMusic = new MediaPlayer();
            BackgroundMusic.Open(new Uri("Assets/background_music.mp3", UriKind.Relative));
            BackgroundMusic.MediaEnded += (s, ev) =>
            {
                BackgroundMusic.Position = TimeSpan.Zero;
                BackgroundMusic.Play();
            };
            BackgroundMusic.Volume = 0.10;
            BackgroundMusic.Play();
        }
    }
}
