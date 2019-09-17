////////////////////////////////////////////////
// © https://github.com/badhitman - @fakegov
////////////////////////////////////////////////
namespace AbstractAsyncScheduler
{
    /// <summary>
    /// Типы статусов планировщика
    /// </summary>
    public enum StatusTypes
    {
        /// <summary>
        /// Статус, который не надо выводить в лог
        /// </summary>
        SystemStatus,

        /// <summary>
        /// Трассирующий статус
        /// </summary>
        DebugStatus,

        /// <summary>
        /// Стандартная установка статуса для информирования
        /// </summary>
        SetValueStatus,

        /// <summary>
        /// Статус об ошибке
        /// </summary>
        ErrorStatus
    }
}
