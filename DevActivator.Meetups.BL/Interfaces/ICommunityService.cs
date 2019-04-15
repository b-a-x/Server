using System.Collections.Generic;
using System.Threading.Tasks;
using DevActivator.Meetups.BL.Models;

namespace DevActivator.Meetups.BL.Interfaces
{
    public interface ICommunityService
    {
        Task<List<AutocompleteRow>> GetAllCommunitiesAsync();

        Task<CommunityVm> GetCommunityAsync(string communityId);

        Task<CommunityVm> AddCommunityAsync(CommunityVm community);

        Task<CommunityVm> UpdateCommunityAsync(CommunityVm community);
    }
}