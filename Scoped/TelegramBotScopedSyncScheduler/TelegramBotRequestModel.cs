////////////////////////////////////////////////
// © https://github.com/badhitman - @fakegov
////////////////////////////////////////////////
using MetadataEntityModel;
using System.Collections.Generic;
using TelegramBot.TelegramMetadata.GettingUpdates;

namespace TelegramBotScopedSyncScheduler
{
    public class TelegramBotRequestModel
    {
        public UserModel User { get; set; }
        public List<Update> Updates { get; set; }
    }
}
