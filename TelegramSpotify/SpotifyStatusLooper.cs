using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Account;
using TLSharp.Core;

namespace TelegramSpotify
{
    class SpotifyStatusLooper
    {
        private TelegramClient telegram;
        private SpotifyClient spotify;
        private string lastSetName = "";

        public SpotifyStatusLooper(TelegramClient telegram, SpotifyClient spotify)
        {
            this.telegram = telegram;
            this.spotify = spotify;
        }

        public async Task Loop()
        {
            while(true)
            {
                var npReq = new PlayerCurrentlyPlayingRequest();
                var nowPlaying = await spotify.Player.GetCurrentlyPlaying(npReq);

                var tgReq = new TLRequestUpdateProfile
                {
                    FirstName = Environment.GetEnvironmentVariable("FIRST_NAME"),
                    LastName = ""
                };

                if (nowPlaying.IsPlaying && nowPlaying.Item.GetType() == typeof(FullTrack))
                {
                    var item = (FullTrack)nowPlaying.Item;

                    var fullTrackName = $"{item.Artists[0].Name} — {item.Name}".Truncate(61);
                    tgReq.LastName = $"🎵 {fullTrackName}";
                }

                if(tgReq.LastName != lastSetName)
                {
                    Console.WriteLine("new name!");
                    await telegram.SendRequestAsync<TLUser>(tgReq);
                }

                lastSetName = tgReq.LastName;

                Thread.Sleep(10000);
            }
        }
    }
}
