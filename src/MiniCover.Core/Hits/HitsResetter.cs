using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using MiniCover.HitServices;

namespace MiniCover.Core.Hits
{
    public class HitsResetter : IHitsResetter
    {
        private readonly ILogger<HitsResetter> _logger;

        public HitsResetter(
            ILogger<HitsResetter> logger)
        {
            _logger = logger;
        }

        public bool ResetHits(IDirectoryInfo hitsDirectory)
        {
            _logger.LogInformation("Resetting hits directory '{directory}'", hitsDirectory.FullName);

            var result = HitService.HitContextStorage.Clear(hitsDirectory.FullName);

            if (result)
                _logger.LogInformation("Reset operation completed without errors");
            else
                _logger.LogError("Reset operation completed with errors");

            return result;
        }
    }
}
