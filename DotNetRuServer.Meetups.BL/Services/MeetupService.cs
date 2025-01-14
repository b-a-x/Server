using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DotNetRuServer.Meetups.BL.Entities;
using DotNetRuServer.Meetups.BL.Extensions;
using DotNetRuServer.Meetups.BL.Interfaces;
using DotNetRuServer.Meetups.BL.Models;

namespace DotNetRuServer.Meetups.BL.Services
{
    public class MeetupService : IMeetupService
    {
        private readonly ICommunityProvider _communityProvider;
        private readonly IFriendProvider _friendProvider;
        private readonly IMeetupProvider _meetupProvider;
        private readonly ITalkProvider _talkProvider;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVenueProvider _venueProvider;

        public MeetupService(IMeetupProvider meetupProvider, IVenueProvider venueProvider, IFriendProvider friendProvider, 
            ICommunityProvider communityProvider, ITalkProvider talkProvider, IUnitOfWork unitOfWork)
        {
            _meetupProvider = meetupProvider;
            _venueProvider = venueProvider;
            _friendProvider = friendProvider;
            _communityProvider = communityProvider;
            _talkProvider = talkProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<AutocompleteRow>> GetAllMeetupsAsync()
        {
            var meetups = await _meetupProvider.GetAllMeetupsAsync().ConfigureAwait(false);
            return meetups
                .Select(x => new AutocompleteRow {Id = x.ExportId, Name = x.Name})
                .ToList();
        }

        public async Task<MeetupVm> GetMeetupAsync(string meetupId)
        {
            var meetup = await _meetupProvider.GetMeetupOrDefaultExtendedAsync(meetupId).ConfigureAwait(false);
            return meetup?.ToVm();
        }

        public async Task<MeetupVm> AddMeetupAsync(MeetupVm meetup)
        {
            meetup.EnsureIsValid();

            var original = await _meetupProvider.GetMeetupOrDefaultExtendedAsync(meetup.Id).ConfigureAwait(false);
            if (original != null) throw new FormatException($"Данный {nameof(meetup.Id)} \"{meetup.Id}\" уже занят");

            var venue = await _venueProvider.GetVenueOrDefaultAsync(meetup.VenueId);
            var community =
                await _communityProvider.GetCommunityOrDefaultAsync(
                    Enum.GetName(typeof(Communities), meetup.CommunityId)
                );

            var entity = new Meetup
            {
                ExportId = meetup.Id,
                Venue = venue,
                Friends = new List<FriendAtMeetup>(),
                Sessions = new List<Session>(),
                Community = community,
                Name = meetup.Name
            };
            foreach (var meetupFriendId in meetup.FriendIds)
            {
                var friend = await _friendProvider.GetFriendOrDefaultAsync(meetupFriendId.FriendId);
                entity.Friends.Add(new FriendAtMeetup
                {
                    Friend = friend,
                    Meetup = entity
                });
            }

            foreach (var meetupSession in meetup.Sessions)
            {
                var talk = await _talkProvider.GetTalkOrDefaultAsync(meetupSession.TalkId);
                entity.Sessions.Add(new Session
                {
                    Talk = talk,
                    Meetup = entity,
                    StartTime = DateTime.Parse(
                        meetupSession.StartTime,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal),
                    EndTime = DateTime.Parse(
                        meetupSession.EndTime,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal)
                });
            }


            var res = await _meetupProvider.SaveMeetupAsync(entity).ConfigureAwait(false);
            return res.ToVm();
        }

        public async Task<MeetupVm> UpdateMeetupAsync(MeetupVm meetup)
        {
            meetup.EnsureIsValid();
            var original = await _meetupProvider.GetMeetupOrDefaultExtendedAsync(meetup.Id).ConfigureAwait(false);
            var venue = await _venueProvider.GetVenueOrDefaultAsync(meetup.VenueId);
            var community = await _communityProvider.GetCommunityOrDefaultAsync(
                Enum.GetName(typeof(Communities), meetup.CommunityId)
            );

            original.Venue = venue;
            original.Community = community;
            original.Name = meetup.Name;
            original.Friends = new List<FriendAtMeetup>();
            original.Sessions = new List<Session>();
            foreach (var meetupFriendId in meetup.FriendIds)
            {
                var friend = await _friendProvider.GetFriendOrDefaultAsync(meetupFriendId.FriendId);
                original.Friends.Add(new FriendAtMeetup
                {
                    Friend = friend,
                    Meetup = original
                });
            }

            foreach (var meetupSession in meetup.Sessions)
            {
                var talk = await _talkProvider.GetTalkOrDefaultAsync(meetupSession.TalkId);
                original.Sessions.Add(new Session
                {
                    Talk = talk,
                    Meetup = original,
                    StartTime = DateTime.Parse(
                        meetupSession.StartTime,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal),
                    EndTime = DateTime.Parse(
                        meetupSession.EndTime,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal)
                });
            }


            return original.ToVm();
        }
    }
}