#pragma warning disable CA1707
#pragma warning disable CA2007
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Caching.Memory;

namespace Jellyfin.Plugin.CollectionImport
{
//     public class Result
//     {
// #pragma warning disable CA1002 // Do not expose generic lists
// #pragma warning disable CA2227 // Collection properties should be read only
//         public required List<Item> Items { get; set; }
// #pragma warning restore CA2227 // Collection properties should be read only
// #pragma warning restore CA1002 // Do not expose generic lists
//     }
    public class MdbItem
    {
        public int? id { get; set; }
        public int? rank { get; set; }
        public int? adult { get; set; }
        public string? title { get; set; }
        public int? tvdbid { get; set; }
        public string? Imdb_id { get; set; }
        public string? mediatype { get; set; }
        public int? release_year { get; set; }
    }

    public class MdbClientManager : IDisposable
    {
        private readonly HttpClient client;

        public MdbClientManager()
        {
            client = new HttpClient();
        }
        public async Task<List<MdbItem>> Request(string url)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return await client.GetFromJsonAsync<List<MdbItem>>(url);
#pragma warning restore CS8603 // Possible null reference return.
            // var response = client.GetAsync("movie/popular");
            // response.EnsureSuccessStatusCode();
            // string responseBody = await response.Content.ReadAsStringAsync();
            // return responseBody;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool v)
        {
            if (v)
            {
                client.Dispose();
            }
        }
    }

    // public class GitHubRepo
    // {
    // }
}