using MetadataEntityModel;

namespace AbstractAsyncScheduler
{

    public class TracertItemModel : RootEntityModel
    {
        public StatusTypes TypeTracert { get; set; } = StatusTypes.SetValueStatus;

        public override string ToString()
        {
            if (Information == null)
                return "null";

            return DateCreate.ToString() + ":" + (IsFavorite ? " [F]" : "") + " <" + TypeTracert.ToString() + "> " + Information.Trim() + (IsOff ? " [off]" : "") + (IsDelete ? " [delete]" : "");
        }
    }
}
