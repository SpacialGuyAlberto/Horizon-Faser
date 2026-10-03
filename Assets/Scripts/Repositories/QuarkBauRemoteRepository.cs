using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HorizonFaser.Models;

// Requires Newtonsoft.Json. If not installed, you can install it via Unity Package Manager 
// by adding 'com.unity.nuget.newtonsoft-json'
using Newtonsoft.Json;

namespace HorizonFaser.Repositories
{
    public class QuarkBauRemoteRepository : IFiberRepository
    {
        private readonly HttpClient _httpClient;
        public string ServerBaseUrl { get; private set; }
        public string ApiToken { get; private set; }

        private class LoggingHandler : DelegatingHandler
        {
            public LoggingHandler(HttpMessageHandler innerHandler)
                : base(innerHandler)
            {
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                Debug.Log($"[QuarkBauRemoteRepository] Calling URL: {request.Method} {request.RequestUri}");
                return await base.SendAsync(request, cancellationToken);
            }
        }

        public QuarkBauRemoteRepository()
        {
            var handler = new LoggingHandler(new HttpClientHandler());
            _httpClient = new HttpClient(handler);
            // Reducimos el timeout a 5 segundos para evitar que la UI se quede congelada (traba) 
            // si el servidor local no responde o no hay conexión.
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public void Configure(string serverBaseUrl, string apiToken)
        {
            ServerBaseUrl = serverBaseUrl.TrimEnd('/');
            ApiToken = apiToken;
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<UserInfo> LoginAsync(string usernameOrEmail, string password)
        {
            // Login Mocked - as requested by the user. 
            // We bypass the real /api/auth/login to avoid 403 / HTML parsing errors.
            Debug.Log("[QuarkBauRemoteRepository] Mocking login without calling the backend.");
            
            var mockUser = new UserInfo
            {
                Email = usernameOrEmail,
                FirstName = "Operario",
                LastName = "Demo",
                Token = "demo_jwt_token_horizon_faser"
            };
            
            ApiToken = mockUser.Token;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            // Optional delay to simulate network
            await Task.Delay(500); 

            return mockUser;
        }

        public async Task<List<QuarkbauProject>> FetchProjectsAsync()
        {
            var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/projects");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                Debug.LogError($"[QuarkBauRemoteRepository] FetchProjects failed! Status: {response.StatusCode}. Response: {errorText}");
                response.EnsureSuccessStatusCode();
            }

            var json = await response.Content.ReadAsStringAsync();
            Debug.Log($"[QuarkBauRemoteRepository] FetchProjects raw JSON: {json}");
            
            if (json.TrimStart().StartsWith("<"))
            {
                throw new System.Exception($"El servidor devolvió una página HTML en lugar de datos JSON. Esto suele ocurrir si la IP es incorrecta (ej. apuntando al dashboard de Angular en el puerto 4200) o si hay un portal cautivo. URL llamada: {ServerBaseUrl}");
            }

            return JsonConvert.DeserializeObject<List<QuarkbauProject>>(json);
        }

        public async Task<List<QuarkBauSegment>> FetchProjectSegmentsByIdAsync(long projectId)
        {
            var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/projects/{projectId}/segments");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<QuarkBauSegment>>(json);
        }

        public async Task<List<QuarkBauSegment>> FetchNearbySegmentsAsync(double centerLat, double centerLng, double radiusMeters)
        {
            var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/segments/nearest?lat={centerLat}&lng={centerLng}&radius={radiusMeters}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<QuarkBauSegment>>(json);
        }

        public async Task<List<QuarkBauSegment>> FetchProjectSegmentsAsync()
        {
            // Assuming this fetches all segments for the currently active project
            var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/segments");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<QuarkBauSegment>>(json);
        }

        public async Task<GeometryPoint> FetchProjectLocationAsync(long projectId)
        {
            var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/projects/{projectId}/location");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GeometryPoint>(json);
        }

        public async Task<List<Cluster>> FetchClusterByProjectIdAsync(long projectId)
        {
            var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/clusters/project/{projectId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Cluster>>(json);
        }

        public async Task<Pop> FetchPopByIdAsync(long popId)
        {
            var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/pops/{popId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Pop>(json);
        }

        public async Task<Netzverteiler> FetchNetzverteilerByIdAsync(long nvtId)
        {
            var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/netzverteilers/{nvtId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Netzverteiler>(json);
        }

        public async Task<List<Netzverteiler>> FetchProjectNetzverteilersAsync(long projectId)
        {
            var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/projects/{projectId}/netzverteilers");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Netzverteiler>>(json);
        }

        public async Task<List<Pop>> FetchProjectPopsAsync(long projectId)
        {
            var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/projects/{projectId}/pops");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Pop>>(json);
        }

        public async Task<List<Pop>> FetchPopsByClusterIdAsync(long clusterId)
        {
            var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/pops/by-cluster/{clusterId}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Pop>>(json);
        }

        public async Task<bool> UpdateSegmentStateAsync(long segmentId, WorkflowState newState)
        {
            var payload = new { state = newState.ToString() };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync($"{ServerBaseUrl}/api/segments/{segmentId}/state", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ReportIncidentAsync(IncidentReport incident)
        {
            var content = new StringContent(JsonConvert.SerializeObject(incident), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{ServerBaseUrl}/api/incidents", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<UtilityLine>> FetchExternalUtilitiesAsync()
        {
            var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/v1/utilities");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            
            // Assuming UtilityLine is abstract, we might need a custom JsonConverter if polymorphic, 
            // but for now we'll deserialize as a list of WaterPipe or fallback to a custom JSON handling if NewtonSoft handles it.
            // Newtonsoft JSON handles it automatically if TypeNameHandling is configured on the server,
            // otherwise we'll deserialize to WaterPipe as fallback since the prompt implies rendering with water texture anyway.
            return JsonConvert.DeserializeObject<List<WaterPipe>>(json).ConvertAll(x => (UtilityLine)x);
        }
    }
}
