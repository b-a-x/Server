using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevActivator.Common.BL.Config;
using DevActivator.Meetups.BL.Entities;
using DevActivator.Meetups.BL.Extensions;
using DevActivator.Meetups.BL.Interfaces;
using DevActivator.Meetups.BL.Models;

namespace DevActivator.Meetups.BL.Services
{
    public class SpeakerService : ISpeakerService
    {
        private readonly Settings _settings;
        private readonly ISpeakerProvider _speakerProvider;
        private readonly IUnitOfWork _unitOfWork;

        public SpeakerService(Settings settings, ISpeakerProvider speakerProvider, IUnitOfWork unitOfWork)
        {
            _settings = settings;
            _speakerProvider = speakerProvider;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<AutocompleteRow>> GetAllSpeakersAsync()
        {
            var speakers = await _speakerProvider.GetAllSpeakersAsync().ConfigureAwait(false);
            return speakers
                .Select(x => new AutocompleteRow {Id = x.ExportId, Name = x.Name})
                .ToList();
        }

        public async Task<SpeakerVm> GetSpeakerAsync(string speakerId)
        {
            var speaker = await _speakerProvider.GetSpeakerOrDefaultAsync(speakerId).ConfigureAwait(false);
            return speaker.ToVm(speaker.GetLastUpdateDate(_settings));
        }

        public async Task<SpeakerVm> AddSpeakerAsync(SpeakerVm speaker)
        {
            speaker.EnsureIsValid();

            var original = await _speakerProvider.GetSpeakerOrDefaultAsync(speaker.Id).ConfigureAwait(false);
            if (original != null)
            {
                throw new FormatException($"Данный {nameof(speaker.Id)} \"{speaker.Id}\" уже занят");
            }

            var entity = new Speaker {ExportId = speaker.Id}.Extend(speaker);
            var res = await _speakerProvider.SaveSpeakerAsync(entity).ConfigureAwait(false);
            return res.ToVm(entity.GetLastUpdateDate(_settings));
        }

        public async Task<SpeakerVm> UpdateSpeakerAsync(SpeakerVm speaker)
        {
            speaker.EnsureIsValid();
            var original = await _speakerProvider.GetSpeakerOrDefaultAsync(speaker.Id).ConfigureAwait(false);
            original.ExportId = speaker.Id;
            original.Name = speaker.Name;
            original.CompanyName = speaker.CompanyName;
            original.CompanyUrl = speaker.CompanyUrl;
            original.Description = speaker.Description;
            original.BlogUrl = speaker.BlogUrl;
            original.ContactsUrl = speaker.ContactsUrl;
            original.HabrUrl = speaker.HabrUrl;
            original.TwitterUrl = speaker.TwitterUrl;
            original.GitHubUrl = speaker.GitHubUrl;
            await _unitOfWork.SaveChangesAsync();

            return original.ToVm(original.GetLastUpdateDate(_settings));
        }
    }
}