using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Client.WinForms.Models;

namespace Client.WinForms.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        public ApiClient(string? baseUrl = null)
        {
            var url = baseUrl
                      ?? Environment.GetEnvironmentVariable("CONNECT4_API")
                      ?? "http://localhost:5221";

            _http = new HttpClient { BaseAddress = new Uri(url) };
        }

        public Task<CreateGameResponse?> CreateGameAsync(int playerId)
        {
            var req = new CreateGameRequest { PlayerId = playerId };
            return _http.PostAsJsonAsync("api/games", req)
                        .Result.Content.ReadFromJsonAsync<CreateGameResponse>();
        }

        public Task<MoveResponse?> SendMoveAsync(int gameId, int column)
        {
            var req = new MoveRequest { GameId = gameId, Column = column };
            return _http.PostAsJsonAsync("api/moves", req)
                        .Result.Content.ReadFromJsonAsync<MoveResponse>();
        }
    }
}
