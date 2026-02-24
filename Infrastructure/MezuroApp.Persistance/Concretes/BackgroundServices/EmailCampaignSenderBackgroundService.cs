using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MezuroApp.Application.Abstracts.Repositories.EmailCampaigns;
using MezuroApp.Application.Abstracts.Services; // IEmailCampaignService
using System;

namespace MezuroApp.Persistance.Concretes.BackgroundServices;

public sealed class EmailCampaignSenderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<EmailCampaignSenderBackgroundService> _log;

    public EmailCampaignSenderBackgroundService(
        IServiceProvider sp,
        ILogger<EmailCampaignSenderBackgroundService> log)
    {
        _sp = sp;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // app ayağa qalxsın
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueCampaignsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "EmailCampaignSenderBackgroundService error (loop continues)");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessDueCampaignsAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();

        var campaignRead = scope.ServiceProvider.GetRequiredService<IEmailCampaignReadRepository>();
        var campaignWrite = scope.ServiceProvider.GetRequiredService<IEmailCampaignWriteRepository>();
        var service = scope.ServiceProvider.GetRequiredService<IEmailCampaignService>();

        var now = DateTime.UtcNow;

        // due campaign-ları çəkirik (tracking OFF)
        var due = await campaignRead.GetAllAsync(
            c => !c.IsDeleted
                 && c.Status == "scheduled"
                 && c.ScheduledAt != null
                 && c.ScheduledAt <= now,
            enableTracking: false
        );

        if (due.Count == 0) return;

        foreach (var c in due)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                // 1) yarış vəziyyəti olmasın deyə: status-u "sending" et
                //    BUNU tracking ilə etməliyik
                var track = await campaignRead.GetAsync(
                    x => x.Id == c.Id && !x.IsDeleted,
                    enableTracking: true
                );

                if (track == null) continue;

                // bu arada başqa process sending edibdisə skip
                if (track.Status != "scheduled") continue;

                track.Status = "sending";
                track.LastUpdatedDate = DateTime.UtcNow;

                await campaignWrite.UpdateAsync(track);
                await campaignWrite.CommitAsync();

                _log.LogInformation("Sending scheduled campaign {Id} - {Name}", track.Id, track.Name);

                // 2) indi log-lar pending üstündən göndər
                await service.SendCampaignInternalAsync(track.Id, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Campaign send failed. CampaignId={CampaignId}", c.Id);

                // fail olsa campaign-i geri "scheduled" etmirik.
                // istəyirsənsə "failed" edək:
                try
                {
                    var failedTrack = await campaignRead.GetAsync(
                        x => x.Id == c.Id && !x.IsDeleted,
                        enableTracking: true
                    );

                    if (failedTrack != null)
                    {
                        failedTrack.Status = "failed"; // və ya "scheduled" (retry üçün)
                        failedTrack.LastUpdatedDate = DateTime.UtcNow;
                        await campaignWrite.UpdateAsync(failedTrack);
                        await campaignWrite.CommitAsync();
                    }
                }
                catch
                {
                    // ikinci erroru uduruq ki loop dayanmasın
                }
            }
        }
    }
}