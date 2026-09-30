using System.Collections.Generic;
using System.Threading.Tasks;
using HorizonFaser.Models;

namespace HorizonFaser.Repositories
{
    public interface IFiberRepository
    {
        Task<UserInfo> LoginAsync(string usernameOrEmail, string password);
        Task<List<QuarkbauProject>> FetchProjectsAsync();
        Task<List<QuarkBauSegment>> FetchProjectSegmentsByIdAsync(long projectId);
        Task<List<QuarkBauSegment>> FetchNearbySegmentsAsync(double centerLat, double centerLng, double radiusMeters);
        Task<List<QuarkBauSegment>> FetchProjectSegmentsAsync();
        Task<GeometryPoint> FetchProjectLocationAsync(long projectId);
        Task<List<Cluster>> FetchClusterByProjectIdAsync(long projectId);
        Task<Pop> FetchPopByIdAsync(long popId);
        Task<Netzverteiler> FetchNetzverteilerByIdAsync(long nvtId);
        Task<List<Netzverteiler>> FetchProjectNetzverteilersAsync(long projectId);
        Task<List<Pop>> FetchProjectPopsAsync(long projectId);
        Task<List<Pop>> FetchPopsByClusterIdAsync(long clusterId);
        Task<bool> UpdateSegmentStateAsync(long segmentId, WorkflowState newState);
        Task<bool> ReportIncidentAsync(IncidentReport incident);
    }
}
