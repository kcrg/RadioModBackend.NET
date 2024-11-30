using RadioModBackend.NET.Models;

namespace RadioModBackend.NET.Services;

public class PermissionsService(AppSettings config, ILogger<PermissionsService> logger)
{
    public bool IsPlayfabIdBanned(string playfabId)
    {
        if (config.BannedPlayfabIDs?.Contains(playfabId) ?? false)
        {
            logger.LogWarning("PlayfabId {PlayfabId} is banned", playfabId);
            return true;
        }
        return false;
    }

    public bool IsVideoIdBanned(string videoId)
    {
        if (config.BannedVideoIDs?.Contains(videoId) ?? false)
        {
            logger.LogWarning("VideoId {VideoId} is banned", videoId);
            return true;
        }
        return false;
    }

    public bool ContainsBannedTerms(string videoTitle)
    {
        var lowerTitleWords = videoTitle.ToLower().Split(' ');
        if (lowerTitleWords.Any(word => config.BannedTerms?.Contains(word) ?? false))
        {
            logger.LogWarning("Video title contains banned terms: {VideoTitle}", videoTitle);
            return true;
        }
        return false;
    }

    public AppSettings Config => config;
}