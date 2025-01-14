using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetRuServer.Meetups.BL.Entities;
using DotNetRuServer.Meetups.BL.Interfaces;
using DotNetRuServer.Meetups.DAL.Database;
using Microsoft.EntityFrameworkCore;

namespace DotNetRuServer.Meetups.DAL.Providers
{
    public class TalkProvider : ITalkProvider
    {
        private readonly DotNetRuServerContext _context;

        public TalkProvider(DotNetRuServerContext context)
        {
            _context = context;
        }

        public Task<List<Talk>> GetAllTalksAsync()
            => _context.Talks.ToListAsync();

        public Task<Talk> GetTalkOrDefaultAsync(string talkId)
            => _context.Talks.FirstOrDefaultAsync(x => x.ExportId == talkId);

        public Task<Talk> GetTalkOrDefaultExtendedAsync(string talkId)
            => _context.Talks
                .Include(x => x.Speakers).ThenInclude(x => x.Speaker)
                .Include(x => x.SeeAlsoTalks)
                .FirstOrDefaultAsync(x => x.ExportId == talkId);

        public async Task<Talk> SaveTalkAsync(Talk talk)
        {
            await _context.Talks.AddAsync(talk);
            await _context.SaveChangesAsync();
            return talk;
        }

        public void RemoveSpeaker(Talk talk, int speakerId)
        {
            var speaker = talk.Speakers.First(x => x.SpeakerId == speakerId);
            _context.SpeakerTalks.Remove(speaker);
        }
    }
}