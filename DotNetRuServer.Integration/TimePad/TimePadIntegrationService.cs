using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DotNetRuServer.Meetups.BL.Entities;
using Microsoft.Extensions.Configuration;
using RazorLight;

namespace DotNetRuServer.Integration.TimePad
{
    public class TimePadIntegrationService
    {
        private const string TimePadSection = "TimePad";
        private const string CommunitySection = "Community";
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly RazorLightEngine _razorEngine;

        public TimePadIntegrationService(IHttpClientFactory factory, IConfiguration configuration)
        {
            _clientFactory = factory;
            _configuration = configuration;
            var rootDir =   Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
            var appRoot = rootDir.Substring(5);
            _razorEngine = new RazorLightEngineBuilder()
                .UseFilesystemProject(appRoot)
                .UseMemoryCachingProvider()
                .Build();
        }

        public async Task CreateDraftEventAsync(Meetup meetup, CancellationToken ct)
        {
            var communityOrganization = await GetOrganizationAsync(meetup.Community, ct);

            var timePadClient = new TimePadClient(_clientFactory.CreateClient($"TimePad-{meetup.CommunityId}"));

            var startsAt = meetup.Sessions.Min(s => s.StartTime);
            var endsAt = meetup.Sessions.Max(s => s.EndTime);

            var dateDescription = startsAt.ToString("dd MMMM", CultureInfo.GetCultureInfo("ru-RU"));
            var friendsDescription = CreateFriendsDescription(meetup.Friends);
            var shortDescription = $"{dateDescription} {friendsDescription} состоится встреча {meetup.Community.Name}";
            var htmlDescription = await _razorEngine.CompileRenderAsync($"TimePad/Templates/Template-community-{meetup.CommunityId}.cshtml",
                new
                {
                    FirstStart = meetup.Sessions[0].StartTime.ToString("HH:mm"),
                    FirstEnd = meetup.Sessions[0].EndTime.ToString("HH:mm"),
                    FirstSpeaker = string.Join(", ", meetup.Sessions[0].Talk.Speakers.Select(s => s.Speaker.Name)),
                    FirstTalkTitle = meetup.Sessions[0].Talk.Title,
                    SecondStart = meetup.Sessions[1].StartTime.ToString("HH:mm"),
                    SecondEnd = meetup.Sessions[1].EndTime.ToString("HH:mm"),
                    SecondSpeaker = string.Join(", ", meetup.Sessions[1].Talk.Speakers.Select(s => s.Speaker.Name)),
                    SecondTalkTitle = meetup.Sessions[1].Talk.Title,
                });
            var ticketType = new TicketTypeRequest
            {
                Name = "Входной билет",
                Price = 0,
                Send_personal_links = true
            };
            var questions = new List<QuestionInclude>
            {
                new QuestionInclude {Field_id = "mail", Is_mandatory = true, Name = "E-mail"},
                new QuestionInclude {Field_id = "surname", Is_mandatory = true, Name = "Фамилия"},
                new QuestionInclude {Field_id = "name", Is_mandatory = true, Name = "Имя"},
                new QuestionInclude {Name = "Компания"},
                new QuestionInclude {Name = "Должность"}
            };

            var createEventBody = new CreateEvent
            {
                Organization = new OrganizationInclude {Id = communityOrganization.Id, Subdomain = communityOrganization.Subdomain},
                Starts_at = startsAt,
                Ends_at = endsAt,
                Name = meetup.Name,
                Description_short = shortDescription,
                Description_html = htmlDescription,
                Location = new LocationInclude {City = meetup.Community.City, Address = meetup.Venue.Address},
                Categories = new[] {new CategoryInclude {Id = 452, Name = "ИТ и интернет"}},
                Access_status = "private",
                Tickets_limit = 150,
                Ticket_types = new [] {ticketType},
                Questions = questions,
                Age_limit = "0"
            };

            await timePadClient.AddEventAsync(createEventBody, ct);

            string CreateFriendsDescription(List<FriendAtMeetup> friends)
            {
                if (friends is null || friends.Count == 0)
                    return string.Empty;

                var description = "в гостях у " +
                                 (friends.Count == 1 ? "компании " : "компаний ") +
                                 string.Join(", ", friends.Select(f => f.Friend.Name));
                return description;
            }
        }

        public async Task PublishEventAsync(Meetup meetup, CancellationToken ct)
        {
            var communityOrganization = await GetOrganizationAsync(meetup.Community, ct);

            var timePadClient = new TimePadClient(_clientFactory.CreateClient($"TimePad-{meetup.Community.Id}"));

            var events = await timePadClient.GetEventsAsync(
                null,
                10,
                0,
                new[] {"+starts_at"},
                null,
                null,
                null,
                null,
                new[] {communityOrganization.Id},
                null,
                null,
                null,
                null,
                null,
                new[] {"private"},
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                ct
            );
            var meetupEvent = events.Values.FirstOrDefault(e => e.Name == meetup.Name);
            if (meetupEvent is null)
                throw new Exception("Can't find meetup event");

            var editEventBody = new EditEvent {Access_status = "public"};
            await timePadClient.EditEventAsync(meetupEvent.Id, editEventBody, ct);
        }

        private async Task<OrganizationResponse> GetOrganizationAsync(Community community, CancellationToken ct)
        {
            var timePadSection = _configuration.GetSection($"{CommunitySection}-{community.Id}")
                .GetSection(TimePadSection);
            if (timePadSection is null)
                throw new Exception("Can't find TimePad section for community");

            var timePadClient = new TimePadClient(_clientFactory.CreateClient($"TimePad-{community.Id}"));
            var introspect = await timePadClient.IntrospectTokenAsync(timePadSection.Value, ct);

            var communityOrganization = introspect.Organizations?.SingleOrDefault(o => o.Name.Contains(community.Name));
            if (communityOrganization is null)
                throw new Exception("Can't find community TimePad organization");
                
            return communityOrganization;
        }
    }
}