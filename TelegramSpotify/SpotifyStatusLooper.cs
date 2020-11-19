using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TeleSharp.TL;
using TeleSharp.TL.Account;
using TeleSharp.TL.Photos;
using TLSharp.Core;
using TLSharp.Core.Utils;

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
                    FirstName = "",
                    LastName = ""
                };

                if (nowPlaying.IsPlaying && nowPlaying.Item.GetType() == typeof(FullTrack))
                {
                    var item = (FullTrack)nowPlaying.Item;

                    var fullTrackName = $"{item.Artists[0].Name} — {item.Name}".Truncate(61);
                    tgReq.FirstName = $"🎵 {fullTrackName}";

                    if (tgReq.FirstName != lastSetName)
                    {
                        Console.WriteLine(item.Album.Images[0].Url);

                        var client = new WebClient();
                        var temp = Path.GetTempFileName();
                        client.DownloadFile(item.Album.Images[0].Url, temp);

                        var file = (TLInputFile)await telegram.UploadFile("album_art.jpg", new StreamReader(temp));

                        var pfpReq = new TLRequestUploadProfilePhoto
                        {
                            File = file
                        };

                        await telegram.SendRequestAsync<TeleSharp.TL.Photos.TLPhoto>(pfpReq);
                    }
                }

                if(tgReq.FirstName != lastSetName)
                {
                    Console.WriteLine("new name!");
                    await telegram.SendRequestAsync<TLUser>(tgReq);
                }

                lastSetName = tgReq.FirstName;

                Thread.Sleep(10000);
            }
        }
    }
}
