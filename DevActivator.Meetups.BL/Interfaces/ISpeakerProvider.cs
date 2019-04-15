using System.Collections.Generic;
using System.Threading.Tasks;
using DevActivator.Meetups.BL.Entities;

namespace DevActivator.Meetups.BL.Interfaces
{
    public interface ISpeakerProvider
    {
        Task<List<Speaker>> GetAllSpeakersAsync();

        Task<Speaker> GetSpeakerOrDefaultAsync(string speakerId);

        Task<List<Speaker>> GetSpeakersByIdsAsync(List<string> ids);

        Task<Speaker> SaveSpeakerAsync(Speaker speaker);
    }
}