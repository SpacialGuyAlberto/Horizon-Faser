using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HorizonFaser.Models;
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
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public void Configure(string serverBaseUrl, string apiToken)
        {
            if (string.IsNullOrEmpty(serverBaseUrl) || serverBaseUrl.Contains("fedora.fritz.box") || serverBaseUrl.Contains("tu-servidor.com") || serverBaseUrl.Contains("example.com"))
            {
                serverBaseUrl = HorizonFaser.Network.NetworkConfig.BaseUrl;
            }
            ServerBaseUrl = serverBaseUrl.TrimEnd('/');
            ApiToken = apiToken;
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        // --- MÉTODOS DE CACHÉ OFFLINE ---
        private void SaveCache(string key, string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json.TrimStart().StartsWith("<"))
            {
                Debug.LogWarning($"[Offline Cache] Ignorando guardado de datos inválidos (HTML o vacío) para {key}");
                return;
            }
            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, key + ".json");
                System.IO.File.WriteAllText(path, json);
                Debug.Log($"[Offline Cache] Saved {key}.json");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Offline Cache] Error saving {key}: {ex.Message}");
            }
        }

        private string LoadCache(string key)
        {
            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, key + ".json");
                if (System.IO.File.Exists(path))
                {
                    string content = System.IO.File.ReadAllText(path);
                    if (string.IsNullOrWhiteSpace(content) || content.TrimStart().StartsWith("<"))
                    {
                        Debug.LogWarning($"[Offline Cache] Cache corrupto detectado en {key}, eliminando archivo.");
                        try { System.IO.File.Delete(path); } catch {}
                        return null;
                    }
                    Debug.Log($"[Offline Cache] Loaded {key}.json");
                    return content;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Offline Cache] Error loading {key}: {ex.Message}");
            }
            return null;
        }

        public async Task<UserInfo> LoginAsync(string usernameOrEmail, string password)
        {
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
            await Task.Delay(500); 
            return mockUser;
        }

        public async Task<List<QuarkbauProject>> FetchProjectsAsync()
        {
            string cacheKey = "projects_list";
            try
            {
                var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/projects");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                
                if (json.TrimStart().StartsWith("<"))
                    throw new System.Exception("HTML returned instead of JSON");

                SaveCache(cacheKey, json);
                return JsonConvert.DeserializeObject<List<QuarkbauProject>>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[QuarkBauRemoteRepository] FetchProjects failed, attempting offline cache. Error: {ex.Message}");
                var cachedJson = LoadCache(cacheKey);
                if (!string.IsNullOrEmpty(cachedJson))
                    return JsonConvert.DeserializeObject<List<QuarkbauProject>>(cachedJson);
                throw;
            }
        }

        public async Task<List<QuarkBauSegment>> FetchProjectSegmentsByIdAsync(long projectId)
        {
            string cacheKey = $"segments_project_{projectId}";
            try
            {
                var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/projects/{projectId}/segments");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(json) || json.TrimStart().StartsWith("<"))
                {
                    throw new System.Exception($"Respuesta del servidor no es JSON válido (posible HTML/error): {json?.Substring(0, Math.Min(json?.Length ?? 0, 80))}");
                }

                var segments = JsonConvert.DeserializeObject<List<QuarkBauSegment>>(json);
                if (segments != null)
                {
                    SaveCache(cacheKey, json);
                }
                return segments ?? new List<QuarkBauSegment>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[QuarkBauRemoteRepository] FetchProjectSegmentsById failed, attempting offline cache. Error: {ex.Message}");
                var cachedJson = LoadCache(cacheKey);
                if (!string.IsNullOrEmpty(cachedJson))
                {
                    try
                    {
                        var cachedSegments = JsonConvert.DeserializeObject<List<QuarkBauSegment>>(cachedJson);
                        if (cachedSegments != null) return cachedSegments;
                    }
                    catch (Exception cacheEx)
                    {
                        Debug.LogWarning($"[QuarkBauRemoteRepository] Error deserializando cache de segmentos: {cacheEx.Message}");
                    }
                }
                throw;
            }
        }

        public async Task<List<QuarkBauSegment>> FetchNearbySegmentsAsync(double centerLat, double centerLng, double radiusMeters)
        {
            // Nearby no lo cacheamos por clave específica o podríamos hacerlo usando lat/lng aproximadas, por ahora no.
            var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/segments/nearest?lat={centerLat}&lng={centerLng}&radius={radiusMeters}");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<QuarkBauSegment>>(json);
        }

        public async Task<List<QuarkBauSegment>> FetchProjectSegmentsAsync()
        {
            string cacheKey = "segments_all";
            try 
            {
                var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/segments");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                SaveCache(cacheKey, json);
                return JsonConvert.DeserializeObject<List<QuarkBauSegment>>(json);
            }
            catch (Exception ex)
            {
                var cachedJson = LoadCache(cacheKey);
                if (!string.IsNullOrEmpty(cachedJson)) return JsonConvert.DeserializeObject<List<QuarkBauSegment>>(cachedJson);
                throw;
            }
        }

        public async Task<GeometryPoint> FetchProjectLocationAsync(long projectId)
        {
            string cacheKey = $"location_project_{projectId}";
            try
            {
                var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/projects/{projectId}/location");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                SaveCache(cacheKey, json);
                return JsonConvert.DeserializeObject<GeometryPoint>(json);
            }
            catch (Exception ex)
            {
                var cachedJson = LoadCache(cacheKey);
                if (!string.IsNullOrEmpty(cachedJson)) return JsonConvert.DeserializeObject<GeometryPoint>(cachedJson);
                throw;
            }
        }

        public async Task<List<Cluster>> FetchClusterByProjectIdAsync(long projectId)
        {
            string cacheKey = $"clusters_project_{projectId}";
            try
            {
                var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/clusters/project/{projectId}");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                SaveCache(cacheKey, json);
                return JsonConvert.DeserializeObject<List<Cluster>>(json);
            }
            catch (Exception ex)
            {
                var cachedJson = LoadCache(cacheKey);
                if (!string.IsNullOrEmpty(cachedJson)) return JsonConvert.DeserializeObject<List<Cluster>>(cachedJson);
                throw;
            }
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

        private static readonly JsonSerializerSettings NullTolerantSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public async Task<List<Netzverteiler>> FetchProjectNetzverteilersAsync(long projectId)
        {
            string cacheKey = $"netzverteilers_project_{projectId}";
            try
            {
                var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/projects/{projectId}/netzverteilers");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                SaveCache(cacheKey, json);
                var nvts = JsonConvert.DeserializeObject<List<Netzverteiler>>(json, NullTolerantSettings);
                Debug.Log($"[QuarkBauRemoteRepository] Project {projectId}: {nvts?.Count ?? 0} NVTs received.");
                return nvts;
            }
            catch (Exception ex)
            {
                var cachedJson = LoadCache(cacheKey);
                Debug.LogWarning($"[QuarkBauRemoteRepository] NVT fetch failed for project {projectId}: {ex.Message}");
                if (!string.IsNullOrEmpty(cachedJson)) return JsonConvert.DeserializeObject<List<Netzverteiler>>(cachedJson, NullTolerantSettings);
                throw;
            }
        }

        public async Task<List<Pop>> FetchProjectPopsAsync(long projectId)
        {
            string cacheKey = $"pops_project_{projectId}";
            try
            {
                var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/projects/{projectId}/pops");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                SaveCache(cacheKey, json);
                var pops = JsonConvert.DeserializeObject<List<Pop>>(json, NullTolerantSettings);
                Debug.Log($"[QuarkBauRemoteRepository] Project {projectId}: {pops?.Count ?? 0} POPs received.");
                return pops;
            }
            catch (Exception ex)
            {
                var cachedJson = LoadCache(cacheKey);
                Debug.LogWarning($"[QuarkBauRemoteRepository] POP fetch failed for project {projectId}: {ex.Message}");
                if (!string.IsNullOrEmpty(cachedJson)) return JsonConvert.DeserializeObject<List<Pop>>(cachedJson, NullTolerantSettings);
                throw;
            }
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
            // Offline queueing could be added here, but for now we require internet
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
            string cacheKey = "utilities_all";
            try
            {
                var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/v1/utilities");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                SaveCache(cacheKey, json);
                
                var jArray = Newtonsoft.Json.Linq.JArray.Parse(json);
                var result = new List<UtilityLine>();
                foreach (var item in jArray)
                {
                    var type = item["utilityType"]?.ToString();
                    if (type == "ELECTRICITY") result.Add(item.ToObject<ElectricityCable>());
                    else if (type == "GAS") result.Add(item.ToObject<GasPipe>());
                    else result.Add(item.ToObject<WaterPipe>());
                }
                return result;
            }
            catch (Exception ex)
            {
                var cachedJson = LoadCache(cacheKey);
                if (!string.IsNullOrEmpty(cachedJson))
                {
                    var jArray = Newtonsoft.Json.Linq.JArray.Parse(cachedJson);
                    var result = new List<UtilityLine>();
                    foreach (var item in jArray)
                    {
                        var type = item["utilityType"]?.ToString();
                        if (type == "ELECTRICITY") result.Add(item.ToObject<ElectricityCable>());
                        else if (type == "GAS") result.Add(item.ToObject<GasPipe>());
                        else result.Add(item.ToObject<WaterPipe>());
                    }
                    return result;
                }
                throw;
            }
        }
        
        public async Task<List<HorizonFaser.Models.FencingPlan>> FetchFencingPlansAsync(long segmentId)
        {
            string cacheKey = $"fencing_segment_{segmentId}";
            try
            {
                var response = await _httpClient.GetAsync($"{ServerBaseUrl}/api/planning/fencing-plans/segment/{segmentId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    SaveCache(cacheKey, json);
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<List<HorizonFaser.Models.FencingPlan>>(json);
                }
                return new List<HorizonFaser.Models.FencingPlan>();
            }
            catch (Exception ex)
            {
                var cachedJson = LoadCache(cacheKey);
                if (!string.IsNullOrEmpty(cachedJson))
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<List<HorizonFaser.Models.FencingPlan>>(cachedJson);
                
                return new List<HorizonFaser.Models.FencingPlan>();
            }
        }
    }
}
