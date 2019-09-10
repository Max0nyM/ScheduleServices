////////////////////////////////////////////////
// © https://github.com/badhitman - @fakegov
////////////////////////////////////////////////
namespace LocalbitcoinsBtcRateSingletonAsyncScheduler
{
    public class BtcRateLocalbitcoinsModel : MetadataEntityModel.RootEntityModel
    {
        /// <summary>
        /// Минимальный курс из выборки
        /// </summary>
        public double MinRate { get; set; }

        /// <summary>
        /// Максимальный курс из выборки
        /// </summary>
        public double MaxRate { get; set; }

        /// <summary>
        /// Размер выборки
        /// </summary>
        public int CountRates { get; set; }

        public override string ToString() => DateCreate.ToString() + " [count: " + CountRates.ToString() + "] : [min " + MinRate.ToString() + "] - " + ((MinRate + MaxRate) / 2).ToString() + " - [max " + MaxRate.ToString() + "]";
    }
}
