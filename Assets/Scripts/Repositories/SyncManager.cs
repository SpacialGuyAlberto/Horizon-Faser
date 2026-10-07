using System;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using HorizonFaser.Models;

namespace HorizonFaser.Repositories
{
    public class SyncManager : MonoBehaviour
    {
        private LocalDatabaseManager _localDb;
        private QuarkBauRemoteRepository _remoteRepo;
        
        [SerializeField]
        private float syncIntervalSeconds = 60f;
        
        private float _timer;

        private void Start()
        {
            _localDb = new LocalDatabaseManager();
            _remoteRepo = new QuarkBauRemoteRepository(); 
            // NOTA: Idealmente _remoteRepo vendría por Inyección de Dependencias
            
            _timer = syncIntervalSeconds;
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                _timer = syncIntervalSeconds;
                _ = ProcessSyncQueueAsync();
            }
        }

        public async Task EnqueueSegmentUpdateAsync(long segmentId, WorkflowState newState)
        {
            var payload = JsonConvert.SerializeObject(new { SegmentId = segmentId, State = newState });
            await _localDb.EnqueueSyncActionAsync("UpdateSegmentState", payload);
        }

        public async Task EnqueueIncidentReportAsync(IncidentReport incident)
        {
            var payload = JsonConvert.SerializeObject(incident);
            await _localDb.EnqueueSyncActionAsync("ReportIncident", payload);
        }

        private async Task ProcessSyncQueueAsync()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                return; // No hay internet, no intentar sincronizar
            }

            try
            {
                var pendingActions = await _localDb.GetPendingSyncActionsAsync();
                if (pendingActions.Count == 0) return;

                Debug.Log($"[SyncManager] Encontradas {pendingActions.Count} acciones pendientes. Sincronizando...");

                foreach (var action in pendingActions)
                {
                    bool success = false;

                    if (action.ActionType == "UpdateSegmentState")
                    {
                        var data = JsonConvert.DeserializeAnonymousType(action.PayloadJson, new { SegmentId = 0L, State = WorkflowState.Plan });
                        success = await _remoteRepo.UpdateSegmentStateAsync(data.SegmentId, data.State);
                    }
                    else if (action.ActionType == "ReportIncident")
                    {
                        var incident = JsonConvert.DeserializeObject<IncidentReport>(action.PayloadJson);
                        success = await _remoteRepo.ReportIncidentAsync(incident);
                    }

                    if (success)
                    {
                        await _localDb.MarkSyncActionProcessedAsync(action.Id);
                        Debug.Log($"[SyncManager] Acción {action.ActionType} sincronizada con éxito.");
                    }
                    else
                    {
                        Debug.LogWarning($"[SyncManager] Fallo al sincronizar acción {action.ActionType}.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SyncManager] Error procesando la cola de sincronización: {ex.Message}");
            }
        }
    }
}
