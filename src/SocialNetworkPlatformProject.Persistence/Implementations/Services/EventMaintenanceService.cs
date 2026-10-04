using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EventMaintenanceService : IEventMaintenanceService
{
    private static readonly string[] AttendeeIncludes = { "Attendees" };

    // A round handles at most this many events; whatever is left is picked up on the next one.
    private const int MaxEventsPerRound = 200;

    private readonly IEventRepository _events;
    private readonly IEventNotifier _notifier;

    public EventMaintenanceService(IEventRepository events, IEventNotifier notifier)
    {
        _events = events;
        _notifier = notifier;
    }

    public async Task<int> SendDueRemindersAsync(DateTime now)
    {
        var dayLimit = now + Event.DayReminderLead;
        var hourLimit = now + Event.HourReminderLead;

        var due = await _events.GetAll(
                filter: e => e.StartsAt > now
                    && ((e.DayReminderSentAt == null && e.StartsAt <= dayLimit)
                        || (e.HourReminderSentAt == null && e.StartsAt <= hourLimit)),
                orderBy: e => e.StartsAt,
                includes: AttendeeIncludes)
            .Take(MaxEventsPerRound)
            .ToListAsync();

        var sent = 0;
        foreach (var ev in due)
        {
            // If the server was down and both reminders are late, one message is enough: the nearer one counts for both.
            var hourDue = ev.HourReminderSentAt == null && ev.StartsAt <= hourLimit;
            if (hourDue)
            {
                ev.HourReminderSentAt = now;
                ev.DayReminderSentAt ??= now;
            }
            else
            {
                ev.DayReminderSentAt = now;
            }

            // Mark first, send second: if two server instances run this at once, RowVersion lets only one save win,
            // so a reminder never goes out twice (at worst it is lost if the process dies in between).
            try
            {
                await _events.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                break; // somebody else changed or handled it; the next round starts from a clean slate
            }

            await _notifier.ReminderAsync(ev);
            sent++;
        }

        return sent;
    }

    public async Task<int> PromoteWaitingListsAsync(DateTime now)
    {
        var waiting = await _events.GetAll(
                filter: e => e.EndsAt > now && e.Attendees.Any(a => a.Status == EventAttendeeStatus.Waitlisted),
                includes: AttendeeIncludes)
            .Take(MaxEventsPerRound)
            .ToListAsync();

        var promotedCount = 0;
        foreach (var ev in waiting)
        {
            var promoted = ev.PromoteWaitlist();
            if (promoted.Count == 0)
                continue;

            try
            {
                await _events.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                break;
            }

            // Nobody acted to cause this, so everyone who moved up is told.
            await _notifier.PromotedAsync(ev, promoted, actingUserId: Guid.Empty);
            promotedCount += promoted.Count;
        }

        return promotedCount;
    }
}
