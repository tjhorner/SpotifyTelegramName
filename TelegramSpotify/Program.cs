using SpotifyAPI.Web;
using System;
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Account;
using TLSharp.Core;
using TLSharp.Core.Exceptions;

namespace TelegramSpotify
{
    class Program
    {
        private static TelegramClient telegram;

        static void Main(string[] args)
        {
            DotNetEnv.Env.Load();

            telegram = new TelegramClient(GetEnvInt("TELEGRAM_API_ID"), Environment.GetEnvironmentVariable("TELEGRAM_API_HASH"));
            telegram.ConnectAsync().Wait();

            if (telegram.IsUserAuthorized())
                Connect();
            else
                LogIn().Wait();
        }

        static int GetEnvInt(string ev)
        {
            return int.Parse(Environment.GetEnvironmentVariable(ev));
        }

        static async Task LogIn()
        {
            Console.WriteLine("Please enter your phone number in international format:");
            var phoneNumber = Console.ReadLine();

            var hash = await telegram.SendCodeRequestAsync(phoneNumber);

            Console.WriteLine("You should have just received a login code. Enter it here:");
            var code = Console.ReadLine();

            TLUser user = null;

            try
            {
                user = await telegram.MakeAuthAsync(phoneNumber, hash, code);
            }
            catch (CloudPasswordNeededException ex)
            {
                var passwordSetting = await telegram.GetPasswordSetting();

                Console.WriteLine("Your account is protected with 2SV. Enter your cloud password:");
                var password = Console.ReadLine();

                user = await telegram.MakeAuthWithPasswordAsync(passwordSetting, password);
            }

            Console.WriteLine("All logged in! You won't have to do this again.");

            Connect();
        }

        static void Connect()
        {
            var response = new AuthorizationCodeTokenResponse
            {
                AccessToken = Environment.GetEnvironmentVariable("SPOTIFY_ACCESS_TOKEN"),
                TokenType = "Bearer",
                ExpiresIn = 0,
                RefreshToken = Environment.GetEnvironmentVariable("SPOTIFY_REFRESH_TOKEN"),
                Scope = "playlist-read-private playlist-read-collaborative user-follow-modify user-library-read user-library-modify user-follow-read playlist-modify-private playlist-modify-public user-read-birthdate user-read-email user-read-private"
            };

            var config = SpotifyClientConfig
              .CreateDefault()
              .WithAuthenticator(new AuthorizationCodeAuthenticator(
                  Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_ID"),
                  Environment.GetEnvironmentVariable("SPOTIFY_CLIENT_SECRET"),
                  response
                ));

            var spotify = new SpotifyClient(config);

            var looper = new SpotifyStatusLooper(telegram, spotify);
            looper.Loop().Wait();
        }
    }
}
